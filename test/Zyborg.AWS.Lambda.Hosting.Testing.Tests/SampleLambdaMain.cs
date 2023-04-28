using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Zyborg.AWS.Lambda.Hosting.Testing.Tests;

public class SampleLambdaMain
{
    public async Task Main(string[] args)
    {
        var builder = CreateBuilder(args, new AmazonS3Client());
        var app = BuildApp(builder);

        await app.RunAsync();
    }

    public static FunctionAppBuilder CreateBuilder(string[] args, IAmazonS3 s3Client)
    {
        var builder = FunctionApp.CreateBuilder();

        builder.Logging.AddLambdaLogger();
        builder.Services.AddSingleton<IAmazonS3>(s3Client);

        return builder;
    }

    public static FunctionApp BuildApp(FunctionAppBuilder builder)
    {
        var app = builder.Build();

        app.AddBuiltInEventMatchers(typeof(S3Event));

        app.HandleDefaultEvent<S3Event, Function>();

        return app;
    }

    public class Function : IFunctionHandler<S3Event>
    {
        public Function(ILambdaContext context, IAmazonS3 s3Client)
        {
            Context = context;
            S3Client = s3Client;
        }

        public ILambdaContext Context { get; }
        public IAmazonS3 S3Client { get; }

        public async Task<object?> Handle(S3Event? ev)
        {
            if (ev != null)
            {
                await FunctionHandler(ev);
            }
            return null;
        }

        public async Task FunctionHandler(S3Event evnt)
        {
            var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
            foreach (var record in eventRecords)
            {
                var s3Event = record.S3;
                if (s3Event == null)
                {
                    continue;
                }

                try
                {
                    var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                    Context.Logger.LogInformation(response.Headers.ContentType);
                }
                catch (Exception e)
                {
                    Context.Logger.LogError($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}."
                        + " Make sure they exist and your bucket is in the same region as this function.");
                    Context.Logger.LogError(e.Message);
                    Context.Logger.LogError(e.StackTrace);
                    throw;
                }
            }
        }
    }

    public Task<object?> AltFunctionHandler(S3Event ev)
    {
        var inventory = new Dictionary<string, List<string>>();

        if (ev != null)
        {
            foreach (var r in ev.Records)
            {
                var bucket = r.S3.Bucket.Name;
                var objkey = r.S3.Object.Key;

                if (!inventory.TryGetValue(bucket, out var list))
                {
                    inventory[bucket] = list = new();
                }
                list.Add(objkey);
            }
        }

        return Task.FromResult((object?)JsonSerializer.Serialize(inventory));
    }
}
