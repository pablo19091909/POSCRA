namespace PulperiaPOS.Models.Api
{
    public enum ApiErrorType
    {
        None,
        BadRequest,
        Unauthorized,
        SessionExpired,
        Forbidden,
        Conflict,
        RateLimited,
        Timeout,
        Network,
        ServiceError,
        InvalidResponse,
        Configuration
    }
}
