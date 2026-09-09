-- SQL Server schema initialization for DapperMany
-- This script creates the test database and tables

-- Create database
USE master;
GO

IF DB_ID('dappermany') IS NULL
BEGIN
    CREATE DATABASE [dappermany];
END
GO

USE [dappermany];
GO

-- Create Pedidos table
IF OBJECT_ID('dbo.Pedidos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos (
        Id INT PRIMARY KEY IDENTITY(1,1),
        NumeroDocumento NVARCHAR(50) NOT NULL UNIQUE,
        DataPedido DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ValorTotal DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pendente',
        Created DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Modified DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Create ItensPedido table
IF OBJECT_ID('dbo.ItensPedido', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido (
        Id INT PRIMARY KEY IDENTITY(1,1),
        PedidoId INT NOT NULL,
        Descricao NVARCHAR(255) NOT NULL,
        Quantidade INT NOT NULL DEFAULT 1,
        ValorUnitario DECIMAL(18, 2) NOT NULL,
        ValorTotal DECIMAL(18, 2) NOT NULL,
        Created DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Modified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos(Id) ON DELETE CASCADE
    );
END
GO

-- Create indexes for better query performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Pedidos') AND name = 'IX_Pedidos_NumeroDocumento')
BEGIN
    CREATE INDEX IX_Pedidos_NumeroDocumento ON dbo.Pedidos(NumeroDocumento);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItensPedido') AND name = 'IX_ItensPedido_PedidoId')
BEGIN
    CREATE INDEX IX_ItensPedido_PedidoId ON dbo.ItensPedido(PedidoId);
END
GO

PRINT 'SQL Server schema initialized successfully';
