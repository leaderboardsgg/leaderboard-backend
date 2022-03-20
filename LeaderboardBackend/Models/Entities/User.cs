using LeaderboardBackend.Models.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

public class User
{
	public Guid Id { get; set; }

	[Required] 
	public string Username { get; set; } = null!;

	[Required] 
	[EmailAddress] 
	public string Email { get; set; } = null!;

	[Required] 
	[Password] 
	public string Password { get; set; } = null!;

	[JsonIgnore] 
	[InverseProperty("BanningUser")] 
	public List<Ban>? BansGiven { get; set; }

	[JsonIgnore] 
	[InverseProperty("BannedUser")] 
	public List<Ban>? BansReceived { get; set; }

	[JsonIgnore] 
	public List<Modship>? Modships { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is User user &&
			   Id.Equals(user.Id) &&
			   Username == user.Username &&
			   Email == user.Email;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Username, Email);
	}
}
