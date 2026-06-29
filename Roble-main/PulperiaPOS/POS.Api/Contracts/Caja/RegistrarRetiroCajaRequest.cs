namespace POS.Api.Contracts.Caja;

public sealed record RegistrarRetiroCajaRequest(
    decimal Monto,
    string? Motivo,
    string? Referencia);
