using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BtmI2p.Newtonsoft.Json;
using NLog;
using Xunit;

namespace BtmI2p.MiscUtils
{
    public interface ILockSemaphoreSlim
    {
        [JsonIgnore]
        SemaphoreSlim LockSem { get; }
    }
    public class SemaphoreSlimDisposableWrapper : IDisposable
    {
        private static readonly Logger _logger
            = LogManager.GetCurrentClassLogger();
        private readonly SemaphoreSlim _lockSem;
        private SemaphoreSlimDisposableWrapper(SemaphoreSlim lockSem)
        {
            _lockSem = lockSem;
        }

        private static readonly ConcurrentDictionary<long, SemaphoreSlimDisposableWrapper> SemWrappersDb
            = new ConcurrentDictionary<long, SemaphoreSlimDisposableWrapper>();

        public static List<SemaphoreSlimDisposableWrapper> WrappersDb
        {
            get
            {
                return new List<SemaphoreSlimDisposableWrapper>(SemWrappersDb.Values);
            }
        }

        private static long _nextDbNum = 0;
        public static async Task<SemaphoreSlimDisposableWrapper> CreateInstance(
            SemaphoreSlim lockSem,
            string mthdName,
            CancellationToken token
        )
        {
            if (lockSem == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => lockSem)
                );
            var wrapperNum = Interlocked.Increment(ref _nextDbNum);
            var result = new SemaphoreSlimDisposableWrapper(lockSem);
            result._wrapperNum = wrapperNum;
            result.MthdName = mthdName;
            result.TryEnterTime = DateTime.UtcNow;
            SemWrappersDb.TryAdd(wrapperNum, result);
            await result._lockSem.WaitAsync(token).ConfigureAwait(false);
            result.EnterTime = DateTime.UtcNow;
            return result;
        }

        public DateTime TryEnterTime = DateTime.MinValue;
        public DateTime EnterTime = DateTime.MinValue;
        public DateTime LeaveTime = DateTime.MinValue;

        private long _wrapperNum;
        public string MthdName;
        public void Dispose()
        {
            _lockSem.Release();
            LeaveTime = DateTime.UtcNow;
            SemaphoreSlimDisposableWrapper wrap;
            SemWrappersDb.TryRemove(_wrapperNum, out wrap);
        }
    }

    public class SemaphoreSlimCalledWrapper
    {
        public SemaphoreSlimCalledWrapper(SemaphoreSlim lockSem)
        {
            LockSem = lockSem;
        }
        public readonly SemaphoreSlim LockSem;
        public bool Called = false;
    }
    public static class SemaphoreSlimExtensions
    {
        private static readonly ConditionalWeakTable<object,SemaphoreSlim> LockSemsTable
            = new ConditionalWeakTable<object, SemaphoreSlim>();
        public static SemaphoreSlim GetLockSem<T1>(
            this T1 obj,
            int initialCount = 1
        ) where T1 : class
        {
            Assert.NotNull(obj);
            return LockSemsTable.GetValue(
                obj,
                x => new SemaphoreSlim(initialCount)
            );
        }
        /**/
        private static readonly ConditionalWeakTable<SemaphoreSlim,SemaphoreSlimCalledWrapper> 
            CalledWrappers = new ConditionalWeakTable<SemaphoreSlim, SemaphoreSlimCalledWrapper>();

        public static SemaphoreSlimCalledWrapper GetCalledWrapper(
            this SemaphoreSlim lockSem
            )
        {
            Assert.NotNull(lockSem);
            return CalledWrappers.GetValue(
                lockSem, 
                _ => new SemaphoreSlimCalledWrapper(lockSem)
            );
        }
        /**/
        public static async Task<IDisposable> GetDisposable(
            this SemaphoreSlim lockSemaphoreSlim,
            bool throwCancelledIfSemLocked = false,
            CancellationToken token = default(CancellationToken),
            [CallerMemberName] string mthdName = "",
            [CallerLineNumber] int lineNum = 0,
#if DEBUG
            [CallerFilePath] 
#endif
            string fp = ""
        )
        {
            int fc = MiscFuncs.CheckStackFrameCount();
            if (
                throwCancelledIfSemLocked
                && lockSemaphoreSlim.CurrentCount == 0
            )
                throw new OperationCanceledException();
            return await SemaphoreSlimDisposableWrapper
                .CreateInstance(
                    lockSemaphoreSlim,
                    $"{fc} {mthdName} line {lineNum} file {fp}",
                    token
                ).ConfigureAwait(false);
        }
    }
    public class SemaphoreSlimSet
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _lockSemDb
            = new ConcurrentDictionary<string, SemaphoreSlim>();
        public SemaphoreSlim this[string index]
        {
            get
            {
                return _lockSemDb.GetOrAdd(
                    index,
                    i => new SemaphoreSlim(1)
                );
            }
        }

        public async Task<IDisposable> GetDisposable(
            string index,
            bool throwCancelledIfSemLocked = false,
            CancellationToken token = default(CancellationToken),
            [CallerMemberName] string mthdName = "",
            [CallerLineNumber] int lineNum = 0,
#if DEBUG
            [CallerFilePath] 
#endif
            string fp = ""
        )
        {
            return await this[index].GetDisposable(
                throwCancelledIfSemLocked,
                token,
                mthdName,
                lineNum,
                fp
            ).ConfigureAwait(false);
        }
    }
}
