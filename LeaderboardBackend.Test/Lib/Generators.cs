using System;
using System.Security.Cryptography;

namespace LeaderboardBackend.Test.Lib;

internal class Generators
{
	public static string GenerateRandomString()
	{
		using Aes crypto = Aes.Create();
		crypto.GenerateKey();
		return Convert.ToBase64String(crypto.Key);
	}
}
