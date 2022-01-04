namespace LeaderboardBackend.Services
{
    // TODO This can definitely be done better
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(long id) : base($"Could not find User with ID {id}") { }
        public UserNotFoundException(string email) : base($"Could not find User with email {email}") { }
    }
}