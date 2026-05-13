using System.Collections.Concurrent;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Jint.Native;

namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Binds Mirth-style variable maps onto a Jint engine:
/// <c>sourceMap</c>, <c>channelMap</c>, <c>connectorMap</c>, <c>responseMap</c>,
/// <c>globalChannelMap</c>, <c>globalMap</c>, <c>configurationMap</c>,
/// plus the unified <c>$(key)</c> walker (precedence per Mirth UG p454).
/// </summary>
internal static class VariableMapsJsBinder
{
    public static BoundMaps BindAll(
        Engine engine,
        FlowExecutionContext ctx,
        IReadOnlyDictionary<string, string> globalChannel,
        IReadOnlyDictionary<string, string> global,
        IReadOnlyDictionary<string, string> configuration)
    {
        var sourceMap = new ReadOnlyMap(ctx.SourceMap);
        var channelMap = new MutableMap(ctx.ChannelMap);
        var connectorMap = new MutableMap(ctx.CurrentConnectorMap);
        var responseMap = new ReadOnlyMap(ctx.ResponseMap);

        var globalChannelDict = new ConcurrentDictionary<string, object?>(
            globalChannel.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)),
            StringComparer.Ordinal);
        var globalDict = new ConcurrentDictionary<string, object?>(
            global.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)),
            StringComparer.Ordinal);

        var globalChannelMap = new MutableMap(globalChannelDict);
        var globalMap = new MutableMap(globalDict);
        var configurationMap = new ReadOnlyMap(configuration.ToDictionary(
            kv => kv.Key,
            kv => (object?)kv.Value,
            StringComparer.Ordinal));

        engine.SetValue("sourceMap", sourceMap);
        engine.SetValue("channelMap", channelMap);
        engine.SetValue("connectorMap", connectorMap);
        engine.SetValue("responseMap", responseMap);
        engine.SetValue("globalChannelMap", globalChannelMap);
        engine.SetValue("globalMap", globalMap);
        engine.SetValue("configurationMap", configurationMap);

        engine.SetValue("$", new Func<string, JsValue>(key =>
        {
            var found = responseMap.get(key)
                ?? connectorMap.get(key)
                ?? channelMap.get(key)
                ?? sourceMap.get(key)
                ?? globalChannelMap.get(key)
                ?? globalMap.get(key)
                ?? configurationMap.get(key);
            return found is null
                ? JsValue.Undefined
                : JsValue.FromObject(engine, found);
        }));

        return new BoundMaps(globalChannelDict, globalDict);
    }

    public sealed record BoundMaps(
        IDictionary<string, object?> GlobalChannel,
        IDictionary<string, object?> Global);

    public sealed class ReadOnlyMap(IReadOnlyDictionary<string, object?> store)
    {
        public object? get(string key) => store.TryGetValue(key, out var v) ? v : null;
        public bool containsKey(string key) => store.ContainsKey(key);
    }

    public sealed class MutableMap(IDictionary<string, object?> store)
    {
        public object? get(string key) => store.TryGetValue(key, out var v) ? v : null;
        public void put(string key, object? value) => store[key] = value;
        public bool containsKey(string key) => store.ContainsKey(key);
        public void remove(string key) => store.Remove(key);
    }
}
