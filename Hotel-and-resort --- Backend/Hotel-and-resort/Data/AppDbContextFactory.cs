using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Hotel_and_resort.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Get the current directory and look for appsettings.json
            var basePath = Directory.GetCurrentDirectory();
            Console.WriteLine($"Looking for appsettings.json in: {basePath}");

            // Build configuration with more robust path handling
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath);

            // Try to find appsettings.json in current directory first
            var appSettingsPath = Path.Combine(basePath, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            }
            else
            {
                // If not found, try parent directories (common in solution structures)
                var parentPath = Directory.GetParent(basePath)?.FullName;
                if (parentPath != null)
                {
                    var parentAppSettingsPath = Path.Combine(parentPath, "appsettings.json");
                    if (File.Exists(parentAppSettingsPath))
                    {
                        configurationBuilder.SetBasePath(parentPath);
                        configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    }
                }
            }

            // Add development settings if they exist
            configurationBuilder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            var configuration = configurationBuilder.Build();

            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Try to get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // If connection string is not found in config, use fallback
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Connection string not found in appsettings.json, using fallback connection string");
                connectionString = "Server=EMRYS\\SQLEXPRESS01;Database=SerenityHavenResortDB;Trusted_Connection=True;MultipleActiveResultSets=True;Connection Timeout=30;TrustServerCertificate=True;";
            }

            Console.WriteLine($"Using connection string: {connectionString}");

            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(180);
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            });

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}