namespace POS.Api.Application.Caja;

public sealed record CajaIdempotencyState(
    long IdCajaIdempotencia,
    int UsuarioId,
    long? IdTurno,
    string? CajaCodigo,
    CajaIdempotencyOperation Operacion,
    Guid IdempotencyKey,
    byte[] RequestHash,
    string Estado,
    long? IdMovimiento,
    long? CierreReferenciaId,
    string? ResultadoCodigo,
    DateTimeOffset CreadoUtc,
    DateTimeOffset ActualizadoUtc,
    DateTimeOffset? CompletadoUtc,
    byte[] RowVersion);
