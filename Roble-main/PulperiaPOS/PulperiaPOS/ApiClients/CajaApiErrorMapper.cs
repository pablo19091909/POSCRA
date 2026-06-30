using PulperiaPOS.Models.Api;

namespace PulperiaPOS.ApiClients
{
    public static class CajaApiErrorMapper
    {
        public static string ToSafeUserMessage(ApiErrorType errorType)
        {
            return errorType switch
            {
                ApiErrorType.None => string.Empty,
                ApiErrorType.Unauthorized or ApiErrorType.SessionExpired => ApiSafeMessages.SessionExpired,
                ApiErrorType.Forbidden => ApiSafeMessages.Forbidden,
                ApiErrorType.NotFound => "No se encontro el recurso de caja solicitado.",
                ApiErrorType.Conflict => "El estado de la caja cambio. Actualice la informacion e intente nuevamente.",
                ApiErrorType.Timeout => "No se pudo confirmar el resultado de caja. Intente nuevamente con la misma operacion.",
                ApiErrorType.Network => ApiSafeMessages.NetworkUnavailable,
                ApiErrorType.BadRequest => ApiSafeMessages.BadRequest,
                ApiErrorType.InvalidResponse => ApiSafeMessages.InvalidResponse,
                ApiErrorType.RateLimited => ApiSafeMessages.RateLimited,
                _ => ApiSafeMessages.ServiceError
            };
        }
    }
}
