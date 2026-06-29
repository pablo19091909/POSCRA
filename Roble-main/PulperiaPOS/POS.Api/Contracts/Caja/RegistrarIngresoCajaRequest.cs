namespace POS.Api.Contracts.Caja;

public sealed record RegistrarIngresoCajaRequest(
    string? CajaCodigo,
    decimal Monto,
    string? Motivo,
    string? Referencia);
