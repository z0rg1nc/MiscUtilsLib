using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BtmI2p.Newtonsoft.Json;
using NLog;
using Xunit;

namespace BtmI2p.MiscUtils
{
    public class ConcreteTypeConverter<TConcrete,TGeneric> : JsonConverter
        where TConcrete : TGeneric
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(
            JsonReader reader, 
            Type objectType, 
            object existingValue, 
            JsonSerializer serializer
        )
        {
            return serializer.Deserialize<TConcrete>(reader);
        }

        public override void WriteJson(
            JsonWriter writer, 
            object value, 
            JsonSerializer serializer
        )
        {
            serializer.Serialize(writer, value, typeof(TGeneric));
        }
    }

    public interface IMyAsyncDisposable
    {
        Task MyDisposeAsync();
    }

    public class CompositeMyAsyncDisposable : IMyAsyncDisposable
    {
        private readonly IMyAsyncDisposable[] _asyncDisposables;
        public CompositeMyAsyncDisposable(params IMyAsyncDisposable[] asyncDisposables)
        {
            _asyncDisposables = asyncDisposables;
        }

        public async Task MyDisposeAsync()
        {
            foreach (var asyncDisposable in _asyncDisposables)
            {
                await asyncDisposable.MyDisposeAsync().ConfigureAwait(true);
            }
        }
    }
    public class ActionDisposable : IDisposable
    {
        private readonly Action _onDisposeAction;
        public ActionDisposable(Action onInit, Action onDispose)
        {
            _onDisposeAction = onDispose;
            if(onInit != null)
                onInit();
        }

        public void Dispose()
        {
            if (_onDisposeAction != null)
                _onDisposeAction();
        }
    }

    public class StopWatchDisposable : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _name;
        private readonly Stopwatch _sw;
        private readonly Guid _swId = Guid.NewGuid();
        public StopWatchDisposable(string name)
        {
            _name = name;
            _sw = new Stopwatch();
            _logger.Trace(
                "SW({1}):'{0}' enter", 
                name, 
                _swId.ToString().Substring(0,5)
            );
            _sw.Start();
        }

        public void PrintScore(string label)
        {
            if (_disposed)
                throw new InvalidOperationException("Disposed");
            _logger.Trace(
                "SW({2}):'{0}' LBL:{3} {1} ms",
                _name,
                _sw.ElapsedMilliseconds,
                _swId.ToString().Substring(0, 5),
                label
            );
        }
        private bool _disposed = false;
        public void Dispose()
        {
            if(_disposed)
                throw new InvalidOperationException("Disposed");
            _sw.Stop();
            _logger.Trace(
                "SW({2}):'{0}' leave {1} ms", 
                _name, 
                _sw.ElapsedMilliseconds, 
                _swId.ToString().Substring(0, 5)
            );
            _disposed = true;
        }
    }
    public interface ICheckable
    {
        void CheckMe();
    }

    public interface ICheckableAsync
    {
        Task CheckMeAsync();
    }
    public static partial class MiscFuncs
    {
        public static async Task<bool> WaitForProcessExitAsync(
            Process p, 
            CancellationToken token, 
            int milliseconds
        )
        {
            if(p == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => p));
            var tDelay = Task.Delay(milliseconds);
            while (true)
            {
                if (p.HasExited)
                    return true;
                if (tDelay.IsCompleted)
                    return false;
                if (token.IsCancellationRequested)
                    return false;
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        public static byte[] Compress(byte[] raw)
        {
            if (raw == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => raw)
                );
            using (var msWrapper = MemoryStreamPool.DefaultPool.GetStreamWrapper())
            {
                var msWrapperStream = msWrapper.MStream;
                using (var gzip = new GZipStream(msWrapperStream, CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return msWrapperStream.ToArray();
            }
        }

        public static byte[] Decompress(byte[] gzip)
        {
            if(gzip == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => gzip)
                );
            using (
                var gzipMsWrapper = MemoryStreamPool.DefaultPool.GetStreamWrapper(gzip)
            )
            {
                var gzipMs = gzipMsWrapper.MStream;
                using (
                    var stream = new GZipStream(
                        gzipMs,
                        CompressionMode.Decompress,
                        true
                    )
                )
                {
                    const int size = 4096;
                    var buffer = new byte[size];
                    using (var msWrapper = MemoryStreamPool.DefaultPool.GetStreamWrapper())
                    {
                        var msWrapperStream = msWrapper.MStream;
                        int count = 0;
                        do
                        {
                            count = stream.Read(buffer, 0, size);
                            if (count > 0)
                            {
                                msWrapperStream.Write(buffer, 0, count);
                            }
                        } while (count > 0);
                        return msWrapperStream.ToArray();
                    }
                }
            }
        }

        public static Guid GenGuidWithFirstBytes(params byte[] firstBytes)
        {
            Assert.NotNull(firstBytes);
            Assert.InRange(firstBytes.Length,0,16);
            var genGuidBytes = Guid.NewGuid().ToByteArray();
            for (int i = 0; i < firstBytes.Length; i++)
            {
                genGuidBytes[i] = firstBytes[i];
            }
            return new Guid(genGuidBytes);
        }

        public static Guid GenMaskedGuid(Guid mask, Guid maskEqual, Guid originGuid)
        {
            var maskBytes = mask.ToByteArray();
            var maskEqualBytes = maskEqual.ToByteArray();
            var genGuidBytes = originGuid.ToByteArray();
            var resultIdBytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                resultIdBytes[i] = (byte)
                (
                    (maskBytes[i] & maskEqualBytes[i])
                    ^ ((0xff ^ maskBytes[i]) & genGuidBytes[i])
                );
            }
            return new Guid(resultIdBytes);
        }

        public static Guid GenRandMaskedGuid(Guid mask, Guid maskEqual)
        {
            return GenMaskedGuid(mask, maskEqual, Guid.NewGuid());
        }

        public static T[] ConcatArrays<T>(params T[][] list)
        {
            var result = new T[list.Sum(a => a.Length)];
            int offset = 0;
            for (int x = 0; x < list.Length; x++)
            {
                list[x].CopyTo(result, offset);
                offset += list[x].Length;
            }
            return result;
        }
        static MiscFuncs()
        {
            CultureFormat.NumberFormat.CurrencyDecimalSeparator = ".";
        }
        public static CultureInfo CultureFormat = 
            (CultureInfo)CultureInfo.InvariantCulture.Clone();
        
        public static Type[] GetExtraTypes(Type[] baseClasses)
        {
            Contract.Requires<ArgumentNullException>(baseClasses != null);
            Contract.Requires<ArgumentOutOfRangeException>(baseClasses.Length > 0);
            var result = new List<Type>();
            AppDomain currentDomain = AppDomain.CurrentDomain;
            var types = new List<Type>();
            foreach (Assembly assembly in currentDomain.GetAssemblies())
            {
                types.AddRange(assembly.GetTypes());
            }
            foreach (Type type in types)
            {
                if (
                    baseClasses.Any(x => type.IsSubclassOf(x))
                    && !result.Contains(type)
                )
                    result.Add(type);
            }
            return result.ToArray();
        }
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static int _maxFrameCount = 350;
        public static int CheckStackFrameCount()
        {
#if DEBUG
            var st = new StackTrace();
            if (st.FrameCount > _maxFrameCount)
            {
                _maxFrameCount = st.FrameCount;
                _log.Trace(
                    "New stack framecount max {0} '{1}'",
                    _maxFrameCount,
                    st.ToString()
                );
            }
            return st.FrameCount;
#else
            return 0;
#endif
        }

        public static string ToBinaryString(byte[] a)
        {
            return String.Join(
                " ",
                a.Select(x => Convert.ToString(x, 2).PadLeft(8, '0'))
            );
        }
        private static readonly RNGCryptoServiceProvider Rng 
            = new RNGCryptoServiceProvider();
        public static void GetRandomBytes(byte[] bArray)
        {
            if (bArray == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => bArray));
            Rng.GetBytes(bArray);
        }
        /**/

        public static T1 GetDeepCopy<T1>(T1 orig)
		{
            return orig.WriteObjectToJson().ParseJsonToType<T1>();
        }

        public static TTo GetDeepCopy<TFrom, TTo>(
			TFrom orig,
			TTo toDefault = null
		)
            where TTo : class, TFrom, new()
			where TFrom : class
        {
            return orig.WriteObjectToJson().ParseJsonToType<TTo>();
        }

        public static async Task<T1> RepeatWhileTimeout<T1>(
            Func<Task<T1>> action,
            params CancellationToken[] tokens
        )
        {
            using (
                var compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    tokens.Length > 0 ? tokens : new[] { CancellationToken.None }
                )
            )
            {
                while (true)
                {
                    try
                    {
                        return await action()
                            .ThrowIfCancelled(compositeTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                    }
                }
            }
        }

        public static async Task RepeatWhileTimeout(
            Func<Task> action,
            params CancellationToken[] tokens
        )
        {
            using (
                var compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    tokens.Length > 0 ? tokens : new[] { CancellationToken.None }
                    )
                )
            {
                while (true)
                {
                    try
                    {
                        await action()
                            .ThrowIfCancelled(compositeTokenSource.Token)
                            .ConfigureAwait(false);
                        break;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
            }
        }
		public static void HandleUnexpectedError(
			Exception exc,
			Logger log,
			[CallerMemberName] string mthdName = "",
			[CallerLineNumber] int lineNum = 0
		)
		{
			log.Error(
				"{0}:{1} unexpected error {2}",
				mthdName,
				lineNum,
				exc.ToString()
			);
		}
		public static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}
        
        public static DateTime RoundDateTimeToSeconds(DateTime origTime)
        {
            var result = new DateTime(
                origTime.Year,
                origTime.Month,
                origTime.Day,
                origTime.Hour,
                origTime.Minute,
                origTime.Second,
                DateTimeKind.Utc
            );
            return result;
        }

        public static class SerializeComparer
        {
            public static SerializeComparer<T1> CreateInstance<T1>(T1 item)
            {
                return new SerializeComparer<T1>();
            }
            public static SerializeComparer<T1> CreateInstanceFromEnumerable<T1>(
                IEnumerable<T1> items)
            {
                return new SerializeComparer<T1>();
            }
        }
        public class SerializeComparer<T1> : IEqualityComparer<T1>
        {
            public bool Equals(T1 x, T1 y)
            {
                return x.WriteObjectToJson() == y.WriteObjectToJson();
            }

            public int GetHashCode(T1 obj)
            {
                return obj.WriteObjectToJson().GetHashCode();
            }
        }
    }
}
