using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Windows;
using System.Diagnostics;
using PulperiaPOS.DataAccess;

namespace PulperiaPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // Inicializar la app normalmente
            AzureConnectionTester.ProbarConexion();
            var login = new LoginWindow();
            login.Show();
        }
    }
}
