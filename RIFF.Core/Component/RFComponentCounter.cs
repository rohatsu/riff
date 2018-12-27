// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Threading;

namespace RIFF.Core
{
    public static class RFComponentCounter
    {
        public static int sNextComponentID = 100;

        public static string GetNextComponentID()
        {
            Interlocked.Increment(ref sNextComponentID);
            return String.Format("#{0}", sNextComponentID);
        }
    }
}
