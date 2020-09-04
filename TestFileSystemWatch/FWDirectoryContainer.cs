using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;

namespace MecalFileWatcher
{
    public class FWDirectoryContainer
    {
        FWDirectory _root;

        private readonly string _fullPath;
        private readonly string _extension;

        //IEnumerable<string> getAllFilePaths();
        private Dictionary<string /*FullPath*/, FWDirectory> _subDirs = new Dictionary<string, FWDirectory>();



        public FWDirectoryContainer(string fullPath, string extension)
        {
            _fullPath = fullPath;
            _extension = extension;

            _root = new FWDirectory(null, fullPath, extension);
        }

        internal void Populate()
        {
            _root.Populate();
        }

        internal IEnumerable<string> OnFileChange(string fullFilePath)
        {
            try
            {
                string dirPath = Path.GetDirectoryName(fullFilePath);
                var subDir = _subDirs.ContainsKey(dirPath) ? _subDirs[dirPath] : addSubDir(dirPath);
                if (subDir==null) //Should never happen
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

        private FWDirectory addSubDir(string dirPath)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<string> OnDirectoryChange(string fullDirPath)
        {
            return Enumerable.Empty<string>();
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
