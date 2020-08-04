using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using TestFileSystemWatch;

namespace TestFileSystemWatcher
{
    public class FSysWatcher : IFSysWatcher
    {
        private int _timerMS;

        private string _folder;
        private string _extension;
        private Action<string> _notifyAction;

        private FileSystemWatcher fsWatcher;
        private System.Timers.Timer fwTimer;
        private List<string> fwPaths = new List<string>();


        static private object lockObj = new object();

        public FSysWatcher(string folder, string extension, int timerMS, Action<string> notifyAction)
        {
            _folder = folder;
            _extension = extension;
            _timerMS = timerMS;
            _notifyAction = notifyAction;

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
                    fsWatcher = new FileSystemWatcher(_folder, $"*{_extension}")
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };
                    fsWatcher.Created += oneFileEvent;
                    fsWatcher.Changed += oneFileEvent;
                    fsWatcher.Deleted += oneFileEvent;
                    fsWatcher.Renamed += twoFilesEvent;
                }

                fwTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

        }

        public void Stop()
        {
            try
            {
                if (fsWatcher != null)
                {
                    fsWatcher.EnableRaisingEvents = false;
                    fsWatcher.Created -= oneFileEvent;
                    fsWatcher.Changed -= oneFileEvent;
                    fsWatcher.Deleted -= oneFileEvent;
                    fsWatcher.Renamed -= twoFilesEvent;
                    fsWatcher.Dispose();
                    fsWatcher = null;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private void oneFileEvent(object sender, FileSystemEventArgs e)
        {
            lock (lockObj)
            {

                if ((sender != null) && !fwPaths.Contains(e.FullPath))
                {
                    fwPaths.Add(e.FullPath);
                    fwTimer.Start();
                }
            }
        }

        private void twoFilesEvent(object sender, RenamedEventArgs e)
        {
            lock (lockObj)
            {
                if ((sender != null) && !fwPaths.Contains(e.OldFullPath))
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
                    fwPaths.RemoveAt(0);
                }
            }

            //fwTimer.Start();
        }

    }
}
