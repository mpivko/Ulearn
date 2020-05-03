using CourseToolHotReloader.Dtos;

namespace CourseToolHotReloader
{
	public interface IConfig
	{
		string Path { get; set; }
		string CourseId { get; set; }
		AccountTokenResponseDto JwtToken { get; set; }
	}

	internal class Config : IConfig
	{
		public string Path { get; set; }
		public string CourseId { get; set; }
		public AccountTokenResponseDto JwtToken { get; set; }
	}
}