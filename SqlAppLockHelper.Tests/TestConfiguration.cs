using System.IO;
using Microsoft.Extensions.Configuration;

namespace SqlAppLockHelper.Tests
{
    public class TestConfiguration
    {
        public static IConfigurationRoot ConfigurationRoot { get; }
        
        static TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            SqlConnectionString = ConfigurationRoot[nameof(SqlConnectionString)];
        }

        public static string SqlConnectionString { get; }
    }
}
