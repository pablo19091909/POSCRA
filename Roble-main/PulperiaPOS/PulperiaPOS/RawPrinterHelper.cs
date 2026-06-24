using PulperiaPOS;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;





public static class RawPrinterHelper
{
    [DllImport("winspool.Drv", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.Drv", SetLastError = true)]
    static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOCINFOA pDocInfo);

    [DllImport("winspool.Drv", SetLastError = true)]
    static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDataType;
    }

    public static bool SendBytesToPrinter(string printerName, byte[] bytes)
    {
        IntPtr hPrinter;
        DOCINFOA di = new DOCINFOA
        {
            pDocName = "Raw Document",
            pDataType = "RAW"
        };

        if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            return false;

        bool success = StartDocPrinter(hPrinter, 1, ref di);
        if (!success)
        {
            ClosePrinter(hPrinter);
            return false;
        }

        StartPagePrinter(hPrinter);

        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
        Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
        WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int _);
        Marshal.FreeCoTaskMem(pUnmanagedBytes);

        EndPagePrinter(hPrinter);
        EndDocPrinter(hPrinter);
        ClosePrinter(hPrinter);

        return true;
    }

    public static string GetVersiculoInspirador()
    {
        var rnd = new Random();
        return VersiculosInspiradores[rnd.Next(VersiculosInspiradores.Count)];
    }


    private static readonly List<string> VersiculosInspiradores = new List<string>
    {
        "Marcos 16:15 - Luego les dijo: «Vayan por todo el mundo y prediquen la Buena Noticia a todos». (NTV)",
        "Juan 13:34 - Así que ahora les doy un nuevo mandamiento: ámense unos a otros. Tal como yo los he amado, ustedes deben amarse unos a otros. (NTV)",
        "Romanos 5:8 - Pero Dios mostró el gran amor que nos tiene al enviar a Cristo a morir por nosotros cuando todavía éramos pecadores. (NTV)",
        "Mateo 28:19 - Por lo tanto, vayan y hagan discípulos de todas las naciones, bautizándolos en el nombre del Padre y del Hijo y del Espíritu Santo. (NTV)",
        "1 Juan 4:8 - Pero el que no ama no conoce a Dios, porque Dios es amor. (NTV)",
        "Hechos 1:8 - Pero recibirán poder cuando el Espíritu Santo descienda sobre ustedes; y serán mis testigos, y les hablarán a la gente acerca de mí en todas partes. (NTV)",
        "Romanos 10:14 - ¿Pero cómo podrán llamarlo para que los salve si no creen en él? ¿Y cómo pueden creer en él si nunca han oído de él? ¿Y cómo pueden oír de él a menos que alguien les predique? (NTV)",
        "Isaías 6:8 - Después oí que el Señor preguntó: «¿A quién enviaré como mensajero a este pueblo? ¿Quién irá por nosotros?». —Aquí estoy yo. ¡Envíame a mí!— le respondí. (NTV)",
        "Juan 3:16 - Pues Dios amó tanto al mundo que dio a su único Hijo, para que todo el que crea en él no se pierda, sino que tenga vida eterna. (NTV)",
        "1 Corintios 13:13 - Tres cosas durarán para siempre: la fe, la esperanza y el amor; y la mayor de las tres es el amor. (NTV)",
        "Efesios 2:8 - Dios los salvó por su gracia cuando creyeron. Ustedes no tienen ningún mérito en eso; es un regalo de Dios. (NTV)",
        "Gálatas 2:20 - Mi antiguo yo ha sido crucificado con Cristo. Ya no vivo yo, sino que Cristo vive en mí. (NTV)",
        "2 Corintios 5:20 - Así que somos embajadores de Cristo; Dios hace su llamado por medio de nosotros. Hablamos en nombre de Cristo. (NTV)",
        "Colosenses 3:14 - Sobre todo, vístanse de amor, lo cual nos une a todos en perfecta armonía. (NTV)",
        "1 Pedro 3:15 - Adoren a Cristo como Señor de su vida. Si alguien les pregunta acerca de la esperanza que tienen como creyentes, estén siempre preparados para dar una explicación. (NTV)",
        "Salmos 96:3 - Publiquen sus gloriosas obras entre las naciones; cuéntenles a todos las cosas asombrosas que él hace. (NTV)",
        "Mateo 5:16 - Hagan brillar su luz delante de todos, para que ellos puedan ver las buenas obras de ustedes y alaben a su Padre que está en el cielo. (NTV)",
        "Lucas 19:10 - Pues el Hijo del Hombre vino a buscar y a salvar a los que están perdidos. (NTV)",
        "Juan 15:12 - Este es mi mandamiento: ámense unos a otros de la misma manera en que yo los he amado. (NTV)",
        "Romanos 12:9 - No finjan amar a los demás; ámenlos de verdad. Aborrezcan lo malo. Aférrense a lo bueno. (NTV)"
    };


    //-----------------------------------------------------------------------------------------------------------------------------------------------------
    public static void ImprimirLogoESC_POS(string printerName)
    {
        using (Stream bmpStream = ObtenerRecursoStream("Verde3_1bit.bmp"))
        {
            if (bmpStream == null)
            {
                MessageBox.Show("No se encontró el recurso de imagen para el logo.");
                return;
            }

            using (var originalBmp = new Bitmap(bmpStream))
            {
                int maxWidth = 384;
                int newHeight = (int)((double)originalBmp.Height / originalBmp.Width * maxWidth);
                using (var resizedBmp = new Bitmap(originalBmp, new System.Drawing.Size(maxWidth, newHeight)))
                {
                    byte[] bytes = ESC_POS_ImageHelper.ConvertToEscPos(resizedBmp);
                    SendBytesToPrinter(printerName, bytes);
                }
            }
        }
    }




    //-----------------------------------------------------------------------------------------------------------------------------------------------------
    private static Stream ObtenerRecursoStream(string nombreArchivo)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string recursoCompleto = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(nombreArchivo, StringComparison.InvariantCultureIgnoreCase));

        if (recursoCompleto == null)
        {
            MessageBox.Show("No se encontró el recurso embebido: " + nombreArchivo);
            return null;
        }

        return assembly.GetManifestResourceStream(recursoCompleto);
    }


    public static class ESC_POS_ImageHelper
    {
        public static byte[] ConvertToEscPos(Bitmap originalBmp)
        {
            int maxWidth = 384; // Ancho máximo típico para impresoras de 58mm
            int width = originalBmp.Width;
            int height = originalBmp.Height;

            // Si el ancho no es múltiplo de 8, ajustarlo
            int paddedWidth = (width + 7) / 8 * 8;
            if (paddedWidth > maxWidth) paddedWidth = maxWidth;

            // Escalar si es necesario
            if (width > paddedWidth)
            {
                height = (int)((double)originalBmp.Height / originalBmp.Width * paddedWidth);
                originalBmp = new Bitmap(originalBmp, new System.Drawing.Size(paddedWidth, height));
            }

            var data = new List<byte>();
            data.Add(0x1B); // ESC
            data.Add(0x33); // Espaciado entre líneas
            data.Add(0x00);

            for (int y = 0; y < height; y += 24)
            {
                data.Add(0x1B);
                data.Add(0x2A); // Comando de gráfico
                data.Add(0x21); // Modo de 24 puntos, single-density
                data.Add((byte)(paddedWidth / 8)); // nL
                data.Add((byte)((paddedWidth / 8) >> 8)); // nH

                for (int x = 0; x < paddedWidth; x++)
                {
                    for (int k = 0; k < 24; k++)
                    {
                        byte slice = 0x00;
                        int yk = y + k;

                        if (x < originalBmp.Width && yk < height)
                        {
                            Color pixel = originalBmp.GetPixel(x, yk);
                            int luminance = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);
                            if (luminance > 127) // Invertido para que fondo blanco no imprima
                                slice |= (byte)(1 << (7 - (k % 8)));
                        }

                        if (k % 8 == 7)
                            data.Add(slice);
                    }
                }

                data.Add(0x0A); // Salto de línea
            }

            return data.ToArray();
        }
    }





    //-----------------------------------------------------------------------------------------------------------------------------------------------------
    public static void ImprimirReciboPOS58(string printerName, string titulo, List<string> lineas, string total,
                                  string metodoPago, string vuelto, string comprobante = "",
                                  string fechaHora = "", string numeroFactura = "",
                                  string nombreCliente = "", string nombreCajero = "")
    {
        var data = new List<byte>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(850);

        // Establecer code page 850 en la impresora
        byte[] setCodePage850 = new byte[] { 0x1B, 0x74, 0x02 };
        data.AddRange(setCodePage850);

        var rnd = new Random();
        var versiculo = VersiculosInspiradores[rnd.Next(VersiculosInspiradores.Count)];

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("      PULPERÍA CRA");
        sb.AppendLine("Un lugar para encontrarse con Dios!");
        if (!string.IsNullOrWhiteSpace(numeroFactura))
            sb.AppendLine("Factura No: " + numeroFactura);
        sb.AppendLine("--------------------------------");

        foreach (var linea in lineas)
        {
            string textoFormateado = linea.Length > 32 ? linea.Substring(0, 32) : linea;
            sb.AppendLine(textoFormateado);
        }

        sb.AppendLine("--------------------------------");
        if (!string.IsNullOrWhiteSpace(fechaHora))
            sb.AppendLine("Fecha: " + fechaHora);
        sb.AppendLine("TOTAL: " + total);
        sb.AppendLine("Método de Pago: " + metodoPago);

        if (!string.IsNullOrWhiteSpace(vuelto))
            sb.AppendLine("Vuelto: " + vuelto);

        if (!string.IsNullOrWhiteSpace(comprobante))
            sb.AppendLine("Comprobante: " + comprobante);

        if (!string.IsNullOrWhiteSpace(nombreCliente))
            sb.AppendLine("Cliente: " + nombreCliente);

        if (!string.IsNullOrWhiteSpace(nombreCajero))
            sb.AppendLine("Cajero: " + nombreCajero);

        sb.AppendLine();
        sb.AppendLine("Gracias por su compra!");
        sb.AppendLine();
        sb.AppendLine("Palabra del día:");
        sb.AppendLine(versiculo);
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(); // Espacio para corte
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("--------------------------------");


        var textoBytes = encoding.GetBytes(sb.ToString());
        data.AddRange(textoBytes);

        SendBytesToPrinter(printerName, data.ToArray());
    }
    public static void AbrirCajaDesdePOS58()
    {
        byte[] abrirCaja = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // ESC/POS
        string printerName = new PrinterSettings().PrinterName;
        RawPrinterHelper.SendBytesToPrinter(printerName, abrirCaja);
    }

    public static void ImprimirCierreDeCaja(string printerName, long idCierre, string fechaHora,
                                         string efectivo, string sinpe, string datafono,
                                         string observaciones, string nombreUsuario,
                                         CajaHelper.TotalesCaja totales)
    {
        var data = new List<byte>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(850);

        // Comando ESC/POS para establecer code page 850
        data.AddRange(new byte[] { 0x1B, 0x74, 0x02 });

        // Versículo aleatorio
        var rnd = new Random();
        var versiculo = VersiculosInspiradores[rnd.Next(VersiculosInspiradores.Count)];

        var sb = new StringBuilder();
        sb.AppendLine("     *** CIERRE DE CAJA ***");
        sb.AppendLine("--------------------------------");
        sb.AppendLine("ID Cierre:     " + idCierre);
        sb.AppendLine("Fecha y Hora:  " + fechaHora);
        sb.AppendLine("Usuario:       " + nombreUsuario);
        sb.AppendLine();

        sb.AppendLine(">>> DETALLE EFECTIVO <<<");
        sb.AppendLine($"Ventas:         ₡{totales.Ventas:N2}");
        sb.AppendLine($"Ingresos:       ₡{totales.Ingresos:N2}");
        sb.AppendLine($"Retiros:       -₡{totales.Retiros:N2}");
        sb.AppendLine($"Cierres prev.: -₡{totales.Cierres:N2}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Total en Caja:  ₡{totales.TotalDisponible:N2}");
        sb.AppendLine();

        sb.AppendLine(">>> OTROS MÉTODOS <<<");
        sb.AppendLine("SINPE:         ₡" + sinpe);
        sb.AppendLine("Datáfono:      ₡" + datafono);
        sb.AppendLine();

        sb.AppendLine("Observaciones:");
        sb.AppendLine(observaciones);
        sb.AppendLine();
        sb.AppendLine("¡Gracias por su trabajo hoy!");
        sb.AppendLine();
        sb.AppendLine("Palabra del día:");
        sb.AppendLine(versiculo);
        sb.AppendLine("--------------------------------");
        sb.AppendLine();
        sb.AppendLine();

        var bytes = encoding.GetBytes(sb.ToString());
        data.AddRange(bytes);

        RawPrinterHelper.SendBytesToPrinter(printerName, data.ToArray());
    }



}

