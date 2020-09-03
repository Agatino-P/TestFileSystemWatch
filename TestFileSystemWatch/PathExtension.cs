using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestFileSystemWatch
{
    static class PathExtension
    {
        public static string GetRelativePath(string relativeTo, string path)
        {
            string rel=null;
            try
            {
                Uri uri = new Uri(relativeTo);
                rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
                {
                    rel = $".{ Path.DirectorySeparatorChar }{ rel }";
                }
            }
            catch (Exception)
            {
            }
            return rel;
        }
    }
}
