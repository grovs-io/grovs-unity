using System;
using System.Threading.Tasks;
using GrovsSDK;

namespace GrovsTestApp
{
    /// <summary>Runs an action, times it and logs the outcome. Exceptions never escape.</summary>
    public sealed class TestRunner
    {
        readonly TestLog _log;
        readonly Func<double> _nowMs;
        readonly Action<Exception> _reportUnexpected;

        /// <param name="reportUnexpected">Gets every exception that is not a GrovsException, with its stack trace.</param>
        public TestRunner(TestLog log, Func<double> nowMs, Action<Exception> reportUnexpected = null)
        {
            _log = log;
            _nowMs = nowMs;
            _reportUnexpected = reportUnexpected;
        }

        public async Task<LogEntry> Run(TestAction action)
        {
            var start = _nowMs();
            ActionResult result;
            try
            {
                result = await action.Run() ?? new ActionResult();
            }
            catch (Exception e)
            {
                if (!(e is GrovsException)) _reportUnexpected?.Invoke(e);
                result = new ActionResult
                {
                    State = LogState.Error,
                    Detail = Describe(e),
                    Code = action.Code + "\n\n-> " + e.GetType().Name + ": " + Describe(e)
                };
            }

            var elapsed = (long)Math.Round(_nowMs() - start);
            var detail = result.Detail;
            if (result.State != LogState.Error)
            {
                var ms = elapsed + " ms";
                detail = string.IsNullOrEmpty(detail) ? ms : detail + " · " + ms;
            }
            var link = action.Type == LogType.Create && result.State != LogState.Error ? result.Value as string : null;
            var entry = _log.Add(action.Type, result.State, action.Title, detail, result.Code ?? action.Code, action, link);
            entry.ElapsedMs = elapsed;
            return entry;
        }

        /// <summary>The message of an exception, with the Grovs error code when there is one. No stack trace.</summary>
        public static string Describe(Exception e)
        {
            if (e is GrovsException grovs && grovs.Error != null) return grovs.Error.ToString();
            return string.IsNullOrEmpty(e.Message) ? e.GetType().Name : e.Message;
        }
    }
}
