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
            private string _fullPath { get; set; }
            private string _extension { get; set; }
            private Action<string> _notifyAction { get; set; }

            private HashSet<string> _files = new HashSet<string>();
            private Dictionary<string /*FullPath*/, FWDirectory> _subDirs = new Dictionary<string, FWDirectory>();

            #region PublicMethods
            public FWDirectory(string fullPath, string extension, Action<string> notifyAction)
            {
                _fullPath = fullPath;
                _notifyAction = notifyAction;
                _extension = extension;
            }

            public void Populate()
            {
                _files.Clear();
                _subDirs.Clear();
                try
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(_fullPath);

                    foreach (string file in dirInfo.GetFiles().Select(f => f.FullName))
                    {
                        _files.Add(file);
                    }

                    foreach (var subDir in dirInfo.GetDirectories())
                    {
                        FWDirectory newSubDir = new FWDirectory(subDir.FullName, _extension, _notifyAction);
                        _subDirs.Add(subDir.FullName, newSubDir);
                        newSubDir.Populate();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }

            private void Clear()
            {
                foreach (FWDirectory subDir in _subDirs.Values)
                {
                    subDir.Clear();
                }
                foreach (string file in _files)
                {
                    delFile(file);
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
                    if (string.IsNullOrEmpty(Path.GetExtension(fullPath))) //assume a directory
                    {
                        FWDirectory subDir = getSubDir(fullPath);
                        if (subDir != null)
                        {
                            if (File.Exists(fullPath))
                            {
                                //We need to check if all subfolders are still there
                                subDir.confirmSubDir();
                            }
                            else
                            {
                                delSubDir(fullPath);
                            }
                        }
                    }

                    if (Path.GetExtension(fullPath) == _extension)
                    {
                        if (File.Exists(fullPath))
                        {
                            notifyFileChange(fullPath);
                        }
                        else
                        {
                            delFile(fullPath);
                        }
                    }
                }
                catch
                {

                }
            }

            public void OnDirectoryChange(string fullPath)
            {
                try
                {
                        FWDirectory subDir = getSubDir(fullPath);
                        if (subDir != null)
                        {
                            if (File.Exists(fullPath))
                            {
                                //We need to check if all subfolders are still there
                                subDir.confirmSubDir();
                            }
                            else
                            {
                                delSubDir(fullPath);
                            }
                        }

                }
                catch
                {

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
            private void addFile(string fullPath)
            {
                if (!(_files.Contains(fullPath)))
                {
                    _files.Add(fullPath);
                    notifyFileChange(fullPath);
                }
            }
            private void delFile(string fullPath)
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
                            subDir.delFile(fullPath);
                        }


                    }
                    catch (Exception)
                    {


                    }

                }
            }
            //renFile is managed as Delete and Add
            #endregion FileMethods
            
            #region SubDirMethods
            private void confirmSubDir()
            {
                throw new NotImplementedException();
            }
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
                targetDir.Clear();
            }
            #endregion SubDirMethods

            #region Utility
            private FWDirectory getSubDir(string fullPath)
            {
                //Leverage Dictionary??
                foreach (FWDirectory subdir in _subDirs.Values)
                {
                    if (subdir._fullPath == fullPath)
                    {
                        return subdir;
                    }
                    else
                    {
                        FWDirectory foundDir = subdir.getSubDir(fullPath);
                        if (foundDir != null)
                        {
                            return foundDir;
                        }
                    }
                }
                return null;
            }
            #endregion Utility

            #endregion Private







        }
    }
}
