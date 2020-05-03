using CommandLine;

namespace CourseToolHotReloader.DirectoryWorkers
{
	internal class Options
	{
		[Option("sendfullarchive", Required = false, HelpText = "send full archive")]
		public bool SendFullArchive { get; set; }

		[Option("login", Required = true, HelpText = "login")]
		public string Login { get; set; }

		[Option("password", Required = true, HelpText = "password")]
		public string Password { get; set; }

		[Option("courseId", Required = true, HelpText = "CourseId")]
		public string CourseId { get; set; }
	}
}