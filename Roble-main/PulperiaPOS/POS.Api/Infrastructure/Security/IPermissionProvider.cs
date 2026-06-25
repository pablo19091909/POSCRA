using POS.Api.Domain;

namespace POS.Api.Infrastructure.Security;

public interface IPermissionProvider
{
    IReadOnlyCollection<string> GetPermissions(string role);
}
