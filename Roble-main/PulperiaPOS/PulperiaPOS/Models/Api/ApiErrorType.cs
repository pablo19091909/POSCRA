namespace PulperiaPOS.Models.Api
{
    public enum ApiErrorType
    {
        None,
        BadRequest,
        Unauthorized,
        SessionExpired,
        Forbidden,
        NotFound,
        Conflict,
        RateLimited,
        Timeout,
        Network,
        ServiceError,
        InvalidResponse,
        Configuration
    }
}
