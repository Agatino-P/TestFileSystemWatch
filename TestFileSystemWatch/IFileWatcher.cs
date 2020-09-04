using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MecalFileWatcher
{
    public interface IFileWatcher
    {
        void Start();
        void Stop();
    }
}
