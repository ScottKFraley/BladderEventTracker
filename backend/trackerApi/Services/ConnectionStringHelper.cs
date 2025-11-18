namespace trackerApi.Services;

using System.Text.RegularExpressions;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Processes connection string by replacing ${VarName} placeholders with environment variable values.
    /// Supports multiple placeholders in the same connection string.
    /// Tries both exact case and DB_ prefixed versions (e.g., DbPassword and DB_PASSWORD).
    /// </summary>
    public static string ProcessConnectionString(string connectionString, IConfiguration configuration)
    {
        // If connection string is empty, it might be provided via environment variable
        if (string.IsNullOrEmpty(connectionString))
        {
            // Try to get from environment variable (set by Container Apps)
            connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string not found in configuration. " +
                    "Ensure DefaultConnection is set in appsettings.json or environment variables.");
            }
        }

        // Use regex to find and replace all ${VarName} placeholders
        var pattern = @"\$\{([^}]+)\}";
        var missingVariables = new List<string>();

        var processedConnectionString = Regex.Replace(
            connectionString,
            pattern,
            match =>
            {
                var variableName = match.Groups[1].Value;
                var value = ResolveEnvironmentVariable(variableName, configuration);

                if (string.IsNullOrEmpty(value))
                {
                    missingVariables.Add(variableName);
                    return match.Value; // Return placeholder if not found
                }

                return value;
            });

        // If any variables were missing, throw a helpful exception
        if (missingVariables.Count > 0)
        {
            var missingList = string.Join(", ", missingVariables);
            throw new InvalidOperationException(
                $"The following environment variables are missing from configuration: {missingList}. " +
                $"Ensure these are set in user secrets, appsettings files, or environment variables. " +
                $"For example, use both exact case (e.g., 'DbPassword') and DB_ prefixed versions (e.g., 'DB_PASSWORD').");
        }

        return processedConnectionString;
    }

    /// <summary>
    /// Resolves an environment variable by trying multiple naming conventions.
    /// First tries the exact variable name, then tries DB_ prefixed version.
    /// Checks configuration, environment variables, and user secrets in order.
    /// </summary>
    private static string? ResolveEnvironmentVariable(string variableName, IConfiguration configuration)
    {
        // Try exact name first
        var value = configuration[variableName] ??
                   Environment.GetEnvironmentVariable(variableName);

        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Try DB_ prefixed version (for environment variables like DB_PASSWORD)
        var dbPrefixedName = "DB_" + variableName.ToUpper();
        value = configuration[dbPrefixedName] ??
               Environment.GetEnvironmentVariable(dbPrefixedName);

        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Try with underscores instead of camelCase (e.g., Db_Password -> DB_PASSWORD)
        var underscoreName = ConvertToUnderscoreFormat(variableName);
        if (underscoreName != dbPrefixedName)
        {
            value = configuration[underscoreName] ??
                   Environment.GetEnvironmentVariable(underscoreName);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts camelCase variable names to UPPER_SNAKE_CASE.
    /// E.g., DbPassword -> DB_PASSWORD, SqlHost -> SQL_HOST
    /// </summary>
    private static string ConvertToUnderscoreFormat(string variableName)
    {
        var result = new System.Text.StringBuilder();

        foreach (var c in variableName)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append('_');
            }
            result.Append(char.ToUpper(c));
        }

        return result.ToString();
    }
}
