namespace POS.Api.Contracts.Clientes;

public sealed record ClienteListItemResponse(
    int IdCliente,
    string Nombre,
    decimal Saldo,
    string Comprobante,
    DateTimeOffset? FechaCargaSaldoUtc);
