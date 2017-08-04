// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    public static class RFDictionary
    {
        public static T2 TryGet<T1, T2>(this Dictionary<T1, T2> dic, T1 key)
        {
            T2 v = default(T2);
            if (dic != null)
            {
                dic.TryGetValue(key, out v);
            }
            return v;
        }
    }
}
