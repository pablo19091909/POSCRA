namespace PulperiaPOS.Configuration
{
    public static class FeatureFlags
    {
        public static bool UseApiLogin
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseApiLogin"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseClientesApi
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseClientesApi"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseVentasClienteSelectorApi
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseVentasClienteSelectorApi"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseVentasProductosApi
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseVentasProductosApi"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseVentasApiWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseVentasApiWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseVentasApiEfectivoWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseVentasApiEfectivoWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseVentasApiReversaWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseVentasApiReversaWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseCajaApiRead
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseCajaApiRead"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseCajaApiOpenWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseCajaApiOpenWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseCajaApiIngresoWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseCajaApiIngresoWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseCajaApiRetiroWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseCajaApiRetiroWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseCajaApiCierreWrite
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseCajaApiCierreWrite"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }

        public static bool UseReportesApiRead
        {
            get
            {
                var configuredValue = AppConfiguration.Current["FeatureFlags:UseReportesApiRead"];
                return bool.TryParse(configuredValue, out var enabled) && enabled;
            }
        }
    }
}
