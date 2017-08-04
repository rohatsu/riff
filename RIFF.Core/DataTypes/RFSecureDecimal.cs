// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract, Serializable]
    public class RFSecureDecimal
    {
        [DataMember]
        public string CipherText { get; set; }

        public static int sSaltLength = 4;
    }
}
