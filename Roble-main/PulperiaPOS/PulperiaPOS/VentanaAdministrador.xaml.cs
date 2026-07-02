using PulperiaPOS;
using PulperiaPOS.DataAccess;
using PulperiaPOS.Views;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
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
    /// Lógica de interacción para VentanaAdministrador.xaml
    /// </summary>
    public partial class VentanaAdministrador : Window
    {
        private string nombreUsuario;
        public VentanaAdministrador(string nombre)
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
        private void BtnDonacionesPage_Click(object sender, RoutedEventArgs e)
        {
            DonacionesPage donacionesPage = new DonacionesPage();
            donacionesPage.ShowDialog();
        }        
        private void BtnCierreCajaPage_Click(object sender, RoutedEventArgs e)
        {
            CierreCajaPage cierreCajaPage = new CierreCajaPage();
            cierreCajaPage.ShowDialog();
        }
        private void BtnRetiroCajaPage_Click(object sender, RoutedEventArgs e)
        {
            RetirosCajaPage retirosCajaPage = new RetirosCajaPage();
            retirosCajaPage.ShowDialog();
        }

        private void BtnUsuariosPage_Click(object sender, RoutedEventArgs e)
        {
            VentanaUsuarios ventanaUsuarios= new VentanaUsuarios();
            ventanaUsuarios.ShowDialog();
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
        private void AbrirCaja_Click(object sender, RoutedEventArgs e)
        {           
            byte[] abrirCaja = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // Comando ESC/POS para abrir gaveta
            bool ok = RawPrinterHelper.SendBytesToPrinter("POS-58", abrirCaja);
            if (!ok)
            {
                MessageBox.Show("No se pudo abrir la caja registradora.");
            }
        }
        private void ImprimirPruebaPOS()
        {
            string textoPrueba = "PULPERÍA POS\nPrueba de impresión\nGracias por su compra!\n\n\n\n";
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(textoPrueba);

            // Puedes usar el nombre exacto de tu impresora, por ejemplo "POS-58"
            string nombreImpresora = new System.Drawing.Printing.PrinterSettings().PrinterName;

            bool exito = RawPrinterHelper.SendBytesToPrinter(nombreImpresora, bytes);

            if (exito)
                MessageBox.Show("Prueba enviada a la impresora correctamente.");
            else
                MessageBox.Show("Error al imprimir la prueba. Verifica el nombre de la impresora.");
        }
        private void BtnImprimirPrueba_Click(object sender, RoutedEventArgs e)
        {
            ImprimirPruebaPOS();
        }

        private void BtnIngresoCajaPage_Click(object sender, RoutedEventArgs e)
        {         
            IngresoCajaPage ingresoCajaPage = new IngresoCajaPage();
            ingresoCajaPage.ShowDialog();
        }

        private void BtnTipoCambio_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new TipoCambioWindow();
            ventana.ShowDialog();
        }

        private void BtnReporteriaApi_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReporteriaApiWindow();
            ventana.ShowDialog();
        }
    }

}


