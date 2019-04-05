// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Component")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Config")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Data")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.DataTypes")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Engine")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Error")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Event")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Graph")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Helpers")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Instruction")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Interval")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Logs")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Processing")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Queue")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.Scheduler")]
[assembly: ContractNamespace("RIFF.Core", ClrNamespace = "RIFF.Core.UserLog")]
[assembly: InternalsVisibleTo("RIFF.Tests")]

namespace RIFF.Core
{
    public enum RFPlane
    {
        System = 1,
        User = 2,
        Ephemeral = 3
    };

    public static class RFCore
    {
        public static readonly string sDateFormat = "yyyy-MM-dd";
        public static readonly string sDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
        public static readonly string sMajorVersion = "2";
        public static readonly string sMinorVersion = "1";

        public static readonly string sShortVersion = String.Format("v{0}.{1}",
            sMajorVersion,
            sMinorVersion);

#if !NETSTANDARD2_0
        public static readonly string sVersion = String.Format("{0}.{1} ({2})",
            sShortVersion,
            Properties.Resources.BuildNumber.Trim(new char[] { '\n', '\r', '\t', ' ' }),
            Properties.Resources.BuildDate.Trim(new char[] { '\n', '\r', '\t', ' ' }));
#else
        public static readonly string sVersion = String.Format("{0} Core", sShortVersion);
#endif
    }
}
