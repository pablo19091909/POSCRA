namespace PulperiaPOS.Models.Api
{
    public enum ApiErrorType
    {
        None,
        BadRequest,
        Unauthorized,
        SessionExpired,
        Forbidden,
        RateLimited,
        Timeout,
        Network,
        ServiceError,
        InvalidResponse,
        Configuration
    }
}
