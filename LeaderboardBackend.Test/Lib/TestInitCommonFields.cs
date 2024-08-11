using System.Text.Json;
using System.Text.Json.Serialization;
using LeaderboardBackend.Models.Entities;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace LeaderboardBackend.Test.Lib;

internal record TestInitCommonFields
{
    public static JsonSerializerOptions JsonSerializerOptions { get; private set; }

    static TestInitCommonFields()
    {
        JsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public static User Admin { get; } =
        new()
        {
            Id = System.Guid.NewGuid(),
            Username = "AyyLmaoGaming",
            Email = "ayylmaogaming@alg.gg",
            Password = "P4ssword",
            Role = UserRole.Administrator,
        };
}
