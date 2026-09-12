using System;

namespace DapperMany.Samples.Infrastructure.Output
{
    public class ConsoleOutputFormatter : IOutputFormatter
    {
        private static readonly object _sync = new();

        private void SafeWrite(ConsoleColor color, string text)
        {
            // Try to set color and write; if console not available, fall back to plain write
            lock (_sync)
            {
                try
                {
                    var prev = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                    Console.WriteLine(text);
                    Console.ForegroundColor = prev;
                }
                catch
                {
                    try { Console.WriteLine(text); } catch { /* swallow */ }
                }
            }
        }

        public void WriteHeader(string text) => SafeWrite(ConsoleColor.Cyan, text);
        public void WriteInfo(string text) => SafeWrite(ConsoleColor.Yellow, text);
        public void WriteSuccess(string text) => SafeWrite(ConsoleColor.Green, text);
        public void WriteError(string text) => SafeWrite(ConsoleColor.Red, text);
        public void WriteLine(string text)
        {
            try { Console.WriteLine(text); } catch { /* swallow */ }
        }
    }
}
