using Dapper;
using DapperMany.Samples.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using MySqlConnector;
using DapperMany.Internal.Abstractions;

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

        // Ensure providers are registered (instantiate provider types via reflection if module initializers didn't run)
        try
        {
            void TryRegister(string providerName, string assemblyName, string dialectType, string bulkType, string identityType)
            {
                try
                {
                    var assemblyQualifiedDialect = dialectType + ", " + assemblyName;
                    var assemblyQualifiedBulk = bulkType + ", " + assemblyName;
                    var assemblyQualifiedIdentity = identityType + ", " + assemblyName;

                    var dt = Type.GetType(assemblyQualifiedDialect, throwOnError: false);
                    var bt = Type.GetType(assemblyQualifiedBulk, throwOnError: false);
                    var it = Type.GetType(assemblyQualifiedIdentity, throwOnError: false);

                    if (dt == null || bt == null || it == null)
                        return; // provider assembly not present or types not accessible

                    var dialect = (ISqlDialect?)Activator.CreateInstance(dt, nonPublic: true);
                    var bulk = (IBulkCopyStrategy?)Activator.CreateInstance(bt, nonPublic: true);
                    var identity = (IIdentityRetrievalStrategy?)Activator.CreateInstance(it, nonPublic: true);

                    if (dialect == null || bulk == null || identity == null) return;

                    ProviderRegistry.RegisterProvider(providerName, dialect, bulk, identity);
                }
                catch { }
            }

            TryRegister("PostgreSQL", "DapperMany.Postgres", "DapperMany.Postgres.PostgreSqlDialect", "DapperMany.Postgres.PostgreSqlBulkCopyStrategy", "DapperMany.Postgres.PostgreSqlIdentityRetrievalStrategy");
            TryRegister("MySQL", "DapperMany.MySql", "DapperMany.MySql.MySqlDialect", "DapperMany.MySql.MySqlBulkCopyStrategy", "DapperMany.MySql.MySqlIdentityRetrievalStrategy");
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

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
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

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
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

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
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
        var demoMenu = true;
        while (demoMenu)
        {
            Console.WriteLine($"\n  Operations:\n");
            Console.WriteLine("    1. Insert Multiple Orders");
            Console.WriteLine("    2. Insert Orders with Items (InsertManyGraph)");
            Console.WriteLine("    3. View Orders");
            Console.WriteLine("    4. Update Orders");
            Console.WriteLine("    5. Delete Orders");
            Console.WriteLine("    0. Back\n");
            Console.Write("    Choice: ");

            if (int.TryParse(Console.ReadLine(), out var choice))
            {
                switch (choice)
                {
                    case 1:
                        await RunInsertDemo(connection);
                        break;
                    case 2:
                        await RunInsertManyGraphDemo(connection);
                        break;
                    case 3:
                        await RunViewDemo(connection);
                        break;
                    case 4:
                        await RunUpdateManyDemo(connection);
                        break;
                    case 5:
                        await RunDeleteManyDemo(connection);
                        break;
                    case 0:
                        demoMenu = false;
                        break;
                    default:
                        Console.WriteLine("\n    Invalid choice. Try again.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("\n    Invalid input. Try again.");
            }
        }
    }

    static async Task RunInsertDemo(System.Data.IDbConnection connection)
    {
        Console.WriteLine("\n    ⏳ Inserting sample orders...\n");

        try
        {
            var orders = new List<Pedido>
            {
                new Pedido
                {
                    NumeroDocumento = $"PED-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    DataPedido = DateTime.UtcNow,
                    ValorTotal = 1500.00m,
                    Status = "Pendente"
                },
                new Pedido
                {
                    NumeroDocumento = $"PED-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    DataPedido = DateTime.UtcNow,
                    ValorTotal = 2500.00m,
                    Status = "Processado"
                }
            };

            var rowsInserted = await connection.InsertManyAsync(orders);
            Console.WriteLine($"    ✓ Inserted {rowsInserted} order(s)\n");
            
            foreach (var order in orders)
            {
                Console.WriteLine($"    - {order.NumeroDocumento}");
                Console.WriteLine($"      Status: {order.Status}, Total: ${order.ValorTotal}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Insert failed: {ex.Message}");
        }
    }

    static async Task RunViewDemo(System.Data.IDbConnection connection)
    {
        Console.WriteLine("\n    ⏳ Querying orders...\n");

        try
        {
            var orders = await connection.QueryAsync<Pedido>("SELECT Top 10 * FROM Pedidos ORDER BY Created DESC;");
            var orderList = orders.ToList();

            if (orderList.Count == 0)
            {
                Console.WriteLine("    (No orders found)");
            }
            else
            {
                Console.WriteLine($"    Found {orderList.Count} order(s):\n");
                foreach (var order in orderList)
                {
                    Console.WriteLine($"    - [{order.Id}] {order.NumeroDocumento}");
                    Console.WriteLine($"      Status: {order.Status}, Total: ${order.ValorTotal}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Query failed: {ex.Message}");
        }
    }

    static async Task RunInsertManyGraphDemo(System.Data.IDbConnection connection)
    {
        Console.WriteLine("\n    ⏳ Inserting orders WITH items (graph insert)...\n");

        try
        {
            // Create orders with nested items - FK will be auto-populated
            var orders = new List<Pedido>
            {
                new Pedido
                {
                    NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    DataPedido = DateTime.UtcNow,
                    ValorTotal = 1200.00m,
                    Status = "Pendente",
                    Itens = new()
                    {
                        new ItemPedido { Descricao = "Laptop 15\"", Quantidade = 1, ValorUnitario = 800.00m },
                        new ItemPedido { Descricao = "Mouse Wireless", Quantidade = 2, ValorUnitario = 50.00m },
                        new ItemPedido { Descricao = "USB-C Cable", Quantidade = 3, ValorUnitario = 15.00m }
                    }
                },
                new Pedido
                {
                    NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    DataPedido = DateTime.UtcNow,
                    ValorTotal = 500.00m,
                    Status = "Processado",
                    Itens = new()
                    {
                        new ItemPedido { Descricao = "Mechanical Keyboard", Quantidade = 1, ValorUnitario = 500.00m }
                    }
                },
                new Pedido
                {
                    NumeroDocumento = $"PED-GRAPH-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    DataPedido = DateTime.UtcNow,
                    ValorTotal = 0.00m,
                    Status = "Cancelado",
                    Itens = new() // Empty - no items
                }
            };

            // Insert all orders and their items in one operation
            // Foreign keys are auto-populated by DapperMany
            int insertedCount = await connection.InsertManyGraphAsync(orders);

            Console.WriteLine($"    ✓ Successfully inserted {insertedCount} order(s) with items\n");
            Console.WriteLine("    Details:");
            foreach (var order in orders)
            {
                Console.WriteLine($"    - Order: {order.NumeroDocumento}");
                Console.WriteLine($"      Items: {order.Itens.Count}");
                foreach (var item in order.Itens)
                {
                    Console.WriteLine($"        • {item.Descricao} (qty: {item.Quantidade}, unit price: ${item.ValorUnitario})");
                }
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Graph insert failed: {ex.Message}");
        }
    }

    static async Task RunUpdateManyDemo(System.Data.IDbConnection connection)
    {
        Console.WriteLine("\n    ⏳ Updating orders...\n");

        try
        {
            // First, get existing orders
            var existingOrders = connection.Query<Pedido>(
                "SELECT TOP 3 * FROM Pedidos ORDER BY Id DESC");

            var ordersToUpdate = existingOrders.ToList();
            if (ordersToUpdate.Count == 0)
            {
                Console.WriteLine("    ℹ  No orders found to update. Insert some orders first.\n");
                return;
            }

            // Update the orders - change status
            foreach (var order in ordersToUpdate)
            {
                order.Status = order.Status switch
                {
                    "Pendente" => "Processado",
                    "Processado" => "Entregue",
                    "Entregue" => "Pendente",
                    _ => "Processado"
                };
                order.Modified = DateTime.UtcNow;
            }

            var updatedCount = await connection.UpdateManyAsync(ordersToUpdate);

            Console.WriteLine($"    ✓ Updated {updatedCount} orders successfully");
            Console.WriteLine("\n    Updated orders:");
            foreach (var order in ordersToUpdate)
            {
                Console.WriteLine($"      • Order {order.NumeroDocumento}: {order.Status}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Update failed: {ex.Message}");
        }
    }

    static async Task RunDeleteManyDemo(System.Data.IDbConnection connection)
    {
        Console.WriteLine("\n    ⏳ Deleting orders...\n");

        try
        {
            // Get the oldest orders for deletion
            var ordersToDelete = connection.Query<Pedido>(
                "SELECT TOP 2 * FROM Pedidos ORDER BY Id ASC").ToList();

            if (ordersToDelete.Count == 0)
            {
                Console.WriteLine("    ℹ  No orders found to delete.\n");
                return;
            }

            var deleteCount = await connection.DeleteManyAsync(ordersToDelete);

            Console.WriteLine($"    ✓ Deleted {deleteCount} orders successfully");
            Console.WriteLine("\n    Deleted orders:");
            foreach (var order in ordersToDelete)
            {
                Console.WriteLine($"      • Order {order.NumeroDocumento}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Delete failed: {ex.Message}");
        }
    }
}
