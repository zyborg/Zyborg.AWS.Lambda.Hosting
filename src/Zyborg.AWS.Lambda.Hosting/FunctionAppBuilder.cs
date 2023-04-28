using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zyborg.AWS.Lambda.Hosting;

public class FunctionAppBuilder
{
    private readonly ServiceCollection _Services = new();
    private Func<IServiceProvider> _createServiceProvider;
    private readonly Lazy<ConfigurationManager> _Configuration;
    private ILoggingBuilder? _logging;

    internal FunctionAppBuilder()
    {
        _Configuration = new Lazy<ConfigurationManager>(() => new());
        _Services.AddSingleton<IConfiguration>(sp => _Configuration.Value);

        _Services.AddScoped<FunctionApp.ScopedState>();
        _Services.AddScoped<ILambdaContext>(sp =>
        {
            var state = sp.GetRequiredService<FunctionApp.ScopedState>();
            if (state == null)
            {
                throw new InvalidOperationException("scoped invocation state could not be resolved");
            }
            if (state._request == null)
            {
                throw new InvalidOperationException("scoped invocation state has not been populated with invocation request");
            }
            if (state._request.LambdaContext == null)
            {
                throw new InvalidOperationException("scoped invocation state request is missing the Lambda Context");
            }
            return state._request.LambdaContext!;
        });

        // Default if not overridden later
        _createServiceProvider = () => _Services.BuildServiceProvider();
    }

    public IServiceCollection Services => _Services;

    public ConfigurationManager Configuration => _Configuration.Value;

    public ILoggingBuilder Logging
    {
        get
        {
            return _logging ??= InitLogging();

            ILoggingBuilder InitLogging()
            {
                Services.AddLogging();
                return new LoggingBuilder(Services);
            }
        }
    }

    public void ConfigureContainer<TBuilder>(IServiceProviderFactory<TBuilder> factory,
        Action<TBuilder>? configure = null)
        where TBuilder : notnull
    {
        ArgumentNullException.ThrowIfNull(factory);

        _createServiceProvider = () =>
        {
            var container = factory.CreateBuilder(_Services);
            configure?.Invoke(container);
            return factory.CreateServiceProvider(container);
        };
    }

    public FunctionApp Build()
    {
        ConfigureDefaultLogging();

        var services = _createServiceProvider();
        _Services.MakeReadOnly();

        return new(services);
    }

    private void ConfigureDefaultLogging() => ConfigureNullLogging();

    private void ConfigureNullLogging()
    {
		// By default, if no one else has configured logging, add a "no-op" LoggerFactory
		// and Logger services with no providers. This way when components try to get an
		// ILogger<> from the IServiceProvider, they don't get 'null'.
		Services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>());
		Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
	}

    private sealed class LoggingBuilder : ILoggingBuilder
    {
        public LoggingBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
