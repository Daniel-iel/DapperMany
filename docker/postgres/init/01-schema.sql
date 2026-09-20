-- PostgreSQL schema initialization for DapperMany
-- This script creates tables in the dappermany database

-- Create Pedidos table
CREATE TABLE IF NOT EXISTS "Pedidos" (
    "Id" SERIAL PRIMARY KEY,
    "NumeroDocumento" VARCHAR(50) NOT NULL UNIQUE,
    "DataPedido" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ValorTotal" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pendente',
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create ItensPedido table
CREATE TABLE IF NOT EXISTS "ItensPedido" (
    "Id" SERIAL PRIMARY KEY,
    "PedidoId" INTEGER NOT NULL,
    "Descricao" VARCHAR(255) NOT NULL,
    "Quantidade" INTEGER NOT NULL DEFAULT 1,
    "ValorUnitario" NUMERIC(18, 2) NOT NULL,
    "ValorTotal" NUMERIC(18, 2) NOT NULL,
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_ItensPedido_Pedidos" FOREIGN KEY ("PedidoId") REFERENCES "Pedidos"("Id") ON DELETE CASCADE
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS "IX_Pedidos_NumeroDocumento" ON "Pedidos"("NumeroDocumento");
CREATE INDEX IF NOT EXISTS "IX_ItensPedido_PedidoId" ON "ItensPedido"("PedidoId");

-- Grant permissions to postgres user
GRANT ALL PRIVILEGES ON "Pedidos" TO postgres;
GRANT ALL PRIVILEGES ON "ItensPedido" TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

-- Print initialization message
DO $$
BEGIN
    RAISE NOTICE 'PostgreSQL schema initialized successfully';
END $$;
