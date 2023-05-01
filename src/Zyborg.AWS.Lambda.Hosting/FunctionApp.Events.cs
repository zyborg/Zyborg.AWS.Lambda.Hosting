using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zyborg.AWS.Lambda.Hosting;

public partial class FunctionApp
{

    // Event Types are represented as strings so that we don't
    // need to take a dependency on each corresponding library
    private readonly static List<KeyValuePair<string, Func<JsonDocument, bool>>>
        _defaultEventMatchers = new()
        {
            // This was assembled with the help of:
            //    https://docs.aws.amazon.com/lambda/latest/dg/eventsources.html
            new("Amazon.Lambda.S3Events.S3Event",
                jdoc => MatchS3Event(jdoc)),
            new("Amazon.Lambda.SNSEvents.SNSEvent",
                jdoc => MatchSNSEvent(jdoc)),
            new("Amazon.Lambda.CloudWatchLogsEvents.CloudWatchLogsEvent",
                jdoc => MatchCWLogsEvent(jdoc)),

            // This one is WRONG in the link above, but is clarified here:
            //    https://docs.aws.amazon.com/ses/latest/DeveloperGuide/receiving-email-action-lambda.html
            new("SESEvent", jdoc => false),

        };
    // Build a map for faster lookup by type
    private readonly static Dictionary<string, Func<JsonDocument, bool>> _defaultEventMatchersByType =
        new(_defaultEventMatchers);

    private readonly List<KeyValuePair<Type, Func<JsonDocument, bool>>> _eventMatchers = new();

    // TODO: in the future may expose this for configuration, perhaps in the FunctionAppBuilder
    private readonly JsonSerializerOptions _eventDecodingJsonSerOptions = DefaultJsonSerializerOptions;

    /// <summary>
    /// Adds all pre-defined event matchers, or a specified subset of
    /// event type to match for.
    /// </summary>
    /// <remarks>
    /// The supported predefined event matchers are:
    /// <list type="bullet">
    /// <item><c>Amazon.Lambda.S3Events.S3Event</c></item>
    /// <item><c>Amazon.Lambda.SNSEvents.SNSEvent</c></item>
    /// <item><c>Amazon.Lambda.CloudWatchLogsEvents.CloudWatchLogsEvent</c></item>
    /// </list>
    /// <para>
    /// If no subset of event types are specified then every defined event type
    /// will be tested to see if it can be resolved in the current context, and
    /// if so it will be added.  There is a predefined order for the matchers
    /// (as listed above).
    /// </para><para>
    /// If a subset of event types is specified, then only those event types will
    /// be registered, and in the ordered provided in the subset array.
    /// However, if an event type is spedcified that is not defined in the
    /// pre-dfined set, then an exception will be thrown.
    /// </para>
    /// </remarks>
    /// <param name="selectTypes"></param>
    public void AddBuiltInEventMatchers(params Type[] selectTypes)
    {
        if (selectTypes == null)
        {
            // Add all the event types and matchers in our pre-defined order
            foreach (var m in _defaultEventMatchers)
            {
                var t = Type.GetType(m.Key);
                if (t != null)
                {
                    AddEventMatcher(t, m.Value);
                }
            }
        }
        else
        {
            foreach (var t in selectTypes)
            {
                var tn = t.FullName;
                if (!_defaultEventMatchersByType.TryGetValue(tn!, out var p))
                {
                    throw new NotSupportedException($"no default event matcher exists for type [{tn}]");
                }

                AddEventMatcher(t, p);
            }
        }
    }

    /// <summary>
    /// Defines a predicate that matches the incoming
    /// request JSON payload to a Lambda event type.
    /// </summary>
    public void AddEventMatcher<TEvent>(Func<JsonDocument, bool> predicate) =>
        AddEventMatcher(typeof(TEvent), predicate);

    /// <summary>
    /// Defines a predicate that matches the incoming
    /// request JSON payload to a Lambda event type.
    /// </summary>
    public void AddEventMatcher(Type eventType, Func<JsonDocument, bool> predicate)
    {
        var typeName = eventType.FullName;
        _eventMatchers.Add(new(eventType, predicate));
    }

    private bool DecodeEvent(JsonDocument jdoc,
        [NotNullWhen(true)]out Type? eventType,
        [NotNullWhen(true)]out object? eventValue)
    {
        foreach (var m in _eventMatchers)
        {
            if (m.Value(jdoc))
            {
                eventType = m.Key;
                eventValue = jdoc.Deserialize(eventType, _eventDecodingJsonSerOptions)!;
                return true;
            }
        }

        eventType = null;
        eventValue = null;
        return false;
    }

    private static bool MatchS3Event(JsonDocument jdoc)
    {
        // S3Event has the JSON Path:  Records[0].s3
        return jdoc.RootElement.TryGetProperty("Records", out var records)
            && records.ValueKind == JsonValueKind.Array
            && records.GetArrayLength() > 0
            && records[0].TryGetProperty("s3", out _);
    }

    private static bool MatchSNSEvent(JsonDocument jdoc)
    {
        // SNSEvent has the JSON Path:  Records[0].Sns
        return jdoc.RootElement.TryGetProperty("Records", out var records)
            && records.ValueKind == JsonValueKind.Array
            && records.GetArrayLength() > 0
            && records[0].TryGetProperty("Sns", out _);
    }

    private static bool MatchCWLogsEvent(JsonDocument jdoc)
    {
        // CloudWatchLogsEvent has the JSON Path:  awslogs
        return jdoc.RootElement.TryGetProperty("awslogs", out _);
    }}
