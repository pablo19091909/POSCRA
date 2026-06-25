using POS.Api.Domain;

namespace POS.Api.Infrastructure.Data;

public interface IUserRepository
{
    Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> TryUpgradeLegacyPasswordHashAsync(int userId, string modernPasswordHash, CancellationToken cancellationToken);
}
