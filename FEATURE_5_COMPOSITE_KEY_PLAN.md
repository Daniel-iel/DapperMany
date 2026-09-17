# Feature 5: Composite Key Support - Implementation Plan

## 📋 Overview

**Objetivo**: Suportar chaves compostas (multi-column) para operações UpdateMany/DeleteMany sem requerer chave substituta.

**Caso de uso**: Tabelas com chaves naturais como TenantId + DocumentNumber, ou Date + AccountId.

**Impacto**: Modelo breaking change em EntityMetadata, mas não na API pública.

---

## 🎯 Phases

### Phase 5.1: EntityMetadata Refactoring
**Status**: ⏳ Pending

Modificar `EntityMetadata` para suportar múltiplas Key properties:
- [ ] Change KeyProperty from single property to IReadOnlyList<PropertyAccessor>
- [ ] Update EntityMapper.GetMetadata() to collect all [Key] attributes
- [ ] Add validation: Composite key cannot have [DatabaseGenerated(Identity)]
- [ ] Add validation: At least one Key required
- [ ] Add property: `IsCompositeKey { get; }`

**Files to modify**:
- `src/DapperMany/Internal/EntityMetadata.cs`
- `src/DapperMany/Internal/EntityMapper.cs`
- `tests/DapperMany.UnitTests/EntityMapperTests.cs`

**Validation rules**:
```csharp
// ✓ Valid
[Key] int TenantId;
[Key] string DocumentNumber;

// ✓ Valid (single key still works)
[Key, DatabaseGenerated(Identity)] int Id;

// ✗ Invalid
[Key, DatabaseGenerated(Identity)] int TenantId;
[Key] string DocumentNumber;
```

---

### Phase 5.2: SQL Dialect Updates
**Status**: ⏳ Pending

Update SQL dialects to generate multi-column WHERE clauses:
- [ ] ISqlDialect.BuildUpdateStatement() → support multiple key columns
- [ ] ISqlDialect.BuildDeleteStatement() → support multiple key columns
- [ ] ISqlDialect.BuildDeleteByKeysStatement() → support mapping multiple keys

**Files to modify**:
- `src/DapperMany/Internal/Abstractions/ISqlDialect.cs`
- `src/DapperMany.SqlServer/SqlServerDialect.cs`
- `src/DapperMany.Postgres/PostgreSqlDialect.cs`
- `src/DapperMany.MySql/MySqlDialect.cs`

**Example output**:
```sql
-- SQL Server
UPDATE [PedidosPorTenant] 
SET DataPedido = @p0 
WHERE [TenantId] = @k0 AND [NumeroDocumento] = @k1

-- PostgreSQL
UPDATE pedidos_por_tenant 
SET data_pedido = $1 
WHERE tenant_id = $2 AND numero_documento = $3

-- MySQL
UPDATE pedidos_por_tenant 
SET data_pedido = ? 
WHERE tenant_id = ? AND numero_documento = ?
```

---

### Phase 5.3: IBulkCopyStrategy Implementation
**Status**: ⏳ Pending

Update all bulk copy strategy implementations to use composite keys:
- [ ] SqlServerBulkCopyStrategy.BulkUpdateAsync() with composite key WHERE
- [ ] SqlServerBulkCopyStrategy.BulkDeleteAsync() with composite key WHERE
- [ ] SqlServerBulkCopyStrategy.BulkDeleteByKeysAsync() with composite key mapping
- [ ] PostgreSQL equivalent updates
- [ ] MySQL equivalent updates

**Implementation approach**:
1. Extract key values from entity using compiled accessors
2. Build WHERE clause using ISqlDialect
3. Use parameter arrays instead of single key parameter
4. Batch processing remains the same (50-row windows)

**Files to modify**:
- `src/DapperMany.SqlServer/SqlServerBulkCopyStrategy.cs`
- `src/DapperMany.Postgres/PostgreSqlBulkCopyStrategy.cs`
- `src/DapperMany.MySql/MySqlBulkCopyStrategy.cs`

---

### Phase 5.4: Testing & Documentation
**Status**: ⏳ Pending

- [ ] Unit tests for composite key detection
- [ ] Integration tests for UpdateMany with composite keys
- [ ] Integration tests for DeleteMany with composite keys
- [ ] Integration tests for DeleteByKeys with composite keys
- [ ] Sample project with composite key example
- [ ] README section on composite keys
- [ ] specs/spec.md update

**Files to create/modify**:
- `tests/DapperMany.UnitTests/CompositeKeyTests.cs`
- `tests/DapperMany.SqlServer.IntegrationTests/CompositeKeyIntegrationTests.cs`
- `tests/DapperMany.Postgres.IntegrationTests/CompositeKeyIntegrationTests.cs`
- `tests/DapperMany.MySql.IntegrationTests/CompositeKeyIntegrationTests.cs`
- `samples/DapperMany.Samples/Models/TenantPedido.cs` (example)
- `README.md` (add section)
- `specs/spec.md` (add documentation)

---

## 🔄 Architecture Impact

### EntityMetadata Changes
```csharp
public record EntityMetadata
{
    // Before
    public PropertyAccessor KeyProperty { get; init; }
    
    // After
    public IReadOnlyList<PropertyAccessor> KeyProperties { get; init; }
    
    // New computed property
    public bool IsCompositeKey => KeyProperties.Count > 1;
}
```

### Parameter Mapping
```csharp
// Single key (existing)
var parameters = new[] { ("@Id", entity.Id) };

// Composite key (new)
var parameters = new[]
{
    ("@TenantId", entity.TenantId),
    ("@DocumentNumber", entity.DocumentNumber)
};
```

### Backward Compatibility
- Single key entities continue to work exactly as before
- No breaking changes to public API
- Only internal EntityMetadata structure changes

---

## 📊 Estimated Effort

| Phase | Effort | Complexity | Risk |
|-------|--------|-----------|------|
| 5.1 | 2-3h | Medium | Low - isolated to metadata |
| 5.2 | 2-3h | Medium | Medium - SQL dialect testing needed |
| 5.3 | 3-4h | High | High - all 3 providers, parameter mapping |
| 5.4 | 3-4h | Medium | Low - testing infrastructure exists |
| **Total** | **10-14h** | **High** | **Medium** |

---

## 🧪 Test Coverage Plan

### Unit Tests (EntityMapperTests.cs)
- Detect single key (backward compat)
- Detect multiple keys
- Reject composite key + DatabaseGenerated(Identity)
- Reject entity with no [Key]

### Integration Tests
- UpdateMany with composite keys
- DeleteMany with composite keys
- DeleteByKeys with composite keys (key array mapping)
- Mixed updates (some keys exist, some don't)

### Sample Models
```csharp
[Table("PedidosPorTenant")]
public class TenantPedido
{
    [Key] public int TenantId { get; set; }
    [Key] public string NumeroDocumento { get; set; }
    public DateTime DataPedido { get; set; }
    public string Cliente { get; set; }
}
```

---

## ✅ Definition of Done

- [ ] All phases 5.1-5.4 completed
- [ ] EntityMetadata supports IReadOnlyList<PropertyAccessor> for keys
- [ ] All 3 SQL dialects generate multi-column WHERE clauses
- [ ] All 3 providers implement composite key support
- [ ] Unit tests passing (EntityMapper composite key detection)
- [ ] Integration tests passing (all 3 databases)
- [ ] Sample project includes composite key example
- [ ] README documented with example
- [ ] specs/spec.md updated
- [ ] Zero compilation errors
- [ ] Backward compatibility verified (single key still works)
- [ ] Single atomic commit on feature/Composite-Key-Support

---

## 📝 Notes

- Composite key with auto-generated ID is NOT supported (validation error)
- BulkInsertAsync may return generated IDs only for single-key scenarios
- For composite keys, generated IDs are not applicable (keys are provided by caller)
- Parameter ordering in WHERE clause must match EntityMetadata.KeyProperties order

---

## 🚀 Next Action

Start Phase 5.1: Update EntityMetadata and EntityMapper
