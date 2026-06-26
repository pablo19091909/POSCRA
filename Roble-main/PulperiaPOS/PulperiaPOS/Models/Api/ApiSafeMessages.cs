namespace PulperiaPOS.Models.Api
{
    public static class ApiSafeMessages
    {
        public const string BadRequest = "La solicitud no pudo ser procesada.";
        public const string Forbidden = "No tiene permiso para realizar esta acción.";
        public const string InvalidResponse = "No se pudo validar la respuesta del servicio.";
        public const string NetworkUnavailable = "No fue posible comunicarse con el servicio. Verifique conexión e intente nuevamente.";
        public const string RateLimited = "Se alcanzó el límite de intentos. Espere un momento e intente nuevamente.";
        public const string ServiceError = "El servicio no está disponible temporalmente. Intente nuevamente más tarde.";
        public const string SessionExpired = "La sesión expiró. Inicia sesión nuevamente.";
    }
}
