using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models
{
	public class User
	{
		/// <summary>A GUID that identifies the User. Generated on creation.</summary>
		/// <example>4b3835ca-dee1-4019-82b4-d2d26a7cce74</example>
		public Guid Id { get; set; }
		/// <summary>
		/// The User's name. Must be:
		/// <ul>
		///   <li>between 2 - 25 characters inclusive; and</li>
		///   <li>a sequence of letters, each separated by zero or one of:</li>
		///   <ul>
		///     <li>an underscore;</li>
		///     <li>a hyphen; or</li>
		///     <li>an apostrophe</li>
		///   </ul>
		/// </ul>
		/// Saving a name is case-sensitive, but matching against existing Users won't be.
		/// </summary>
		/// <example>Ayylmao Gaming</example>
		[Required] public string Username { get; set; } = null!;
		/// <summary>The User's email. Must be, well, an email.</summary>
		/// <example>ayylmao.gaming@alg.gg</example>
		[Required] public string Email { get; set; } = null!;
		[Required] [JsonIgnore] public string Password { get; set; } = null!;
	}
}
