using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using TestFileSystemWatch;

namespace TestFileSystemWatcher
{
    public class FSysWatcher : IFSysWatcher
    {


        private class FSWdirectory
        {
            private string _fullPath { get; set; }
            private string _extension { get; set; }
            private Action<string> _notifyAction { get; set; }

            public HashSet<string> Files = new HashSet<string>();
            public Dictionary<string /*FullPath*/, FSWdirectory> SubDirs = new Dictionary<string, FSWdirectory>();

            public FSWdirectory(string fullPath, string extension, Action<string> notifyAction)
            {
                _fullPath = fullPath;
                _notifyAction = notifyAction;
                _extension = extension;
            }
            
            public void Populate()
            {
                Files.Clear();
                SubDirs.Clear();

                DirectoryInfo dirInfo = new DirectoryInfo(_fullPath);

                foreach (string file in dirInfo.GetFiles().Select(f => f.FullName))
                {
                    Files.Add(file);
                }

                foreach (var subDir in dirInfo.GetDirectories())
                {
                    FSWdirectory newSubDir = new FSWdirectory(subDir.FullName, _extension, _notifyAction);
                    SubDirs.Add(subDir.FullName, newSubDir);
                    newSubDir.Populate();
                }
            }

            public ReadOnlyCollection<string> GetAllFiles()
            {
                List<string> allfiles = new List<string>(Files);
                foreach (FSWdirectory subDir in SubDirs.Values)
                {
                    allfiles.AddRange(subDir.GetAllFiles());
                }
                return new ReadOnlyCollection<string>(allfiles);
            }

            public void Update(string fullPath)
            {
                try
                {
                    if (string.IsNullOrEmpty(Path.GetExtension(fullPath))) //assume a directory
                    {
                        FSWdirectory subDir = GetSubDir(fullPath);
                        if (subDir != null)
                        {
                            if (File.Exists(fullPath))
                            {
                                //We need to check if all subfolders are still there
                                subDir.ConfirmSubDirs();
                            }
                            else
                            {
                                DeleteDirectory(fullPath);
                            }
                        }
                    }

                    if (Path.GetExtension(fullPath) == _extension) 
                    {
                        if (File.Exists(fullPath))
                            NotifyFileChange(fullPath);
                        else
                        {
                            DeleteFile(fullPath);
                        }
                    }
                }
                catch
                {

                }
            }

            private void ConfirmSubDirs()
            {
                throw new NotImplementedException();
            }

            private void NotifyFileChange(string fullPath)
            {
                _notifyAction(fullPath);
            }

            private void AddFile(string fullPath)
            {
                if (!(Files.Contains(fullPath)))
                {
                    Files.Add(fullPath);
                    NotifyFileChange(fullPath);
                }
            }

            public void DeleteFile(string fullPath)
            {
                if ((Files.Contains(fullPath)))
                {
                    Files.Remove(fullPath);
                    NotifyFileChange(fullPath);
                }
                else
                {
                    try
                    {
                        string dirPath = Path.GetDirectoryName(fullPath);
                        FSWdirectory subDir = GetSubDir(dirPath);
                        if(subDir!=null)
                        {
                            subDir.DeleteFile(fullPath);
                        }


                    }
                    catch (Exception)
                    {

                        
                    }

                }
            }

            //RenameFile is managed as Delete and Add

            public void AddDirectory(string fullPath)
            {
                if (SubDirs.ContainsKey(fullPath))
                {
                    SubDirs[fullPath].Populate();
                }
            }

            public FSWdirectory GetSubDir(string fullPath)
            {
                foreach (FSWdirectory subdir in SubDirs.Values)
                {
                    if (subdir._fullPath == fullPath)
                    {
                        return subdir;
                    }
                    else
                    {
                        FSWdirectory foundDir = subdir.GetSubDir(fullPath);
                        if (foundDir != null)
                        {
                            return foundDir;
                        }
                    }
                }
                return null;
            }

            public void DeleteDirectory(string fullPath)
            {
                FSWdirectory targetDir = GetSubDir(fullPath);
                if (targetDir == null)
                {
                    return;
                }
                targetDir.Delete();
            }

            private void Delete()
            {
                foreach (FSWdirectory subDir in SubDirs.Values)
                {
                    subDir.Delete();
                }
                foreach (string file in Files)
                {
                    DeleteFile(file);
                }
            }

            public void RenameDirectory(string fullPath)
            {

            }

        }

        private string _folder;
        private FSWdirectory _fswDirectory;
        private int _timerMS;


        private string _extension;
        private Action<string> _notifyAction;

        private FileSystemWatcher fsWatcher;
        private System.Timers.Timer fwTimer;
        private List<string> fwPaths = new List<string>();


        static private object lockObj = new object();

        //Works on absolute Paths
        //Needs to keep a structure of all Folders and file names to be able to:
        //Update itself on FileSystemWatch events
        //Notify when any file is changed/added/deleted (including multiple notification on directory change/add/delete)


        public FSysWatcher(string folder, string extension, int timerMS, Action<string> notifyAction)
        {
            _folder = folder;
            _extension = extension;
            _timerMS = timerMS;
            _notifyAction = notifyAction;

            _fswDirectory = new FSWdirectory(_folder, extension, _notifyAction);
            _fswDirectory.Populate();


            fwTimer = new System.Timers.Timer(_timerMS);
            fwTimer.Elapsed += onTimedEvent;
            fwTimer.AutoReset = false;
            
        }

        public void Start()
        {
            try
            {
                if (fsWatcher == null)
                {
                    fsWatcher = new FileSystemWatcher(_folder)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };
                    fsWatcher.Created += onOneFileEvent;
                    fsWatcher.Changed += onOneFileEvent;
                    fsWatcher.Deleted += onOneFileEvent;
                    fsWatcher.Renamed += onTwoFilesEvent;
                    fsWatcher.Error += onErrorEvent;

                }

                fwTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

        }

        private void onErrorEvent(object sender, ErrorEventArgs e)
        {
            Debug.Print($"{DateTime.Now}: Eror on FileSystemWatcher");
        }

        public void Stop()
        {
            try
            {
                if (fsWatcher != null)
                {
                    fsWatcher.EnableRaisingEvents = false;
                    fsWatcher.Created -= onOneFileEvent;
                    fsWatcher.Changed -= onOneFileEvent;
                    fsWatcher.Deleted -= onOneFileEvent;
                    fsWatcher.Renamed -= onTwoFilesEvent;
                    fsWatcher.Dispose();
                    fsWatcher = null;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private void onOneFileEvent(object sender, FileSystemEventArgs e)
        {
            Debug.Print($"{e.FullPath} {e.ChangeType}");

            lock (lockObj)
            {
                if (sender == null)
                {
                    return;
                }

                if (!fwPaths.Contains(e.FullPath))
                {
                    fwPaths.Add(e.FullPath);
                    fwTimer.Start();
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

                Debug.Print($"{e.OldFullPath} {e.FullPath} {e.ChangeType}");

                if (!fwPaths.Contains(e.OldFullPath))
                {
                    fwPaths.Add(e.OldFullPath);
                    fwTimer.Start();
                }

                if ((sender != null) && !fwPaths.Contains(e.FullPath))
                {
                    fwPaths.Add(e.FullPath);
                    fwTimer.Start();
                }
            }

        }

        private void onTimedEvent(object sender, ElapsedEventArgs e)
        {
            fwTimer.Stop();

            lock (lockObj)
            {
                if (fwPaths.Count == 0)
                {
                    return;
                }

                while (fwPaths.Count > 0)
                {
                    _fswDirectory.Update(fwPaths[0]);
                    /*
                    if ((Path.GetExtension(fwPaths[0]).ToLower() == _extension.ToLower()))
                    {
                        try
                        {
                            _notifyAction(fwPaths[0]);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    */
                    fwPaths.RemoveAt(0);
                    
                }
            }

            //fwTimer.Start();
        }

    }
}
