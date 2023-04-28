using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;

namespace Zyborg.AWS.Lambda.Hosting;

internal class FunctionInvocationRequest : IDisposable
{
    public FunctionInvocationRequest(InvocationRequest inner)
        : this(inner.InputStream, inner.LambdaContext)
    {
        InnerRequest = inner;
    }

    internal FunctionInvocationRequest(Stream stream, ILambdaContext context)
    {
        InputStream = stream;
        LambdaContext = context;
    }

    internal InvocationRequest? InnerRequest { get; }

    public Stream InputStream { get; }

    public ILambdaContext LambdaContext { get; }

    public void Dispose()
    {
        InnerRequest?.Dispose();
    }
}
