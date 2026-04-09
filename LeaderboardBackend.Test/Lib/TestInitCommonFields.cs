using System.Text.Json;
using System.Text.Json.Serialization;
using LeaderboardBackend.Converters;
using LeaderboardBackend.Models.Entities;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using BcryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.Lib;

internal record TestInitCommonFields
{
    public static JsonSerializerOptions JsonSerializerOptions { get; private set; }

    static TestInitCommonFields()
    {
        JsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, false));
        JsonSerializerOptions.Converters.Add(new GuidJsonConverter());
        JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        SystemTextJsonSerializerConfig.Options.Converters.Add(new GuidJsonConverter());
        SystemTextJsonSerializerConfig.Options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public static User Admin { get; } =
        new()
        {
            Username = "AyyLmaoGaming",
            Email = "ayylmaogaming@alg.gg",
            Password =  BcryptNet.EnhancedHashPassword("P4ssword"),
            Role = UserRole.Administrator,
        };
}
