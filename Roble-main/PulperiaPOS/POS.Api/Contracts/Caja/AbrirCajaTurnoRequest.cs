namespace POS.Api.Contracts.Caja;

public sealed record AbrirCajaTurnoRequest(
    string? CajaCodigo,
    decimal FondoInicial,
    string? Observacion);
