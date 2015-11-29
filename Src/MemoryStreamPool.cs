using System;
using System.Collections.Concurrent;
using System.IO;
using NLog;

namespace BtmI2p.MiscUtils
{
    public class NoClosingMemoryStream : MemoryStream
    {
        public override void Close()
        {
            /* Suppress close, all in dispose */
        }
    }
    public class MemoryStreamWrapper : IDisposable
    {
        private readonly NoClosingMemoryStream _ms;
        private readonly ConcurrentQueue<NoClosingMemoryStream> _addOnExit;
        public MemoryStreamWrapper(
            NoClosingMemoryStream ms,
            ConcurrentQueue<NoClosingMemoryStream> addOnExit 
        )
        {
            if(ms == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => ms));
            if (addOnExit == null)
                throw new ArgumentNullException(
                    MyNameof.GetLocalVarName(() => addOnExit));
            _addOnExit = addOnExit;
            _ms = ms;
        }
        public MemoryStream MStream { get { return _ms; } }
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public void Dispose()
        {
            try
            {
                var curLength = (int) _ms.Length;
                Array.Clear(_ms.GetBuffer(), 0, curLength);
                _ms.Seek(0, SeekOrigin.Begin);
                _ms.SetLength(0);
            }
            finally
            {
                _addOnExit.Enqueue(_ms);
            }
        }
    }
    public class MemoryStreamPool
    {
        public static readonly MemoryStreamPool DefaultPool = new MemoryStreamPool();
        private readonly ConcurrentQueue<NoClosingMemoryStream> _streams 
            = new ConcurrentQueue<NoClosingMemoryStream>();
        public MemoryStreamWrapper GetStreamWrapper(byte[] initData)
        {
            var wrapper = GetStreamWrapper();
            wrapper.MStream.Write(initData,0,initData.Length);
            wrapper.MStream.Seek(0, SeekOrigin.Begin);
            return wrapper;
        }
        public MemoryStreamWrapper GetStreamWrapper()
        {
            NoClosingMemoryStream returnStream;
            if(!_streams.TryDequeue(out returnStream))
                returnStream = new NoClosingMemoryStream();
            return new MemoryStreamWrapper(
                returnStream,
                _streams
            );
        }
    }
}
