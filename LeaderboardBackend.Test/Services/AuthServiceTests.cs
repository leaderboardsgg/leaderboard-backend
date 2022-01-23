using NUnit.Framework;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

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
		IConfiguration config = ConfigurationMockBuilder.BuildConfigurationFromJson(
			configJson
		);
		var authService = new AuthService(config);

		User user = new User
		{
			Id = Guid.NewGuid(),
			Username = "RageCage",
			Email = "x@y.com"
		};

		string token = authService.GenerateJSONWebToken(user);

		var jwtHandler = new JwtSecurityTokenHandler();
		var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		Assert.DoesNotThrow(() =>
		{
			jwtHandler.ValidateToken(
				token,
				new TokenValidationParameters
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
