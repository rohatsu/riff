// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    public static class RFNumeric
    {
        public static decimal? Absolute(decimal? v)
        {
            return v.HasValue ? (decimal?)Math.Abs(v.Value) : null;
        }

        public static decimal? NullIfZero(decimal? v)
        {
            return v.HasValue && v.Value == 0 ? null : v;
        }

        public static decimal ZeroIfNA(decimal? v)
        {
            return v.HasValue ? v.Value : 0;
        }
    }
}
