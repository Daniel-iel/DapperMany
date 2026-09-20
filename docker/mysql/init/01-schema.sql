-- MySQL schema initialization for DapperMany
-- This script creates tables in the dappermany database

USE dappermany;

-- Create Pedidos table
CREATE TABLE IF NOT EXISTS Pedidos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NumeroDocumento VARCHAR(50) NOT NULL UNIQUE,
    DataPedido DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ValorTotal DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    Status VARCHAR(50) NOT NULL DEFAULT 'Pendente',
    Created DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create ItensPedido table
CREATE TABLE IF NOT EXISTS ItensPedido (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PedidoId INT NOT NULL,
    Descricao VARCHAR(255) NOT NULL,
    Quantidade INT NOT NULL DEFAULT 1,
    ValorUnitario DECIMAL(18, 2) NOT NULL,
    ValorTotal DECIMAL(18, 2) NOT NULL,
    Created DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE
);

-- Create indexes for better query performance
CREATE INDEX IX_Pedidos_NumeroDocumento ON Pedidos(NumeroDocumento);
CREATE INDEX IX_ItensPedido_PedidoId ON ItensPedido(PedidoId);

-- Set default character set and collation
ALTER TABLE Pedidos CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE ItensPedido CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

SELECT 'MySQL schema initialized successfully' AS message;
