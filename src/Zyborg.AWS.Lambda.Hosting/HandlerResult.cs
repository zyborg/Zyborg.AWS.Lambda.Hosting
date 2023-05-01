using System.Text;
using System.Text.Json;

namespace Zyborg.AWS.Lambda.Hosting;
public partial class FunctionApp
{
    /// <summary>
    /// Base class implementation for <see cref="IHandlerResult"/> including
    /// a convert for some of the most common response types.
    /// </summary>
    public abstract class HandlerResult : IHandlerResult
    {
        public abstract Task ExecuteResultAsync(FunctionHandlerContext hctx);

        public static HandlerResult ToHandler(object? value)
        {
            return value switch
            {
                null => EmptyHandlerResult.Instance,
                string s => s,
                Stream s => s,
                ValueTuple<Stream, bool> s => s,
                _ => new JsonHandlerResult { Value = value },
            };
        }

        public static implicit operator HandlerResult(Stream s) =>
            new StreamHandlerResult { Stream = s };
        public static implicit operator HandlerResult((Stream, bool) s) =>
            new StreamHandlerResult { Stream = s.Item1, DisposeOutputStream = s.Item2 };
        public static implicit operator HandlerResult(string s) =>
            new StringHandlerResult { String = s };
    }

    public class EmptyHandlerResult : HandlerResult
    {
        public static readonly EmptyHandlerResult Instance = new EmptyHandlerResult();

        private EmptyHandlerResult() { }

        public override Task ExecuteResultAsync(FunctionHandlerContext hctx)
        {
            hctx.SetResponse(EmptyStream, false);
            return Task.CompletedTask;
        }
    }

    public class StringHandlerResult : HandlerResult
    {
        public string String { get; init; } = default!;

        public override Task ExecuteResultAsync(FunctionHandlerContext hctx)
        {
            hctx.SetResponse(new MemoryStream(Encoding.UTF8.GetBytes(String)), true);
            return Task.CompletedTask;
        }
    }

    public class StreamHandlerResult : HandlerResult
    {
        public Stream Stream { get; init; } = default!;

        public bool DisposeOutputStream { get; init; } = true;

        public override Task ExecuteResultAsync(FunctionHandlerContext hctx)
        {
            hctx.SetResponse(Stream, DisposeOutputStream);
            return Task.CompletedTask;
        }
    }

    public class JsonHandlerResult : HandlerResult
    {
        public object Value { get; init; } = default!;

        public override async Task ExecuteResultAsync(FunctionHandlerContext hctx)
        {
            var resultType = Value.GetType();
            await JsonSerializer.SerializeAsync(hctx.ResponseStream, Value, resultType,
                hctx.ResultJsonSerializerOptions);
        }
    }
}
