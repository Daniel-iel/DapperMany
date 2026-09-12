using System;
using DapperMany.Samples.Infrastructure.Output;

namespace DapperMany.Samples.Infrastructure.Error
{
    public class ConsoleErrorHandler : IErrorHandler
    {
        private readonly IOutputFormatter _output;

        public ConsoleErrorHandler(IOutputFormatter output)
        {
            _output = output;
        }

        public void Handle(Exception ex, string? contextMessage = null)
        {
            if (!string.IsNullOrEmpty(contextMessage))
            {
                _output.WriteError($"  ✗ {contextMessage}: {ex.Message}");
            }
            else
            {
                _output.WriteError($"  ✗ Error: {ex.Message}");
            }
        }
    }
}
