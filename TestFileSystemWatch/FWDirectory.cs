using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TestFileSystemWatcher
{
    public partial class FileWatcher
    {
        private class FWDirectory
        {
            private FWDirectory _parentDir;
            private string _fullPath { get; set; }
            private string _extension { get; set; }
            private Action<string> _notifyAction { get; set; }

            private HashSet<string> _files = new HashSet<string>();
            private Dictionary<string /*FullPath*/, FWDirectory> _subDirs = new Dictionary<string, FWDirectory>();

            #region PublicMethods
            public FWDirectory(FWDirectory parentDir, string fullPath, string extension, Action<string> notifyAction)
            {
                _parentDir = parentDir;
                _fullPath = fullPath;
                _notifyAction = notifyAction;
                _extension = extension;
            }

            public void Populate() //this doesn't notify
            {
                try
                {
                    if (!Directory.Exists(_fullPath))
                    {
                        return;
                    }

                    DirectoryInfo dirInfo = new DirectoryInfo(_fullPath);
                    foreach (string file in dirInfo.GetFiles().Select(f => f.FullName))
                    {
                        _files.Add(file);
                    }

                    foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                    {
                        FWDirectory newSubDir = new FWDirectory(this, subDir.FullName, _extension, _notifyAction);
                        _subDirs.Add(subDir.FullName, newSubDir);
                        newSubDir.Populate();
                    }
                }
                catch (Exception ex)
                {
                    logException(ex);
                }

            }

            public ReadOnlyCollection<string> GetAllFiles()
            {
                List<string> allfiles = new List<string>(_files);
                foreach (FWDirectory subDir in _subDirs.Values)
                {
                    allfiles.AddRange(subDir.GetAllFiles());
                }
                return new ReadOnlyCollection<string>(allfiles);
            }
            #endregion PublicMethods

            #region Changes
            public void OnFileChange(string fullPath)
            {
                try
                {
                    //if (Path.GetExtension(fullPath) == _extension)
                    //{
                    if (!File.Exists(fullPath))
                    {
                        delFileAndNotify(fullPath);
                    }
                    else
                    {
                        addFileAndNotify(fullPath);
                    }
                }
                catch
                {

                }
            }

            public void OnDirectoryChange(string fullPath)
            {
                if (fullPath != this._fullPath)
                {
                    try
                    {
                        if (Directory.Exists(fullPath))
                        {
                            addSubDir(fullPath);
                        }
                        else
                        {
                            delSubDir(fullPath);
                        }

                    }
                    catch (Exception ex)
                    {

                        logException(ex);
                    }
                    if (true) ;
                    getSubDir(fullPath)?.OnDirectoryChange(fullPath);
                    return;
                }
                
                if (!Directory.Exists(_fullPath))
                {
                    FWDirectory parentDir = getParentDir(_fullPath);
                    parentDir.delSubDir(_fullPath);
                }

                List<string> oldFiles = new List<string>(GetAllFiles()); //saving this to avoid double notifications
                clearAll();
                Populate();
                List<string> newFiles = GetAllFiles().ToList();
                List<string> mergedList = oldFiles.Union(newFiles).ToList();
                foreach (string file in mergedList)
                {
                    notifyFileChange(file);
                }
            }

            private void clearAll() //this doesn't notify
            {
                _files.Clear();
                foreach (FWDirectory subdir in _subDirs.Values)
                {
                    subdir.clearAll();
                }

            }
            #endregion Changes

            #region Private

            #region Notification
            private void notifyFileChange(string fullPath)
            {
                _notifyAction(fullPath);
            }
            #endregion Notification

            #region FileMethods
            private void addFileAndNotify(string fullPath)
            {
                if (!(_files.Contains(fullPath)))
                {
                    _files.Add(fullPath);
                    notifyFileChange(fullPath);
                }
            }
            private void delFileAndNotify(string fullPath) //notifies accordingly
            {
                if ((_files.Contains(fullPath)))
                {
                    _files.Remove(fullPath);
                    notifyFileChange(fullPath);
                }
                else
                {
                    try
                    {
                        string dirPath = Path.GetDirectoryName(fullPath);
                        FWDirectory subDir = getSubDir(dirPath);
                        if (subDir != null)
                        {
                            subDir.delFileAndNotify(fullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        logException(ex);
                    }
                }
            }

            //renFile is managed as Delete and Add
            #endregion FileMethods

            #region SubDirMethods

            private void addSubDir(string fullPath)
            {
                if (_subDirs.ContainsKey(fullPath))
                {
                    _subDirs[fullPath].Populate();
                }
            }
            private void delSubDir(string fullPath)
            {
                FWDirectory targetDir = getSubDir(fullPath);
                if (targetDir == null)
                {
                    return;
                }
                targetDir.clear();
            }
            #endregion SubDirMethods

            #region Utility
            private FWDirectory getSubDir(string fullPath)
            {
                if (_subDirs.ContainsKey(fullPath))
                    return _subDirs[fullPath];
                return null;
            }

            private FWDirectory getParentDir(string fullPath)
            {
                string parentPath = Path.GetFileName(Path.GetDirectoryName(fullPath));
                return getSubDir(parentPath);
            }


            private void clear()
            {
                foreach (FWDirectory subDir in _subDirs.Values)
                {
                    subDir.clear();
                }
                foreach (string file in _files)
                {
                    delFileAndNotify(file);
                }
            }


            #endregion Utility

            #endregion Private

            #region Logging
            private void log(string text)
            {
                Debug.Print(text);
            }

            private void logException(Exception ex)
            {
                Debug.Print(ex.Message);
            }
            #endregion Logging
        }
    }
}
