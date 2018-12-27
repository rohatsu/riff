// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    /// <summary>
    /// Declare your fixed string identifiers in enums and pass them to members expecting RFEnum inputs.
    /// </summary>
    public class RFEnum
    {
        public static RFEnum NullEnum { get { return RFEnum.FromString("NULL"); } }

        public string Enum { get; set; }

        public static RFEnum FromEnum(Enum e)
        {
            return FromString(EnumToString(e));
        }

        public static RFEnum FromString(string s)
        {
            return new RFEnum
            {
                Enum = s
            };
        }

        public static implicit operator RFEnum(Enum e)
        {
            return new RFEnum { Enum = EnumToString(e) };
        }

        public static implicit operator string(RFEnum e)
        {
            return e.ToString();
        }

        public static bool operator !=(RFEnum re, Enum e)
        {
            return (re.ToString() != EnumToString(e));
        }

        public static bool operator !=(RFEnum re, RFEnum e)
        {
            return (re.ToString() != e.ToString());
        }

        public static bool operator ==(RFEnum re, Enum e)
        {
            return (re.ToString() == EnumToString(e));
        }

        public static bool operator ==(RFEnum re, RFEnum e)
        {
            return (re.ToString() == e.ToString());
        }

        public override bool Equals(object obj)
        {
            return Enum.Equals(obj.ToString());
        }

        public override int GetHashCode()
        {
            return Enum.GetHashCode();
        }

        public T ToEnum<T>()
        {
            return (T)System.Enum.Parse(typeof(T), Enum.Substring(Enum.IndexOf('.') + 1));
        }

        public override string ToString()
        {
            return Enum;
        }

        protected static string EnumToString(Enum e)
        {
            return String.Format("{0}.{1}", e.GetType().Name, e);
        }
    }
}
