namespace POS.Api.Infrastructure.Security;

public interface ILegacyPasswordVerifier
{
    bool Verify(string password, string legacyHash);
    string ComputeHash(string password);
}
