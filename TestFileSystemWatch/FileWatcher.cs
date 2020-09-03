﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using TestFileSystemWatch;

namespace TestFileSystemWatcher
{
    public partial class FileWatcher : IFileWatcher
    {


        //Works on absolute Paths
        //Needs to keep a structure of all Folders and file names to be able to:
        //Update itself on FileSystemWatch events
        //Notify when any file is changed/added/deleted (including multiple notification on directory change/add/delete)

        #region Private
        private string _rootFolderPath;
        private FWDirectory _rootFswDirectory;
        private int _timerMS;

        private string _extension;
        private Action<string> _notifyAction;

        private FileSystemWatcher _fswFiles;
        private FileSystemWatcher _fswDirectories;

        private System.Timers.Timer _timer;
        private List<string> _fileChanges = new List<string>();
        private List<string> _directoryChanges = new List<string>();

        static private object lockObj = new object();
        #endregion Private

        #region Public
        public FileWatcher(string folder, string extension, int timerMS, Action<string> notifyAction)
        {
            _rootFolderPath = folder;
            _extension = extension;
            _timerMS = timerMS;
            _notifyAction = notifyAction;

            _rootFswDirectory = new FWDirectory(_rootFolderPath, extension, OnChanges);
            _rootFswDirectory.Populate();

            _timer = new System.Timers.Timer(_timerMS);
            _timer.Elapsed += onTimedEvent;
            _timer.AutoReset = false;

        }

        public void Start()
        {
            try
            {
                if (_fswFiles == null)
                {
                    _fswFiles = new FileSystemWatcher(_rootFolderPath)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size ,
                        EnableRaisingEvents = true,
                        Filter = "*"+_extension
                    };
                    _fswFiles.Created += onOneFileEvent;
                    _fswFiles.Changed += onOneFileEvent;
                    _fswFiles.Deleted += onOneFileEvent;
                    _fswFiles.Renamed += onTwoFilesEvent;
                    _fswFiles.Error += onFileErrorEvent;
                }

                if (_fswDirectories == null)
                {
                    _fswDirectories = new FileSystemWatcher(_rootFolderPath)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.DirectoryName,
                        EnableRaisingEvents = true
                    };
                    _fswDirectories.Created += onOneDirectoryEvent;
                    _fswDirectories.Changed += onOneDirectoryEvent;
                    _fswDirectories.Deleted += onOneDirectoryEvent;
                    _fswDirectories.Renamed += onTwoDirectoriesEvent;
                    _fswDirectories.Error += onDirectoryErrorEvent;
                }

               
                _timer.Enabled = true;
            }
            catch (Exception ex)
            {
                logException(ex);
            }

        }

        public void Stop()
        {
            try
            {
                if (_fswFiles != null)
                {
                    _fswFiles.EnableRaisingEvents = false;
                    _fswFiles.Created -= onOneFileEvent;
                    _fswFiles.Changed -= onOneFileEvent;
                    _fswFiles.Deleted -= onOneFileEvent;
                    _fswFiles.Renamed -= onTwoFilesEvent;
                    _fswFiles.Dispose();
                    _fswFiles = null;
                }
                if (_fswDirectories!= null)
                {
                    _fswDirectories.EnableRaisingEvents = false;
                    _fswDirectories.Created -= onOneDirectoryEvent;
                    _fswDirectories.Changed -= onOneDirectoryEvent;
                    _fswDirectories.Deleted -= onOneDirectoryEvent;
                    _fswDirectories.Renamed -= onTwoDirectoriesEvent;
                    _fswDirectories.Dispose();
                    _fswDirectories = null;
                }

            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void OnChanges(string fullPath) //callback for changes detected by the FWDirectory
        {
            notifyAction(fullPath);
        }

        private void notifyAction(string fullPath)
        {
            string relativePath;
            try
            {
                relativePath = PathExtension.GetRelativePath(_rootFolderPath, fullPath);
                if (relativePath == null)
                    return;
            }
            catch (Exception)
            {
                throw;
            }
            _notifyAction(relativePath);
        }

        #endregion Public

        #region FileWatcherEvents

        private void onFileErrorEvent(object sender, ErrorEventArgs e)
        {
            log("Error on FileSystemWatcher");
        }

        private void onOneFileEvent(object sender, FileSystemEventArgs e)
        {
            lock (lockObj)
            {
                if (sender == null)
                {
                    return;
                }

                if (!_fileChanges.Contains(e.FullPath))
                {
                    _fileChanges.Add(e.FullPath);
                    _timer.Start();
                }
            }
        }

        private void onTwoFilesEvent(object sender, RenamedEventArgs e)
        {
            lock (lockObj)
            {
                if (sender == null)
                {
                    return;
                }

                if (!_fileChanges.Contains(e.OldFullPath))
                {
                    _fileChanges.Add(e.OldFullPath);
                }

                if (!_fileChanges.Contains(e.FullPath))
                {
                    _fileChanges.Add(e.FullPath);
                }
                    _timer.Start();
            }

        }
        #endregion FileWatcherEvents

        #region DirectoryWatcherEvents
        private void onDirectoryErrorEvent(object sender, ErrorEventArgs e)
        {
            log("Error on FileSystemWatcher");
        }

        private void onOneDirectoryEvent(object sender, FileSystemEventArgs e)
        {
            lock (lockObj)
            {
                if (sender == null)
                {
                    return;
                }

                if (!_directoryChanges.Contains(e.FullPath))
                {
                    _directoryChanges.Add(e.FullPath);
                    _timer.Start();
                }
            }
        }

        private void onTwoDirectoriesEvent(object sender, RenamedEventArgs e)
        {
            lock (lockObj)
            {
                if (sender == null)
                {
                    return;
                }

                if (!_directoryChanges.Contains(e.OldFullPath))
                {
                    _directoryChanges.Add(e.OldFullPath);
                }

                if (!_fileChanges.Contains(e.FullPath))
                {
                    _directoryChanges.Add(e.FullPath);
                }
                _timer.Start();
            }

        }
        #endregion DirectoryWatcherEvents

        private void onTimedEvent(object sender, ElapsedEventArgs e)
        {

            _timer.Stop();

            lock (lockObj)
            {

                while (_fileChanges.Count > 0)
                {
                    _rootFswDirectory.OnFileChange(_fileChanges[0]);
                    _fileChanges.RemoveAt(0);
                    //_notifyAction(_fileChanges[0]);
                }

                while (_directoryChanges.Count > 0)
                {
                    _rootFswDirectory.OnDirectoryChange(_directoryChanges[0]);
                    _directoryChanges.RemoveAt(0);
                    //_notifyAction(_directoryChanges[0]);
                }
            }

            this._timer.Start();
        }

        private void log(string text)
        {
            Debug.Print(text);
        }

        private void logException(Exception ex)
        {
            Debug.Print(ex.Message);
        }
    }
}