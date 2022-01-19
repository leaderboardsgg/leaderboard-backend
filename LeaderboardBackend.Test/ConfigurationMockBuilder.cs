using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
