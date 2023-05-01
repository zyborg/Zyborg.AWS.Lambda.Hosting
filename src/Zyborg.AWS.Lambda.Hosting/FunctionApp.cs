using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Expose some of our internals to the test-supporting library
[assembly: InternalsVisibleTo("Zyborg.AWS.Lambda.Hosting.Testing")]
[assembly: InternalsVisibleTo("Zyborg.AWS.Lambda.Hosting.Tests")]

namespace Zyborg.AWS.Lambda.Hosting;

public partial class FunctionApp : IDisposable, IAsyncDisposable
{
    private static readonly Stream EmptyStream = new MemoryStream(new byte[0]);

    private readonly IServiceProvider _Services;

    // TODO: in the future may expose this for configuration, perhaps in the FunctionAppBuilder
    private readonly JsonSerializerOptions _resultEncodingJsonSerOptions = DefaultJsonSerializerOptions;

    internal FunctionApp(IServiceProvider services)
    {
        _Services = services;
    }

    public IServiceProvider Services => _Services;

    public IConfiguration Configuration => _Services.GetRequiredService<IConfiguration>();

    public InvocationResponse EmptyResponse => new(EmptyStream, false);

    // Based on the defaults used in the official AWS JSON Ser Opts:
    //    https://github.com/aws/aws-lambda-dotnet/blob/a7b60810bed4e1eb44678630050f1d2f202986ed/Libraries/src/Amazon.Lambda.Serialization.SystemTextJson/AbstractLambdaJsonSerializer.cs#L131
    //    https://github.com/aws/aws-lambda-dotnet/blob/a7b60810bed4e1eb44678630050f1d2f202986ed/Libraries/src/Amazon.Lambda.Serialization.SystemTextJson/DefaultLambdaJsonSerializer.cs#L20
    public static JsonSerializerOptions DefaultJsonSerializerOptions => new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = new Amazon.Lambda.Serialization.SystemTextJson.AwsNamingPolicy(),
        Converters =
        {
            new Amazon.Lambda.Serialization.SystemTextJson.Converters.DateTimeConverter(),
            new Amazon.Lambda.Serialization.SystemTextJson.Converters.MemoryStreamConverter(),
            new Amazon.Lambda.Serialization.SystemTextJson.Converters.ConstantClassConverter(),
            new Amazon.Lambda.Serialization.SystemTextJson.Converters.ByteArrayConverter(),
        },
    };

    public static FunctionAppBuilder CreateBuilder()
    {
        var builder = new FunctionAppBuilder();

        return builder;
    }

    public void Dispose()
    {
        (Configuration as IDisposable)?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            (_Services as IDisposable)?.Dispose();
        }
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var bootstrap = new LambdaBootstrap(HandleLambdaBootstrapEvent);
        return bootstrap.RunAsync(cancellationToken);
    }

    private Task<InvocationResponse> HandleLambdaBootstrapEvent(InvocationRequest request)
    {
        var fir = new FunctionInvocationRequest(request);
        return RouteEventToHandler(fir);
    }

    internal async Task<InvocationResponse> RouteEventToHandler(FunctionInvocationRequest request)
    {
        var hctx = new FunctionHandlerContext(request)
        {
            ResultJsonSerializerOptions = _resultEncodingJsonSerOptions,
        };

        await ResolveEventAndHandler(hctx);
        await ExecuteHandler(hctx);
        await EncodeResult(hctx);

        hctx.FinalizeResponseStream();

        return hctx.Response ?? EmptyResponse;
    }

    // TODO: in the future we may expose these as extension points
    //       for customization, if there is value and a real need

    internal Task ResolveEventAndHandler(FunctionHandlerContext hctx)
    {
        hctx.InputJson = JsonDocument.Parse(hctx.Request.InputStream);

        if (DecodeEvent(hctx.InputJson, out var eventType, out var eventValue))
        {
            hctx.EventType = eventType;
            hctx.EventValue = eventValue;

            if (!_eventHandlers.TryGetValue(eventType, out var handler))
            {
                throw new Exception(
                    $"no handler registered for decoded event of type [{eventType.FullName}]");
            }

            hctx.Handler = handler;
        }
        else if (_defaultHandler != null)
        {
            hctx.Handler = _defaultHandler;
            hctx.EventValue = hctx.InputJson;
        }
        else
        {
            throw new Exception("no default handler registered for unresolved event types");
        }

        return Task.CompletedTask;
    }

    internal async Task ExecuteHandler(FunctionHandlerContext hctx)
    {
        using (var scope = _Services.CreateScope())
        {
            // Get the scoped state and populate it
            var sp = scope.ServiceProvider;
            var state = sp.GetRequiredService<ScopedState>();
            state._request = hctx.Request;

            // Invoke the resolved handler in the context of the current scope
            if (hctx.Handler != null)
            {
                hctx.HandlerResult = await hctx.Handler(sp, hctx.EventValue);
            }
        }
    }

    internal async Task EncodeResult(FunctionHandlerContext hctx)
    {
        var result = hctx.HandlerResult;

        if (result is null or not IHandlerResult)
        {
            result = HandlerResult.ToHandler(result);
        }

        await ((IHandlerResult)result).ExecuteResultAsync(hctx);
    }

    internal async Task EncodeResult_OLD(FunctionHandlerContext hctx)
    {
        var result = hctx.HandlerResult;

        if (result is null)
        {
            hctx.SetResponse(EmptyStream, false);
        }
        else if (result is IHandlerResult hr)
        {
            await hr.ExecuteResultAsync(hctx);
        }
        else if (result is (Stream stream1, bool dispose))
        {
            hctx.SetResponse(stream1, dispose);
        }
        else if (result is Stream stream2)
        {
            hctx.SetResponse(stream2, true);
        }
        else if (result is string s) // TODO: expand this to Spans, char[], Memory<char>, ect...
        {
            hctx.SetResponse(new MemoryStream(Encoding.UTF8.GetBytes(s)), true);
        }
        else
        {
            var resultType = result.GetType();
            await JsonSerializer.SerializeAsync(hctx.ResponseStream, result, resultType,
                _resultEncodingJsonSerOptions);
        }
    }

    internal class ScopedState
    {
        public FunctionInvocationRequest? _request;
    }
}
