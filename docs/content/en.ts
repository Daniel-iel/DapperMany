import { CODE } from "./code";
import type { LocaleContent } from "./types";

const en: LocaleContent = {
  hero: {
    eyebrow: "Bulk-operations extension for Dapper in .NET",
    title:
      "Insert, update and delete thousands of records without writing a line of SQL.",
    sub: `DapperMany maps your classes to tables with attributes, resolves parent-child graphs automatically, and uses each database's native bulk copy — SQL Server, PostgreSQL and MySQL — behind a minimal API over <code>IDbConnection</code>.`,
    ctaPrimary: "See quickstart",
    ctaSecondary: "Installation",
    stat1: "supported databases",
    stat2: "manual SQL",
    stat3: "call for whole graphs",
  },
  topbar: {
    badge: "v1.0 · pre-release",
    install: "Installation",
    githubAria: "GitHub repository",
  },
  nav: {
    toggleLabel: "Navigation",
    toggleAria: "Open navigation",
  },
  misc: {
    tocTitle: "On this page",
    copyLabel: "Copy",
    copiedLabel: "Copied",
    footer:
      "DapperMany is an open source framework for .NET. Documentation generated from the project specification.",
    metaTitle: "DapperMany — Documentation",
    metaDescription:
      "DapperMany — a Dapper extension for bulk operations: InsertMany, UpdateMany, DeleteMany and automatic parent-child graph insertion, with providers for SQL Server, PostgreSQL and MySQL.",
  },
  groups: [
    { id: "start", label: "Getting started" },
    { id: "mapping", label: "Mapping" },
    { id: "operations", label: "Operations" },
    { id: "internals", label: "Internals" },
    { id: "project", label: "Project" },
  ],
  sections: [
    {
      id: "why",
      group: "start",
      navLabel: "Why DapperMany",
      title: "Why DapperMany",
      blocks: [
        {
          type: "p",
          html: "Dapper already handles line-by-line object-relational mapping very well. What's missing is a performant way to move <strong>many</strong> records at once — including trees of related objects — without falling back to hand-written SQL or losing control over database-generated Ids.",
        },
        {
          type: "grid",
          cards: [
            {
              title: "Minimal public API",
              body: "Consumers only see attributes on the class and extension methods over <code>IDbConnection</code>. All internal mechanics stay hidden.",
              color: "var(--orange)",
            },
            {
              title: "Zero manual SQL",
              body: "SQL generation, bulk copy, generated-Id resolution and parent-child correlation all happen internally, by convention.",
              color: "var(--magenta)",
            },
            {
              title: "Core decoupled from drivers",
              body: "The <code>DapperMany</code> package doesn't reference any database driver. Each provider lives in its own separate NuGet package.",
              color: "var(--teal)",
            },
            {
              title: "Performance via cached reflection",
              body: "No attribute or property is read via reflection repeatedly at runtime — everything is compiled with Expression Trees and cached per type.",
              color: "var(--teal-dark)",
            },
          ],
        },
      ],
    },
    {
      id: "install",
      group: "start",
      navLabel: "Installation",
      title: "Installation",
      blocks: [
        {
          type: "p",
          html: "Install the core package and the provider for the database you use. When the assembly loads, the provider registers itself — no extra configuration needed.",
        },
        {
          type: "tabs",
          tabs: [
            { label: "SQL Server", lang: "bash", code: CODE.installSqlServer },
            { label: "PostgreSQL", lang: "bash", code: CODE.installPostgres },
            { label: "MySQL", lang: "bash", code: CODE.installMySql },
          ],
        },
        {
          type: "callout",
          variant: "info",
          html: "Extensible by third parties: any external package can register a new provider (e.g. <code>DapperMany.Sqlite</code>) by implementing <code>ISqlDialect</code>, <code>IBulkCopyStrategy</code> and <code>IIdentityRetrievalStrategy</code>.",
        },
      ],
    },
    {
      id: "quickstart",
      group: "start",
      navLabel: "Quickstart",
      title: "Quickstart",
      blocks: [
        {
          type: "p",
          html: "Map your classes with attributes and call the extension operations directly on the connection.",
        },
        { type: "code", lang: "csharp", code: CODE.entityModel },
        { type: "code", lang: "csharp", code: CODE.quickstartUsage },
      ],
    },
    {
      id: "mapping",
      group: "mapping",
      navLabel: "Entity attributes",
      title: "Entity attributes",
      blocks: [
        {
          type: "p",
          html: "DapperMany reuses <code>System.ComponentModel.DataAnnotations.Schema</code> whenever possible — anyone who already knows EF Core will recognize <code>[Table]</code>, <code>[Key]</code> and <code>[DatabaseGenerated]</code> right away. Relationships are declared with <code>[HasMany]</code> (1:N) and <code>[HasOne]</code> (1:1).",
        },
        { type: "code", lang: "csharp", code: CODE.entityModelWithDetail },
        {
          type: "table",
          headers: ["Attribute", "Usage"],
          rows: [
            [
              `<code>[Table("Name")]</code>`,
              "Maps the class to a database table.",
            ],
            [
              "<code>[Key]</code>",
              "Marks the primary key, used in updates, deletes and Id correlation.",
            ],
            [
              "<code>[DatabaseGenerated(Identity)]</code>",
              "Indicates the value is generated by the database on insert (the default scenario assumed by the lib).",
            ],
            [
              "<code>[HasMany(foreignKey:)]</code>",
              "1:N relationship — the FK is propagated from the parent to each item in the children collection.",
            ],
            [
              "<code>[HasOne(foreignKey:)]</code>",
              "1:1 relationship — the FK is propagated from the parent to the child's single reference.",
            ],
          ],
        },
      ],
    },
    {
      id: "api",
      group: "mapping",
      navLabel: "Public API",
      title: "Public API",
      blocks: [
        {
          type: "p",
          html: "Five extension methods over <code>IDbConnection</code> — nothing else is exposed to the consumer.",
        },
        { type: "code", lang: "csharp", code: CODE.publicApi },
      ],
    },
    {
      id: "insert-many",
      group: "operations",
      navLabel: "InsertMany / InsertManyGraph",
      title: "InsertMany / InsertManyGraph",
      blocks: [
        {
          type: "p",
          html: "<code>InsertManyAsync</code> performs bulk inserts for flat collections or entity graphs with automatic relationship detection. When relationships are detected, parent IDs are populated back into each entity and FK values are propagated to children.",
        },
        {
          type: "ol",
          items: [
            `Inserts the parent records — the strategy depends on the provider (see <a href="#providers">Providers</a>).`,
            "Populates the generated <code>Id</code> back into each parent entity via a compiled setter.",
            "For each marked relationship, checks whether there's data: a non-null, non-empty collection (<code>[HasMany]</code>) or a non-null reference (<code>[HasOne]</code>) — otherwise it skips without building a batch.",
            "Propagates the parent's FK to the children and groups them by type.",
            "Executes native bulk copy for the grouped children, when supported by the provider.",
            "Recurses for nested relationships (grandchildren), if any.",
          ],
        },
        {
          type: "callout",
          variant: "warn",
          html: "The parent insert is not native bulk copy in v1 — it's sequential or multi-<code>VALUES</code> with Id return. This trade-off is accepted because, in the typical use case (Order → Items), the volume of parents is orders of magnitude smaller than children, where the real performance gain lives.",
        },
        { type: "h3", html: "Edge cases" },
        {
          type: "p",
          html: "Null or empty children: inserts only the parent.",
        },
        { type: "code", lang: "csharp", code: CODE.edgeNull },
        {
          type: "p",
          html: "<code>[HasOne]</code> (1:1) is treated as a single child — same FK propagation, same bulk flow.",
        },
        { type: "code", lang: "csharp", code: CODE.edgeHasOne },
      ],
    },
    {
      id: "update-many",
      group: "operations",
      navLabel: "UpdateMany",
      title: "UpdateMany",
      blocks: [
        {
          type: "p",
          html: "Takes a <strong>partial</strong> object: <code>[Key]</code> is required, used for the <code>JOIN</code>/<code>MERGE</code>, plus only the fields to update. The <code>SET</code> clause is generated dynamically from the properties present on the type — no manual column list.",
        },
        {
          type: "p",
          html: "Mechanism: staging table (bulk copy of the partial object) followed by <code>UPDATE ... FROM</code> (SQL Server/Postgres) or <code>UPDATE ... JOIN</code> (MySQL).",
        },
        {
          type: "callout",
          variant: "info",
          html: "Out of scope for v1: automatic dirty-tracking from a full entity. Documented as a possible v2.",
        },
      ],
    },
    {
      id: "delete-many",
      group: "operations",
      navLabel: "DeleteMany",
      title: "DeleteMany",
      blocks: [
        {
          type: "p",
          html: "Accepts a list of full entities or a list of keys (<code>IEnumerable&lt;object&gt; keys</code>).",
        },
        { type: "code", lang: "csharp", code: CODE.deleteManyCall },
        {
          type: "callout",
          variant: "warn",
          html: "Out of scope for v1: automatic cascade via <code>[HasMany]</code>. Each level must be deleted explicitly, respecting FKs.",
        },
      ],
    },
    {
      id: "transactions",
      group: "operations",
      navLabel: "Transactions",
      title: "Transactions",
      blocks: [
        {
          type: "p",
          html: "Every bulk operation is required to run inside a transaction tied to the <code>IDbConnection</code>.",
        },
        {
          type: "grid",
          columns: 2,
          cards: [
            {
              title: "Caller-supplied transaction",
              body: "The lib uses and propagates that transaction to all internal operations (parents, children, staging tables, bulk copy) and <strong>does not</strong> commit or roll back — that responsibility stays with the caller.",
              color: "var(--orange)",
            },
            {
              title: "Internally created transaction",
              body: "When <code>tx == null</code>, the lib creates the transaction via <code>BeginTransaction()</code>, commits automatically on success, rolls back on exception, and guarantees disposal in a <code>finally</code> block.",
              color: "var(--teal)",
            },
          ],
        },
        { type: "code", lang: "csharp", code: CODE.txUsage },
        {
          type: "p",
          html: "The transaction spans every phase of the operation — inserting/updating/deleting parents, populating FKs, bulk copying children and any identity reads. No operation touches the database outside that scope. Isolation in v1: the provider's default, typically <code>ReadCommitted</code>.",
        },
      ],
    },
    {
      id: "providers",
      group: "internals",
      navLabel: "Providers",
      title: "Providers",
      blocks: [
        {
          type: "p",
          html: "Each database has its own package and a different strategy for bulk copy and generated-Id retrieval.",
        },
        {
          type: "table",
          headers: ["Aspect", "SQL Server", "PostgreSQL", "MySQL"],
          rows: [
            [
              "Native bulk copy",
              "<code>SqlBulkCopy</code>",
              "<code>COPY</code> via <code>NpgsqlBinaryImporter</code>",
              "<code>LOAD DATA</code> via temporary CSV (<code>MySqlBulkLoader</code>)",
            ],
            [
              "Generated Id return",
              "<code>OUTPUT INSERTED.Id</code>",
              "<code>RETURNING id</code> (multi-row)",
              "<code>LAST_INSERT_ID()</code> + sequential increment",
            ],
            [
              "Bulk update",
              "Staging + <code>UPDATE...FROM</code> / <code>MERGE</code>",
              "Staging + <code>UPDATE...FROM</code>",
              "Staging + <code>UPDATE...JOIN</code>",
            ],
            ["Implementation complexity", "Baseline", "Low", "High"],
          ],
        },
        {
          type: "callout",
          variant: "warn",
          html: "Id correlation by <code>VALUES</code> position (SQL Server/Postgres) is consistent and used in production, but it isn't a contract formally documented by Microsoft — that's why it has dedicated integration test coverage.",
        },
        { type: "h3", html: "Provider auto-registration" },
        {
          type: "p",
          html: "The core doesn't know any concrete driver. Each provider package registers itself when loaded, via <code>[ModuleInitializer]</code>:",
        },
        { type: "code", lang: "csharp", code: CODE.moduleInit },
      ],
    },
    {
      id: "cache",
      group: "internals",
      navLabel: "Cache & thread-safety",
      title: "Cache & thread-safety",
      blocks: [
        {
          type: "p",
          html: "A cross-cutting foundation used by every operation — no attribute or property access repeats reflection at runtime.",
        },
        {
          type: "table",
          headers: ["Item", "Scope", "Structure"],
          rows: [
            [
              "EntityMetadata",
              "Static, global, per Type",
              "<code>ConcurrentDictionary&lt;Type, Lazy&lt;EntityMetadata&gt;&gt;</code>",
            ],
            [
              "Compiled getters/setters",
              "Static, global, per PropertyInfo",
              "<code>ConcurrentDictionary&lt;PropertyInfo, Delegate&gt;</code>",
            ],
            [
              "DataTable, buffers, graph correlation",
              "Local, per call",
              "local variable in the method",
            ],
          ],
        },
        {
          type: "p",
          html: "<code>EntityMetadata</code> is immutable once built — concurrent reads need no locking. Connections (<code>SqlConnection</code>, <code>NpgsqlConnection</code>, <code>MySqlConnection</code>) aren't thread-safe for simultaneous use on the same instance, the same premise as plain Dapper.",
        },
      ],
    },
    {
      id: "logging",
      group: "internals",
      navLabel: "Logging (Debug)",
      title: "Logging (Debug only)",
      blocks: [
        {
          type: "p",
          html: "In Debug builds, bulk operations emit diagnostics via <code>Debug.WriteLine()</code>, measured with a <code>Stopwatch</code>. In Release, those messages don't exist — everything sits behind <code>#if DEBUG</code>.",
        },
        { type: "code", lang: "bash", code: CODE.loggingExample },
        {
          type: "p",
          html: "For graph inserts, both the parent phase and the children phase are logged, with one aggregated total log at the end. This doesn't replace observability integrations like OpenTelemetry, which may be added in the future.",
        },
      ],
    },
    {
      id: "roadmap",
      group: "project",
      navLabel: "Roadmap",
      title: "Implementation roadmap",
      blocks: [
        {
          type: "ol",
          items: [
            "Foundation: <code>EntityMapper</code> + <code>AccessorFactory</code> + <code>EntityMetadata</code>, with isolated concurrency tests.",
            "Docker environment with the three databases and initial schema.",
            "<code>DapperMany</code> (core) + <code>DapperMany.SqlServer</code>: a simple <code>InsertManyAsync</code>, validated with Testcontainers.",
            "Samples project: an <code>InsertMany</code> scenario against SQL Server via local Docker.",
            "<code>InsertManyAsync</code> on SQL Server — one relationship level first, recursion after.",
            "<code>UpdateManyAsync</code> / <code>DeleteManyAsync</code> on SQL Server, with matching scenarios in samples.",
            "Extract <code>ISqlDialect</code> / <code>IBulkCopyStrategy</code> / <code>IIdentityRetrievalStrategy</code> as formal interfaces.",
            "<code>DapperMany.Postgres</code> + scenarios in samples.",
            "<code>DapperMany.MySql</code> + scenarios in samples.",
            "Benchmarks with BenchmarkDotNet comparing against row-by-row insert, per provider.",
            "Documentation and package publishing on NuGet.",
          ],
        },
      ],
    },
    {
      id: "out-of-scope",
      group: "project",
      navLabel: "Out of scope (v1)",
      title: "Out of scope (v1)",
      blocks: [
        {
          type: "ul",
          items: [
            "Automatic dirty-tracking for <code>UpdateMany</code> from a full entity.",
            "Truly bulk parent inserts (multi-<code>VALUES</code> + <code>OUTPUT</code>) for high volume on the parent side too.",
            "Automatic cascading <code>DeleteMany</code> via <code>[HasMany]</code>.",
            "Partial <code>RETURNING</code>/<code>OUTPUT</code> (column subset) to reduce traffic on wide updates.",
            "Additional providers (SQLite, Oracle) as independent packages.",
          ],
        },
      ],
    },
  ],
};

export default en;
