-- PostgreSQL schema initialization for DapperMany
-- This script creates tables in the dappermany database

-- Create Pedidos table
CREATE TABLE IF NOT EXISTS public.pedidos (
    id SERIAL PRIMARY KEY,
    numero_documento VARCHAR(50) NOT NULL UNIQUE,
    data_pedido TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    valor_total NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    status VARCHAR(50) NOT NULL DEFAULT 'Pendente',
    created TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create ItensPedido table
CREATE TABLE IF NOT EXISTS public.itens_pedido (
    id SERIAL PRIMARY KEY,
    pedido_id INTEGER NOT NULL,
    descricao VARCHAR(255) NOT NULL,
    quantidade INTEGER NOT NULL DEFAULT 1,
    valor_unitario NUMERIC(18, 2) NOT NULL,
    valor_total NUMERIC(18, 2) NOT NULL,
    created TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_itens_pedido_pedidos FOREIGN KEY (pedido_id) REFERENCES public.pedidos(id) ON DELETE CASCADE
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_pedidos_numero_documento ON public.pedidos(numero_documento);
CREATE INDEX IF NOT EXISTS idx_itens_pedido_pedido_id ON public.itens_pedido(pedido_id);

-- Grant permissions to postgres user
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

-- Print initialization message
DO $$
BEGIN
    RAISE NOTICE 'PostgreSQL schema initialized successfully';
END $$;
