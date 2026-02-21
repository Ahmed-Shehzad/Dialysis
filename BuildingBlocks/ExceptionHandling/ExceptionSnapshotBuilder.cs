namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Builds a complete ExceptionSnapshot from an Exception, including inner exceptions and Data.
/// </summary>
public static class ExceptionSnapshotBuilder
{
    public static ExceptionSnapshot Build(Exception exception)
    {
        var data = new Dictionary<string, string>();
        if (exception.Data.Count > 0)
        {
            foreach (System.Collections.DictionaryEntry entry in exception.Data)
            {
                string key = entry.Key?.ToString() ?? "(null)";
                string value = entry.Value?.ToString() ?? "(null)";
                data[key] = value;
            }
        }

        return new ExceptionSnapshot
        {
            Type = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Source = exception.Source,
            HelpLink = exception.HelpLink,
            Data = data,
            InnerException = exception.InnerException is not null ? Build(exception.InnerException) : null,
            ToStringOutput = exception.ToString(),
        };
    }
}
