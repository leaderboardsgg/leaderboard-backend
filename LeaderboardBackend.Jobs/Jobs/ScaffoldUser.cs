using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Jobs.Core;
using LeaderboardBackend.Models.Entities;


namespace LeaderboardBackend.Jobs.Jobs;

internal class ScaffoldUser : IJob
{
	private ApplicationContext Context;

	public ScaffoldUser(ApplicationContext context)
	{
		Context = context;
	}

	public string CommandName => "user";
	public string Description => "Create a new user.";
	public override string ToString() => $"{CommandName}: {Description}";

	async Task IJob.Run()
	{
		User newUser = Options.User();
		newUser.Admin = Options.YesOrNo("Will this user be an admin?");
		Context.Users.Add(newUser);
		await Context.SaveChangesAsync();
	}
}
