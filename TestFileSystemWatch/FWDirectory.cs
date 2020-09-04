using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MecalFileWatcher
{

    internal class FWDirectory
    {
        private string _fullPath { get; set; }
        private string _extension { get; set; }
        
        private FWDirectory _parentDir;
        private List<string> _files = new List<string>();
        private List<FWDirectory> _subDirs = new List<FWDirectory>();

        #region PublicMethods
        public FWDirectory(FWDirectory parentDir, string fullPath, string extension)
        {
            _parentDir = parentDir;
            _fullPath = fullPath;
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
                    _SubDirFiles.Add(file);
                }

                foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                {
                    FWDirectory newSubDir = new FWDirectory(this, subDir.FullName, _extension);
                    _subDirs.Add(subDir.FullName, newSubDir);
                    newSubDir.Populate();
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }

        }

        public IEnumerable<string> GetAllFiles()
        {
            List<string> allfiles = new List<string>(_SubDirFiles);
            foreach (FWDirectory subDir in _subDirs.Values)
            {
                allfiles.AddRange(subDir.GetAllFiles());
            }
            return allfiles;
        }
        #endregion PublicMethods

        #region Changes
        public IEnumerable<string> FileChange(string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    if (_files.AddIfNotPresent(fullPath))
                    {
                        return new string[] { fullPath };
                    }
                }
                else
                {
                    if (_files.RemoveIfPresent(fullPath))
                    {
                        return new string[] { fullPath };
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
            return Enumerable.Empty<string>();
        }

        public void DirectoryChange(string fullPath)
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
                if (true)
                {
                    ;
                }

                getSubDir(fullPath)?.DirectoryChange(fullPath);
                return;
            }

            if (!Directory.Exists(_fullPath))
            {
                //FWDirectory parentDir = getParentDir(_fullPath);
                //parentDir.delSubDir(_fullPath);
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

        public IEnumerable<string> ClearAll() 
        {
            List<string> changedFiles = new List<string>(_files);
            _files.Clear();

            foreach (FWDirectory subdir in _subDirs)
            {
                changedFiles.AddRange(subdir.ClearAll());
            }
            _subDirs.Clear();
            return changedFiles;
        }
        #endregion Changes

        #region Private



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
            {
                return _subDirs[fullPath];
            }

            return null;
        }

        //private FWDirectory getParentDir(string fullPath)
        //{
        //    //string parentPath = PathHelper.GetDirectoryParenthPath(fullPath);
        //    return getSubDir(parentPath);
        //}


        private void clear()
        {
            foreach (FWDirectory subDir in _subDirs.Values)
            {
                subDir.clear();
            }
            foreach (string file in _SubDirFiles)
            {
                updateRemovedfile(file);
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
