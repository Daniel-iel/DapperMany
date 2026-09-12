namespace DapperMany.Samples.Infrastructure.Output
{
    public interface IOutputFormatter
    {
        void WriteHeader(string text);
        void WriteInfo(string text);
        void WriteSuccess(string text);
        void WriteError(string text);
        void WriteLine(string text);
    }
}
