namespace trackerApi.Services;

public static class ConnectionStringHelper
{
    public static string ProcessConnectionString(string connectionString, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found in configuration. " +
                "Ensure DefaultConnection is set in appsettings.json.");
        }

        // Only try to replace password if placeholder exists
        if (connectionString.Contains("${SqlPassword}"))
        {
            var sqlPassword = configuration["SqlPassword"] ??
                             configuration["SQL_PASSWORD"] ??
                             Environment.GetEnvironmentVariable("SqlPassword") ??
                             Environment.GetEnvironmentVariable("SQL_PASSWORD");

            if (string.IsNullOrEmpty(sqlPassword))
            {
                throw new InvalidOperationException(
                    "SQL password not found in configuration. " +
                    "Ensure SqlPassword is set in user secrets or SQL_PASSWORD in environment variables.");
            }

            connectionString = connectionString.Replace("${SqlPassword}", sqlPassword);
        }

        return connectionString;
    }
}
