using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using System.Text.Json;

namespace Zyborg.AWS.Lambda.Hosting;

public class FunctionHandlerContext
{
    private Stream? _ResponseStream;

    internal FunctionHandlerContext(FunctionInvocationRequest request)
    {
        Request = request;
    }

    internal FunctionInvocationRequest Request { get; }

    public ILambdaContext InvocationContext => Request.LambdaContext;

    public InvocationResponse? Response { get; private set; }

    public JsonDocument? InputJson { get; set; }

    public Type? EventType { get; set; }

    public object? EventValue { get; set; }

    public Func<IServiceProvider, object?, Task<object?>>? Handler { get; set; }

    public object? HandlerResult { get; set; }

    public Stream ResponseStream
    {
        get
        {
            if (_ResponseStream == null)
            {
                _ResponseStream = new MemoryStream();
                SetResponse(_ResponseStream, true);
            }
            return _ResponseStream;
        }
    }

    public void FinalizeResponseStream()
    {
        _ResponseStream?.Seek(0, SeekOrigin.Begin);
    }

    public void SetResponse(Stream stream, bool disposeOutputStream = true)
    {
        if (Response != null && Response.DisposeOutputStream)
        {
            Response.OutputStream.Dispose();
        }
        Response = new(stream, disposeOutputStream: disposeOutputStream);
    }
}
