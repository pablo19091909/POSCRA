using PulperiaPOS;
using PulperiaPOS.DataAccess;
using PulperiaPOS.Views;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PulperiaPOS
{
    /// <summary>
    /// Lógica de interacción para VentanaAnfitrion.xaml
    /// </summary>
    public partial class VentanaAnfitrion : Window
    {
        private string nombreUsuario;
        public VentanaAnfitrion(string nombre)
        {
            InitializeComponent();
            nombreUsuario = nombre;
            TextoBienvenida.Text = $"Bienvenido Administrador:  {UserSession.NombreUsuario}";
        }
        private void BtnVerInventario_Click(object sender, RoutedEventArgs e)
        {
            InventarioWindow inventarioWindow = new InventarioWindow();
            inventarioWindow.ShowDialog();
        }
        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {

            ClientePage clientePage = new ClientePage();
            clientePage.ShowDialog();
        }
        private void BtnVentasPage_Click(object sender, RoutedEventArgs e)
        {
            VentasPage ventasPage = new VentasPage();
            ventasPage.ShowDialog();
        }
        private void BtnCierreCajaPage_Click(object sender, RoutedEventArgs e)
        {
            CierreCajaPage cierreCajaPage = new CierreCajaPage();
            cierreCajaPage.ShowDialog();
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                UserSession.Clear();

                new LoginWindow().Show();
                this.Close();
            }
        }

        private void txtNombre_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void BtnTipoCambio_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new TipoCambioWindow();
            ventana.ShowDialog();
        }

    }
}
    
