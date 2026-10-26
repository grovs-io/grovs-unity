using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class TestRunnerTests
    {
        class NoStore : ILinkHistoryStore
        {
            public string Load() => null;
            public void Save(string json) { }
        }

        double _ms;
        TestLog _log;
        TestRunner _runner;
        List<Exception> _reported;

        [SetUp]
        public void SetUp()
        {
            _ms = 0;
            _log = new TestLog(new NoStore(), () => DateTime.Now);
            _reported = new List<Exception>();
            _runner = new TestRunner(_log, () => { var v = _ms; _ms += 12; return v; }, _reported.Add);
        }

        LogEntry Run(TestAction action) => _runner.Run(action).GetAwaiter().GetResult();

        static TestAction Action(Func<Task<ActionResult>> run) => new TestAction
        {
            Type = LogType.Event,
            Title = "Track",
            Code = "track()",
            Run = run
        };

        [Test]
        public void Success_logs_detail_with_elapsed_time()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult { Detail = "sent" })));
            Assert.AreEqual(LogState.Ok, entry.State);
            Assert.AreEqual("sent · 12 ms", entry.Detail);
            Assert.AreEqual("Track", entry.Title);
            Assert.AreEqual("track()", entry.Code);
            Assert.AreEqual(LogType.Event, entry.Type);
            Assert.IsNotNull(entry.Action);
            Assert.AreSame(entry, _log.Entries[0]);
        }

        [Test]
        public void Elapsed_time_is_kept_on_the_entry()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult())));
            Assert.AreEqual(12, entry.ElapsedMs);
        }

        [Test]
        public void Created_link_is_kept_on_a_create_entry()
        {
            var create = Action(() => Task.FromResult(new ActionResult { Value = "https://x.link/abc" }));
            create.Type = LogType.Create;
            Assert.AreEqual("https://x.link/abc", Run(create).Link);
        }

        [Test]
        public void Failed_create_has_no_link()
        {
            var create = Action(() => Task.FromResult(new ActionResult { State = LogState.Error, Value = "https://x.link/abc" }));
            create.Type = LogType.Create;
            Assert.IsNull(Run(create).Link);
        }

        [Test]
        public void Value_of_other_types_is_not_a_link()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult { Value = "https://x.link/abc" })));
            Assert.IsNull(entry.Link);
        }

        [Test]
        public void Detail_less_result_gets_only_the_time()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult())));
            Assert.AreEqual("12 ms", entry.Detail);
        }

        [Test]
        public void Result_code_replaces_the_action_code()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult { Code = "other()", Value = 5 })));
            Assert.AreEqual("other()", entry.Code);
        }

        [Test]
        public void Warning_keeps_state_and_time()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult { State = LogState.Warning, Detail = "hmm" })));
            Assert.AreEqual(LogState.Warning, entry.State);
            Assert.AreEqual("hmm · 12 ms", entry.Detail);
        }

        [Test]
        public void Error_result_has_no_time()
        {
            var entry = Run(Action(() => Task.FromResult(new ActionResult { State = LogState.Error, Detail = "nope" })));
            Assert.AreEqual("nope", entry.Detail);
        }

        [Test]
        public void Grovs_exception_becomes_an_error_entry()
        {
            var ex = new GrovsException(new GrovsError { Code = GrovsErrorCode.SdkDisabled, Message = "off" });
            var entry = Run(Action(() => throw ex));
            Assert.AreEqual(LogState.Error, entry.State);
            Assert.AreEqual("SdkDisabled: off", entry.Detail);
            Assert.AreEqual("track()\n\n-> GrovsException: SdkDisabled: off", entry.Code);
            Assert.IsTrue(_log.HasUnseenError);
            Assert.IsEmpty(_reported);
        }

        [Test]
        public void Other_exceptions_are_logged_the_same_way()
        {
            var ex = new ArgumentException("bad arg");
            var entry = Run(Action(() => Task.FromException<ActionResult>(ex)));
            Assert.AreEqual(LogState.Error, entry.State);
            Assert.AreEqual(ex.Message, entry.Detail);
            Assert.AreEqual("track()\n\n-> ArgumentException: bad arg", entry.Code);
            CollectionAssert.AreEqual(new[] { ex }, _reported);
        }
    }
}
