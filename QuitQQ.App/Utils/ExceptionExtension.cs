using Flurl.Util;

namespace QuitQQ.App.Utils;

internal static class ExceptionExtension
{
    public static string FormatException(this Exception ex, uint indent = 0)
    {
        var indentStr = new string(' ', (int)indent);
        return $@"-- BEGIN Exception Log <{ex.GetType()}> {DateTime.Now} --
Message: {ex.Message}
Data: 
{ex.Data
    .ToKeyValuePairs()
    .Select((k, v) => $"    {k}: {v}")
    .Aggregate(
        string.Empty,
        (x, y) => string.Join('\n', x, y)
    )}
Source: {ex.Source}
HResult: {ex.HResult}
StackTrace: 
{ex.StackTrace}
InnerException: {ex.InnerException?.FormatException(indent + 4) ?? "<None>" }
-- END Exception Log <{ex.GetType()}> {DateTime.Now} --"
            .Split('\n')
            .Select(x => indentStr + x)
            .Aggregate(
                string.Empty,
                (x, y) => string.Join('\n', x, y)
            );
    }
}

