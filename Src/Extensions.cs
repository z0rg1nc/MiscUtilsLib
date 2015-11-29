using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BtmI2p.Newtonsoft.Json;
using BtmI2p.Newtonsoft.Json.Serialization;
using NLog;
using Xunit;

namespace BtmI2p.MiscUtils
{
    public static partial class MyExtensionMethods
    {
        public static bool CheckMask(
            this Guid testedGuid,
            Guid mask,
            Guid maskEqual
            )
        {
            var testedGuidBytes = testedGuid.ToByteArray();
            var maskBytes = mask.ToByteArray();
            var maskEqualBytes = maskEqual.ToByteArray();
            for (int i = 0; i < 16; i++)
            {
                if (
                    (testedGuidBytes[i] & maskBytes[i]) 
                    != maskEqualBytes[i]
                )
                    return false;
            }
            return true;
        }

        public static async Task ThrowIfCancelled(
            this Task task, 
            CancellationToken token
        )
        {
            var tokenTaskSource = new TaskCompletionSource<object>();
            using (token.Register(() => tokenTaskSource.TrySetResult(null)))
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
                if (
                    await Task.WhenAny(
                        task,
                        tokenTaskSource.Task
                    ).ConfigureAwait(true)
                    == tokenTaskSource.Task
                )
                    token.ThrowIfCancellationRequested();
                await task.ConfigureAwait(true);
            }
        }
        public static async Task<T1> ThrowIfCancelled<T1>(
            this Task<T1> task,
            CancellationToken token
            )
        {
            var tokenTaskSource = new TaskCompletionSource<object>();
            using (token.Register(() => tokenTaskSource.TrySetResult(null)))
            {
                if(token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
                if (
                    await Task.WhenAny(
                        task,
                        tokenTaskSource.Task
                    ).ConfigureAwait(true)
                    == tokenTaskSource.Task
                )
                    token.ThrowIfCancellationRequested();
                return await task.ConfigureAwait(true);
            }
        }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private class LogTraceWriter : ITraceWriter
        {
            private readonly Logger _log;
            public LogTraceWriter(Logger log)
            {
                _log = log;
            }

            public TraceLevel LevelFilter
            {
                get { return TraceLevel.Verbose; }
            }

            public void Trace(TraceLevel level, string message, Exception ex)
            {
                _log.Trace(
                    string.Format(
                        "LogTraceWriter {0} {1} '{2}'",
                        level,
                        message ?? "",
                        ex != null ? ex.ToString() : ""
                    )
                );
            }
        }
        public static readonly JsonSerializerSettings DefaultSerializerSettings
            = new JsonSerializerSettings()
            {
                Culture = CultureInfo.InvariantCulture,
                MaxDepth = 10,
                //TraceWriter = new LogTraceWriter(_logger)
            };

        public static T1 ParseJsonToTypeWithSettings<T1>(
            this string s,
            JsonSerializerSettings serializerSettings
        )
        {
            return JsonConvert.DeserializeObject<T1>(
                s,
                serializerSettings
            );
        }

        public static T1 ParseJsonToType<T1>(this string s)
        {
            return ParseJsonToTypeWithSettings<T1>(
                s,
                DefaultSerializerSettings
            );
        }

        public static object ParseJsonToType(this string s, Type t)
        {
            Assert.NotNull(s);
            Assert.NotNull(t);
            return JsonConvert.DeserializeObject(
                s,
                t,
                DefaultSerializerSettings
            );
        }

        public static string WriteObjectToJsonWithSettings<T1>(
            this T1 obj,
            JsonSerializerSettings serializerSettings,
            Type objType = null
        )
        {
            if (objType == null)
                objType = typeof (T1);
            return JsonConvert.SerializeObject(
                obj,
                objType,
                Formatting.Indented,
                serializerSettings
            );
        }

        public static string WriteObjectToJson(
            this object obj,
            Type objType
        )
        {
            return WriteObjectToJsonWithSettings(
                obj,
                DefaultSerializerSettings, 
                objType
            );
        }

        public static string WriteObjectToJson<T1>(
            this T1 obj
        )
        {
            return WriteObjectToJsonWithSettings(
                obj,
                DefaultSerializerSettings
            );
        }
        /**/
        public static byte[] Compress(this byte[] b)
        {
            return MiscFuncs.Compress(b);
        }

        public static byte[] Decompress(this byte[] b)
        {
            return MiscFuncs.Decompress(b);
        }
        /*
        /// <summary>
        /// JSON Deserialize(Serialize(T1 value))
        /// </summary>
        /// <typeparam name="T1">Type</typeparam>
        /// <param name="obj">Object to copy</param>
        /// <param name="specifiedT1Type">Specified type</param>
        /// <returns>Copy of object</returns>
        public static T1 GetCopy<T1>(this T1 obj, Type specifiedT1Type = null)
        {
            if (specifiedT1Type == null)
                specifiedT1Type = typeof (T1);
            var serializedObj = JsonConvert.SerializeObject(obj, Formatting.Indented);
            var copy = (T1)JsonConvert.DeserializeObject(serializedObj, specifiedT1Type);
            return copy;
        }
        */
        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof (T), false);
            return (T) attributes[0];
        }

        public static void With<T>(
            this T item, Action<T> work)
        {
            work(item);
        }

        public static async Task WithAsync<T>(
            this T item, Func<T,Task> work)
        {
            await work(item).ConfigureAwait(true);
        }
        /**/
        public static async Task WithAsyncLockSem<T, TRes>(
            this T item,
            Func<T, Task> work
        )
            where T : class
        {
            Assert.NotNull(item);
            using (await item.GetLockSem().GetDisposable().ConfigureAwait(true))
            {
                await work(item).ConfigureAwait(true);
            }
        }
        public static async Task<TRes> WithAsyncLockSem<T,TRes>(
            this T item,
            Func<T, Task<TRes>> work
        )
            where T : class
        {
            Assert.NotNull(item);
            using (await item.GetLockSem().GetDisposable().ConfigureAwait(true))
            {
                return await work(item).ConfigureAwait(true);
            }
        }
        public static async Task WithAsyncLockSem<T>(
            this T item,
            Action<T> work
        )
            where T : class
        {
            Assert.NotNull(item);
            using (await item.GetLockSem().GetDisposable().ConfigureAwait(true))
            {
                work(item);
            }
        }
        public static async Task<TRes> WithAsyncLockSem<T, TRes>(
            this T item,
            Func<T, TRes> work
        )
            where T : class
        {
            Assert.NotNull(item);
            using (await item.GetLockSem().GetDisposable().ConfigureAwait(true))
            {
                return work(item);
            }
        }
        /**/
        public static TRes With<T, TRes>(
            this T item, Func<T, TRes> work)
        {
            return work(item);
        }

        public static async Task<TRes> WithAsync<T, TRes>(
            this T item, Func<T, Task<TRes>> work)
        {
            return await work(item).ConfigureAwait(true);
        }
        /**/

        public static async Task WithMyAsyncDisposable(
		    this IMyAsyncDisposable disp,
		    Func<Task> asyncWork
		    )
	    {
			Assert.NotNull(disp);
		    try
		    {
			    await asyncWork().ConfigureAwait(true);
		    }
		    finally
		    {
			    await disp.MyDisposeAsync().ConfigureAwait(true);
		    }
	    }
		
		public static bool In<T1>(this T1 arg, params T1[] args)
	    {
			Assert.NotEmpty(args);
		    return args.Contains(arg);
	    }
		/**/
		public static IObservable<IList<TSource>> BufferNotEmpty<TSource>(
			this IObservable<TSource> source, 
			TimeSpan timeSpan
		)
	    {
		    return source.Buffer(timeSpan).Where(_ => _.Any());
	    }

	    public static IObservable<IList<TSource>> BufferNotEmpty<TSource>(
		    this IObservable<TSource> source,
		    int count)
	    {
		    return source.Buffer(count).Where(_ => _.Any());
	    }

		public static IObservable<IList<TSource>> BufferNotEmpty<TSource>(
		    this IObservable<TSource> source,
		    TimeSpan timeSpan,
		    int count)
		{
			return source.Buffer(timeSpan, count).Where(_ => _.Any());
		}
	}
}
