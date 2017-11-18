// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using log4net.Config;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Xunit;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace RIFF.Tests
{
    [Collection("Framework")]
    public class FrameworkTests
    {
        private FrameworkFixture _fixture;

        public FrameworkTests(FrameworkFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void ComplexWriteTest()
        {
            var testDoc = CreateTestDocument();
            var key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "TestDoc1", TestEngine.TestKeys.Key1, null);
            _fixture.Context.SaveDocument(key, testDoc);
            var reloaded = _fixture.Context.LoadDocumentContent<TestDocument>(key);

            CompareTestDoc(reloaded);
        }

        [Fact]
        public void ConcurrentAccessTest()
        {
            var key1 = RFGenericCatalogKey.Create(_fixture.KeyDomain, "ConcurrentAccessTest", TestEngine.TestKeys.Key1, null);
            var key2 = RFGenericCatalogKey.Create(_fixture.KeyDomain, "ConcurrentAccessTest", TestEngine.TestKeys.Key2, null);

            RFStatic.Log.Info(this, "Starting ConcurrentTest");

            var factory = new TaskFactory();
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(factory.StartNew((j) => _fixture.Context.SaveDocument(key1, j.ToString(), false), i));
                tasks.Add(factory.StartNew(() => _fixture.Context.LoadDocumentContent<string>(key1)));
                tasks.Add(factory.StartNew((j) => _fixture.Context.SaveDocument(key2, j.ToString(), false), i));
                tasks.Add(factory.StartNew(() => _fixture.Context.LoadDocumentContent<string>(key2)));
            }

            Task.WaitAll(tasks.ToArray());
            RFStatic.Log.Info(this, "Finished ConcurrentTest, checking result");

            var entry1 = _fixture.Context.LoadEntry(key1);
            var entry2 = _fixture.Context.LoadEntry(key2);

            Assert.Equal(5, entry1.Version);
            Assert.Equal(5, entry2.Version);
        }

        [Fact]
        public void GraphDependencyTest()
        {
            var instance = new RFGraphInstance { Name = "default", ValueDate = RFDate.Today() };

            var s1Key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "S", TestEngine.TestKeys.Key1, instance);
            var s2Key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "S", TestEngine.TestKeys.Key2, instance);

            _fixture.Context.SaveDocument(s1Key, "S", true);
            _fixture.Context.SaveDocument(s2Key, "S", true);

            Thread.Sleep(2000);

            // results
            var e1Key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "E", TestEngine.TestKeys.Key1, instance);
            var e2Key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "E", TestEngine.TestKeys.Key2, instance);

            // execution counts
            var a1Count = RFGenericCatalogKey.Create(_fixture.KeyDomain, "A_Counter", TestEngine.TestKeys.Key1, instance);
            var b1Count = RFGenericCatalogKey.Create(_fixture.KeyDomain, "B_Counter", TestEngine.TestKeys.Key1, instance);
            var c1Count = RFGenericCatalogKey.Create(_fixture.KeyDomain, "C_Counter", TestEngine.TestKeys.Key1, instance);
            var a2Count = RFGenericCatalogKey.Create(_fixture.KeyDomain, "A_Counter", TestEngine.TestKeys.Key2, instance);
            var b2Count = RFGenericCatalogKey.Create(_fixture.KeyDomain, "B_Counter", TestEngine.TestKeys.Key2, instance);
            var c2Count = RFGenericCatalogKey.Create(_fixture.KeyDomain, "C_Counter", TestEngine.TestKeys.Key2, instance);

            Assert.Equal(1, _fixture.Context.LoadDocumentContent<object>(a1Count));
            Assert.Equal(1, _fixture.Context.LoadDocumentContent<object>(b1Count));
            Assert.Equal(1, _fixture.Context.LoadDocumentContent<object>(c1Count));

            Assert.Equal(1, _fixture.Context.LoadDocumentContent<object>(a2Count));
            Assert.Equal(1, _fixture.Context.LoadDocumentContent<object>(b2Count));
            Assert.Equal(1, _fixture.Context.LoadDocumentContent<object>(c2Count));

            Assert.Equal("SASABC", _fixture.Context.LoadDocumentContent<string>(e1Key));
            Assert.Equal("SASABC", _fixture.Context.LoadDocumentContent<string>(e2Key));
        }

        [Fact]
        public void IntervalTest()
        {
            var key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "IntervalTest", TestEngine.TestKeys.Key2, null);
            Thread.Sleep(2000);
            var content = _fixture.Context.LoadDocumentContent<string>(key);

            Assert.NotNull(content);
            Assert.Equal("Complete", content);
        }

        [Fact]
        public void KeyInstancesTest()
        {
            var key1 = RFGenericCatalogKey.Create(_fixture.KeyDomain, "KeyInstancesTest", TestEngine.TestKeys.Key1, new RFGraphInstance { Name = "dummy", ValueDate = RFDate.Today() });
            var key2 = RFGenericCatalogKey.Create(_fixture.KeyDomain, "KeyInstancesTest", TestEngine.TestKeys.Key2, new RFGraphInstance { Name = "dummy", ValueDate = RFDate.Today() });

            foreach (var date in RFDate.Range(new RFDate(2016, 7, 1), new RFDate(2016, 7, 12), d => true))
            {
                // 12 of these
                var key11 = key1.CreateForInstance(new RFGraphInstance
                {
                    Name = "default1",
                    ValueDate = date
                });
                _fixture.Context.SaveDocument(key11, "Test", false);

                // 6 of these
                if (date.Day % 2 == 0)
                {
                    var key12 = key1.CreateForInstance(new RFGraphInstance
                    {
                        Name = "default2",
                        ValueDate = date
                    });
                    _fixture.Context.SaveDocument(key12, "Test", false);
                }

                // 4 of these
                if (date.Day % 3 == 0)
                {
                    var key21 = key2.CreateForInstance(new RFGraphInstance
                    {
                        Name = "default1",
                        ValueDate = date
                    });
                    _fixture.Context.SaveDocument(key21, "Test", false);
                }

                // 3 of these
                if (date.Day % 4 == 0)
                {
                    var key22 = key2.CreateForInstance(new RFGraphInstance
                    {
                        Name = "default2",
                        ValueDate = date
                    });
                    _fixture.Context.SaveDocument(key22, "Test", false);
                }
            }

            _fixture.Context.SaveDocument(key2.CreateForInstance(null), "Test", false); // this should be ignored

            var keys1 = _fixture.Context.GetKeyInstances(key1);
            Assert.Equal(18, keys1.Count);
            Assert.Equal(12, keys1.Where(k => k.Value.GraphInstance.Name == "default1").Count());
            Assert.Equal(6, keys1.Where(k => k.Value.GraphInstance.Name == "default2").Count());

            var keys2 = _fixture.Context.GetKeyInstances(key2);
            Assert.Equal(7, keys2.Count);
            Assert.Equal(4, keys2.Where(k => k.Value.GraphInstance != null && k.Value.GraphInstance.Name == "default1").Count());
            Assert.Equal(3, keys2.Where(k => k.Value.GraphInstance != null && k.Value.GraphInstance.Name == "default2").Count());

            // invalidate
            _fixture.Context.Invalidate(key1.CreateForInstance(new RFGraphInstance
            {
                Name = "default1",
                ValueDate = new RFDate(2016, 7, 12)
            }));

            // get latest
            var latest1 = _fixture.Context.LoadEntry(key1.CreateForInstance(new RFGraphInstance
            {
                Name = "default1",
                ValueDate = RFDate.Today()
            }), new RFCatalogOptions
            {
                DateBehaviour = RFDateBehaviour.Latest
            });
            Assert.Equal(11, latest1.Key.GraphInstance.ValueDate.Value.Day);

            var latest2 = _fixture.Context.LoadEntry(key1.CreateForInstance(new RFGraphInstance
            {
                Name = "default2",
                ValueDate = RFDate.Today()
            }), new RFCatalogOptions
            {
                DateBehaviour = RFDateBehaviour.Latest
            });
            Assert.Equal(12, latest2.Key.GraphInstance.ValueDate.Value.Day);
        }

        [Fact]
        public void QueuedTriggerTest()
        {
            var triggerKey = RFGenericCatalogKey.Create(_fixture.KeyDomain, "Trigger Key", TestEngine.TestKeys.Key1, null);
            _fixture.Context.SaveEntry(RFDocument.Create(triggerKey, new RFScheduleTrigger { LastTriggerTime = DateTime.Now }), true);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            _fixture.Context.SaveEntry(RFDocument.Create(triggerKey, new RFScheduleTrigger { LastTriggerTime = DateTime.Now }), true);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            _fixture.Context.SaveEntry(RFDocument.Create(triggerKey, new RFScheduleTrigger { LastTriggerTime = DateTime.Now }), true);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            var result = _fixture.Context.LoadDocumentContent<string>(RFGenericCatalogKey.Create(_fixture.KeyDomain, "Queued Trigger", TestEngine.TestKeys.Key1, null));

            Assert.Equal("StartStopStartStop", result); // we expect 2x, not 1x or 3x
        }

        [Fact]
        public void SerializationTests()
        {
            // type-aware serialization
            var testDoc = CreateTestDocument();
            var reserializeText = RFXMLSerializer.DeserializeContract(typeof(TestDocument).FullName, RFXMLSerializer.PrettySerializeContract(testDoc));
            var reserializeBinary = RFXMLSerializer.BinaryDeserializeContract(typeof(TestDocument).FullName, RFXMLSerializer.BinarySerializeContract(testDoc));

            CompareTestDoc(reserializeText as TestDocument);
            CompareTestDoc(reserializeBinary as TestDocument);

            // generic XML serialization, we reserialize to avoid namespace declaration order issues
            var originalXmlString = RFXMLSerializer.BinaryDeserializeXML(RFXMLSerializer.BinarySerializeContract(testDoc));
            Assert.True(originalXmlString.NotBlank());
            Assert.True(originalXmlString.Length > 500);
            var binaryRepresentation = RFXMLSerializer.BinarySerializeXML(originalXmlString);
            var reserializedXmlString = RFXMLSerializer.BinaryDeserializeXML(binaryRepresentation);

            Assert.Equal(originalXmlString, reserializedXmlString);
        }

        [Fact]
        public void SimpleWriteTest()
        {
            var key = RFGenericCatalogKey.Create(_fixture.KeyDomain, "WriteTest", TestEngine.TestKeys.Key1, null);

            _fixture.Context.SaveDocument(key, "test1");
            Assert.Equal(_fixture.Context.LoadDocumentContent<string>(key), "test1");

            _fixture.Context.SaveDocument(key, "test2");
            Assert.Equal(_fixture.Context.LoadDocumentContent<string>(key), "test2");
        }

        [Fact]
        public void TasksTest()
        {
            Thread.Sleep(3000);

            var task1Result = _fixture.Context.LoadDocumentContent<string>(RFGenericCatalogKey.Create(_fixture.KeyDomain, "Task 1 Result", TestEngine.TestKeys.Key1, null));
            var task2Result = _fixture.Context.LoadDocumentContent<string>(RFGenericCatalogKey.Create(_fixture.KeyDomain, "Task 2 Result", TestEngine.TestKeys.Key1, null));
            var task3Result = _fixture.Context.LoadDocumentContent<string>(RFGenericCatalogKey.Create(_fixture.KeyDomain, "Task 3 Result", TestEngine.TestKeys.Key1, null));

            Assert.Equal("Complete", task1Result);
            Assert.Equal("Complete", task2Result);
            Assert.Equal("Complete", task3Result);
        }

        /* proper transactions not implemented yet
        [TestMethod]
        public void TransactionTest()
        {
            var testKey = RFGenericCatalogKey.Create(_fixture.KeyDomain, "Transaction", "Test1", null);
            _fixture.Context.SaveDocument(testKey, "Initialized", false);
            _fixture.Context.QueueInstruction(this, new RFProcessInstruction(null, "Transaction Tester"));
            Thread.Sleep(2000);
            var loaded = _fixture.Context.LoadDocumentContent<string>(testKey);
            Assert.AreEqual("Initialized", loaded);
        }*/

        private static void CompareTestDoc(TestDocument reloaded)
        {
            var testDoc = CreateTestDocument();

            Assert.Equal(testDoc.Decimal, reloaded.Decimal);
            Assert.Equal(testDoc.Int, reloaded.Int);
            Assert.Equal(testDoc.RFDate, reloaded.RFDate);
            Assert.Equal(5, reloaded.IgnoreMe);
            foreach (var d in testDoc.Dict.Keys)
            {
                Assert.Equal(testDoc.Dict[d], reloaded.Dict[d]);
            }
            for (int i = 0; i < testDoc.StringList.Count; i++)
            {
                Assert.Equal(testDoc.StringList[i], reloaded.StringList[i]);
            }
        }

        private static TestDocument CreateTestDocument()
        {
            return new TestDocument
            {
                Decimal = 5.55M,
                Dict = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } },
                IgnoreMe = 10,
                Int = 505,
                RFDate = RFDate.Today(),
                StringList = new List<string> { "S1", "S2", "S3 " }
            };
        }
    }

    [DataContract]
    public class TestDocument
    {
        [DataMember]
        public decimal? Decimal { get; set; }

        [DataMember]
        public Dictionary<string, string> Dict { get; set; }

        [IgnoreDataMember]
        public int IgnoreMe { get; set; }

        [DataMember]
        public int Int { get; set; }

        [DataMember]
        public RFDate RFDate { get; set; }

        [DataMember]
        public List<string> StringList { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            IgnoreMe = 5;
        }
    }

    public class TestEngine : IRFEngineBuilder
    {
        public enum TestKeys
        {
            Key1,
            Key2
        }

        public RFEngineDefinition BuildEngine(string database, string environment)
        {
            var keyDomain = new RFSimpleKeyDomain("TEST");

            var engineConfig = RFEngineDefinition.Create(
                "TestEngine",
                keyDomain,
                intervalSeconds: 1,
                maxRuntime: TimeSpan.FromMinutes(20));

            // interval and direct dependency

            engineConfig.AddIntervalTrigger(
                engineConfig.AddProcess("Interval test", "Checks for internal", () => new ActionProcessor((c) =>
                    c.SaveDocument(RFGenericCatalogKey.Create(keyDomain, "IntervalTest", TestKeys.Key1, null), "Complete")
            )));

            engineConfig.AddProcessWithCatalogTrigger<RFEngineProcessorKeyParam>("Interval sink", "Step 2", () => new ActionProcessor((c) =>
                    c.SaveDocument(RFGenericCatalogKey.Create(keyDomain, "IntervalTest", TestKeys.Key2, null), "Complete")),
                    RFGenericCatalogKey.Create(keyDomain, "IntervalTest", TestKeys.Key1, null));

            // direct and indirect dependency test (A -> B -> C) with reverse

            var graph = engineConfig.CreateGraph("TestGraph");

            graph.AddProcess("A1", "A1", () => new AppendProcessor(new AppendProcessor.Config { Append = "A" }))
                .Map(d => d.Input1, RFGenericCatalogKey.Create(keyDomain, "S", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Output, RFGenericCatalogKey.Create(keyDomain, "Y", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Counter, RFGenericCatalogKey.Create(keyDomain, "A_Counter", TestKeys.Key1, null), RFDateBehaviour.Exact);

            graph.AddProcess("B1", "B1", () => new AppendProcessor(new AppendProcessor.Config { Append = "B" }))
                .Map(d => d.Input1, RFGenericCatalogKey.Create(keyDomain, "Y", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Output, RFGenericCatalogKey.Create(keyDomain, "Z", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Counter, RFGenericCatalogKey.Create(keyDomain, "B_Counter", TestKeys.Key1, null), RFDateBehaviour.Exact);

            graph.AddProcess("C1", "C1", () => new AppendProcessor(new AppendProcessor.Config { Append = "C" }))
                .Map(d => d.Input1, RFGenericCatalogKey.Create(keyDomain, "Y", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Input2, RFGenericCatalogKey.Create(keyDomain, "Z", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Output, RFGenericCatalogKey.Create(keyDomain, "E", TestKeys.Key1, null), RFDateBehaviour.Exact)
                .Map(d => d.Counter, RFGenericCatalogKey.Create(keyDomain, "C_Counter", TestKeys.Key1, null), RFDateBehaviour.Exact);

            // reverse

            graph.AddProcess("A2", "A2", () => new AppendProcessor(new AppendProcessor.Config { Append = "C" }))
                .Map(d => d.Input1, RFGenericCatalogKey.Create(keyDomain, "Y", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Input2, RFGenericCatalogKey.Create(keyDomain, "Z", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Output, RFGenericCatalogKey.Create(keyDomain, "E", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Counter, RFGenericCatalogKey.Create(keyDomain, "C_Counter", TestKeys.Key2, null), RFDateBehaviour.Exact);

            graph.AddProcess("B2", "B2", () => new AppendProcessor(new AppendProcessor.Config { Append = "B" }))
                .Map(d => d.Input1, RFGenericCatalogKey.Create(keyDomain, "Y", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Output, RFGenericCatalogKey.Create(keyDomain, "Z", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Counter, RFGenericCatalogKey.Create(keyDomain, "B_Counter", TestKeys.Key2, null), RFDateBehaviour.Exact);

            graph.AddProcess("C2", "C2", () => new AppendProcessor(new AppendProcessor.Config { Append = "A" }))
                .Map(d => d.Input1, RFGenericCatalogKey.Create(keyDomain, "S", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Output, RFGenericCatalogKey.Create(keyDomain, "Y", TestKeys.Key2, null), RFDateBehaviour.Exact)
                .Map(d => d.Counter, RFGenericCatalogKey.Create(keyDomain, "A_Counter", TestKeys.Key2, null), RFDateBehaviour.Exact);

            // tasks

            var task1 = engineConfig.AddProcess("Task 1 Process", "Task 1", () => new ActionProcessor((c) =>
                    c.SaveDocument(RFGenericCatalogKey.Create(keyDomain, "Task 1 Result", TestKeys.Key1, null), "Complete")));

            var task2 = engineConfig.AddProcess("Task 2 Process", "Task 2", () => new ActionProcessor((c) =>
                    c.SaveDocument(RFGenericCatalogKey.Create(keyDomain, "Task 2 Result", TestKeys.Key1, null), "Complete")));

            var task3 = engineConfig.AddProcess("Task 3 Process", "Task 3", () => new ActionProcessor((c) =>
                    c.SaveDocument(RFGenericCatalogKey.Create(keyDomain, "Task 3 Result", TestKeys.Key1, null), "Complete")));

            engineConfig.AddScheduledTask("Task 1", new RFIntervalSchedule(new TimeSpan(0, 0, 1)).Single(), RFWeeklyWindow.AllWeek(), task1, false);

            engineConfig.AddChainedTask("Task 2", task1, task2, false);

            engineConfig.AddTriggeredTask("Task 3", RFGenericCatalogKey.Create(keyDomain, "Task 2 Result", TestKeys.Key1, null), task3);

            // queued trigger test
            var queuedTrigger = engineConfig.AddProcess("Queued Trigger Process", "Queued Trigger Process", () => new ActionProcessor((c) =>
                    {
                        var _in = c.LoadDocumentContent<string>(RFGenericCatalogKey.Create(keyDomain, "Queued Trigger", TestKeys.Key1, null)) ?? String.Empty;
                        _in += "Start";
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        _in += "Stop";
                        c.SaveDocument(RFGenericCatalogKey.Create(keyDomain, "Queued Trigger", TestKeys.Key1, null), _in);
                    }));

            engineConfig.AddTriggeredTask("Queued Scheduler", RFGenericCatalogKey.Create(keyDomain, "Trigger Key", TestEngine.TestKeys.Key1, null), queuedTrigger);

            // transaction
            engineConfig.AddProcess("Transaction Tester", "Tests SQL+MSMQ Transaction", () => new TransactionProcessor(
                RFGenericCatalogKey.Create(keyDomain, "Transaction", "Test1", null),
                RFGenericCatalogKey.Create(keyDomain, "Transaction", "Test2", null)));

            return engineConfig;
        }
    }

    internal class TransactionProcessor : RFEngineProcessor<RFEngineProcessorParam>
    {
        private RFCatalogKey _testKey1, _testKey2;

        public TransactionProcessor(RFCatalogKey testKey1, RFCatalogKey testKey2)
        {
            _testKey1 = testKey1;
            _testKey2 = testKey2;
        }

        public override RFProcessingResult Process()
        {
            Context.SaveDocument(_testKey1, "Written", true);
            Context.SaveDocument(_testKey2, "Written", true);
            Log.Info("Written under transaction.");
            return RFProcessingResult.Success(true);
        }
    }

    internal class ActionProcessor : RFEngineProcessor<RFEngineProcessorParam>
    {
        private Action<IRFProcessingContext> _action;

        public ActionProcessor(Action<IRFProcessingContext> action)
        {
            _action = action;
        }

        public override RFProcessingResult Process()
        {
            _action(Context);
            return RFProcessingResult.Success(true);
        }
    }

    internal class AppendProcessor : RFGraphProcessorWithConfig<AppendProcessor.Domain, AppendProcessor.Config>
    {
        internal class Config : IRFGraphProcessorConfig
        {
            public string Append { get; set; }
        }

        internal class Domain : RFGraphProcessorDomain
        {
            [RFIOBehaviour(RFIOBehaviour.State, false)]
            public int? Counter { get; set; }

            [RFIOBehaviour(RFIOBehaviour.Input, true)]
            public string Input1 { get; set; }

            [RFIOBehaviour(RFIOBehaviour.Input, false)]
            public string Input2 { get; set; }

            [RFIOBehaviour(RFIOBehaviour.Output, true)]
            public string Output { get; set; }
        }

        public AppendProcessor(Config config) : base(config)
        {
        }

        public override bool HasInstance(RFGraphInstance instance)
        {
            return true;
        }

        public override void Process(Domain domain)
        {
            domain.Output = domain.Input1 + (domain.Input2 ?? String.Empty) + _config.Append;
            if (domain.Counter == null)
            {
                domain.Counter = 1;
            }
            else
            {
                domain.Counter++;
            }
        }
    }
}
