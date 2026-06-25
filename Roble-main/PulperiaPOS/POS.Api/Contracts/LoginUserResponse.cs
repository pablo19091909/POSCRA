namespace POS.Api.Contracts;

public sealed record LoginUserResponse(int Id, string Username, string Role, IReadOnlyCollection<string> Permissions);
