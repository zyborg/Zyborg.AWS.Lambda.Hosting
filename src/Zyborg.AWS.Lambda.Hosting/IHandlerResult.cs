namespace Zyborg.AWS.Lambda.Hosting;

public partial class FunctionApp
{
    public interface IHandlerResult
    {
        Task ExecuteResultAsync(FunctionHandlerContext hctx);
    }
}
