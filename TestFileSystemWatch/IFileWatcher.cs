using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFileSystemWatch
{
    public interface IFileWatcher
    {
        void Start();
        void Stop();
    }
}
