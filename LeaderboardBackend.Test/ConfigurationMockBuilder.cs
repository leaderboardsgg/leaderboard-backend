using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace LeaderboardBackend.Test
{
	internal static class ConfigurationMockBuilder
	{
		public static IConfiguration BuildConfigurationFromJson(string json)
		{
			return new ConfigurationBuilder()
				.AddJsonStream(
					new MemoryStream(Encoding.UTF8.GetBytes(json))
				)
				.Build();
		}
	}
}
