using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrovsSDK;

namespace GrovsTestApp
{
    static class Calls
    {
        /// <summary>Turns a callback style SDK call into a task. A reported error becomes a <see cref="GrovsException"/>.</summary>
        public static Task<T> FromCallback<T>(Action<Action<T, GrovsError>> start) where T : class
        {
            var source = new TaskCompletionSource<T>();
            start((result, error) =>
            {
                if (error != null) source.TrySetException(new GrovsException(error));
                else if (result == null) source.TrySetException(new InvalidOperationException("No result and no error"));
                else source.TrySetResult(result);
            });
            return source.Task;
        }

        public static string StyleLabel(CallStyle style) => style == CallStyle.Async ? "async" : "callback";

        /// <summary>A copy, so an action replays what it was built with even if the form changes later.</summary>
        public static IList<string> NullIfEmpty(List<string> list) => list == null || list.Count == 0 ? null : new List<string>(list);

        public static IDictionary<string, object> MapOrNull(IEnumerable<KeyValueRow> rows)
        {
            var map = KeyValue.ToMap(rows);
            return map.Count == 0 ? null : map;
        }

        public static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
