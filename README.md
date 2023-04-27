
# README - Zyborg.AWS.Lambda.Hosting

This library implements an application hosting model for AWS Lambda functions
that conform to the _App Builder_ pattern.  The App Builder pattern has become
a common and familiar approach in the modern .NET community for structuring
various application types such as Web Apps (ASP.NET Core), Workers and MAUI apps.

## Examples

Here are some basic examples to give you a taste...

### Example 1 - Lambda Simple S3 Function

Here one of the most basic Lambda functions based on the
"Lambda Simple S3 Function" (`lambda.s3`) template.

```csharp
using Amazon.S3;
using Amazon.Lambda.S3Events;
using Zyborg.AWS.Lambda.Hosting;

// For simplicity, this example is shown using the
// "top-level statements" feature of newer C# dialects.

IAmazonS3 s3client = new AmazonS3Client();

var builder = FunctionApp.CreateBuilder();
var app = builder.Build();

// In this example we define the Lambda handler logic inline
app.HandleDefaultEvent<S3Event>(async (services, evnt) =>
{
    var logger = services.GetRequiredService<ILogger>();
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
            var response = await s3client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
            logger.LogInformation(response.Headers.ContentType);
            return null;
        }
        catch (Exception e)
        {
            logger.LogError($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
            logger.LogError(e.Message);
            logger.LogError(e.StackTrace);
            throw;
        }
    }
});

await app.RunAsync();
```

> TODO:  explain the above example and contrast with Template

In this case, there is a bit more ceremony and exposure of implementation details
when compared to the template, however we'll soon see that these make things easier
when you start filling out the Function for real world applications and scenarios.

> TODO:  explain the above example and contrast with Template

### Example 2 - Lambda S3 With Additional Features

In this example, we've taken the first example above and added a few
more features that the App Builder makes easier to structure and manage.

```csharp
using Amazon.S3;
using Amazon.Lambda.S3Events;
using Zyborg.AWS.Lambda.Hosting;

// Converted top-level statements to a class so that we can better support
// optionally injecting resource dependencies like S3 client for testing.

public class LambdaMain
{
    public static IAmazonS3 S3Client { get; set; }

    public static async Task Main(string[] args)
    {
        var builder = FunctionApp.CreateBuilder();
        // TODO: check if this can be made singleton???
        builder.Services.AddScoped<IAmazonS3>(_ => S3Client ?? New AmazonS3Client());

        var app = builder.Build();

        // We've moved the inline logic to its own dedicated handler class
        app.HandleDefaultEvent<S3Event, MyS3EventHandler>();
    }
}

// A separate class helps structure and organize the code better.

public class MyS3EventHandler : IFunctionHandler<S3Event>
{
    private readonly ILogger _logger;
    private readonly IAmazonS3 _s3Client;

    // The handler class can define its dependencies explicitly
    public MyS3EventHandler(ILogger<MyS3EventHandler logger> logger, IAmazonS3 s3Client)
    {
        _logger = logger;
        _s3Client = s3Client;
    }

    public Task<object?> Handle(S3Event? evnt)
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
                var response = await _s3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                _logger.LogInformation(response.Headers.ContentType);
                return null;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
}
```
