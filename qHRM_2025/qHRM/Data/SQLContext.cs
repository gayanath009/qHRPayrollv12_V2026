using Microsoft.Extensions.Configuration;
using System.IO;

namespace qHRM.Data
{
    public class SQLContext
    {
        public static string conString()
        {
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();
            return configuration.GetSection("ConnectionStrings").GetSection("qHR").Value;
        }
    }
}
