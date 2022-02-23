using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
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
		var key = "testkeythatsatisfiesthecharacterminimum";
		var issuer = "leaderboards.gg";

		var configJson = string.Format(@"
		{{
			""Jwt"": {{
				""Key"": ""{0}"",
				""Issuer"": ""{1}""
			}}
		}}
		", key, issuer);

		var config = ConfigurationMockBuilder.BuildConfigurationFromJson(configJson);
		var authService = new AuthService(config);

		var user = new User
		{
			Id = Guid.NewGuid(),
			Username = "RageCage",
			Email = "x@y.com"
		};

		var token = authService.GenerateJSONWebToken(user);
		var jwtHandler = new JwtSecurityTokenHandler();
		var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

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
