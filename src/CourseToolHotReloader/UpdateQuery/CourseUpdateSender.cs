using System.IO;
using CourseToolHotReloader.ApiClient;

namespace CourseToolHotReloader.UpdateQuery
{
	public interface ICourseUpdateSender
	{
		void SendCourseUpdates();
		void SendFullCourse();
	}

	public class CourseUpdateSender : ICourseUpdateSender
	{
		private readonly ICourseUpdateQuery courseUpdateQuery;
		private readonly IUlearnApiClient ulearnApiClient;
		private readonly IConfig config;

		public CourseUpdateSender(ICourseUpdateQuery courseUpdateQuery, IUlearnApiClient ulearnApiClient, IConfig config)
		{
			this.courseUpdateQuery = courseUpdateQuery;
			this.ulearnApiClient = ulearnApiClient;
			this.config = config;
		}

		public void SendCourseUpdates()
		{
			ulearnApiClient.SendCourseUpdates(courseUpdateQuery.GetAllCourseUpdate(), courseUpdateQuery.GetAllDeletedFiles());
		}

		public void SendFullCourse()
		{
			ulearnApiClient.SendFullCourse(config.Path, config.JwtToken.Token, config.CourseId);
		}
	}
}