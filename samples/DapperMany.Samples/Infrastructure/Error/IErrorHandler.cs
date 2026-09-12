using System;

namespace DapperMany.Samples.Infrastructure.Error
{
    public interface IErrorHandler
    {
        void Handle(Exception ex, string? contextMessage = null);
    }
}
