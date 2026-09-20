using System.Diagnostics;
using MySqlConnector;
using Testcontainers.MySql;
using Xunit;

namespace DapperMany.MySql.IntegrationTests;

/// <summary>
/// Shared database fixture for MySQL integration tests.
/// Implements IAsyncLifetime to manage database container lifecycle (docker-compose or Testcontainers).
/// Can be shared across multiple test classes using IClassFixture&lt;MySqlDatabaseFixture&gt;.
/// </summary>
public class MySqlDatabaseFixture : IAsyncLifetime
{
    private MySqlContainer? _container;
    private string? _connectionString;

    /// <summary>
    /// Gets the connection string for the initialized database.
    /// Available after InitializeAsync completes.
    /// </summary>
    public string ConnectionString
    {
        get => _connectionString ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
    }

    /// <summary>
    /// Initializes the database connection and schema.
    /// First attempts to connect to docker-compose container; falls back to Testcontainers if unavailable.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Try docker-compose container first
        _connectionString = "Server=localhost;Port=3306;Database=dappermany;Uid=root;Pwd=MySql123!;";
        
        var ready = false;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(5);
        
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var testConn = new MySqlConnection(_connectionString);
                await testConn.OpenAsync();
                await testConn.CloseAsync();
                ready = true;
                break;
            }
            catch
            {
                // Connection failed, will create Testcontainer
            }
        }
        
        // If docker-compose not available, create Testcontainers instance
        if (!ready)
        {
            _container = new MySqlBuilder()
                .WithDatabase("dappermany")
                .WithUsername("root")
                .WithPassword("MySql123!")
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Register MySQL provider
        DapperMany.MySql.MySqlProvider.Register();

        // Initialize database schema
        await InitializeSchema();
    }

    /// <summary>
    /// Cleans up the container (if created via Testcontainers) when tests complete.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container != null)
            await _container.StopAsync();
    }

    /// <summary>
    /// Initializes the database schema and cleans up any existing data.
    /// Creates Pedidos and ItensPedido tables, establishes FK relationships, and truncates existing data.
    /// Idempotent - safe to call multiple times.
    /// </summary>
    private async Task InitializeSchema()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create tables
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Pedidos (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                NumeroDocumento VARCHAR(50) NOT NULL UNIQUE,
                DataPedido DATETIME NOT NULL,
                ValorTotal DECIMAL(18,2) NOT NULL,
                Status VARCHAR(50) NOT NULL,
                Created DATETIME NOT NULL,
                Modified DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ItensPedido (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                PedidoId INT NOT NULL,
                Descricao VARCHAR(255) NOT NULL,
                Quantidade INT NOT NULL,
                ValorUnitario DECIMAL(18,2) NOT NULL,
                ValorTotal DECIMAL(18,2) NOT NULL,
                Created DATETIME NOT NULL,
                Modified DATETIME NOT NULL,
                CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE
            );
        ";
        await cmd.ExecuteNonQueryAsync();

        // Clean tables
        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"
            SET FOREIGN_KEY_CHECKS=0;
            TRUNCATE TABLE ItensPedido;
            TRUNCATE TABLE Pedidos;
            SET FOREIGN_KEY_CHECKS=1;
        ";
        await cleanup.ExecuteNonQueryAsync();

        // Ensure columns exist (schema migration)
        using var alter = connection.CreateCommand();
        alter.CommandText = @"ALTER TABLE ItensPedido ADD COLUMN IF NOT EXISTS ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0;";
        try
        {
            await alter.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore if MySQL version doesn't support ADD COLUMN IF NOT EXISTS
        }

        using var alterCreated = connection.CreateCommand();
        alterCreated.CommandText = @"ALTER TABLE ItensPedido ADD COLUMN IF NOT EXISTS Created DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;";
        try
        {
            await alterCreated.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore if MySQL version doesn't support ADD COLUMN IF NOT EXISTS
        }

        using var alterModified = connection.CreateCommand();
        alterModified.CommandText = @"ALTER TABLE ItensPedido ADD COLUMN IF NOT EXISTS Modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;";
        try
        {
            await alterModified.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore if MySQL version doesn't support ADD COLUMN IF NOT EXISTS
        }
    }
}
