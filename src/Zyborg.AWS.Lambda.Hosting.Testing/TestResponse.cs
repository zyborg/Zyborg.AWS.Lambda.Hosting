using Amazon.Lambda.RuntimeSupport;
using System.Text.Json;

namespace Zyborg.AWS.Lambda.Hosting.Testing;

public class TestResponse : IDisposable
{
    private readonly InvocationResponse _Response;

    public TestResponse(InvocationResponse response)
    {
        _Response = response;
    }

    public InvocationResponse Response => _Response;

    public virtual void Dispose()
    {
        if (_Response.DisposeOutputStream)
        {
            _Response.OutputStream.Dispose();
        }
    }
}

public class TestResponse<TResult> : TestResponse
{
    public TestResponse(InvocationResponse response) : base(response)
    {
        Result = JsonSerializer.Deserialize<TResult>(response.OutputStream);
        if (response.DisposeOutputStream)
        {
            response.OutputStream.Dispose();
        }
    }

    public TResult? Result { get; }

    public override void Dispose()
    {
        // We override to circumvent the optional
        // the Response OutputStream Dispose
    }
}
