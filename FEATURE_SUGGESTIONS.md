# 🚀 10 Feature Suggestions for DapperMany

## Overview
Estas sugestões levam em conta o estado atual do projeto (Fases 1-5 completas com InsertMany, UpdateMany, DeleteMany) e apontam para melhorias estratégicas que aumentarão adoção, performance e experiência do desenvolvedor.

---

## 1. **Batch Merge/Upsert Operations** ⭐ ALTA PRIORIDADE

### Descrição
Implementar um método `MergeManyAsync<T>()` que insere registros novos e atualiza existentes em uma única operação, baseado em chave primária ou chaves compostas.

### Benefício
- **Caso de uso comum**: Sincronização de dados de APIs externas, feeds, ou sistemas legados
- **Performance**: Evita lógica cliente (Select + Insert + Update separados)
- **Simplifica código**: 1 chamada em vez de múltiplas transações

### Exemplo
```csharp
var pedidos = new List<Pedido> { /* mix de novos e existentes */ };
int inserted, updated = await connection.MergeManyAsync(pedidos, 
    mergeKey: p => p.Id);
// Retorna count de inserted + updated
```

### Tecnicamente
- Usar SQL MERGE (SQL Server/PostgreSQL) ou INSERT...ON DUPLICATE KEY (MySQL)
- Manter transação atômica
- Reuse da infraestrutura existente (EntityMetadata, Dialects)

---

## 2. **Soft Delete Support** ⭐ ALTA PRIORIDADE

### Descrição
Adicionar suporte a soft deletes via atributo `[SoftDelete]` que marca coluna de timestamp/flag de deleção em vez de excluir fisicamente.

### Benefício
- **Auditoria & compliance**: Dados nunca são perdidos, apenas marcados
- **Recuperação**: Poder "undelete" registros
- **Histórico**: Manter rastreabilidade completa

### Exemplo
```csharp
[Table("Pedidos")]
public class Pedido
{
    [Key]
    public int Id { get; set; }
    
    [SoftDelete] // Auto-populated com DateTime.UtcNow em DeleteMany
    public DateTime? DeletadoEm { get; set; }
}

await connection.DeleteManyAsync(pedidoIds); 
// UPDATE Pedidos SET DeletadoEm = GETUTCDATE() WHERE Id IN (...)
```

### Tecnicamente
- Adicionar atributo `[SoftDelete]` em DapperMany.Attributes
- Modificar EntityMetadata para detectar soft-delete column
- Atualizar IBulkCopyStrategy para UPDATE em vez de DELETE quando SoftDelete presente
- Adicionar `DeleteManyPhysicalAsync<T>()` para hard delete explícito

---

## 3. **Batch Change Tracking / Change Sets** ⭐ MÉDIA PRIORIDADE

### Descrição
Retornar metadados sobre mudanças: quantas linhas foram afetadas por operação, quais IDs foram gerados, mapeamento parent→child.

### Benefício
- **Debugging**: Saber exatamente quais registros foram inseridos/atualizados/deletados
- **Webhooks/Events**: Disparar eventos apenas para registros realmente afetados
- **Reconciliação**: Validar e reconciliar com sistemas externos

### Exemplo
```csharp
var result = await connection.InsertManyAsync(pedidos);
// Retorna:
// result.RowsInserted = 5
// result.RowsUpdated = 0
// result.GeneratedIds = [101, 102, 103, 104, 105]
// result.RelatedEntities = { ItemPedido: 12 }
// result.Duration = TimeSpan.FromMilliseconds(42)
```

### Tecnicamente
- Criar record `BulkOperationResult<T>` com campos de telemetria
- Atualizar assinatura de métodos para retornar `BulkOperationResult<T>` em vez de `int`
- Breaking change, mas com alto valor

---

## 4. **Batch Filtering & Validation Before Execution** ⭐ MÉDIA PRIORIDADE

### Descrição
Adicionar pipeline de validação/filtro que rode antes de qualquer operação de bulk, permitindo rejeitar/alertar sobre dados problemáticos.

### Benefício
- **Data quality**: Garantir integridade antes de ir ao banco
- **Feedback rico**: Retornar lista de erros com índices exatos no batch
- **Auditoria**: Registrar rejeições em log estruturado

### Exemplo
```csharp
var validator = new PedidoValidator(); // FluentValidation

var result = await connection.InsertManyAsync(pedidos,
    preExecutionValidator: (entity, index) =>
    {
        var validation = validator.Validate(entity);
        if (!validation.IsValid)
            throw new BatchValidationException(index, validation.Errors);
    });
```

### Tecnicamente
- Adicionar parâmetro opcional `Func<T, int, Task>` em extensões
- Executar loop serial antes de bulk operation
- Retornar `BulkValidationResult` com lista de rejeitados (índices + erros)

---

## 5. **Composite Key / Natural Key Support** ⭐ MÉDIA PRIORIDADE

### Descrição
Suporte a chaves compostas (multi-column) para operações de Update/Delete sem requerer chave substituta em [Key].

### Benefício
- **Modelos de negócio**: Muitos domínios usam chaves compostas naturais (Tenant + Entity, ou Date + Account)
- **Sem surrogates desnecessários**: Economizar coluna de ID artificial
- **Integridade**: Queries naturalmente mais seguras

### Exemplo
```csharp
[Table("PedidosPorTenant")]
public class TenantPedido
{
    [Key] public int TenantId { get; set; }
    [Key] public string NumeroDocumento { get; set; }
    
    public DateTime DataPedido { get; set; }
}

await connection.UpdateManyAsync(pedidos); // WHERE TenantId = ? AND NumeroDocumento = ?
```

### Tecnicamente
- Modificar EntityMetadata para suportar `IEnumerable<PropertyAccessor>` em vez de único Key
- Atualizar SQL dialects para WHERE clauses multi-coluna
- Adicionar validação: composite key não permitido com [DatabaseGenerated(Identity)]

---

## 6. **Retry & Resilience Policies (Polly Integration)** ⭐ MÉDIA PRIORIDADE

### Descrição
Integração com Polly para retry automático em caso de falhas transientes (connection timeout, deadlock).

### Benefício
- **Production-ready**: Lidar com falhas de rede/banco transientes sem código cliente
- **Backoff inteligente**: Exponential backoff, jitter automático
- **Monitoramento**: Logging de tentativas e sucesso eventual

### Exemplo
```csharp
var policy = Policy
    .Handle<SqlException>(ex => ex.Number == -2)  // Connection timeout
    .Or<TimeoutException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));

await connection.InsertManyAsync(pedidos, retryPolicy: policy);
```

### Tecnicamente
- Adicionar parâmetro `IAsyncPolicy?` em métodos de extensão
- Wrappear chamadas de bulk operation com `policy.ExecuteAsync(...)`
- Zero breaking change se optional

---

## 7. **Batch Streaming / Cursor-based Insertion** ⭐ BAIXA PRIORIDADE

### Descrição
Suporte a inserção/update via `IAsyncEnumerable<T>` em vez de `IEnumerable<T>` para casos com muitos dados (1M+ rows) ou dados vindo de stream (API, arquivo).

### Benefício
- **Memory efficiency**: Não carregar todos os dados em memória
- **Streaming de dados**: Integrar com HttpClient, arquivos grandes, etc.
- **Backpressure**: Controlar velocidade de processamento

### Exemplo
```csharp
async IAsyncEnumerable<Pedido> StreamPedidosFromApi()
{
    var response = await httpClient.GetAsync("https://api.example.com/pedidos");
    using var stream = await response.Content.ReadAsStreamAsync();
    await foreach (var pedido in JsonAsyncEnumerable<Pedido>(stream))
        yield return pedido;
}

await connection.InsertManyAsync(StreamPedidosFromApi(), batchSize: 1000);
```

### Tecnicamente
- Aceitar `IAsyncEnumerable<T>` em overload
- Acumular items até atingir batchSize
- Executar bulk insert quando batchSize atingido
- Flush final ao fim do stream

---

## 8. **Performance Profiling & Built-in Diagnostics** ⭐ BAIXA PRIORIDADE

## 9. **Column Projection / Selective Upsert** ⭐ BAIXA PRIORIDADE

### Descrição
Permitir atualizar apenas colunas específicas (projection) em vez de todas as propriedades do objeto.

### Benefício
- **Segurança**: Evitar sobrescrever colunas sensíveis (LastModifiedBy, Timestamp)
- **Performance**: Atualizar apenas colunas que mudam
- **Auditoria**: Rastrear quais campos foram alterados

### Exemplo
```csharp
var updates = new List<Pedido> { /* parciais */ };

await connection.UpdateManyAsync(updates,
    projection: p => new { p.Status, p.ValorTotal });
// UPDATE Pedidos SET Status = ?, ValorTotal = ? WHERE Id = ?
// (DeletadoEm, DataPedido, etc. NOT updated)
```

### Tecnicamente
- Aceitar `Expression<Func<T, object>>` para definir projeção
- Compilar expression e extrair property names
- Gerar SQL dinâmico apenas com colunas projetadas
- Usar `MemberExpression` visitor para type-safety

---

## 10. **Multi-Database Transaction Coordination** ⭐ BAIXA PRIORIDADE

## 11. **Batch Export to CSV/Excel/JSON** ⭐ MÉDIA PRIORIDADE

### Descrição
Adicionar métodos de extensão para exportar resultados de operações bulk diretamente para formatos populares sem materializar em memória.

### Benefício
- **Relatórios**: Exportar dados inseridos/atualizados para compartilhar
- **Integração**: Gerar arquivos compatíveis com sistemas legados
- **Memory efficiency**: Streaming direto para arquivo, não carrega tudo na RAM

### Exemplo
```csharp
var result = await connection.InsertManyAsync(pedidos);

await result.ExportToCsvAsync("pedidos.csv", 
    columns: p => new { p.Id, p.NumeroDocumento, p.ValorTotal });

await result.ExportToJsonAsync("pedidos.json");

await result.ExportToExcelAsync("pedidos.xlsx");
```

### Tecnicamente
- Adicionar métodos `ExportToCsvAsync`, `ExportToJsonAsync`, `ExportToExcelAsync`
- Reuse da `BulkOperationResult<T>` para dados processados
- Usar bibliotecas lightweight (CsvHelper, EPPlus, System.Text.Json)

---

## 12. **Batch Deduplication / Distinct by Key** ⭐ MÉDIA PRIORIDADE

### Descrição
Detectar e remover duplicatas no batch antes de inserção, opcionalmente mantendo estatísticas de deduplicação.

### Benefício
- **Data quality**: Evitar violações de constraint UNIQUE no banco
- **Eficiência**: Reduzir volume real inserido
- **Auditoria**: Rastrear quantas duplicatas foram encontradas

### Exemplo
```csharp
var pedidos = new List<Pedido> { /* com duplicatas */ };

var result = await connection.InsertManyAsync(pedidos,
    deduplicateBy: p => p.NumeroDocumento);
// result.DeduplicatedCount = 3
// result.ActualInserted = 97 (de 100)

Console.WriteLine($"Removidas {result.DeduplicatedCount} duplicatas");
```

### Tecnicamente
- Adicionar parâmetro `Func<T, object>` para chave de deduplicação
- Usar HashSet<T> antes de bulk insert
- Opcional: manter mapping de índices originais para logging

---

## 13. **Batch Transformation / Mapping Pipeline** ⭐ MÉDIA PRIORIDADE

### Descrição
Adicionar pipeline de transformação que roda entre leitura e inserção (ex: normalizar strings, converter enums, calcular campos derivados).

### Benefício
- **Data transformation**: Limpar/normalizar dados de fontes sujas
- **Enriquecimento**: Calcular campos derivados (preço total, desconto aplicado)
- **Validação**: Rejeitar linhas que falham em transformação

### Exemplo
```csharp
var pedidos = new List<Pedido> { /* brutos */ };

var result = await connection.InsertManyAsync(pedidos,
    transformBefore: (pedido, index) =>
    {
        pedido.NumeroDocumento = pedido.NumeroDocumento?.Trim().ToUpper();
        pedido.Status ??= "Pendente";
        pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        return pedido;
    });
```

### Tecnicamente
- Adicionar parâmetro `Func<T, int, T>` para transformação
- Executar antes de deduplicação/validação
- Permitir exception handling por índice

---

## 14. **Batch Watermarking / Incremental Sync** ⭐ MÉDIA PRIORIDADE

### Descrição
Suportar sincronização incremental rastreando "watermark" (last_modified, sequence) para carregar apenas registros novos/modificados.

### Benefício
- **Eficiência**: Não carregar tudo toda vez
- **Sync incremental**: Ideal para integrações CDCs (Change Data Capture)
- **Auditoria**: Timestamp de última sincronização persistido

### Exemplo
```csharp
var lastSync = await connection.GetLastSyncWatermarkAsync<Pedido>("pedidos_sync");
// 2026-09-16 10:30:00

var newPedidos = await externalApi.GetPedidosSince(lastSync);
var result = await connection.InsertManyAsync(newPedidos);

await connection.SetSyncWatermarkAsync<Pedido>("pedidos_sync", DateTime.UtcNow);
```

### Tecnicamente
- Criar tabela `DapperManySyncMetadata` (EntityName, Watermark, LastSyncTime)
- Métodos `GetLastSyncWatermarkAsync<T>`, `SetSyncWatermarkAsync<T>`
- Integração opcional com entidades que têm `[LastModified]` column

---

## 15. **Batch Conditional Insert / Upsert with Rules** ⭐ BAIXA PRIORIDADE

### Descrição
Permitir regras condicionais complexas na inserção (ex: "insira se não existir, mas atualize Status se existir, deixe ValorTotal inalterado").

### Benefício
- **Lógica flexível**: Não é tudo-ou-nada insert/update
- **Business rules**: Regras precisas de merge/reconciliação
- **Evitar procedimento**: Não precisa de stored procedure

### Exemplo
```csharp
var result = await connection.InsertManyAsync(pedidos,
    mergeRules: new MergeRule<Pedido>()
    {
        OnMatch = p => new Pedido { Status = "Atualizado", DataModificacao = DateTime.UtcNow },
        PreserveColumns = p => new { p.ValorTotal, p.DataCriacao },
        OnInsert = p => { p.Status ??= "Novo"; }
    });
```

### Tecnicamente
- Criar record `MergeRule<T>` com delegates para OnMatch, OnInsert, PreserveColumns
- Compilar regras em SQL MERGE statement
- Suportar SQL Server, PostgreSQL MERGE, MySQL CASE statements

---

## 16. **Batch Dependency Resolution / Topological Sort** ⭐ BAIXA PRIORIDADE

### Descrição
Detectar e respeitar dependências entre entidades no batch (ex: Pedido → ItemPedido → NfeItem) e ordenar inserções corretamente.

### Benefício
- **Autorelacionamentos**: Lidar com auto-referências (Employee.ManagerId)
- **Múltiplos grafos**: Inserir árvores complexas em ordem topológica
- **Sem erro de FK**: Respeita constraints sem reordenação manual

### Exemplo
```csharp
var allEntities = new List<object> { pedidos, itens, nfeItems, departamentos };

var result = await connection.InsertManyAsync(allEntities,
    autoResolveDependencies: true);
// Detecta: Departamento → Pedido → ItemPedido → NfeItem
// Insere na ordem correta automaticamente
```

### Tecnicamente
- Usar reflection para mapear [ForeignKey] attributes entre tipos
- Implementar topological sort (DFS) nos tipos
- Validar ciclos; lançar exception se houver ciclo

---

## 17. **Batch Error Recovery / Partial Commit** ⭐ BAIXA PRIORIDADE

### Descrição
Permitir que operações de bulk continuem mesmo após falhas em alguns registros (modo "best-effort"), retornando lista de sucessos/falhas.

### Benefício
- **Resiliência**: 95 de 100 registros inserem, 5 falham (não rejeita tudo)
- **Debugging**: Saber exatamente quais registros falharam e por quê
- **Retry lógico**: Retentar só os falhos depois

### Exemplo
```csharp
var result = await connection.InsertManyAsync(pedidos,
    continueOnError: true,
    onError: (entity, ex, index) => 
        logger.LogWarning($"Erro na linha {index}: {ex.Message}"));

Console.WriteLine($"Sucesso: {result.Successful.Count}");
Console.WriteLine($"Falhas: {result.Failed.Count}");

// Retentar falhos
var retryResult = await connection.InsertManyAsync(result.Failed.Select(f => f.Entity));
```

### Tecnicamente
- Adicionar classe `PartialCommitResult<T>` com Successful/Failed lists
- Modificar BulkCopyStrategy para capturar exceções por linha
- Manter transação consistente (inserir grupos de sucesso, sem os falhados)

---

## 18. **Audit Trail / Auto-logging of All Changes** ⭐ MÉDIA PRIORIDADE

### Descrição
Manter trilha de auditoria automática: quem, quando, o quê foi modificado (INSERT/UPDATE/DELETE).

### Benefício
- **Compliance**: LGPD, SOX, HIPAA requerem auditoria
- **Rastreabilidade**: Saber exatamente quem inseriu/alterou cada registro
- **Investigação**: Histórico completo de mudanças

### Exemplo
```csharp
[Table("Pedidos")]
[AuditTable("PedidosAudit")] // Auto-create audit table
public class Pedido
{
    [Key] public int Id { get; set; }
    public string NumeroDocumento { get; set; }
    public decimal ValorTotal { get; set; }
}

var result = await connection.InsertManyAsync(pedidos, 
    auditUserId: currentUser.Id,
    auditMetadata: new { IP = Request.RemoteIp, SessionId = sessionId });

// PedidosAudit recebe automaticamente:
// (Id, AuditAction='INSERT', AuditUser='alice', AuditTime=2026-09-16..., AuditMetadata='{...}', ...)
```

### Tecnicamente
- Adicionar atributo `[AuditTable]` para nomear tabela de audit
- Criar schema de PedidoAudit em tempo de operação se não existir
- Inserir em paralelo: Pedidos + PedidosAudit em mesma transação

---

## 19. **Batch Query Result Caching** ⭐ BAIXA PRIORIDADE

### Descrição
Cache automático de resultados de bulk operations (generados IDs, counts) em cache distribuído (Redis, AppFabric).

### Benefício
- **Performance**: Evitar queries repetidas em curto prazo
- **Distribuído**: Sincronizar cache entre múltiplos workers
- **TTL control**: Expiração automática de cache stale

### Exemplo
```csharp
var result = await connection.InsertManyAsync(pedidos,
    resultCache: new RedisCacheProvider("localhost:6379", ttl: TimeSpan.FromMinutes(5)));

// Próximas chamadas com mesmos dados retornam do cache
var cachedResult = await connection.InsertManyAsync(pedidos);
// Cache HIT: retorna instantaneamente sem hit ao BD
```

### Tecnicamente
- Criar interface `IResultCacheProvider` (Get/Set/Invalidate)
- Implementações: InMemoryCache, RedisCache, DistributedCache
- Chave de cache: hash(tipo + dados + operação)
- Invalidar cache ao UPDATE/DELETE

---

## 20. **Batch Notification / Event Publishing** ⭐ MÉDIA PRIORIDADE
