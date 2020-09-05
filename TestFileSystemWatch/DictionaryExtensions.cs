using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MecalFileWatcher
{
    public static class DictionaryExtensions
    {
        public static void AddRange<K,V>(this Dictionary<K,V> toDict, Dictionary<K,V> fromDict)
        {
            if (toDict == null || fromDict == null)
                return;
            foreach (KeyValuePair<K, V> kvpFrom in fromDict)
            {
                if (!toDict.ContainsKey(kvpFrom.Key))
                {
                    toDict.Add(kvpFrom.Key, kvpFrom.Value);
                }
            }
        }

    }
}
