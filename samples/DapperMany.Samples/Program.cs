using DapperMany.MySql;
using DapperMany.Postgres;
using DapperMany.Samples.Data;
using DapperMany.Samples.Infrastructure.Connections;
using DapperMany.Samples.Infrastructure.Error;
using DapperMany.Samples.Infrastructure.Logging;
using DapperMany.Samples.Services.Demos;
using DapperMany.SqlServer;
using Microsoft.Extensions.Configuration;

namespace DapperMany.Samples;

class Program
{
    static async Task Main(string[] args)
    {
        var logger = new SampleLogger();

        logger.Header(@"
╔════════════════════════════════════════════════╗
║         DapperMany - Bulk Operations Demo      ║
╚════════════════════════════════════════════════╝
");

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
            logger.Info(@"
Select a database provider:
   1. SQL Server
   2. PostgreSQL
   3. MySQL
   0. Exit");
            var input = logger.PromptInline("Choice: ");

            if (int.TryParse(input, out var choice))
            {
                switch (choice)
                {
                    case 1:
                        {
                            var batchInput = logger.PromptInline("Bulk batch size (default 1500): ");
                            if (!int.TryParse(batchInput, out var batchSize) || batchSize <= 0)
                                batchSize = 1500;
                            await RunSqlServerDemo(config, logger, batchSize);
                            break;
                        }
                    case 2:
                        {
                            var batchInput = logger.PromptInline("Bulk batch size (default 1500): ");
                            if (!int.TryParse(batchInput, out var batchSize) || batchSize <= 0)
                                batchSize = 1500;
                            await RunPostgresDemo(config, logger, batchSize);
                            break;
                        }
                    case 3:
                        {
                            var batchInput = logger.PromptInline("Bulk batch size (default 1500): ");
                            if (!int.TryParse(batchInput, out var batchSize) || batchSize <= 0)
                                batchSize = 1500;
                            await RunMySqlDemo(config, logger, batchSize);
                            break;
                        }
                    case 0:
                        showMenu = false;
                        logger.Info("Goodbye!");
                        break;
                    default:
                        logger.Info(@"Invalid choice. Try again.");
                        break;
                }
            }
            else
            {
                logger.Info(@"Invalid input. Try again.");
            }
        }
    }

    static async Task RunSqlServerDemo(IConfiguration config, SampleLogger logger, int batchSize)
    {
        logger.Header(@"###### SQL Server Demo ######");

        var connectionString = config.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("SQL Server connection string not found");

        var factory = new SqlServerConnectionFactory();

        try
        {
            using var connection = factory.Create(connectionString);
            await ((System.Data.Common.DbConnection)connection).OpenAsync();

            logger.Success("Connected to SQL Server");

            await ShowProviderDemo(connection, logger, batchSize);
        }
        catch (Exception ex)
        {
            logger.Error($"Error: {ex.Message}");
            logger.Info($"Make sure SQL Server is running and accessible");
            logger.Info($"Connection: {connectionString}");
        }
    }

    static async Task RunPostgresDemo(IConfiguration config, SampleLogger logger, int batchSize)
    {
        logger.Header(@"###### PostgreSQL Demo ######");

        var connectionString = config.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("PostgreSQL connection string not found");

        var factory = new PostgresConnectionFactory();

        try
        {
            using var connection = factory.Create(connectionString);
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            logger.Success("Connected to PostgreSQL");

            await ShowProviderDemo(connection, logger, batchSize);
        }
        catch (Exception ex)
        {
            logger.Error($"Error: {ex.Message}");
            logger.Info($"Make sure PostgreSQL is running and accessible");
            logger.Info($"Connection: {connectionString}");
        }
    }

    static async Task RunMySqlDemo(IConfiguration config, SampleLogger logger, int batchSize)
    {
        logger.Header(@"###### MySQL Demo ######");

        var connectionString = config.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException("MySQL connection string not found");

        var factory = new MySqlConnectionFactory();

        try
        {
            using var connection = factory.Create(connectionString);
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            logger.Success("Connected to MySQL");

            await ShowProviderDemo(connection, logger, batchSize);
        }
        catch (Exception ex)
        {
            logger.Error($"Error: {ex.Message}");
            logger.Info($"Make sure MySQL is running and accessible");
            logger.Info($"Connection: {connectionString}");
        }
    }

    static async Task ShowProviderDemo(System.Data.IDbConnection connection, SampleLogger logger, int batchSize)
    {
        // Create local infrastructure components (no DI in samples by design)
        var output = logger.Formatter;
        var errorHandler = new ConsoleErrorHandler(output);
        var generator = new RandomPedidoGenerator();

        // Use the new operation for InsertMany (refactored)
        var insertOp = new InsertManyDemoOperation(batchSize);
        await insertOp.ExecuteAsync(connection, output, errorHandler, generator);

        // Execute other demos via refactored operations
        var graphOp = new InsertGraphDemoOperation(batchSize);
        await graphOp.ExecuteAsync(connection, output, errorHandler, generator);

        var queryOp = new QueryDemoOperation();
        await queryOp.ExecuteAsync(connection, output, errorHandler, generator);

        var updateOp = new UpdateManyDemoOperation(batchSize);
        await updateOp.ExecuteAsync(connection, output, errorHandler, generator);

        var deleteOp = new DeleteManyDemoOperation(batchSize);
        await deleteOp.ExecuteAsync(connection, output, errorHandler, generator);
    }
}
