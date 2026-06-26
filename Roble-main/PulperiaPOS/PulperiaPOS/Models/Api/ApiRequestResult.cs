namespace PulperiaPOS.Models.Api
{
    public sealed class ApiRequestResult<T>
    {
        private ApiRequestResult(bool success, T? data, ApiErrorType errorType, string message, string? traceId)
        {
            Success = success;
            Data = data;
            ErrorType = errorType;
            Message = message;
            TraceId = traceId;
        }

        public bool Success { get; }
        public T? Data { get; }
        public ApiErrorType ErrorType { get; }
        public string Message { get; }
        public string? TraceId { get; }

        public static ApiRequestResult<T> Succeeded(T? data)
            => new(true, data, ApiErrorType.None, string.Empty, null);

        public static ApiRequestResult<T> Failed(ApiErrorType errorType, string message, string? traceId = null)
            => new(false, default, errorType, message, traceId);
    }
}
