using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace LeaderboardBackend.Test.Services;

public class AuthServiceTests
{
	[Test]
	public void GenerateJSONWebToken_CreatesTokenWithConfig()
	{
		string key = "testkeythatsatisfiesthecharacterminimum";
		string issuer = "leaderboards.gg";
		string configJson = string.Format(@"
		{{
			""Jwt"": {{
				""Key"": ""{0}"",
				""Issuer"": ""{1}""
			}}
		}}
		", key, issuer);

		IConfiguration config = ConfigurationMockBuilder.BuildConfigurationFromJson(configJson);
		AuthService authService = new(config);

		User user = new()
		{
			Id = Guid.NewGuid(),
			Username = "RageCage",
			Email = "x@y.com"
		};

		string token = authService.GenerateJSONWebToken(user);
		JwtSecurityTokenHandler jwtHandler = new();
		SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(key));

		Assert.DoesNotThrow(() =>
		{
			jwtHandler.ValidateToken(
				token,
				new()
				{
					IssuerSigningKey = signingKey,
					ValidAudience = issuer,
					ValidIssuer = issuer,
				},
				out _
			);
		});
	}
}
