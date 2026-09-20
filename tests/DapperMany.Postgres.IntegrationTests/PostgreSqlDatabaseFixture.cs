using System.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace DapperMany.Postgres.IntegrationTests;

/// <summary>
/// Shared database fixture for PostgreSQL integration tests.
/// Implements IAsyncLifetime to manage database container lifecycle (docker-compose or Testcontainers).
/// Can be shared across multiple test classes using IClassFixture&lt;PostgreSqlDatabaseFixture&gt;.
/// </summary>
public class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
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
        _connectionString = "Host=localhost;Port=5432;Database=dappermany;Username=postgres;Password=Postgres123!;";
        
        var ready = false;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(5);
        
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var testConn = new NpgsqlConnection(_connectionString);
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
            _container = new PostgreSqlBuilder()
                .WithDatabase("dappermany")
                .WithUsername("postgres")
                .WithPassword("Postgres123!")
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Register PostgreSQL provider
        DapperMany.Postgres.PostgreSqlProvider.Register();

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
    /// Creates Pedidos and ItensPedido tables, establishes FK relationships, and resets identity seeds.
    /// Idempotent - safe to call multiple times.
    /// </summary>
    private async Task InitializeSchema()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create Pedidos table
        using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""Pedidos"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""NumeroDocumento"" VARCHAR(50) NOT NULL UNIQUE,
                ""DataPedido"" TIMESTAMP NOT NULL,
                ""ValorTotal"" NUMERIC(18,2) NOT NULL,
                ""Status"" VARCHAR(50) NOT NULL,
                ""Created"" TIMESTAMP NOT NULL,
                ""Modified"" TIMESTAMP NOT NULL
            );
        ";
        await cmd1.ExecuteNonQueryAsync();

        // Create ItensPedido table
        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""ItensPedido"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""PedidoId"" INTEGER NOT NULL,
                ""Descricao"" VARCHAR(255) NOT NULL,
                ""Quantidade"" INTEGER NOT NULL,
                ""ValorUnitario"" NUMERIC(18,2) NOT NULL,
                ""ValorTotal"" NUMERIC(18,2) NOT NULL,
                ""Created"" TIMESTAMP NOT NULL,
                ""Modified"" TIMESTAMP NOT NULL,
                FOREIGN KEY (""PedidoId"") REFERENCES ""Pedidos""(""Id"") ON DELETE CASCADE
            );
        ";
        await cmd2.ExecuteNonQueryAsync();

        // Clean tables and reseed sequences
        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"TRUNCATE TABLE ""ItensPedido"", ""Pedidos"" RESTART IDENTITY CASCADE;";
        await cleanup.ExecuteNonQueryAsync();

        // Ensure columns exist (schema migration)
        using var alter = connection.CreateCommand();
        alter.CommandText = @"ALTER TABLE ""ItensPedido"" ADD COLUMN IF NOT EXISTS ""ValorTotal"" NUMERIC(18,2) NOT NULL DEFAULT 0;";
        await alter.ExecuteNonQueryAsync();

        using var alterCreated = connection.CreateCommand();
        alterCreated.CommandText = @"ALTER TABLE ""ItensPedido"" ADD COLUMN IF NOT EXISTS ""Created"" TIMESTAMP NOT NULL DEFAULT now();";
        await alterCreated.ExecuteNonQueryAsync();

        using var alterModified = connection.CreateCommand();
        alterModified.CommandText = @"ALTER TABLE ""ItensPedido"" ADD COLUMN IF NOT EXISTS ""Modified"" TIMESTAMP NOT NULL DEFAULT now();";
        await alterModified.ExecuteNonQueryAsync();
    }
}
