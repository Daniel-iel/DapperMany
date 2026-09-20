using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace DapperMany.SqlServer.IntegrationTests;

/// <summary>
/// Shared database fixture for SQL Server integration tests.
/// Implements IAsyncLifetime to manage database container lifecycle (docker-compose or Testcontainers).
/// Can be shared across multiple test classes using IClassFixture&lt;SqlServerDatabaseFixture&gt;.
/// </summary>
public class SqlServerDatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
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
        // Try docker-compose container first (default connection string)
        _connectionString = "Server=localhost,1433;Initial Catalog=DapperMany;User Id=sa;Password=SqlServer123!;Encrypt=false;";
        
        var ready = false;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(5);
        
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var testConn = new SqlConnection(_connectionString);
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
            var saPassword = "SqlServer123!";
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithPassword(saPassword)
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Register SQL Server provider
        DapperMany.SqlServer.SqlServerProvider.Register();

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
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create database if it doesn't exist
        using var useDb = connection.CreateCommand();
        useDb.CommandText = @"
            IF DB_ID('DapperMany') IS NULL
            BEGIN
                CREATE DATABASE [DapperMany];
            END
            USE [DapperMany];
        ";
        await useDb.ExecuteNonQueryAsync();

        // Create Pedidos table
        using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
            IF OBJECT_ID('dbo.Pedidos','U') IS NULL
            BEGIN
                CREATE TABLE dbo.Pedidos (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    NumeroDocumento NVARCHAR(50) NOT NULL UNIQUE,
                    DataPedido DATETIME2 NOT NULL,
                    ValorTotal DECIMAL(12,2) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    Created DATETIME2 NOT NULL,
                    Modified DATETIME2 NOT NULL
                );
            END
        ";
        await cmd1.ExecuteNonQueryAsync();

        // Create ItensPedido table
        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
            IF OBJECT_ID('dbo.ItensPedido','U') IS NULL
            BEGIN
                CREATE TABLE dbo.ItensPedido (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    PedidoId INT NOT NULL,
                    Descricao NVARCHAR(255) NOT NULL,
                    Quantidade INT NOT NULL,
                    ValorUnitario DECIMAL(12,2) NOT NULL,
                    ValorTotal DECIMAL(12,2) NOT NULL,
                    FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos(Id)
                );
            END
        ";
        await cmd2.ExecuteNonQueryAsync();

        // Clean tables and reseed identities
        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = @"
            DELETE FROM dbo.ItensPedido;
            DELETE FROM dbo.Pedidos;
            DBCC CHECKIDENT('dbo.Pedidos', RESEED, 0);
            DBCC CHECKIDENT('dbo.ItensPedido', RESEED, 0);
        ";
        await cleanup.ExecuteNonQueryAsync();

        // Ensure columns exist (schema migration)
        using var alter = connection.CreateCommand();
        alter.CommandText = @"
            IF COL_LENGTH('dbo.ItensPedido', 'ValorTotal') IS NULL
            BEGIN
                ALTER TABLE dbo.ItensPedido ADD ValorTotal DECIMAL(12,2) NOT NULL DEFAULT(0);
            END
        ";
        await alter.ExecuteNonQueryAsync();

        using var alterCreated = connection.CreateCommand();
        alterCreated.CommandText = @"
            IF COL_LENGTH('dbo.ItensPedido', 'Created') IS NULL
            BEGIN
                ALTER TABLE dbo.ItensPedido ADD Created DATETIME2 NOT NULL DEFAULT(GETDATE());
            END
        ";
        await alterCreated.ExecuteNonQueryAsync();

        using var alterModified = connection.CreateCommand();
        alterModified.CommandText = @"
            IF COL_LENGTH('dbo.ItensPedido', 'Modified') IS NULL
            BEGIN
                ALTER TABLE dbo.ItensPedido ADD Modified DATETIME2 NOT NULL DEFAULT(GETDATE());
            END
        ";
        await alterModified.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates composite key test table (TenantPedidos) for multi-tenant scenarios.
    /// Called by composite key test classes.
    /// Idempotent - safe to call multiple times.
    /// </summary>
    public async Task InitializeCompositeKeySchema()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                IF OBJECT_ID('dbo.TenantPedidos', 'U') IS NOT NULL
                    DROP TABLE dbo.TenantPedidos;
                
                CREATE TABLE dbo.TenantPedidos (
                    TenantId NVARCHAR(50) NOT NULL,
                    DocumentNumber NVARCHAR(50) NOT NULL,
                    OrderDate DATETIME2 NOT NULL,
                    TotalAmount DECIMAL(18, 2) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    ModifiedAt DATETIME2 NOT NULL,
                    PRIMARY KEY (TenantId, DocumentNumber)
                )";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
