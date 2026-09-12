using System;

namespace DapperMany.Samples.Infrastructure.Output
{
    public class ConsoleOutputFormatter : IOutputFormatter
    {
        public void WriteHeader(string text) => Console.WriteLine(text);
        public void WriteInfo(string text) => Console.WriteLine(text);
        public void WriteSuccess(string text) => Console.WriteLine(text);
        public void WriteError(string text) => Console.WriteLine(text);
        public void WriteLine(string text) => Console.WriteLine(text);
    }
}
