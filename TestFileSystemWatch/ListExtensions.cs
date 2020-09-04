using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TestFileSystemWatch
{
    public static class ListExtensions
    {
        public static void AddIfNotPresent<T> (this IList<T> list, T item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }
    }
}
