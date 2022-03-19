using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Helpers;

internal class Generators
{
	public static string RandomString()
	{
		using Aes crypto = Aes.Create();
		crypto.GenerateKey();
		return Convert.ToBase64String(crypto.Key);
	}

	public static string RandomEmailAddress()
	{
		return $"{RandomString()}@{RandomString()}.com";
	}
}
