namespace POS.Api.Application.Ventas;

public sealed record VentaIdempotenciaState(
    long IdIdempotencia,
    Guid IdempotencyKey,
    int UsuarioId,
    byte[] RequestHash,
    string Estado,
    int? Factura,
    byte[]? ResponseHash,
    string? ErrorCode,
    DateTimeOffset CreadoUtc,
    DateTimeOffset ActualizadoUtc,
    DateTimeOffset? ExpiraUtc);
