using Amazon.CloudWatchLogs;
using AWS.Logger.SeriLog;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.AwsCloudWatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerlessWithSerilog
{
    public static class LoggerConfig
    {
        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static void Create()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()

                // configure https://github.com/aws/aws-logging-dotnet
                .WriteTo.AWSSeriLog(configuration: Configuration)

                // configure https://github.com/Cimpress-MCP/serilog-sinks-awscloudwatch
                .WriteTo.AmazonCloudWatch(new CloudWatchSinkOptions
                {
                    // the name of the CloudWatch Log group for logging
                    LogGroupName = "Serilog.Cimpress",
                    TextFormatter = new CompactJsonFormatter(),
                    // other defaults defaults
                    MinimumLogEventLevel = LogEventLevel.Information,
                    BatchSizeLimit = 30,
                    QueueSizeLimit = 10000,
                    Period = TimeSpan.FromSeconds(1),
                    CreateLogGroup = true,
                    LogStreamNameProvider = new DefaultLogStreamProvider(),
                    RetryAttempts = 5
                }, new AmazonCloudWatchLogsClient())

                // write to console too
                .WriteTo.Console()

                // in order to prove periodic PeriodicBatchingSink is not working as expected on AWS
                .WriteTo.Sink(new CloudWatchLogSinkWithLogging(new AmazonCloudWatchLogsClient(), new CloudWatchSinkOptions
                {
                    // the name of the CloudWatch Log group for logging
                    LogGroupName = "Serilog.Cimpress.Custom",
                    TextFormatter = new CompactJsonFormatter(),
                    // other defaults defaults
                    MinimumLogEventLevel = LogEventLevel.Information,
                    BatchSizeLimit = 30,
                    QueueSizeLimit = 10000,
                    Period = TimeSpan.FromSeconds(1),
                    CreateLogGroup = true,
                    LogStreamNameProvider = new DefaultLogStreamProvider(),
                    RetryAttempts = 5
                }))

                .CreateLogger();

            SelfLog.Enable(Console.Error);
        }
    }

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
}