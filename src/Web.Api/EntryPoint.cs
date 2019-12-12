using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Ulearn.Common.Api;
using Ulearn.Core.Configuration;
using Vostok.Hosting;
using Vostok.Logging;
using Vostok.Logging.Serilog;

namespace Ulearn.Web.Api
{
	public static class EntryPoint
	{
		public static void Main(string[] args)
		{
			BuildVostokHost(args).Run();
		}

		private static IVostokHost BuildVostokHost(params string[] args)
		{
			return new VostokHostBuilder<WebApplication>()
				.SetServiceInfo("ulearn", "Web.Api")
				.ConfigureAppConfiguration(configurationBuilder =>
				{
					configurationBuilder.AddCommandLine(args);
					configurationBuilder.AddEnvironmentVariables();
					ApplicationConfiguration.BuildAppsettingsConfiguration(configurationBuilder);
				})
				.ConfigureHost((context, hostConfigurator) =>
				{
					var loggerConfiguration = new LoggerConfiguration().MinimumLevel.Information();
					if (context.Configuration.GetSection("hostLog").GetValue<bool>("console"))
					{
						loggerConfiguration = loggerConfiguration
							.WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{Thread}] {Message:l}{NewLine}{Exception}", restrictedToMinimumLevel: LogEventLevel.Information);
					}

					var pathFormat = context.Configuration.GetSection("hostLog")["pathFormat"];
					if (!string.IsNullOrEmpty(pathFormat))
					{
						var minimumLevelString = context.Configuration.GetSection("hostLog").GetValue("minimumLevel", "debug");
						var dbMinimumLevelString = context.Configuration.GetSection("hostLog").GetValue("dbMinimumLevel", "");
						if (!Enum.TryParse(minimumLevelString, true, out LogEventLevel minimumLevel) || !Enum.IsDefined(typeof(LogEventLevel), minimumLevel))
							minimumLevel = LogEventLevel.Debug;
						if (!Enum.TryParse(dbMinimumLevelString, true, out LogEventLevel dbMinimumLevel) || !Enum.IsDefined(typeof(LogEventLevel), dbMinimumLevel))
							dbMinimumLevel = minimumLevel;
						if (Path.IsPathRooted(pathFormat))
						{
							var directory = Path.GetDirectoryName(pathFormat);
							var fileName = Path.GetFileName(pathFormat);
							pathFormat = Path.Combine(directory, context.Configuration["graphiteServiceName"], fileName);
						}

						loggerConfiguration = loggerConfiguration
							.WriteTo.RollingFile(
								pathFormat,
								outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{Thread}] {Message:l}{NewLine}{Exception}",
								restrictedToMinimumLevel: minimumLevel,
								fileSizeLimitBytes: 4 * 1073741824L
							);

						if (dbMinimumLevel != minimumLevel)
						{
							Func<Serilog.Events.LogEvent, bool> isDbSource = le => ((le.Properties.GetValueOrDefault("SourceContext") as ScalarValue)?.Value as string)?.StartsWith("\"Microsoft.EntityFrameworkCore.Database.Command") ?? false;
							loggerConfiguration = loggerConfiguration.Filter.ByIncludingOnly(le =>
								le.Level >= dbMinimumLevel || !isDbSource(le));
						}
					}

					var hostLog = new SerilogLogAdapter(new SerilogLog(loggerConfiguration.CreateLogger()));
					hostConfigurator.SetHostLog(hostLog);
				})
				.ConfigureAirlock((context, configurator) => { configurator.SetLog(context.HostingEnvironment.Log.FilterByLevel(LogLevel.Warn)); })
				.Build();
		}
	}
}