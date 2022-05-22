using System.Text.Json;
using System.Text.Json.Serialization;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Test.Lib;

internal record TestInitCommonFields
{
	public static JsonSerializerOptions JsonSerializerOptions = new()
	{
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	public static User Admin = new()
	{
		Id = System.Guid.NewGuid(),
		Username = "AyyLmaoGaming",
		Email = "ayylmaogaming@alg.gg",
		Password = "P4ssword",
		Admin = true,
	};
}
