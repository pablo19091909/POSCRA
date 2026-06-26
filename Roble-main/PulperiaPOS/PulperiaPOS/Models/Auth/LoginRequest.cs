namespace PulperiaPOS.Models.Auth
{
    public sealed class LoginRequest
    {
        public LoginRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; }
        public string Password { get; }
    }
}
