namespace Zyborg.AWS.Lambda.Hosting;

public interface IFunctionHandler<TEvent>
{
    Task<object?> Handle(TEvent? ev);
}
