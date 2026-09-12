using DapperMany.Samples.Infrastructure.Output;

namespace DapperMany.Samples.Infrastructure.Logging
{
    public class SampleLogger
    {
        private readonly IOutputFormatter _formatter;

        public SampleLogger() : this(new ConsoleOutputFormatter()) { }

        public SampleLogger(IOutputFormatter formatter)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public IOutputFormatter Formatter => _formatter;

        public void Header(string text) => _formatter.WriteHeader(text);
        public void Info(string text) => _formatter.WriteInfo(text);
        public void Success(string text) => _formatter.WriteSuccess(text);
        public void Error(string text) => _formatter.WriteError(text);
        public void Write(string text) => _formatter.WriteLine(text);

        // Prompt inline (writes without newline and reads user input)
        public string PromptInline(string prompt)
        {
            if (!string.IsNullOrEmpty(prompt))
                Console.Write(prompt);

            return Console.ReadLine() ?? string.Empty;
        }
    }
}
