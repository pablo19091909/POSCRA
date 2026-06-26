namespace PulperiaPOS.Models.Auth
{
    public sealed class ApiErrorResponse
    {
        public string TraceId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
