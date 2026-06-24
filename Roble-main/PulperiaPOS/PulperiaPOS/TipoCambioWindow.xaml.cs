using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PulperiaPOS
{
    /// <summary>
    /// Lógica de interacción para TipoCambioWindow.xaml
    /// </summary>
    public partial class TipoCambioWindow : Window
    {
        public TipoCambioWindow()
        {
            InitializeComponent();

            // Cargar tipo si ya existe
            if (TipoCambioHelper.ExisteTipoCambioParaHoy())
            {
                var (compra, venta) = TipoCambioHelper.ObtenerTipoCambioHoy();
                CompraTextBox.Text = compra.ToString("N2");
                VentaTextBox.Text = venta.ToString("N2");
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(CompraTextBox.Text, out double compra) &&
                double.TryParse(VentaTextBox.Text, out double venta))
            {
                TipoCambioHelper.GuardarTipoCambio(DateTime.Today, compra, venta);
                MessageBox.Show("Tipo de cambio guardado correctamente.");
                this.DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor ingrese valores válidos.");
            }
        }
    }
}
