using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Zyborg.AWS.Lambda.Hosting;

public partial class FunctionApp
{
    private Func<IServiceProvider, object?, Task<object?>>? _defaultHandler;
    private readonly Dictionary<Type, Func<IServiceProvider, object?, Task<object?>>> _eventHandlers = new();

    public void HandleEvent<TEvent>(Func<IServiceProvider, TEvent?, Task<object?>> handler)
    {
        _eventHandlers.Add(typeof(TEvent), (sp, ev) => handler(sp, (TEvent?)ev));
    }

    public void HandleEvent<TEvent, THandler>() where THandler : IFunctionHandler<TEvent>
    {
        _eventHandlers.Add(typeof(TEvent), (sp, ev) =>
        {
            var handler = ActivatorUtilities.GetServiceOrCreateInstance<THandler>(sp);
            return handler.Handle((TEvent?)ev);
        });
    }

    /// <summary>
    /// Registers a default event handler that will take the input event as a generic
    /// JSON document.
    /// Only one default handler may be registered per FunctionApp
    /// (via the <c>HandleDefault*</c> family of methods).
    /// </summary>
    public void HandleDefault<THandler>() where THandler : IFunctionHandler<JsonDocument>
    {
        HandleDefault((sp, arg) =>
        {
            var jdoc = (JsonDocument?)arg;
            var handler = ActivatorUtilities.GetServiceOrCreateInstance<THandler>(sp);
            return handler.Handle(jdoc);
        });
    }

    /// <summary>
    /// Registers a default event handler that will take the input event as a generic
    /// JSON document.
    /// Only one default handler may be registered per FunctionApp
    /// (via the <c>HandleDefault*</c> family of methods).
    /// </summary>
    public void HandleDefault(Func<IServiceProvider, JsonDocument?, Task<object?>> handler)
    {
        if (_defaultHandler != null)
        {
            throw new InvalidOperationException("a default handler has already been registered");
        }
        _defaultHandler = (sp, arg) =>
        {
            var jdoc = (JsonDocument?)arg;
            return handler(sp, jdoc);
        };
    }

    /// <summary>
    /// Registers a default event handler for a specific, strongly-typed event.
    /// The input event data will be deserialized to the target event type.
    /// Only one default handler may be registered per FunctionApp
    /// (via the <c>HandleDefault*</c> family of methods).
    /// </summary>
    public void HandleDefaultEvent<TEvent, THandler>()
        where TEvent : class
        where THandler : IFunctionHandler<TEvent?>
    {
        HandleDefaultEvent<TEvent>((sp, ev) =>
        {
            var handler = ActivatorUtilities.GetServiceOrCreateInstance<THandler>(sp);
            return handler.Handle((TEvent?)ev);
        });
    }

    /// <summary>
    /// Registers a default event handler for a specific, strongly-typed event.
    /// The input event data will be deserialized to the target event type.
    /// Only one default handler may be registered per FunctionApp
    /// (via the <c>HandleDefault*</c> family of methods).
    /// </summary>
    public void HandleDefaultEvent<TEvent>(Func<IServiceProvider, TEvent?, Task<object?>> handler)
        where TEvent : class
    {
        if (_defaultHandler != null)
        {
            throw new InvalidOperationException("a default handler has already been registered");
        }
        _defaultHandler = (sp, arg) =>
        {
            var jdoc = (JsonDocument?)arg;
            var ev = jdoc?.Deserialize<TEvent>();
            return handler(sp, ev);
        };
    }
}
