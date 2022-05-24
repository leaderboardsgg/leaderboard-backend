using LeaderboardBackend.Jobs.Core;
using LeaderboardBackend.Models.Entities;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Jobs.Jobs;

internal class ScaffoldLeaderboardAndMod : IJob
{
	private ApplicationContext Context;

	public ScaffoldLeaderboardAndMod(ApplicationContext context)
	{
		Context = context;
	}

	public string CommandName => "modsetup";
	public string Description => "Create a new leaderboard and a new user to mod it.";
	public override string ToString() => $"{CommandName}: {Description}";

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
		Context.Leaderboards.Add(newLeaderboard);
		await Context.SaveChangesAsync();
		long? newLeaderboardId = Context.Leaderboards.Where(l => l.Name == name).FirstOrDefault()?.Id;
		if (newLeaderboardId is null)
		{
			throw new Exception("Unexpected error creating leaderboard.");
		}

		User newMod = Options.User();
		Context.Users.Add(newMod);
		await Context.SaveChangesAsync();
		Guid? newModId = Context.Users.Where(l => l.Username == newMod.Username).FirstOrDefault()?.Id;
		if (newModId is null)
		{
			throw new Exception("Unexpected error creating mod user.");
		}

		Modship newModship = new()
		{
			UserId = newModId.Value,
			LeaderboardId = newLeaderboardId.Value,
		};
		Context.Modships.Add(newModship);
		await Context.SaveChangesAsync();
	}
}
