
# NOTES - Lambda App Builder pattern

## References and Inspiration

* Heavily inspired by MAUI App Builder plumbing:
	* https://github.com/dotnet/maui/blob/main/src/Core/src/Hosting/MauiAppBuilder.cs
	* https://github.com/dotnet/maui/blob/main/src/Core/src/Hosting/MauiApp.cs
* Other Builder patterns:
	* https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting/src/HostApplicationBuilder.cs
	* https://github.com/dotnet/aspnetcore/blob/main/src/DefaultBuilder/src/WebApplicationBuilder.cs
	* https://github.com/dotnet/aspnetcore/blob/main/src/DefaultBuilder/src/WebApplication.cs
* Lambda RUntime Support:
	* https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/
	* https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.RuntimeSupport/README.md#using-amazonlambdaruntimesupport-as-a-class-library


## Testing Support

* [Unit testing in C#/.NET using xUnit](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
* [Test Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/test-min-api?view=aspnetcore-7.0)
* [Minimal APIs Test Samples](https://github.com/dotnet/AspNetCore.Docs.Samples/tree/main/fundamentals/minimal-apis/samples/MinApiTestsSample)
* [Amazon.Lambda.TestUtilities](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.TestUtilities/README.md)
* 
