using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BtmI2p.MiscUtils
{
    public class ConsoleAsync
    {
        // Only for keyboard, Console.KeyAvailable doesn't work with redirected input
        public static async Task<string> ReadLine(CancellationToken token)
        {
            var inStream = Console.In;
            var sb = new StringBuilder();
            while (true)
            {
                if(token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
                if (Console.KeyAvailable)
                {
                    int ch = inStream.Read();
                    if(ch == -1) break;
                    if(ch == '\r') continue;
                    if (ch == '\n') return sb.ToString();
                    sb.Append((char) ch);
                }
                else
                {
                    await Task.Delay(50, token).ConfigureAwait(false);
                }
            }
            if (sb.Length > 0) return sb.ToString();
            return null;
        }
    }
}
