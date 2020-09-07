using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestApp
{
    public interface INotifiable
    {
        void NotifyChanges(IEnumerable<string> changes);
    }
}
