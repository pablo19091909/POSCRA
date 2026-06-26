namespace PulperiaPOS.Models.Auth
{
    public sealed class AuthApiResult
    {
        private AuthApiResult(bool success, LoginResponse? response, AuthApiFailure failure)
        {
            Success = success;
            Response = response;
            Failure = failure;
        }

        public bool Success { get; }
        public LoginResponse? Response { get; }
        public AuthApiFailure Failure { get; }

        public static AuthApiResult Succeeded(LoginResponse response) => new(true, response, AuthApiFailure.None);

        public static AuthApiResult Failed(AuthApiFailure failure) => new(false, null, failure);
    }
}
