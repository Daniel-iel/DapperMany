import { CODE } from "./code";
import type { LocaleContent } from "./types";

const es: LocaleContent = {
  hero: {
    eyebrow: "Extensión de Dapper para operaciones en masa en .NET",
    title:
      "Inserta, actualiza y elimina miles de registros sin escribir una línea de SQL.",
    sub: `DapperMany mapea tus clases a tablas con attributes, resuelve grafos padre-hijo automáticamente y usa el bulk copy nativo de cada base de datos — SQL Server, PostgreSQL y MySQL — detrás de una API mínima sobre <code>IDbConnection</code>.`,
    ctaPrimary: "Ver inicio rápido",
    ctaSecondary: "Instalación",
    stat1: "bases de datos soportadas",
    stat2: "SQL manual",
    stat3: "llamada para grafos completos",
  },
  topbar: {
    badge: "v1.0 · pre-lanzamiento",
    install: "Instalación",
    githubAria: "Repositorio en GitHub",
  },
  nav: {
    toggleLabel: "Navegación",
    toggleAria: "Abrir navegación",
  },
  misc: {
    tocTitle: "En esta página",
    copyLabel: "Copiar",
    copiedLabel: "Copiado",
    footer:
      "DapperMany es un framework open source para .NET. Documentación generada a partir de la especificación del proyecto.",
    metaTitle: "DapperMany — Documentación",
    metaDescription:
      "DapperMany — extensión de Dapper para operaciones en masa: InsertMany, UpdateMany, DeleteMany e inserción automática de grafos padre-hijo, con providers para SQL Server, PostgreSQL y MySQL.",
  },
  groups: [
    { id: "start", label: "Empezando" },
    { id: "mapping", label: "Mapeo" },
    { id: "operations", label: "Operaciones" },
    { id: "internals", label: "Internals" },
    { id: "project", label: "Proyecto" },
  ],
  sections: [
    {
      id: "why",
      group: "start",
      navLabel: "Por qué DapperMany",
      title: "Por qué DapperMany",
      blocks: [
        {
          type: "p",
          html: "Dapper ya resuelve muy bien el mapeo objeto-relacional línea a línea. Lo que falta es una forma performante de mover <strong>muchos</strong> registros a la vez — incluyendo árboles de objetos relacionados — sin recurrir a SQL escrito a mano ni perder el control sobre los Ids generados por la base de datos.",
        },
        {
          type: "grid",
          cards: [
            {
              title: "API pública mínima",
              body: "El consumidor solo ve attributes en la clase y métodos de extensión sobre <code>IDbConnection</code>. Toda la mecánica interna queda oculta.",
              color: "var(--orange)",
            },
            {
              title: "Cero SQL manual",
              body: "La generación de SQL, el bulk copy, la resolución del Id generado y la correlación padre-hijo ocurren internamente, por convención.",
              color: "var(--magenta)",
            },
            {
              title: "Núcleo desacoplado del driver",
              body: "El paquete <code>DapperMany</code> no referencia ningún driver de base de datos. Cada provider vive en un paquete NuGet independiente.",
              color: "var(--teal)",
            },
            {
              title: "Rendimiento vía reflection cacheada",
              body: "Ningún attribute o propiedad se lee por reflection repetidamente en runtime — todo se compila con Expression Trees y se cachea por tipo.",
              color: "var(--teal-dark)",
            },
          ],
        },
      ],
    },
    {
      id: "install",
      group: "start",
      navLabel: "Instalación",
      title: "Instalación",
      blocks: [
        {
          type: "p",
          html: "Instala el paquete core y el provider de la base de datos que usas. Al cargar el assembly, el provider se registra solo — no hace falta configuración adicional.",
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
          html: "Extensible por terceros: cualquier paquete externo puede registrar un nuevo provider (ej.: <code>DapperMany.Sqlite</code>) implementando <code>ISqlDialect</code>, <code>IBulkCopyStrategy</code> e <code>IIdentityRetrievalStrategy</code>.",
        },
      ],
    },
    {
      id: "quickstart",
      group: "start",
      navLabel: "Inicio rápido",
      title: "Inicio rápido",
      blocks: [
        {
          type: "p",
          html: "Mapea tus clases con attributes y llama a las operaciones de extensión directamente sobre la conexión.",
        },
        { type: "code", lang: "csharp", code: CODE.entityModel },
        { type: "code", lang: "csharp", code: CODE.quickstartUsage },
      ],
    },
    {
      id: "mapping",
      group: "mapping",
      navLabel: "Attributes de entidad",
      title: "Attributes de entidad",
      blocks: [
        {
          type: "p",
          html: "DapperMany reutiliza <code>System.ComponentModel.DataAnnotations.Schema</code> siempre que es posible — quien ya conoce EF Core reconoce <code>[Table]</code>, <code>[Key]</code> y <code>[DatabaseGenerated]</code> de inmediato. Las relaciones se declaran con <code>[HasMany]</code> (1:N) y <code>[HasOne]</code> (1:1).",
        },
        { type: "code", lang: "csharp", code: CODE.entityModelWithDetail },
        {
          type: "table",
          headers: ["Attribute", "Uso"],
          rows: [
            [
              `<code>[Table("Name")]</code>`,
              "Mapea la clase a una tabla de la base de datos.",
            ],
            [
              "<code>[Key]</code>",
              "Marca la clave primaria, usada en updates, deletes y correlación de Id.",
            ],
            [
              "<code>[DatabaseGenerated(Identity)]</code>",
              "Indica que el valor es generado por la base de datos en el insert (escenario por defecto asumido por la lib).",
            ],
            [
              "<code>[HasMany(foreignKey:)]</code>",
              "Relación 1:N — la FK se propaga del padre a cada elemento de la colección de hijos.",
            ],
            [
              "<code>[HasOne(foreignKey:)]</code>",
              "Relación 1:1 — la FK se propaga del padre a la referencia única del hijo.",
            ],
          ],
        },
      ],
    },
    {
      id: "api",
      group: "mapping",
      navLabel: "API pública",
      title: "API pública",
      blocks: [
        {
          type: "p",
          html: "Cinco métodos de extensión sobre <code>IDbConnection</code> — nada más se expone al consumidor.",
        },
        { type: "code", lang: "csharp", code: CODE.publicApi },
      ],
    },
    {
      id: "insert-many",
      group: "operations",
      navLabel: "InsertMany",
      title: "InsertMany",
      blocks: [
        {
          type: "p",
          html: "<code>InsertManyAsync</code> detecta automáticamente las relaciones. Si el tipo de entidad tiene atributos <code>[HasMany]</code> o <code>[HasOne]</code>, inserta padres, rellena el Id generado de vuelta en cada entidad y propaga la FK a los hijos recursivamente. Si no, realiza una inserción de colección plana.",
        },
        {
          type: "ol",
          items: [
            `Inserta los registros padre — la estrategia depende del provider (ver <a href="#providers">Providers</a>).`,
            "Rellena el <code>Id</code> generado de vuelta en cada entidad padre mediante un setter compilado.",
            "Para cada relación marcada, verifica si hay datos: colección no nula y no vacía (<code>[HasMany]</code>) o referencia no nula (<code>[HasOne]</code>) — de lo contrario, la omite sin construir el batch.",
            "Propaga la FK del padre a los hijos y los agrupa por tipo.",
            "Ejecuta bulk copy nativo de los hijos agrupados, cuando el provider lo soporta.",
            "Repite la recursión para relaciones anidadas (nietos), si existen.",
          ],
        },
        {
          type: "callout",
          variant: "warn",
          html: `El insert del "padre" no es bulk copy nativo en la v1 — es secuencial o multi-<code>VALUES</code> con retorno de Id. Decisión aceptada porque, en el caso de uso típico (Order → Items), el volumen de padres es órdenes de magnitud menor que el de hijos, donde está la ganancia real de rendimiento.`,
        },
        { type: "h3", html: "Casos límite" },
        {
          type: "p",
          html: "Hijos <code>null</code> o colección vacía: inserta solo el padre.",
        },
        { type: "code", lang: "csharp", code: CODE.edgeNull },
        {
          type: "p",
          html: "<code>[HasOne]</code> (1:1) se trata como un único hijo — misma propagación de FK, mismo flujo de bulk.",
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
          html: "Recibe un objeto <strong>parcial</strong>: <code>[Key]</code> obligatoria, usada para el <code>JOIN</code>/<code>MERGE</code>, más solo los campos a actualizar. El <code>SET</code> se genera dinámicamente a partir de las propiedades presentes en el tipo — sin lista de columnas manual.",
        },
        {
          type: "p",
          html: "Mecanismo: staging table (bulk copy del objeto parcial) seguida de <code>UPDATE ... FROM</code> (SQL Server/Postgres) o <code>UPDATE ... JOIN</code> (MySQL).",
        },
        {
          type: "callout",
          variant: "info",
          html: "Fuera de alcance en v1: dirty-tracking automático a partir de una entidad completa. Documentado como posible v2.",
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
          html: "Acepta una lista de entidades completas o una lista de claves (<code>IEnumerable&lt;object&gt; keys</code>).",
        },
        { type: "code", lang: "csharp", code: CODE.deleteManyCall },
        {
          type: "callout",
          variant: "warn",
          html: "Fuera de alcance en v1: cascade automático vía <code>[HasMany]</code>. Cada nivel debe eliminarse explícitamente, respetando las FKs.",
        },
      ],
    },
    {
      id: "transactions",
      group: "operations",
      navLabel: "Transacciones",
      title: "Transacciones",
      blocks: [
        {
          type: "p",
          html: "Toda operación en masa se ejecuta obligatoriamente dentro de una transacción asociada al <code>IDbConnection</code>.",
        },
        {
          type: "grid",
          columns: 2,
          cards: [
            {
              title: "Transacción proporcionada por el llamador",
              body: "La lib usa y propaga esa transacción a todas las operaciones internas (padres, hijos, staging tables, bulk copy) y <strong>no</strong> realiza commit ni rollback — esa responsabilidad permanece con quien la llamó.",
              color: "var(--orange)",
            },
            {
              title: "Transacción creada internamente",
              body: "Cuando <code>tx == null</code>, la lib crea la transacción vía <code>BeginTransaction()</code>, hace commit automático en caso de éxito, rollback en caso de excepción, y garantiza el dispose en un bloque <code>finally</code>.",
              color: "var(--teal)",
            },
          ],
        },
        { type: "code", lang: "csharp", code: CODE.txUsage },
        {
          type: "p",
          html: "La transacción abarca todas las fases de la operación — inserción/actualización/eliminación de padres, población de FKs, bulk copy de los hijos y cualquier lectura de identities. Ninguna operación modifica la base de datos fuera de ese alcance. Aislamiento en v1: el predeterminado del provider, típicamente <code>ReadCommitted</code>.",
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
          html: "Cada base de datos tiene su propio paquete y una estrategia diferente de bulk copy y recuperación del Id generado.",
        },
        {
          type: "table",
          headers: ["Aspecto", "SQL Server", "PostgreSQL", "MySQL"],
          rows: [
            [
              "Bulk copy nativo",
              "<code>SqlBulkCopy</code>",
              "<code>COPY</code> via <code>NpgsqlBinaryImporter</code>",
              "<code>LOAD DATA</code> vía CSV temporal (<code>MySqlBulkLoader</code>)",
            ],
            [
              "Retorno del Id generado",
              "<code>OUTPUT INSERTED.Id</code>",
              "<code>RETURNING id</code> (multi-fila)",
              "<code>LAST_INSERT_ID()</code> + incremento secuencial",
            ],
            [
              "Actualización en masa",
              "Staging + <code>UPDATE...FROM</code> / <code>MERGE</code>",
              "Staging + <code>UPDATE...FROM</code>",
              "Staging + <code>UPDATE...JOIN</code>",
            ],
            ["Complejidad", "Base", "Baja", "Alta"],
          ],
        },
        {
          type: "callout",
          variant: "warn",
          html: "La correlación del Id generado por posición en el <code>VALUES</code> (SQL Server/Postgres) es consistente y se usa en producción, pero no es un contrato formalmente documentado por Microsoft — por eso cuenta con cobertura de pruebas de integración dedicada.",
        },
        { type: "h3", html: "Autoregistro de provider" },
        {
          type: "p",
          html: "El core no conoce ningún driver concreto. Cada paquete de provider se registra al cargarse, vía <code>[ModuleInitializer]</code>:",
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
          html: "Una base transversal usada por todas las operaciones — ningún attribute o acceso a propiedad repite reflection en runtime.",
        },
        {
          type: "table",
          headers: ["Item", "Alcance", "Estructura"],
          rows: [
            [
              "EntityMetadata",
              "Estático, global, por Type",
              "<code>ConcurrentDictionary&lt;Type, Lazy&lt;EntityMetadata&gt;&gt;</code>",
            ],
            [
              "Getters/Setters compilados",
              "Estático, global, por PropertyInfo",
              "<code>ConcurrentDictionary&lt;PropertyInfo, Delegate&gt;</code>",
            ],
            [
              "DataTable, buffers, correlación de grafo",
              "Local, por llamada",
              "variable local al método",
            ],
          ],
        },
        {
          type: "p",
          html: "<code>EntityMetadata</code> es inmutable una vez construido — lectura concurrente sin necesidad de lock. Las conexiones (<code>SqlConnection</code>, <code>NpgsqlConnection</code>, <code>MySqlConnection</code>) no son thread-safe para uso simultáneo en la misma instancia, la misma premisa que Dapper puro.",
        },
      ],
    },
    {
      id: "logging",
      group: "internals",
      navLabel: "Logging (Debug)",
      title: "Logging (solo Debug)",
      blocks: [
        {
          type: "p",
          html: "En builds de Debug, las operaciones bulk emiten diagnóstico vía <code>Debug.WriteLine()</code>, medido con <code>Stopwatch</code>. En Release, esos mensajes no existen — todo queda detrás de <code>#if DEBUG</code>.",
        },
        { type: "code", lang: "bash", code: CODE.loggingExample },
        {
          type: "p",
          html: "Para inserciones de grafo, se registran tanto la fase de padres como la de hijos, con un log agregado total al final. Esto no reemplaza integraciones de observability como OpenTelemetry, que podrían añadirse en el futuro.",
        },
      ],
    },
    {
      id: "roadmap",
      group: "project",
      navLabel: "Hoja de ruta",
      title: "Hoja de ruta de implementación",
      blocks: [
        {
          type: "ol",
          items: [
            "Fundamento: <code>EntityMapper</code> + <code>AccessorFactory</code> + <code>EntityMetadata</code>, con pruebas de concurrencia aisladas.",
            "Entorno Docker con las tres bases de datos y esquema inicial.",
            "<code>DapperMany</code> (core) + <code>DapperMany.SqlServer</code>: <code>InsertManyAsync</code> simple, validado con Testcontainers.",
            "Proyecto samples: escenario de <code>InsertMany</code> contra SQL Server vía Docker local.",
            "<code>InsertManyAsync</code> en SQL Server — primero 1 nivel de relación, luego recursión.",
            "<code>UpdateManyAsync</code> / <code>DeleteManyAsync</code> en SQL Server, con escenarios en samples.",
            "Extraer <code>ISqlDialect</code> / <code>IBulkCopyStrategy</code> / <code>IIdentityRetrievalStrategy</code> como interfaces formales.",
            "<code>DapperMany.Postgres</code> + escenarios en samples.",
            "<code>DapperMany.MySql</code> + escenarios en samples.",
            "Benchmarks con BenchmarkDotNet comparando con insert fila a fila, por provider.",
            "Documentación y publicación de los paquetes en NuGet.",
          ],
        },
      ],
    },
    {
      id: "out-of-scope",
      group: "project",
      navLabel: "Fuera de alcance (v1)",
      title: "Fuera de alcance (v1)",
      blocks: [
        {
          type: "ul",
          items: [
            "Dirty-tracking automático para <code>UpdateMany</code> a partir de una entidad completa.",
            `Inserts de "padres" verdaderamente bulk (multi-<code>VALUES</code> + <code>OUTPUT</code>) para alto volumen también del lado padre.`,
            "<code>DeleteMany</code> en cascada automática vía <code>[HasMany]</code>.",
            "<code>RETURNING</code>/<code>OUTPUT</code> parcial (subconjunto de columnas) para reducir tráfico en updates amplios.",
            "Providers adicionales (SQLite, Oracle) como paquetes independientes.",
          ],
        },
      ],
    },
  ],
};

export default es;
