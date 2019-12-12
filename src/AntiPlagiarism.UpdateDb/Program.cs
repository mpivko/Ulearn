using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.UpdateDb.Configuration;
using AntiPlagiarism.Web.CodeAnalyzing;
using AntiPlagiarism.Web.CodeAnalyzing.CSharp;
using AntiPlagiarism.Web.Configuration;
using AntiPlagiarism.Web.Database;
using AntiPlagiarism.Web.Database.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Ulearn.Common.Api;
using Ulearn.Core.Configuration;
using ILogger = Serilog.ILogger;

namespace AntiPlagiarism.UpdateDb
{
	public class Program
	{
		public static void Main(string[] args)
		{
			new Program().RunAsync(args).Wait();
		}

		public async Task RunAsync(string[] args)
		{
			Console.WriteLine(
				@"This tool will help you to update antiplagiarism database. " +
				@"I.e. in case when new logic for code units extraction have been added."
			);

			var firstSubmissionId = 0;
			if (args.Contains("--start"))
			{
				var startArgIndex = args.IndexOf("--start");
				if (startArgIndex + 1 >= args.Length || !int.TryParse(args[startArgIndex + 1], out firstSubmissionId))
					firstSubmissionId = 0;
			}

			var updateOnlyTokensCount = args.Contains("--update-tokens-count");

			var provider = GetServiceProvider();
			var updater = provider.GetService<AntiPlagiarismSnippetsUpdater>();
			await updater.UpdateAsync(firstSubmissionId, updateOnlyTokensCount).ConfigureAwait(false);
		}

		private ServiceProvider GetServiceProvider()
		{
			var configuration = ApplicationConfiguration.Read<AntiPlagiarismUpdateDbConfiguration>();

			var services = new ServiceCollection();

			services.AddOptions();

			services.Configure<AntiPlagiarismUpdateDbConfiguration>(ApplicationConfiguration.GetConfiguration());
			services.Configure<AntiPlagiarismConfiguration>(ApplicationConfiguration.GetConfiguration().GetSection("antiplagiarism"));

			var logger = GetLogger(configuration);
			services.AddSingleton(logger);
			services.AddScoped(_ => GetDatabase(configuration, logger));
			services.AddScoped<AntiPlagiarismSnippetsUpdater>();
			services.AddScoped<ISnippetsRepo, SnippetsRepo>();
			services.AddScoped<ISubmissionsRepo, SubmissionsRepo>();
			services.AddSingleton<CodeUnitsExtractor>();
			services.AddSingleton<SnippetsExtractor>();
			services.AddSingleton<SubmissionSnippetsExtractor>();

			return services.BuildServiceProvider();
		}

		private AntiPlagiarismDb GetDatabase(AntiPlagiarismUpdateDbConfiguration configuration, ILogger logger)
		{
			var optionsBuilder = new DbContextOptionsBuilder<AntiPlagiarismDb>();
			optionsBuilder.UseSqlServer(configuration.Database);
			if (configuration.HostLog.EnableEntityFrameworkLogging)
				optionsBuilder.UseLoggerFactory(new SerilogLoggerFactory(logger));

			return new AntiPlagiarismDb(optionsBuilder.Options);
		}

		private ILogger GetLogger(AntiPlagiarismUpdateDbConfiguration configuration)
		{
			var loggerConfiguration = new LoggerConfiguration()
				.Enrich.With<ThreadEnricher>()
				.Enrich.With<FlowContextEnricher>()
				.MinimumLevel.Debug();

			if (configuration.HostLog.Console)
				loggerConfiguration = loggerConfiguration.WriteTo.Console(
					outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{Thread}] {Message:l}{NewLine}{Exception}",
					restrictedToMinimumLevel: LogEventLevel.Information
				);

			var pathFormat = configuration.HostLog.PathFormat;
			if (!Enum.TryParse<LogEventLevel>(configuration.HostLog.MinimumLevel, true, out var minimumLevel))
				minimumLevel = LogEventLevel.Debug;
			if (Path.IsPathRooted(pathFormat))
			{
				var directory = Path.GetDirectoryName(pathFormat);
				var fileName = Path.GetFileName(pathFormat);
				pathFormat = Path.Combine(directory, configuration.GraphiteServiceName, fileName);
			}

			loggerConfiguration = loggerConfiguration
				.WriteTo.RollingFile(
					pathFormat,
					outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{Thread}] {Message:l}{NewLine}{Exception}",
					restrictedToMinimumLevel: minimumLevel,
					fileSizeLimitBytes: 4 * 1073741824L
				);

			return loggerConfiguration.CreateLogger();
		}
	}

	internal class SerilogLoggerFactory : ILoggerFactory, IDisposable
	{
		private readonly SerilogLoggerProvider provider;

		public SerilogLoggerFactory(ILogger logger = null, bool dispose = false)
		{
			provider = new SerilogLoggerProvider(logger, dispose);
		}

		public void Dispose()
		{
			provider.Dispose();
		}

		public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
		{
			return provider.CreateLogger(categoryName);
		}

		public void AddProvider(ILoggerProvider provider)
		{
			SelfLog.WriteLine("Ignoring added logger provider {0}", provider);
		}
	}
}