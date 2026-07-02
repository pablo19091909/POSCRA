using POS.Api.Domain;

namespace POS.Api.Infrastructure.Security;

public sealed class RolePermissionProvider : IPermissionProvider
{
    private static readonly IReadOnlyCollection<string> AdministratorPermissions =
    [
        PermissionNames.UsuariosAdministrar,
        PermissionNames.UsuariosVer,
        PermissionNames.VentasCrear,
        PermissionNames.VentasVer,
        PermissionNames.VentasAnular,
        PermissionNames.VentasDevolver,
        PermissionNames.VentasReversar,
        PermissionNames.InventarioVer,
        PermissionNames.InventarioEditar,
        PermissionNames.ClientesVer,
        PermissionNames.ClientesEditar,
        PermissionNames.ClientesAjustarSaldo,
        PermissionNames.CajaVer,
        PermissionNames.CajaAbrir,
        PermissionNames.CajaIngresar,
        PermissionNames.CajaRetirar,
        PermissionNames.CajaPreCerrar,
        PermissionNames.CajaCerrar,
        PermissionNames.CajaReabrir,
        PermissionNames.CajaVerResumen,
        PermissionNames.ReportesVer,
        PermissionNames.TipoCambioVer,
        PermissionNames.TipoCambioEditar,
        PermissionNames.DonacionesVer,
        PermissionNames.DonacionesRegistrar,
        PermissionNames.ConfiguracionAdministrar
    ];

    private static readonly IReadOnlyCollection<string> HostPermissions =
    [
        PermissionNames.VentasCrear,
        PermissionNames.VentasVer,
        PermissionNames.InventarioVer,
        PermissionNames.ClientesVer,
        PermissionNames.CajaVer,
        PermissionNames.CajaPreCerrar,
        PermissionNames.CajaCerrar,
        PermissionNames.CajaVerResumen,
        PermissionNames.TipoCambioVer
    ];

    public IReadOnlyCollection<string> GetPermissions(string role)
    {
        return role switch
        {
            RoleNames.Administrador => AdministratorPermissions,
            RoleNames.Anfitrion => HostPermissions,
            _ => []
        };
    }
}
