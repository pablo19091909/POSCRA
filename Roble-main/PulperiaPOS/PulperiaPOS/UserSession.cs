using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulperiaPOS
{
    public static class UserSession
    {
        public static int IdUsuario { get; set; } = 0;
        public static string NombreUsuario { get; set; } = string.Empty;
        public static string RolUsuario { get; set; } = string.Empty;
    }
}
