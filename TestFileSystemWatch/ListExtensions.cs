using System.Collections.Generic;

namespace MecalFileWatcher
{
    public static class ListExtensions
    {
        public static bool AddIfNotPresent<T>(this IList<T> list, T item)
        {
            if (list.Contains(item))
            {
                return false;
            }
            else
            {
                list.Add(item);
                return true;
            }
        }

        public static bool RemoveIfPresent<T>(this IList<T> list, T item)
        {
            if (list.Contains(item))
            {
                list.Remove(item);
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
