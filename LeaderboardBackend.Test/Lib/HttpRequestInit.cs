using System.Net.Http;

namespace LeaderboardBackend.Test.Lib;

internal record HttpRequestInit
{
	public object? Body { get; init; }
	public string? Jwt { get; init; }
	public HttpMethod Method { get; init; } = HttpMethod.Get;
}
