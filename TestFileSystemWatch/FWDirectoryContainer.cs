using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MecalFileWatcher
{
    public class FWDirectoryContainer
    {
        private FWDirectory _root;

        private readonly string _rootPath;
        private readonly string _extension;
        private bool _recursive;


        private Dictionary<string, FWDirectory> _fileEntries = new Dictionary<string, FWDirectory>(); //FilePath, ParentDir

        private List<FWDirectory> _dirs = new List<FWDirectory>();
        private FWDirectory getDirByPath(string fullPath)
        {
            return _dirs.Where(fwd => fwd.FullPath == fullPath).FirstOrDefault();
        }

        public IEnumerable<string> FilePaths => _fileEntries.Keys;
        public IEnumerable<string> DirPaths => _dirs.Select(fwd => fwd.FullPath);


        public FWDirectoryContainer(string rootPath, string extension, bool recursive)
        {
            _rootPath = rootPath;
            _extension = extension;
            _recursive = recursive;

            Populate();
        }

        internal void Populate()
        {
            //grab root dir
            _dirs.Clear();
            _root = new FWDirectory(null, _rootPath, _extension);
            _dirs.Add(_root);

            //grab subdirs            
            if (_recursive)
            {
                IEnumerable<FWDirectory> foundDirs = _root.PopulateDirsRecursive();
                _dirs.AddRange(_root.PopulateDirsRecursive());
            }

            //grab files from all dirs
            _fileEntries.Clear();
            if (_recursive)
            {
                _fileEntries.AddRange(_root.GetFileEntriesRecursive());
            }

            if (!_recursive)
            {
                foreach (string file in _root.GetFiles())
                {
                    _fileEntries.Add(file, _root);
                }
            }
        }

        internal IEnumerable<string> OnFileChange(string fullFilePath)
        {
            List<string> changedFiles = new List<string>();
            try
            {
                bool fileExists = File.Exists(fullFilePath);
                if (fileExists)
                {
                    //If I know the file
                    if (_fileEntries.ContainsKey(fullFilePath))
                    {
                        //just notify the change for file contents
                        changedFiles.Add(fullFilePath);
                    }
                    else
                    {
                        string parentDirPath = Path.GetDirectoryName(fullFilePath);
                        FWDirectory parentDir = getDirByPath(parentDirPath);

                        //if I know the directory, 
                        if (parentDir == null)
                        {
                            //I need to create the dir and all needed "gran-parent" dirs
                            parentDir = addDir(parentDirPath);
                        }

                        //Now just add the file
                        parentDir.AddFile(fullFilePath);
                        _fileEntries.Add(fullFilePath, parentDir);
                        changedFiles.Add(fullFilePath);
                    }
                }

                if (!fileExists)
                {
                    if (_fileEntries.ContainsKey(fullFilePath))
                    {
                        FWDirectory parentDir = _fileEntries[fullFilePath];
                        parentDir.RemoveFile(fullFilePath);
                        _fileEntries.Remove(fullFilePath);
                        changedFiles.Add(fullFilePath);
                    }
                    else
                    {
                        //nothing to do
                    }
                }
                return changedFiles; ;
            }
            catch (Exception ex)
            {
                logException(ex);
                return Enumerable.Empty<string>();
            }
        }

        internal IEnumerable<string> OnDirectoryChange(string fullDirPath)
        {
            List<string> changedFiles = new List<string>();

            if (Directory.Exists(fullDirPath))
            {
                FWDirectory fwd = getDirByPath(fullDirPath);
                if (fwd == null)
                {
                    addDir(fullDirPath);
                }
                else
                {
                    //Nothing to do
                }
            }
            else
            {
                FWDirectory fwd = getDirByPath(fullDirPath);
                if (fwd == null)
                {
                    //Nothing to do
                }
                else
                {
                    //We need to 
                    //- remove this folder and all subfolders
                    //- update the dirs and fileEntries
                    //- notify all changed fileEntries
                    List<FWDirectory> impactedDirs = new List<FWDirectory> { fwd };
                    impactedDirs.AddRange(fwd.GetSubDirs());

                    foreach (FWDirectory impactedDir in impactedDirs)
                    {
                        IEnumerable<string> impactedDirFiles = impactedDir.GetFiles();
                        changedFiles.AddRange(impactedDirFiles);
                        foreach (string impactedDirFile in impactedDirFiles)
                        {
                            _fileEntries.Remove(impactedDirFile);
                        }
                        _dirs.Remove(impactedDir);
                    }
                }
            }
            return changedFiles;
        }

        private FWDirectory addDir(string newDirPath)
        {
            //this must create the object after having created all the needed parent objects
            //in the end return the newly created object
            try
            {
                string parentDirPath = Path.GetDirectoryName(newDirPath);
                FWDirectory newParentDir = getDirByPath(parentDirPath);

                if (newParentDir == null)
                {
                    newParentDir = addDir(parentDirPath);
                }
                FWDirectory newDir = new FWDirectory(newParentDir, newDirPath, _extension);
                _dirs.Add(newDir);
                return newDir;
            }
            catch (Exception ex)
            {
                logException(ex);
                return null;
            }
        }


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
