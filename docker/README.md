# DapperMany Docker Environment

This Docker setup provides containerized databases for DapperMany integration testing with three providers: SQL Server, PostgreSQL, and MySQL.

## Prerequisites

- Docker Desktop (or Docker Engine + Docker Compose)
- Minimum 6GB available disk space
- Ports 1433, 5432, 3306 available on your machine

## Quick Start

### Start All Databases

```bash
# Navigate to the docker directory
cd docker

# Start all services
docker compose up -d

# Wait for databases to be healthy (30-60 seconds)
docker compose ps
```

Check the STATUS column:
- ✅ All services should show `(healthy)` after initialization
- 🟨 If `(starting)` or `(unhealthy)`, wait a few seconds and check again

### Stop All Databases

```bash
docker compose down
```

### Clean Up (Remove Data Volumes)

```bash
docker compose down -v
```

## Database Connection Strings

### SQL Server
```
Server=localhost,1433;Database=dappermany;User Id=sa;Password=SqlServer123!;Encrypt=false;
```

### PostgreSQL
```
User ID=postgres;Password=Postgres123!;Host=localhost;Port=5432;Database=dappermany;
```

### MySQL
```
Server=localhost;Port=3306;Database=dappermany;Uid=root;Pwd=MySql123!;
```

## Service Details

| Database | Version | Port | Username | Password | Database |
|----------|---------|------|----------|----------|----------|
| SQL Server | 2019 | 1433 | sa | SqlServer123! | dappermany |
| PostgreSQL | 14 | 5432 | postgres | Postgres123! | dappermany |
| MySQL | 8.0 | 3306 | root | MySql123! | dappermany |

## Schema

All databases are initialized with:
- **Pedidos** table: Contains order headers
  - Id (Primary Key, Auto-increment)
  - NumeroDocumento (Unique string identifier)
  - DataPedido (Timestamp)
  - ValorTotal (Decimal)
  - Status (String: Pendente, Processado, etc.)
  - Created, Modified timestamps

- **ItensPedido** table: Contains order items
  - Id (Primary Key, Auto-increment)
  - PedidoId (Foreign Key to Pedidos)
  - Descricao (Item description)
  - Quantidade (Quantity)
  - ValorUnitario (Unit price)
  - ValorTotal (Total price)
  - Created, Modified timestamps

## Testing Connectivity

### SQL Server
```powershell
docker exec dappermany-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SqlServer123! -Q "SELECT @@VERSION;"
```

### PostgreSQL
```bash
docker exec dappermany-postgres psql -U postgres -d dappermany -c "SELECT version();"
```

### MySQL
```bash
docker exec dappermany-mysql mysql -u root -pMySql123! -e "SELECT VERSION();"
```

## Integration Tests

The project includes integration tests that will:
1. Start Docker containers automatically (if Testcontainers is configured)
2. Run the schema initialization scripts
3. Execute DapperMany operations (InsertMany, UpdateMany, DeleteMany, etc.)
4. Clean up after tests complete

## Troubleshooting

### Databases won't start
1. Check if ports 1433, 5432, 3306 are already in use
2. Ensure you have enough disk space
3. Review Docker logs: `docker compose logs`

### Schema not initialized
1. Check init script files in `sqlserver/init/`, `postgres/init/`, `mysql/init/`
2. Verify proper file permissions
3. Restart containers: `docker compose restart`

### Connection failures
1. Verify containers are running: `docker compose ps`
2. Check container health: `docker compose ps` (should show `(healthy)`)
3. Wait 30-60 seconds for full initialization
4. Verify credentials match connection strings

### Performance slow
1. Check available disk space
2. Check Docker resource limits in Docker Desktop settings
3. Consider running only the database you need

## Customization

### Change credentials
Edit `docker-compose.yml` and modify:
- `MYSQL_ROOT_PASSWORD`
- `SA_PASSWORD` (SQL Server)
- `POSTGRES_PASSWORD`

### Persistent data
Data is stored in Docker volumes:
- `sqlserver-data`
- `postgres-data`
- `mysql-data`

Remove with: `docker compose down -v`

### Add more initialization scripts
Place `.sql` files in:
- `sqlserver/init/`
- `postgres/init/`
- `mysql/init/`

They will execute in alphabetical order during container startup.

## Performance Notes

- **First start**: 30-60 seconds for full initialization
- **Subsequent starts**: 10-20 seconds
- **Memory usage**: ~2GB total for all three databases
- **Disk usage**: ~3GB for data volumes

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [SQL Server Docker Images](https://mcr.microsoft.com/en-us/product/mssql/server/about)
- [PostgreSQL Docker Images](https://hub.docker.com/_/postgres)
- [MySQL Docker Images](https://hub.docker.com/_/mysql)
