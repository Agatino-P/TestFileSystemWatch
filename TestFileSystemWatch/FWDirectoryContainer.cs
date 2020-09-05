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
        //IEnumerable<string> getAllFilePaths();
        private Dictionary<string, FWDirectory> _fileEntries = new Dictionary<string, FWDirectory>(); //FilePath, ParentDir
        private List<FWDirectory> _dirs = new List<FWDirectory>();

        public IEnumerable<string> FilePaths => _fileEntries.Keys;
        public IEnumerable<string> DirPaths => _dirs.Select(fwd=>fwd.FullPath);


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
                    _fileEntries.Add(file, _root);
            }
        }



/*
        internal IEnumerable<string> OnFileChange(string fullFilePath)
        {
            try
            {
                string dirPath = Path.GetDirectoryName(fullFilePath);
                var subDir = _subDirs.ContainsKey(dirPath) ? _subDirs[dirPath] : addSubDir(dirPath);
                if (subDir == null) //Should never happen
                {
                    log("file.watcher: Couldn't locate or create dir for file: {fullFilePath}");
                    return Enumerable.Empty<string>();
                }
                return subDir.FileChange(fullFilePath);
            }
            catch (Exception ex)
            {
                logException(ex);
                return Enumerable.Empty<string>();
            }
        }
        internal IEnumerable<string> OnDirectoryChange(string fullDirPath)
        {
            return Enumerable.Empty<string>();
        }


        private FWDirectory addSubDir(string dirPath)
        {
            throw new NotImplementedException();
        }
*/

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
