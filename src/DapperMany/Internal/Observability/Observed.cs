namespace DapperMany.Internal.Observability;

/// <summary>
/// Minimal observability helpers used across the library.
/// </summary>
internal static class Observed
{
    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("DapperMany.Observability");

    public static System.Diagnostics.Activity? Start(string name)
    {
        return ActivitySource.StartActivity(name);
    }
}
