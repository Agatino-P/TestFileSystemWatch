using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace MecalFileWatcher
{

    internal class FWDirectory
    {
        public readonly string FullPath;
        public readonly string Extension;
        public readonly FWDirectory ParentDir;

        private List<string> _files = new List<string>();
        private List<FWDirectory> _subDirs = new List<FWDirectory>();

        #region PublicMethods
        public FWDirectory(FWDirectory parentDir, string fullPath, string extension)
        {
            ParentDir = parentDir;
            FullPath = fullPath;
            Extension = extension;

            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(fullPath);
                List<FileInfo> files = directoryInfo.GetFiles("*" + extension).ToList();
                _files = new  List<string>(files.Select(fi=>fi.FullName));

            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }


        internal IEnumerable<FWDirectory> PopulateDirsRecursive()
        {
            try
            {
                if (!Directory.Exists(FullPath))
                {
                    return Enumerable.Empty<FWDirectory>();
                }

                _subDirs.Clear();

                DirectoryInfo dirInfo = new DirectoryInfo(FullPath);

                List<FWDirectory> subDirsRecursive = new List<FWDirectory>(_subDirs);

                foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
                {
                    FWDirectory newSubDir = new FWDirectory(this, subDir.FullName, Extension);
                    _subDirs.Add(newSubDir);
                    subDirsRecursive.Add(newSubDir);
                    subDirsRecursive.AddRange(newSubDir.PopulateDirsRecursive());
                }
                return subDirsRecursive;
            }
            catch (Exception ex)
            {
                logException(ex);
                return Enumerable.Empty<FWDirectory>();
            }
        }


        public IEnumerable<string> GetFiles() => _files;
        
        public Dictionary<string, FWDirectory> GetFileEntriesRecursive()
        {
            Dictionary<string, FWDirectory> recursiveFiles = new Dictionary<string, FWDirectory>();
            foreach (string file in _files)
                recursiveFiles.Add(file, this);
            foreach(FWDirectory subDir in _subDirs)
            {
                recursiveFiles.AddRange(subDir.GetFileEntriesRecursive());
            }
            return recursiveFiles;
        }

        public IEnumerable<FWDirectory> GetSubDirs() => _subDirs;

        internal void AddFile(string fullFilePath)
        {
            throw new NotImplementedException();
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

        internal void RemoveFile(string fullFilePath)
        {
            throw new NotImplementedException();
        }

        /*
         * public void DirectoryChange(string fullPath)
                {
                    if (fullPath != this.FullPath)
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
                        return;
                    }

                    if (!Directory.Exists(_fullPath))
                    {
                        //FWDirectory parentDir = getParentDir(_fullPath);
                        //parentDir.delSubDir(_fullPath);
                    }
                }

                */
        public IEnumerable<string> ClearRecursive()
        {
            List<string> changedFiles = new List<string>(_files);
            _files.Clear();

            foreach (FWDirectory subdir in _subDirs)
            {
                changedFiles.AddRange(subdir.ClearRecursive());
            }
            _subDirs.Clear();
            return changedFiles;
        }


        #endregion Changes


        #region Private

        #region SubDirMethods

        private FWDirectory getSubDirByPath(string fullPath)
        {
            return _subDirs.Where(sd => sd.FullPath == fullPath).FirstOrDefault();
        }

        private FWDirectory addSubDir(string fullPath)
        {
            FWDirectory fWDirectory = getSubDirByPath(fullPath);
            return fWDirectory ?? new FWDirectory(this, fullPath, Extension);
        }

        private void delSubDir(string fullPath)
        {
            FWDirectory targetDir = getSubDirByPath(fullPath);
            if (targetDir == null)
            {
                return;
            }
            targetDir.clear();
            _subDirs.Remove(targetDir);
        }
        #endregion SubDirMethods

        #region Utility
        private IEnumerable<string> clear()
        {
            List<string> changedFiles = new List<string>(_files);
            _files.Clear();

            foreach (FWDirectory subDir in _subDirs)
            {
                changedFiles.AddRange(subDir.clear());
            }
            return changedFiles;
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
