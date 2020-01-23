# Replicates which might be an issue with Serilog CloudWatch sinks on an AWS Lambda

This project replicates a Serilog issue that happens while running on AWS Lambda

Not all logging messages everything are sent to CloudWatch, there are always some remaining messages in a pending queue.

Once you publish this to AWS Lambda do several requests on it, usualy more than dozens

```
async void Main()
{
    var client = new HttpClient();
    
    foreach (var value in Enumerable.Range(1, 100))
    {
        await client.GetAsync($"https://{your-lambda}.execute-api.eu-west-1.amazonaws.com/Prod/api/values/{value}");
    }
    
    "ready".Dump();
}
```

Once the lambda is called 100 times we will see that not all messages are logged in none of the LogGroups we've set. The messages are logged in order, as expected, but there are always some of them that remain in pending.
Once a new Lambda request is made, then those remaining messages are processed, otherwise they are never sent to CloudWatch.

This issues happens with both most used sinks

| Sink   |      Configurd LogGroup Name      |
|----------|:-------------:|------:|
| [AWS Logging .NET](https://github.com/aws/aws-logging-dotnet) |  Serilog.AWS |
| [Serilog Sink for AWS CloudWatch](https://github.com/Cimpress-MCP/serilog-sinks-awscloudwatch) |  Serilog.Cimpress |
| [Serilog Sink for AWS CloudWatch - Custom Sink](https://github.com/Cimpress-MCP/serilog-sinks-awscloudwatch) |  Serilog.Cimpress.Custom |

It looks like what goes in background processing is not executed.

The following code proves that PeriodicBatching is not working on AWS Lambda, the SelfLog does not log at every second so this might explain why some messages remain in a pending queue

```
 internal class CloudWatchLogSinkWithLogging : CloudWatchLogSink
    {
        public CloudWatchLogSinkWithLogging(IAmazonCloudWatchLogs cloudWatchClient, ICloudWatchSinkOptions options) : base(cloudWatchClient, options)
        {
        }

        protected override async Task OnEmptyBatchAsync()
        {
            // this should be called at every second, but it doesn't happen on AWS Lambda
            // on the local machine it happens as expected

            // this proves the `PeriodicBatchingSink` does not work as expected on AWS Lambda

            SelfLog.WriteLine("OnEmptyBatchAsync");
            await base.OnEmptyBatchAsync();
        }

        protected override Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            SelfLog.WriteLine("EmitBatchAsync - events to emit " + events?.Count());
            return base.EmitBatchAsync(events);
        }
    }
```


## Here are some steps to follow from Visual Studio:

To deploy your Serverless application, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed application open the Stack View window by double-clicking the stack name shown beneath the AWS CloudFormation node in the AWS Explorer tree. The Stack View also displays the root URL to your published application.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "ServerlessWithSerilog/test/ServerlessWithSerilog.Tests"
    dotnet test
```

Deploy application
```
    cd "ServerlessWithSerilog/src/ServerlessWithSerilog"
    dotnet lambda deploy-serverless
```
