namespace PulperiaPOS.Models.Auth
{
    public enum AuthApiFailure
    {
        None,
        InvalidCredentials,
        RateLimited,
        ServiceUnavailable,
        NetworkError,
        InvalidResponse,
        ConfigurationError
    }
}
