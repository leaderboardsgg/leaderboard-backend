using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Jobs.Core;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Jobs.Jobs;

internal class ScaffoldUser : IJob
{
	private readonly ApplicationContext _applicationContext;

	public ScaffoldUser(ApplicationContext context)
	{
		_applicationContext = context;
	}

	public string CommandName
	{
		get => "user";
	}

	public string Description
	{
		get => "Create a new user.";
	}

	public override string ToString()
	{
		return $"{CommandName}: {Description}";
	}

	public async Task Run()
	{
		User newUser = Options.User();
		newUser.Admin = Options.YesOrNo("Will this user be an admin?");

		_applicationContext.Users.Add(newUser);
		await _applicationContext.SaveChangesAsync();
	}
}
