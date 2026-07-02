using System;
using System.Windows;

namespace PulperiaPOS.Views
{
    public partial class ReversaVentaApiWindow : Window
    {
        public ReversaVentaApiWindow(long factura, decimal monto, string metodoPago)
        {
            InitializeComponent();
            ResumenTextBlock.Text = $"Venta {factura} | Monto: ₡{monto:N2} | Método: {metodoPago} | Turno abierto requerido.";
        }

        public string Motivo { get; private set; } = string.Empty;

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            var motivo = MotivoTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Ingrese una razón para reversar la venta.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmacion = MessageBox.Show(
                "Confirme la reversa total. Esta acción compensará inventario y caja por API.",
                "Modo Reversa API",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmacion != MessageBoxResult.Yes)
            {
                return;
            }

            Motivo = motivo;
            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
