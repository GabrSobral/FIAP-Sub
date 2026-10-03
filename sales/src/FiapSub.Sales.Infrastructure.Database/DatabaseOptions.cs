namespace FiapSub.Sales.Infrastructure.Database;

/// <summary>Bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Applies pending EF Core migrations when the service starts. Convenient for docker-compose
    /// and for the k8s rollout; turn it off if migrations are run as a separate job.
    /// </summary>
    public bool RunMigrationsOnStartup { get; set; }

    public int MaxRetryCount { get; set; } = 5;

    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Verbose EF Core diagnostics, including parameter values. Development only: it writes
    /// user data to the logs.
    /// </summary>
    public bool EnableDetailedLogging { get; set; }
}
