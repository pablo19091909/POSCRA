using System;
using Microsoft.Extensions.Configuration;

namespace PulperiaPOS.Configuration
{
    public static class AppConfiguration
    {
        private static readonly Lazy<IConfigurationRoot> configuration = new(LoadConfiguration);

        public static IConfigurationRoot Current => configuration.Value;

        private static IConfigurationRoot LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();
        }
    }
}
