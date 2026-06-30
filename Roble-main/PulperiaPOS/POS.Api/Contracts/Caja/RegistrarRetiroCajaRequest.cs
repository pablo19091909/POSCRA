namespace POS.Api.Contracts.Caja;

public sealed record RegistrarRetiroCajaRequest(
    string? CajaCodigo,
    decimal Monto,
    string? Motivo,
    string? Referencia);
