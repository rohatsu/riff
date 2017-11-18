// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using Xunit;
using RIFF.Core;
using RIFF.Framework;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Tests
{
    public class UnitTests
    {
        [Fact]
        public void SerializationTests()
        {
            var ds1 = new TestSet
            {
                Rows = new List<TestSet.Row>
            {
                new TestSet.Row { A = "b", B = 2 },
                new TestSet.Row { A = "a", B = 1 },
                new TestSet.Row { A = "c", B = 3 },
            }
            };

            var ds2 = new TestSet
            {
                Rows = new List<TestSet.Row>
            {
                new TestSet.Row { A = "a", B = 1 },
                new TestSet.Row { A = "c", B = 3 },
                new TestSet.Row { A = "b", B = 2 },
            }
            };

            var ds1text = RFXMLSerializer.SerializeContract(ds1);
            var ds2text = RFXMLSerializer.SerializeContract(ds2);

            Assert.Equal(ds1text, ds2text);
        }

        private static void Log(string text, params object[] formats)
        {
            System.Diagnostics.Trace.WriteLine(string.Format(text, formats));
        }

        [DataContract]
        public class TestSet : RFDataSet<TestSet.Row>
        {
            [DataContract]
            public class Row : RFDataRow
            {
                [DataMember]
                public string A { get; set; }

                [DataMember]
                public int B { get; set; }
            }
        }
    }
}
