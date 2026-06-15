using Serilog.Core;
using Serilog.Events;

namespace ProjectFill.API;

public sealed class ShortSourceContextEnricher : ILogEventEnricher
{
    private const string SourceContextPropertyName = "SourceContext";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue(SourceContextPropertyName, out var property)
            || property is not ScalarValue { Value: string sourceContext }
            || string.IsNullOrWhiteSpace(sourceContext))
        {
            return;
        }

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
            SourceContextPropertyName,
            Shorten(sourceContext)));
    }

    private static string Shorten(string sourceContext)
    {
        var genericMarkerIndex = sourceContext.IndexOf('`');
        if (genericMarkerIndex >= 0)
            sourceContext = sourceContext[..genericMarkerIndex];

        var separatorIndex = Math.Max(sourceContext.LastIndexOf('.'), sourceContext.LastIndexOf('+'));
        return separatorIndex >= 0 && separatorIndex < sourceContext.Length - 1
            ? sourceContext[(separatorIndex + 1)..]
            : sourceContext;
    }
}
