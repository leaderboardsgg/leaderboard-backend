namespace LeaderboardBackend.Jobs.Core;

internal interface IJob
{
	string CommandName { get; }
	string Description { get; }
	Task Run();
}
