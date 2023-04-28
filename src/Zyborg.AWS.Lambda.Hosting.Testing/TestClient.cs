using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using System.Text.Json;

namespace Zyborg.AWS.Lambda.Hosting.Testing;

public class TestClient
{
    private readonly FunctionApp _app;

    public TestClient(FunctionApp app)
    {
        _app = app;
    }

    public async Task<TestResponse> Invoke(InvocationRequest request)
    {
        var response = await _app.RouteEventToHandler(new(request));

        return new TestResponse(response);
    }

    public async Task<TestResponse> Invoke<TEvent>(TEvent ev, ILambdaContext context)
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, ev);
        stream.Seek(0, SeekOrigin.Begin);

        var response = await _app.RouteEventToHandler(new(stream, context));

        return new TestResponse(response);
    }
}
