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
    }
}
