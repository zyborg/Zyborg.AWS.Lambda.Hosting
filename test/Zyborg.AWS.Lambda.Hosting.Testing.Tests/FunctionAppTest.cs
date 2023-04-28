using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;

namespace Zyborg.AWS.Lambda.Hosting.Testing.Tests;

public class FunctionAppTest
{
    [Fact]
    public async Task TestS3EventLambdaFunction()
    {
        var mockS3Client = new Mock<IAmazonS3>();
        var getObjectMetadataResponse = new GetObjectMetadataResponse();
        getObjectMetadataResponse.Headers.ContentType = "text/plain";

        mockS3Client
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(getObjectMetadataResponse));

        // Setup the S3 event object that S3 notifications would create with the fields used by the Lambda function.
        var s3Event = new Amazon.Lambda.S3Events.S3Event
        {
            Records = new List<S3Event.S3EventNotificationRecord>
            {
                new S3Event.S3EventNotificationRecord
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity {Name = "s3-bucket" },
                        Object = new S3Event.S3ObjectEntity {Key = "text.txt" }
                    }
                }
            }
        };

        // Invoke the lambda function and confirm the content type was returned.
        ILambdaLogger testLambdaLogger = new TestLambdaLogger();
        var testLambdaContext = new TestLambdaContext
        {
            Logger = testLambdaLogger
        };

        // This is the ORIGINAL code from the Lambda Template for S3 Function Example

        //var function = new Function(mockS3Client.Object);
        //await function.FunctionHandler(s3Event, testLambdaContext);

        // And this is our version that uses the App Builder pattern

        var builder = SampleLambdaMain.CreateBuilder(new string[0], mockS3Client.Object);
        var app = SampleLambdaMain.BuildApp(builder);
        var testClient = new TestClient(app);
        await testClient.Invoke(s3Event, testLambdaContext);


        Assert.Equal("text/plain", ((TestLambdaLogger)testLambdaLogger).Buffer.ToString().Trim());
    }
}
