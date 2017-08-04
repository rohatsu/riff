// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    public class RFRangeInput<T> : IRFRangeInput where T : class
    {
        public Dictionary<RFDate, T> Instances { get; private set; } = new Dictionary<RFDate, T>();

        public void Add(RFDate vd, object t)
        {
            if (!Instances.ContainsKey(vd) && t is T && t != null)
            {
                Instances.Add(vd, t as T);
            }
        }
    }

    public interface IRFRangeInput
    {
        void Add(RFDate vd, object t);
    }
}
