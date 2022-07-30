using LeaderboardBackend.Jobs.Core;
using LeaderboardBackend.Models.Entities;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Jobs.Jobs;

internal class ScaffoldLeaderboardAndMod : IJob
{
	private readonly ApplicationContext _applicationContext;

	public ScaffoldLeaderboardAndMod(ApplicationContext context)
	{
		_applicationContext = context;
	}

	public string CommandName
	{
		get => "modsetup";
	}

	public string Description
	{
		get => "Create a new leaderboard and a new user to mod it.";
	}

	public override string ToString()
	{
		return $"{CommandName}: {Description}";
	}

	public async Task Run()
	{
		Console.WriteLine("Please provide new leaderboard info.");
		string name = Options.StringLine("Leaderboard Name");
		string slug = Options.StringLine("Leaderboard Slug", validator: _ => true);

		Leaderboard newLeaderboard = new()
		{
			Name = name,
			Slug = slug,
		};

		_applicationContext.Leaderboards.Add(newLeaderboard);
		await _applicationContext.SaveChangesAsync();

		long? newLeaderboardId = _applicationContext.Leaderboards.Where(l => l.Name == name).FirstOrDefault()?.Id;

		if (newLeaderboardId is null)
		{
			throw new Exception("Unexpected error creating leaderboard.");
		}

		User newMod = Options.User();
		_applicationContext.Users.Add(newMod);
		await _applicationContext.SaveChangesAsync();
		Guid? newModId = _applicationContext.Users.Where(l => l.Username == newMod.Username).FirstOrDefault()?.Id;

		if (newModId is null)
		{
			throw new Exception("Unexpected error creating mod user.");
		}

		Modship newModship = new()
		{
			UserId = newModId.Value,
			LeaderboardId = newLeaderboardId.Value,
		};

		_applicationContext.Modships.Add(newModship);
		await _applicationContext.SaveChangesAsync();
	}
}
