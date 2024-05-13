using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Example.NoProxyApp
{
    internal class Settings
    {
        private sealed record AppSettings(string RootCertificatePath);

        private static AppSettings? appsettings;

        public static string? RootCertificatePath => appsettings?.RootCertificatePath;

        static Settings()
        {
            var EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{EnvironmentName}.json")
                .Build();
            appsettings = config.GetRequiredSection("Example").Get<AppSettings>();
        }

    }
}
