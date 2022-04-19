using LeaderboardBackend.Models.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Test.Lib;

internal record TestInitCommonFields {
	public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
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
