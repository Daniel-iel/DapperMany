using Dapper;
using DapperMany.MySql;
using DapperMany.Postgres;
using DapperMany.Samples.Models;
using DapperMany.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;
using DapperMany.Samples.Infrastructure.Output;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Data;
using DapperMany.Samples.Services.Demos;
using DapperMany.Samples.Infrastructure.Connections;

namespace DapperMany.Samples;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║         DapperMany - Bulk Operations Demo      ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝\n");

        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

        // Ensure providers are registered explicitly (idempotent)
        try
        {
            SqlServerProvider.Register();
            PostgreSqlProvider.Register();
            MySqlProvider.Register();
        }
        catch { }

        var showMenu = true;
        while (showMenu)
        {
            Console.WriteLine("\n Select a database provider:\n");
            Console.WriteLine("  1. SQL Server");
            Console.WriteLine("  2. PostgreSQL");
            Console.WriteLine("  3. MySQL");
            Console.WriteLine("  0. Exit\n");
            Console.Write("  Choice: ");

            if (int.TryParse(Console.ReadLine(), out var choice))
            {
                switch (choice)
                {
                    case 1:
                        await RunSqlServerDemo(config);
                        break;
                    case 2:
                        await RunPostgresDemo(config);
                        break;
                    case 3:
                        await RunMySqlDemo(config);
                        break;
                    case 0:
                        showMenu = false;
                        Console.WriteLine("\n  Goodbye!");
                        break;
                    default:
                        Console.WriteLine("\n  Invalid choice. Try again.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("\n  Invalid input. Try again.");
            }
        }
    }

    static async Task RunSqlServerDemo(IConfiguration config)
    {
        Console.WriteLine("\n▶ SQL Server Demo\n");

        var connectionString = config.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("SQL Server connection string not found");

        var factory = new SqlServerConnectionFactory();

        try
        {
            using var connection = factory.Create(connectionString);
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            Console.WriteLine("  ✓ Connected to SQL Server");

            await ShowProviderDemo(connection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
            Console.WriteLine($"     Make sure SQL Server is running and accessible");
            Console.WriteLine($"     Connection: {connectionString}");
        }
    }

    static async Task RunPostgresDemo(IConfiguration config)
    {
        Console.WriteLine("\n▶ PostgreSQL Demo\n");

        var connectionString = config.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("PostgreSQL connection string not found");

        var factory = new PostgresConnectionFactory();

        try
        {
            using var connection = factory.Create(connectionString);
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            Console.WriteLine("  ✓ Connected to PostgreSQL");

            await ShowProviderDemo(connection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
            Console.WriteLine($"     Make sure PostgreSQL is running and accessible");
            Console.WriteLine($"     Connection: {connectionString}");
        }
    }

    static async Task RunMySqlDemo(IConfiguration config)
    {
        Console.WriteLine("\n▶ MySQL Demo\n");

        var connectionString = config.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException("MySQL connection string not found");

        var factory = new MySqlConnectionFactory();

        try
        {
            using var connection = factory.Create(connectionString);
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            Console.WriteLine("  ✓ Connected to MySQL");

            await ShowProviderDemo(connection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
            Console.WriteLine($"     Make sure MySQL is running and accessible");
            Console.WriteLine($"     Connection: {connectionString}");
        }
    }

    static async Task ShowProviderDemo(System.Data.IDbConnection connection)
    {
        // Create local infrastructure components (no DI in samples by design)
        var output = new ConsoleOutputFormatter();
        var errorHandler = new ConsoleErrorHandler(output);
        var generator = new RandomPedidoGenerator();

        // Use the new operation for InsertMany (refactored)
        var insertOp = new InsertManyDemoOperation();
        await insertOp.ExecuteAsync(connection, output, errorHandler, generator);

        // Execute other demos via refactored operations
        var graphOp = new InsertGraphDemoOperation();
        await graphOp.ExecuteAsync(connection, output, errorHandler, generator);

        var queryOp = new QueryDemoOperation();
        await queryOp.ExecuteAsync(connection, output, errorHandler, generator);

        var updateOp = new UpdateManyDemoOperation();
        await updateOp.ExecuteAsync(connection, output, errorHandler, generator);

        var deleteOp = new DeleteManyDemoOperation();
        await deleteOp.ExecuteAsync(connection, output, errorHandler, generator);
    }
    // Unused legacy demo methods removed; implementations live in Services/Demos
}
