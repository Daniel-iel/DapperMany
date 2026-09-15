import { CODE } from "./code";
import type { LocaleContent } from "./types";

const pt: LocaleContent = {
  hero: {
    eyebrow: "Extensão do Dapper para operações em massa em .NET",
    title:
      "Insira, atualize e remova milhares de registros sem escrever uma linha de SQL.",
    sub: `DapperMany mapeia suas classes para tabelas com attributes, resolve grafos pai‑filho automaticamente e usa o bulk copy nativo de cada banco — SQL Server, PostgreSQL e MySQL — por trás de uma API mínima sobre <code>IDbConnection</code>.`,
    ctaPrimary: "Ver início rápido",
    ctaSecondary: "Instalação",
    stat1: "bancos suportados",
    stat2: "SQL manual",
    stat3: "chamada para grafos inteiros",
  },
  topbar: {
    badge: "v1.0 · pré-lançamento",
    install: "Instalação",
    githubAria: "Repositório no GitHub",
  },
  nav: {
    toggleLabel: "Navegação",
    toggleAria: "Abrir navegação",
  },
  misc: {
    tocTitle: "Nesta página",
    copyLabel: "Copiar",
    copiedLabel: "Copiado",
    footer:
      "DapperMany é um framework open source para .NET. Documentação gerada a partir da especificação do projeto.",
    metaTitle: "DapperMany — Documentação",
    metaDescription:
      "DapperMany — extensão do Dapper para operações em massa: InsertMany, UpdateMany, DeleteMany e inserção automática de grafos pai-filho, com providers para SQL Server, PostgreSQL e MySQL.",
  },
  groups: [
    { id: "start", label: "Começando" },
    { id: "mapping", label: "Mapeamento" },
    { id: "operations", label: "Operações" },
    { id: "internals", label: "Internals" },
    { id: "project", label: "Projeto" },
  ],
  sections: [
    {
      id: "why",
      group: "start",
      navLabel: "Por que DapperMany",
      title: "Por que DapperMany",
      blocks: [
        {
          type: "p",
          html: "O Dapper já resolve muito bem o mapeamento objeto‑relacional linha a linha. O que falta é uma forma performática de mover <strong>muitos</strong> registros de uma vez — inclusive árvores de objetos relacionados — sem cair para SQL escrito à mão nem perder o controle sobre Ids gerados pelo banco.",
        },
        {
          type: "grid",
          cards: [
            {
              title: "API pública mínima",
              body: "O consumidor só enxerga attributes na classe e métodos de extensão sobre <code>IDbConnection</code>. Toda a mecânica interna fica escondida.",
              color: "var(--orange)",
            },
            {
              title: "Zero SQL manual",
              body: "Geração de SQL, bulk copy, resolução de Id gerado e correlação pai‑filho acontecem internamente, por convenção.",
              color: "var(--magenta)",
            },
            {
              title: "Core desacoplado de driver",
              body: "O pacote <code>DapperMany</code> não referencia nenhum driver de banco. Cada provider vive em um pacote NuGet separado.",
              color: "var(--teal)",
            },
            {
              title: "Performance via reflection cacheada",
              body: "Nenhum attribute ou propriedade é lido por reflection em runtime repetidamente — tudo é compilado com Expression Trees e cacheado por tipo.",
              color: "var(--teal-dark)",
            },
          ],
        },
      ],
    },
    {
      id: "install",
      group: "start",
      navLabel: "Instalação",
      title: "Instalação",
      blocks: [
        {
          type: "p",
          html: "Instale o pacote core e o provider do banco que você usa. Ao carregar o assembly, o provider se registra sozinho — não é preciso configuração adicional.",
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
          html: "Extensível por terceiros: qualquer pacote externo pode registrar um novo provider (ex.: <code>DapperMany.Sqlite</code>) implementando <code>ISqlDialect</code>, <code>IBulkCopyStrategy</code> e <code>IIdentityRetrievalStrategy</code>.",
        },
      ],
    },
    {
      id: "quickstart",
      group: "start",
      navLabel: "Início rápido",
      title: "Início rápido",
      blocks: [
        {
          type: "p",
          html: "Mapeie suas classes com attributes e chame as operações de extensão diretamente sobre a conexão.",
        },
        { type: "code", lang: "csharp", code: CODE.entityModel },
        { type: "code", lang: "csharp", code: CODE.quickstartUsage },
      ],
    },
    {
      id: "mapping",
      group: "mapping",
      navLabel: "Attributes de entidade",
      title: "Attributes de entidade",
      blocks: [
        {
          type: "p",
          html: "DapperMany reaproveita <code>System.ComponentModel.DataAnnotations.Schema</code> sempre que possível — quem já conhece EF Core reconhece <code>[Table]</code>, <code>[Key]</code> e <code>[DatabaseGenerated]</code> de imediato. Relações são declaradas com <code>[HasMany]</code> (1:N) e <code>[HasOne]</code> (1:1).",
        },
        { type: "code", lang: "csharp", code: CODE.entityModelWithDetail },
        {
          type: "table",
          headers: ["Attribute", "Uso"],
          rows: [
            [
              `<code>[Table("Name")]</code>`,
              "Mapeia a classe para uma tabela do banco.",
            ],
            [
              "<code>[Key]</code>",
              "Marca a chave primária, usada em updates, deletes e correlação de Id.",
            ],
            [
              "<code>[DatabaseGenerated(Identity)]</code>",
              "Indica que o valor é gerado pelo banco no insert (cenário padrão assumido pela lib).",
            ],
            [
              "<code>[HasMany(foreignKey:)]</code>",
              "Relação 1:N — a FK é propagada do pai para cada item da coleção de filhos.",
            ],
            [
              "<code>[HasOne(foreignKey:)]</code>",
              "Relação 1:1 — a FK é propagada do pai para a referência única do filho.",
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
          html: "Cinco métodos de extensão sobre <code>IDbConnection</code> — nada além disso é exposto ao consumidor.",
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
          html: "<code>InsertManyAsync</code> insere uma coleção plana. <code>InsertManyGraphAsync</code> insere o pai, popula o Id gerado de volta em cada entidade e propaga a FK para os filhos declarados com <code>[HasMany]</code> ou <code>[HasOne]</code>, recursivamente para netos.",
        },
        {
          type: "ol",
          items: [
            `Insere os registros pai — estratégia depende do provider (ver <a href="#providers">Providers</a>).`,
            "Popula o <code>Id</code> gerado de volta em cada entidade pai via setter compilado.",
            "Para cada relação marcada, verifica se há dados: coleção não nula e não vazia (<code>[HasMany]</code>) ou referência não nula (<code>[HasOne]</code>) — do contrário, pula sem montar batch.",
            "Propaga a FK do pai para os filhos e agrupa por tipo.",
            "Executa bulk copy nativo dos filhos agrupados, quando suportado pelo provider.",
            "Repete a recursão para relações aninhadas (netos), se existirem.",
          ],
        },
        {
          type: "callout",
          variant: "warn",
          html: `O insert do "pai" não é bulk‑copy nativo na v1 — é sequencial ou multi‑<code>VALUES</code> com retorno de Id. Decisão aceita porque, no caso de uso típico (Order → Items), o volume de pais é ordens de magnitude menor que o de filhos, onde está o ganho real de performance.`,
        },
        { type: "h3", html: "Casos de borda" },
        {
          type: "p",
          html: "Filhos <code>null</code> ou coleção vazia: insere apenas o pai.",
        },
        { type: "code", lang: "csharp", code: CODE.edgeNull },
        {
          type: "p",
          html: "<code>[HasOne]</code> (1:1) é tratado como um único filho — mesma propagação de FK, mesmo fluxo de bulk.",
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
          html: "Recebe um objeto <strong>parcial</strong>: <code>[Key]</code> obrigatória, usada para o <code>JOIN</code>/<code>MERGE</code>, mais apenas os campos a atualizar. O <code>SET</code> é gerado dinamicamente a partir das propriedades presentes no tipo — sem lista de colunas manual.",
        },
        {
          type: "p",
          html: "Mecanismo: staging table (bulk copy do objeto parcial) seguida de <code>UPDATE ... FROM</code> (SQL Server/Postgres) ou <code>UPDATE ... JOIN</code> (MySQL).",
        },
        {
          type: "callout",
          variant: "info",
          html: "Fora de escopo na v1: dirty‑tracking automático a partir de uma entidade completa. Documentado como possível v2.",
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
          html: "Aceita uma lista de entidades completas ou uma lista de chaves (<code>IEnumerable&lt;object&gt; keys</code>).",
        },
        { type: "code", lang: "csharp", code: CODE.deleteManyCall },
        {
          type: "callout",
          variant: "warn",
          html: "Fora de escopo na v1: cascade automático via <code>[HasMany]</code>. Cada nível precisa ser deletado explicitamente, respeitando as FKs.",
        },
      ],
    },
    {
      id: "transactions",
      group: "operations",
      navLabel: "Transações",
      title: "Transações",
      blocks: [
        {
          type: "p",
          html: "Toda operação em massa é executada obrigatoriamente dentro de uma transação associada ao <code>IDbConnection</code>.",
        },
        {
          type: "grid",
          columns: 2,
          cards: [
            {
              title: "Transação fornecida pelo chamador",
              body: "A lib usa e propaga essa transação para todas as operações internas (pais, filhos, staging tables, bulk copy) e <strong>não</strong> efetua commit ou rollback — essa responsabilidade permanece com quem chamou.",
              color: "var(--orange)",
            },
            {
              title: "Transação criada internamente",
              body: "Quando <code>tx == null</code>, a lib cria a transação via <code>BeginTransaction()</code>, faz commit automático em caso de sucesso, rollback em caso de exceção, e garante o dispose em um bloco <code>finally</code>.",
              color: "var(--teal)",
            },
          ],
        },
        { type: "code", lang: "csharp", code: CODE.txUsage },
        {
          type: "p",
          html: "A transação envolve todas as fases da operação — inserção/atualização/exclusão de pais, população de FKs, bulk copy dos filhos e qualquer leitura de identities. Nenhuma operação modifica o banco fora desse escopo. Isolamento na v1: o padrão do provider, tipicamente <code>ReadCommitted</code>.",
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
          html: "Cada banco tem um pacote próprio e uma estratégia diferente de bulk copy e recuperação de Id gerado.",
        },
        {
          type: "table",
          headers: ["Aspecto", "SQL Server", "PostgreSQL", "MySQL"],
          rows: [
            [
              "Bulk copy nativo",
              "<code>SqlBulkCopy</code>",
              "<code>COPY</code> via <code>NpgsqlBinaryImporter</code>",
              "<code>LOAD DATA</code> via CSV temporário (<code>MySqlBulkLoader</code>)",
            ],
            [
              "Retorno de Id gerado",
              "<code>OUTPUT INSERTED.Id</code>",
              "<code>RETURNING id</code> (multi‑linha)",
              "<code>LAST_INSERT_ID()</code> + incremento sequencial",
            ],
            [
              "Update em massa",
              "Staging + <code>UPDATE...FROM</code> / <code>MERGE</code>",
              "Staging + <code>UPDATE...FROM</code>",
              "Staging + <code>UPDATE...JOIN</code>",
            ],
            ["Complexidade", "Baseline", "Baixa", "Alta"],
          ],
        },
        {
          type: "callout",
          variant: "warn",
          html: "A correlação de Id gerado por posição no <code>VALUES</code> (SQL Server/Postgres) é consistente e usada em produção, mas não é contrato formalmente documentado pela Microsoft — por isso tem cobertura de teste de integração dedicada.",
        },
        { type: "h3", html: "Auto-registro de provider" },
        {
          type: "p",
          html: "O core não conhece nenhum driver concreto. Cada pacote de provider se registra ao ser carregado, via <code>[ModuleInitializer]</code>:",
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
          html: "Fundação transversal usada por todas as operações — nenhuma leitura de attribute ou acesso a propriedade repete reflection em runtime.",
        },
        {
          type: "table",
          headers: ["Item", "Escopo", "Estrutura"],
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
              "DataTable, buffers, correlação de grafo",
              "Local, por chamada",
              "variável local no método",
            ],
          ],
        },
        {
          type: "p",
          html: "<code>EntityMetadata</code> é imutável após construído — leitura concorrente sem necessidade de lock. Conexões (<code>SqlConnection</code>, <code>NpgsqlConnection</code>, <code>MySqlConnection</code>) não são thread‑safe para uso simultâneo na mesma instância, mesma premissa do Dapper puro.",
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
          html: "Em builds de Debug, as operações bulk emitem diagnóstico via <code>Debug.WriteLine()</code>, medido com <code>Stopwatch</code>. Em Release, essas mensagens não existem — tudo fica atrás de <code>#if DEBUG</code>.",
        },
        { type: "code", lang: "bash", code: CODE.loggingExample },
        {
          type: "p",
          html: "Para inserções de grafo, tanto a fase de pais quanto a de filhos são registradas, com um log agregado total ao final. Isso não substitui integrações de observability como OpenTelemetry, que podem ser adicionadas futuramente.",
        },
      ],
    },
    {
      id: "roadmap",
      group: "project",
      navLabel: "Roadmap",
      title: "Roadmap de implementação",
      blocks: [
        {
          type: "ol",
          items: [
            "Fundação: <code>EntityMapper</code> + <code>AccessorFactory</code> + <code>EntityMetadata</code>, com testes de concorrência isolados.",
            "Ambiente Docker com os três bancos e schema inicial.",
            "<code>DapperMany</code> (core) + <code>DapperMany.SqlServer</code>: <code>InsertManyAsync</code> simples, validado com Testcontainers.",
            "Projeto samples: cenário de <code>InsertMany</code> contra SQL Server via Docker local.",
            "<code>InsertManyGraphAsync</code> no SQL Server — 1 nível de relacionamento primeiro, recursão depois.",
            "<code>UpdateManyAsync</code> / <code>DeleteManyAsync</code> no SQL Server, com cenários no samples.",
            "Extrair <code>ISqlDialect</code> / <code>IBulkCopyStrategy</code> / <code>IIdentityRetrievalStrategy</code> como interfaces formais.",
            "<code>DapperMany.Postgres</code> + cenários no samples.",
            "<code>DapperMany.MySql</code> + cenários no samples.",
            "Benchmarks com BenchmarkDotNet comparando com insert linha a linha, por provider.",
            "Documentação e publicação dos pacotes no NuGet.",
          ],
        },
      ],
    },
    {
      id: "out-of-scope",
      group: "project",
      navLabel: "Fora do escopo (v1)",
      title: "Fora do escopo (v1)",
      blocks: [
        {
          type: "ul",
          items: [
            "Dirty‑tracking automático para <code>UpdateMany</code> a partir de entidade completa.",
            `Insert de "pais" verdadeiramente bulk (multi‑<code>VALUES</code> + <code>OUTPUT</code>) para alto volume também no lado pai.`,
            "<code>DeleteMany</code> em cascata automática via <code>[HasMany]</code>.",
            "<code>RETURNING</code>/<code>OUTPUT</code> parcial (subset de colunas) para reduzir tráfego em updates largos.",
            "Providers adicionais (SQLite, Oracle) como pacotes independentes.",
          ],
        },
      ],
    },
  ],
};

export default pt;
