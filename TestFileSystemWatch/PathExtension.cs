using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestFileSystemWatch
{
    static class PathExtension
    {
        public static string GetRelativePath(string fromPath, string toPath) // (C:\a\a.txt, a.txt)
        {
            string rel=null;
            try
            {
                if (String.IsNullOrEmpty(fromPath)) return null;
                if (String.IsNullOrEmpty(toPath)) return null;

                Uri fromUri = new Uri(fromPath);
                Uri toUri = new Uri(toPath);

                if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.
                if (fromUri.AbsolutePath == toUri.AbsolutePath) { return ""; }

                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

                if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
                {
                    relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                }

                return relativePath;
            }
            catch (Exception)
            {
            }
            return rel;
        }
    }
}
