# DapperMany — Spec

Framework open source que estende o Dapper com operações em massa (`InsertMany`, `UpdateMany`, `DeleteMany`), mapeando classes para tabelas via attributes, com suporte a inserção automática de grafos pai-filho e múltiplos bancos de dados via pacotes de provider separados.

---

## 1. Objetivo

Permitir que o consumidor declare classes com attributes representando tabelas do banco e execute operações de massa performáticas (bulk copy nativo por banco) sem escrever SQL manual, sem gerenciar Ids gerados, e sem se preocupar com a mecânica interna de correlação pai-filho.

**Bancos suportados:** SQL Server, PostgreSQL, MySQL — cada um em pacote NuGet separado.

---

## 2. Princípios de design

- **API pública mínima**: o consumidor só vê attributes na classe e métodos de extensão sobre `IDbConnection`.
- **Zero SQL manual**: toda geração de SQL, bulk copy, resolução de Id gerado e correlação pai-filho é interna.
- **Core desacoplado de driver de banco**: o pacote principal (`DapperMany`) não referencia `Microsoft.Data.SqlClient`, `Npgsql` ou `MySqlConnector`. Cada provider é um pacote à parte.
- **Performance via reflection cacheada**: nenhuma leitura de attribute ou acesso a propriedade repete reflection em runtime — tudo é compilado (Expression Trees) e cacheado após a primeira execução por tipo.
- **Thread-safety por design**: caches globais são imutáveis após construção ou usam primitivas seguras para concorrência (`Lazy<T>`, `ConcurrentDictionary`).

---

## 3. Nomenclatura da API pública

```csharp
public static class DbConnectionExtensions
{
    // Bulk Insert with telemetry
    Task<BulkOperationResult<T>> InsertManyAsync<T>(
        this IDbConnection cn, 
        IEnumerable<T> entities, 
        IDbTransaction? tx = null);
    
    // Graph insert with parent-child relationship tracking
    Task<BulkOperationResult<T>> InsertManyGraphAsync<T>(
        this IDbConnection cn, 
        IEnumerable<T> entities, 
        IDbTransaction? tx = null);
    
    // Bulk Update with telemetry
    Task<BulkOperationResult<T>> UpdateManyAsync<T>(
        this IDbConnection cn, 
        IEnumerable<T> entities, 
        IDbTransaction? tx = null);
    
    // Bulk Delete (by entities)
    Task<BulkOperationResult<T>> DeleteManyAsync<T>(
        this IDbConnection cn, 
        IEnumerable<T> entities, 
        IDbTransaction? tx = null);
    
    // Bulk Delete (by keys)
    Task<BulkOperationResult<T>> DeleteManyAsync<T>(
        this IDbConnection cn, 
        IEnumerable<object> keys, 
        IDbTransaction? tx = null);
}

// Result type with comprehensive telemetry (v3.0+)
public record BulkOperationResult<T> where T : class
{
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsDeleted { get; init; }
    public int TotalRowsAffected { get; }  // = RowsInserted + RowsUpdated + RowsDeleted
    
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<object> GeneratedIds { get; init; }
    public IReadOnlyDictionary<string, int> RelatedEntities { get; init; }
    
    public IReadOnlyList<OperationError> Errors { get; init; }
    public bool IsSuccessful { get; }  // = Errors.Count == 0
}

public record OperationError
{
    public int EntityIndex { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
}
```

Attributes de mapeamento (reaproveitando `System.ComponentModel.DataAnnotations.Schema` quando possível, para compatibilidade com quem já conhece EF Core):

```csharp
[Table("Pedidos")]
public class Pedido
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Cliente { get; set; }
    public DateTime Data { get; set; }

    [HasMany(foreignKey: nameof(ItemPedido.PedidoId))]
    public List<ItemPedido> Itens { get; set; }

    // Exemplo 1:1 — usa [HasOne] para uma relação um-para-um (propriedade de referência única)
    [HasOne(foreignKey: nameof(PedidoDetalhe.PedidoId))]
    public PedidoDetalhe Detalhe { get; set; }
}

[Table("ItensPedido")]
public class ItemPedido
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public string Produto { get; set; }
}
```

Uso final:

```csharp
await connection.InsertManyGraphAsync(pedidos);    // pai + filhos, FK resolvida automaticamente
await connection.UpdateManyAsync(pedidosParciais); // objeto só com [Key] + campos a atualizar
await connection.DeleteManyAsync(pedidoIds);
```

---

## 4. Decisões de design por operação

### 4.1 InsertMany / InsertManyGraph

- **PK Identity é o cenário assumido como padrão** (não exige Guid no modelo do consumidor).
- Correlação pai-filho é resolvida **inteiramente pela lib**, via reflection sobre a referência de objeto em memória — o consumidor nunca atribui FK manualmente.
Fluxo interno (`InsertManyGraphExecutor`):

1. Insere os registros pai (estratégia depende do provider — ver seção 6).
2. Popula o `Id` gerado de volta em cada entidade pai via setter compilado.

Nota de implementação — verificação de relações:
Antes de processar qualquer relação marcada (`[HasMany]` ou `[HasOne]`), o executor deve verificar dois pontos importantes:

- A propriedade de navegação existe e está mapeada (atributo presente na classe).
- Há dados a inserir: para `[HasMany]`, a coleção não é nula e contém pelo menos um elemento; para `[HasOne]`, a propriedade de referência não é nula.

Se não houver dados, o executor deve pular o processamento/insert dessa relação sem tentar construir batches ou chamar o bulk insert.

3. Para cada relação marcada (`[HasMany]` ou `[HasOne]`):
    - `[HasMany]`: para cada pai, enumera a coleção de filhos (ex: `List<TChild>`), propaga a FK do pai para cada filho via setter compilado e acumula os filhos em um lote por tipo.
    - `[HasOne]`: para cada pai, se a propriedade de referência do filho não for nula, propaga a FK do pai para o filho via setter compilado e adiciona esse filho ao lote correspondente.
4. Executa bulk copy nativo dos filhos agrupados por tipo (quando suportado pelo provider). Relações 1:1 são tratadas uniformemente com 1:N no fluxo de build: todos os filhos (sejam únicos ou coleções) são coletados em DataTables/batches e inseridos via a estratégia de bulk do provider quando possível.
5. Recursão para relações aninhadas (netos), se existirem.
- Insert do "pai" não é bulk-copy nativo na v1 (é sequencial ou multi-`VALUES` com retorno de Id) — decisão aceita porque, no caso de uso típico (Pedido → Itens), o volume de "pais" é ordens de magnitude menor que o de "filhos", onde está o real ganho de performance do bulk copy.

#### 4.1.1 Edge Cases

- **Children `null`**: Se a propriedade de navegação do pai for `null`, `InsertManyGraphAsync` deve inserir apenas o pai e pular os filhos.

    ```csharp
    var pedido = new Pedido { NumeroDocumento = "PED-NULL", Itens = null };
    await connection.InsertManyGraphAsync(new[] { pedido }); // Insere apenas o pai
    ```

- **Children vazio**: Se a coleção estiver vazia, também insere apenas o pai.

    ```csharp
    var pedido = new Pedido { NumeroDocumento = "PED-EMPTY", Itens = new List<ItemPedido>() };
    await connection.InsertManyGraphAsync(new[] { pedido }); // Insere apenas o pai
    ```

- **`[HasOne]` (1:1)**: Propriedades marcadas com `[HasOne]` são tratadas como um único filho. A implementação deve aceitar tanto coleções (`[HasMany]`) quanto referências simples (`[HasOne]`) e propagar a FK do pai para o filho.

    ```csharp
    [Table("Pedidos")]
    public class Pedido {
            [HasOne(nameof(PedidoDetalhe.PedidoId))]
            public PedidoDetalhe Detalhe { get; set; }
    }

    var pedido = new Pedido { NumeroDocumento = "PED-DET", Detalhe = new PedidoDetalhe { /* ... */ } };
    await connection.InsertManyGraphAsync(new[] { pedido }); // Insere pai + detalhe (1:1)
    ```

- **Transações (obrigatório para operações em massa)**: Todas as operações em lote (`InsertMany`, `InsertManyGraph`, `UpdateMany`, `DeleteMany`) devem obrigatoriamente ser executadas dentro de uma transação associada ao `IDbConnection` usado pela operação.

    Regras de comportamento:

    - Se o chamador fornecer um `IDbTransaction` (parâmetro `tx`), a biblioteca deve usar e propagar essa transação para todas as operações internas (pais, filhos, staging tables, bulk copy). A biblioteca NÃO deve efetuar `Commit` ou `Rollback` quando a transação foi fornecida externamente; a responsabilidade por `Commit`/`Rollback` permanece com o chamador.
    - Se o chamador NÃO fornecer um `IDbTransaction` (`tx == null`), a biblioteca deve criar uma transação local via `connection.BeginTransaction()` antes de iniciar qualquer operação que modifique o banco. Quando a biblioteca cria a transação, ela é proprietária dela e deve:
        - `Commit` automaticamente quando a operação completar com sucesso;
        - `Rollback` automaticamente em caso de exceção durante a operação;
        - Garantir o `Dispose`/liberação da transação em um bloco `finally`.
    - A transação (fornecida ou criada internamente) deve envolver todas as fases da operação: inserção/atualização/exclusão de pais, população de FKs, bulk copy dos filhos e qualquer leitura/recuperação de identities necessária. Não devem existir operações que modifiquem o estado do banco fora do escopo dessa transação.
    - Isolamento: na v1 a biblioteca usará o `IsolationLevel` padrão do provider (tipicamente `ReadCommitted`). Expor controle de `IsolationLevel` ou sobrecarga com parâmetro de isolamento fica para versões futuras.

    Observações por provider (nota de implementação):
    - **SQL Server**: a transação é necessária para garantir a consistência do `OUTPUT/INSERTED` e para que operações como `SqlBulkCopy` sejam executadas no contexto correto da connection/transaction quando aplicável.
    - **PostgreSQL**: o `RETURNING` deve ser executado na mesma transação criada/fornecida para garantir correlação e consistência de dados.
    - **MySQL**: estratégias que dependem de `LAST_INSERT_ID()` ou de loaders (ex: `MySqlBulkLoader`/CSV) devem executar-se no mesmo `IDbConnection` e dentro da transação fornecida/criada; garantir ordenação de commit/flush para manter consistência.

    Exemplo de uso com transação fornecida pelo chamador:

    ```csharp
    using var tx = connection.BeginTransaction();
    await connection.InsertManyGraphAsync(pedidos, tx);
    tx.Commit();
    ```

    Quando a biblioteca cria a transação internamente, o comportamento esperado é:

    ```csharp
    // impl. interna da biblioteca (exemplo conceitual)
    using var tx = connection.BeginTransaction();
    try {
        // executar inserts/updates/deletes de pai, bulk dos filhos, identity retrieval
        tx.Commit();
    }
    catch {
        tx.Rollback();
        throw;
    }
    ```

    A responsabilidade pela política de commit/rollback (ownership) deve ser documentada no README e refletida em logs/diagnósticos para facilitar troubleshooting.

### 4.2 UpdateMany

- Recebe um objeto **parcial**: `[Key]` (obrigatória, usada para o `JOIN`/`MERGE`) + apenas os campos que devem ser atualizados.
- O `SET` do `UPDATE` é gerado dinamicamente a partir das propriedades presentes no tipo passado — sem lista de colunas manual.
- Mecanismo: staging table (bulk copy do objeto parcial) + `UPDATE ... FROM` (SQL Server/Postgres) ou `UPDATE ... JOIN` (MySQL).
- Fora de escopo na v1: dirty-tracking automático (atualizar só campos alterados de uma entidade completa). Fica documentado como possível v2.
 - Transações: `UpdateManyAsync` deve obedecer à política de transação definida na seção "Transações (obrigatório para operações em massa)" — use a transação fornecida pelo chamador ou crie/gerencie internamente uma transação que envolva o staging table + o `UPDATE` final e qualquer leitura necessária para recuperação de identidades.

### 4.3 DeleteMany

- Aceita lista de entidades completas ou lista de chaves (`IEnumerable<object> keys`).
- Fora de escopo na v1: cascade automático via `[HasMany]` — cada nível deve ser deletado explicitamente respeitando FKs.
 - Transações: `DeleteManyAsync` deve executar dentro de uma transação conforme a política "Transações (obrigatório para operações em massa)". Quando a lib criar a transação internamente, ela deve garantir rollback em falha e commit em sucesso; se a transação for fornecida, a biblioteca deve apenas propagar as operações para essa transação sem efetuar commit/rollback.

### 4.4 Logging de Operações e Duração (Debug only)

Em builds de `Debug`, todas as operações bulk relevantes (`InsertMany`, `InsertManyGraph`, `UpdateMany`, `DeleteMany`) devem emitir informações de diagnóstico via `System.Diagnostics.Debug.WriteLine()` usando um `Stopwatch` para medir a duração da operação.

Regras e formato:
- Mensagens somente em `#if DEBUG` (não devem aparecer em builds Release).
- Formato padrão: `[DAPPERMANY] <Op> <Entity> (<Provider>): affected=<N>, elapsed=<Tms>ms`
    - Exemplo: `[DAPPERMANY] BulkInsert Pedido (SqlServer): affected=10, elapsed=45ms`
- Adicionar logs nas implementações provider-específicas (`IBulkCopyStrategy`) e no orquestrador de grafos (`GraphInsertOrchestrator`) — não instrumentar nas camadas de extensão pública.
- Para inserções de grafo, registrar tanto a fase de pais quanto a fase de filhos, e um log agregado total ao final da operação.
- Usar `typeof(T).Name` para o nome da entidade e identificar o provider pelo nome do provider (`SqlServer`, `Postgres`, `MySql`).

Propósito:
- Fornecer sinais rápidos de troubleshooting local: quantas linhas foram afetadas por batch/operação e quanto tempo levou.
- Não substitui integrações de observability (OpenTelemetry) — que podem ser adicionadas futuramente.

---

## 5. Estrutura de solution (multi-DLL)

```
DapperMany.sln
├── src/
│   ├── DapperMany/                         (core — nenhuma dependência de driver de banco)
│   │   ├── Attributes/
│   │   ├── Extensions/DbConnectionExtensions.cs
│   │   └── Internal/
│   │       ├── Mapping/            (EntityMapper, AccessorFactory, EntityMetadata)
│   │       ├── DataTableBuilder.cs
│   │       ├── Execution/          (InsertManyExecutor, InsertManyGraphExecutor, UpdateManyExecutor, DeleteManyExecutor)
│   │       ├── Abstractions/       (ISqlDialect, IBulkCopyStrategy, IIdentityRetrievalStrategy)
│   │       └── Providers/ProviderRegistry.cs
│   │
│   ├── DapperMany.SqlServer/                (referencia Microsoft.Data.SqlClient)
│   ├── DapperMany.Postgres/                 (referencia Npgsql)
│   └── DapperMany.MySql/                    (referencia MySqlConnector)
│
├── tests/
│   ├── DapperMany.UnitTests/                (geração de SQL, mapper, accessors — sem banco real)
│   ├── DapperMany.SqlServer.IntegrationTests/
│   ├── DapperMany.Postgres.IntegrationTests/
│   └── DapperMany.MySql.IntegrationTests/
│
├── samples/
│   └── DapperMany.Samples/                  (projeto executável para testar manualmente os 3 bancos)
│
└── docker/
    ├── docker-compose.yml                   (sobe SQL Server, Postgres e MySQL localmente)
    ├── sqlserver/init/
    ├── postgres/init/
    └── mysql/init/
```

Pacotes NuGet publicados de forma independente: `DapperMany`, `DapperMany.SqlServer`, `DapperMany.Postgres`, `DapperMany.MySql`.

### 5.1 Auto-registro de provider via ModuleInitializer

O core não conhece nenhum driver concreto. Cada pacote de provider se registra sozinho ao ser carregado:

```csharp
// No core
public static class ProviderRegistry
{
    private static readonly ConcurrentDictionary<string, ProviderModule> _modules = new();

    public static void Register(string connectionTypeFullName, ProviderModule module) =>
        _modules[connectionTypeFullName] = module;

    public static ProviderModule Resolve(IDbConnection connection)
    {
        var typeName = connection.GetType().FullName!;
        if (_modules.TryGetValue(typeName, out var module)) return module;

        throw new NotSupportedException(
            $"Nenhum provider registrado para '{typeName}'. Instale o pacote correspondente, ex: DapperMany.SqlServer.");
    }
}

public sealed record ProviderModule(
    ISqlDialect Dialect,
    IBulkCopyStrategy BulkCopyStrategy,
    IIdentityRetrievalStrategy IdentityStrategy);
```

```csharp
// Em DapperMany.SqlServer
internal static class SqlServerModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        ProviderRegistry.Register("Microsoft.Data.SqlClient.SqlConnection",
            new ProviderModule(new SqlServerDialect(), new SqlServerBulkCopyStrategy(), new SqlServerIdentityStrategy()));
}
```

Consumidor só precisa instalar o pacote do provider desejado — nenhuma configuração manual adicional. Extensível por terceiros (ex: `DapperMany.Sqlite` fora do repositório principal).

---

## 6. Diferenças entre providers

| Aspecto | SQL Server | PostgreSQL | MySQL |
|---|---|---|---|
| Bulk copy nativo | `SqlBulkCopy` | `COPY` via `NpgsqlBinaryImporter` | `LOAD DATA` via CSV temporário (`MySqlBulkLoader`) |
| Retorno de Id gerado | `OUTPUT INSERTED.Id` | `RETURNING id` (funciona em insert multi-linha) | Sem `OUTPUT`/`RETURNING` confiável em todas as versões — usa `LAST_INSERT_ID()` + incremento sequencial (válido para InnoDB/`AUTO_INCREMENT` padrão, dentro de transação) |
| Update em massa | Staging table + `UPDATE...FROM` / `MERGE` | Staging table + `UPDATE...FROM` | Staging table + `UPDATE...JOIN` |
| Complexidade de implementação | Baseline | Baixa (muito parecido com SQL Server) | Alta (sem bulk copy direto de `DataTable`, sem retorno de Id nativo) |

Correlação de Id gerado por posição no `VALUES` (SQL Server/Postgres) é comportamento consistente e usado em produção, mas deve ter cobertura de teste de integração dedicada, já que não é contrato formalmente documentado pela Microsoft no caso do `OUTPUT`.


## 7. Cache e thread-safety (fundação transversal)

| Item | Escopo | Estrutura | Motivo |
|---|---|---|---|
| `EntityMetadata` (attributes lidos) | Estático, global, por `Type` | `ConcurrentDictionary<Type, Lazy<EntityMetadata>>` | Evita reflection repetida; `Lazy` com `ExecutionAndPublication` garante que a factory roda exatamente 1x por tipo mesmo sob concorrência |
| Getters/Setters compilados | Estático, global, por `PropertyInfo` | `ConcurrentDictionary<PropertyInfo, Delegate>` | Expression Trees compiladas substituem `PropertyInfo.GetValue/SetValue`; função de build é pura, segura sem `Lazy` |
| `DataTable`, buffers, correlação de grafo em memória | Local, por chamada | variável local no método | Nunca compartilhado entre threads ou chamadas concorrentes |

`EntityMetadata` é um objeto imutável após construído (`IReadOnlyList` em todas as coleções expostas) — leitura concorrente sem necessidade de lock.

Pontos de atenção adicionais:
- `SqlConnection`/`NpgsqlConnection`/`MySqlConnection` não são thread-safe para uso simultâneo na mesma instância — mesma premissa do Dapper puro; documentar no README.
- Qualquer estrutura de correlação usada durante a resolução de grafo deve ser instanciada localmente por chamada, nunca estática.

---

## 8. Testes

- **Unit tests**: geração de SQL por dialect, `EntityMapper`, `AccessorFactory` — incluindo testes de concorrência (`Parallel.For` disparando `GetMetadata`/`CreateGetter` simultaneamente) sem depender de banco real.
- **Integration tests**: Testcontainers com SQL Server, PostgreSQL e MySQL em containers no CI, rodando a mesma suíte de testes de contrato contra os três providers para garantir comportamento observável idêntico.

---

## 8.1 Ambiente local de bancos (Docker Compose)

Para desenvolvimento e testes manuais/exploratórios (independente dos testes automatizados com Testcontainers), o repositório deve incluir um `docker-compose.yml` (ou `Dockerfile`s individuais) subindo os três bancos localmente, com schema de exemplo já criado (tabelas `Pedidos`/`ItensPedido` ou equivalente) para uso pelo projeto `samples` (seção 8.2).

```
docker/
├── docker-compose.yml
├── sqlserver/
│   └── init/01-schema.sql       // cria tabelas Pedidos, ItensPedido
├── postgres/
│   └── init/01-schema.sql
└── mysql/
    └── init/01-schema.sql
```

`docker-compose.yml` deve expor os três bancos em portas distintas (ex: SQL Server `1433`, Postgres `5432`, MySQL `3306`), com usuário/senha padrão documentados no README, e usar os mecanismos nativos de cada imagem para rodar os scripts de schema automaticamente na primeira subida (`entrypoint-initdb.d` para Postgres/MySQL; script de inicialização via `command`/healthcheck para SQL Server, que não tem suporte nativo a init scripts).

Comando único para subir o ambiente completo: `docker compose up -d`.

---

## 8.2 Projeto Samples

Projeto executável (`console app`, `.NET`) dedicado a exercitar manualmente todas as operações (`InsertMany`, `InsertManyGraph`, `UpdateMany`, `DeleteMany`) contra os três bancos, usando o ambiente Docker da seção 8.1. Serve tanto como validação exploratória durante o desenvolvimento quanto como exemplo de uso vivo para quem for conhecer a lib.

```
samples/
└── DapperMany.Samples/
    ├── Models/
    │   ├── Pedido.cs
    │   └── ItemPedido.cs
    ├── Scenarios/
    │   ├── InsertManyScenario.cs
    │   ├── InsertManyGraphScenario.cs
    │   ├── UpdateManyScenario.cs
    │   └── DeleteManyScenario.cs
    ├── appsettings.json           // connection strings dos 3 bancos (apontando pro Docker local)
    ├── Program.cs                 // menu simples: escolhe o banco + o cenário a rodar
    └── DapperMany.Samples.csproj  // referencia DapperMany + DapperMany.SqlServer + .Postgres + .MySql
```

Requisitos do projeto Samples:
- Deve rodar o **mesmo conjunto de cenários** contra os três bancos, usando as connection strings do `docker-compose.yml`, para servir como validação cruzada de que o comportamento é equivalente entre providers.
- `Program.cs` deve permitir escolher o banco (SQL Server/Postgres/MySQL) e o cenário a executar, imprimindo no console os dados antes/depois de cada operação para inspeção visual.
- Deve ser mantido atualizado conforme novas operações forem implementadas — é o principal ponto de fumo (smoke test) manual do projeto antes de cada release.

---

## 9. Roadmap de implementação

1. Fundação: `EntityMapper` + `AccessorFactory` + `EntityMetadata`, com testes de concorrência isolados.
2. Ambiente Docker (seção 8.1): `docker-compose.yml` com os três bancos + scripts de schema inicial.
3. `DapperMany` (core) + `DapperMany.SqlServer`: `InsertManyAsync` simples, validado com Testcontainers.
4. Projeto `samples` (seção 8.2): cenário de `InsertMany` rodando contra SQL Server via Docker local, como primeiro smoke test manual.
5. `InsertManyGraphAsync` no SQL Server — 1 nível de relacionamento primeiro, recursão depois.
6. `UpdateManyAsync` / `DeleteManyAsync` no SQL Server, com cenários correspondentes adicionados ao `samples`.
7. Extrair `ISqlDialect` / `IBulkCopyStrategy` / `IIdentityRetrievalStrategy` como interfaces formais (refatorando o que já existir hardcoded).
8. `DapperMany.Postgres` (reaproveita a maior parte da lógica; troca as 3 estratégias) + cenários no `samples`.
9. `DapperMany.MySql` (mais custoso: CSV temporário para bulk copy, cálculo de Id sequencial) + cenários no `samples`.
10. Benchmarks (`BenchmarkDotNet`) comparando com insert linha a linha, por provider.
11. Documentação (README com exemplos antes/depois) + publicação dos pacotes no NuGet.

---

## 10. Escopo explicitamente fora da v1 (backlog futuro)

- Dirty-tracking automático para `UpdateMany` a partir de entidade completa (hoje: DTO parcial explícito).
- Insert de "pais" verdadeiramente bulk (multi-`VALUES` + `OUTPUT`) para cenários de alto volume também no lado pai.
- `DeleteMany` em cascata automática via `[HasMany]`.
- `RETURNING`/`OUTPUT` parcial (subset de colunas) para reduzir tráfego em updates largos.
- Providers adicionais (SQLite, Oracle) como pacotes independentes, seguindo o mesmo contrato de `ISqlDialect`/`IBulkCopyStrategy`/`IIdentityRetrievalStrategy`.

---

## 11. Nome do projeto

**DapperMany** — reflete diretamente a nomenclatura dos métodos públicos (`InsertMany`, `UpdateMany`, `DeleteMany`) e segue a convenção de nomenclatura já reconhecida na comunidade .NET para extensões do Dapper (ex: `Dapper.Contrib`, `Dapper.SimpleCRUD`).
