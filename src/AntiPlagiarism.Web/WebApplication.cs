using AntiPlagiarism.Web.CodeAnalyzing;
using AntiPlagiarism.Web.CodeAnalyzing.CSharp;
using AntiPlagiarism.Web.Configuration;
using AntiPlagiarism.Web.Database;
using AntiPlagiarism.Web.Database.Repos;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using Ulearn.Common.Api;
using Vostok.Hosting;

namespace AntiPlagiarism.Web
{
	public class WebApplication : BaseApiWebApplication
	{
		protected override IApplicationBuilder ConfigureWebApplication(IApplicationBuilder app)
		{
			var database = app.ApplicationServices.GetService<AntiPlagiarismDb>();
			database.MigrateToLatestVersion();

			return app;
		}

		protected override void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment, ILogger logger)
		{
			base.ConfigureServices(services, hostingEnvironment, logger);

			services.AddDbContext<AntiPlagiarismDb>(
				options => options.UseSqlServer(hostingEnvironment.Configuration["database"])
			);

			services.Configure<AntiPlagiarismConfiguration>(options => hostingEnvironment.Configuration.GetSection("antiplagiarism").Bind(options));

			services.AddSwaggerExamplesFromAssemblyOf<WebApplication>();
		}

		public override void ConfigureDi(IServiceCollection services, ILogger logger)
		{
			base.ConfigureDi(services, logger);

			/* Database repositories */
			/* TODO (andgein): make auto-discovering of repositories */
			services.AddScoped<IClientsRepo, ClientsRepo>();
			services.AddScoped<ISubmissionsRepo, SubmissionsRepo>();
			services.AddScoped<ISnippetsRepo, SnippetsRepo>();
			services.AddScoped<ITasksRepo, TasksRepo>();

			/* Other services */
			services.AddScoped<PlagiarismDetector>();
			services.AddScoped<StatisticsParametersFinder>();
			services.AddSingleton<CodeUnitsExtractor>();
			services.AddScoped<SnippetsExtractor>();
			services.AddScoped<SubmissionSnippetsExtractor>();
		}
	}
}