# DapperMany — Documentation (Next.js)

Documentation site for **DapperMany**, a Dapper extension for bulk
operations. Built with Next.js 16 (App Router) + TypeScript, no CSS
framework — a hand-written design system that mirrors the project's logo
colors.

## Getting started

```bash
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

To build for production:

```bash
npm run build
npm run start
```

## Features

- **Trilingual** (English / Portuguese / Spanish), switchable instantly
  without a page reload, with the choice persisted in `localStorage`.
  Code samples stay in English across all languages — the common
  convention in technical documentation.
- **"On this page" right rail** (like the Next.js docs) that mirrors the
  sidebar group of whichever section is currently in view.
- Scroll-synced left sidebar with a sliding active-section indicator.
- Tabs for the install snippets (SQL Server / PostgreSQL / MySQL).
- A small dependency-free syntax highlighter for the C# and bash code
  blocks, plus a copy-to-clipboard button.
- Responsive: the right rail hides below `1220px`, and the left sidebar
  becomes a slide-in drawer below `860px`.

## Project structure

```
app/
  layout.tsx        Root layout — wraps everything in <LanguageProvider>
  page.tsx           Assembles ProgressBar + Topbar + Hero + DocsShell
  globals.css         The entire design system (tokens, layout, components)
  icon.png            Favicon (auto-picked up by Next.js)

components/
  Topbar.tsx, Hero.tsx, Sidebar.tsx, Toc.tsx, DocsShell.tsx
  Section.tsx          Generic renderer for a section's content "blocks"
  CodeBlock.tsx, CodeTabs.tsx, LangSwitch.tsx, ProgressBar.tsx

content/
  types.ts             Shared TypeScript types (Block, DocSection, ...)
  code.ts               C#/bash snippets, shared across all 3 languages
  en.ts / pt.ts / es.ts  Full site copy per language, as an array of
                          sections, each made of typed content "blocks"
                          (paragraph, code, table, callout, list, grid, tabs)
  index.ts              CONTENT map + getContent(lang) helper

lib/
  LanguageContext.tsx   React Context holding the active language
  useActiveSection.ts   Scrollspy hook shared by Sidebar and Toc
  highlight.ts           The syntax highlighter
```

## Editing content

All copy lives in `content/en.ts`, `content/pt.ts` and `content/es.ts`.
Each file exports the same shape (see `content/types.ts`): a `sections`
array where every section is a list of typed **blocks**. To change text,
find the matching block and edit its `html` (or `items`/`rows`/`cards`)
field — no JSX editing required. To add a new section, add an entry to
all three language files with a matching `id` and `group`, then add its
`id` next to its siblings' order (sections render in array order).

Code snippets are shared (not translated) and live in `content/code.ts`.

## Adding a fourth language

1. Add the new code to the `Lang` union in `content/types.ts`.
2. Create `content/xx.ts` following the same shape as `en.ts`.
3. Register it in `content/index.ts` (`CONTENT` and `LANGS`).

No other changes are needed — the language switcher, persistence and all
components read from `LANGS`/`CONTENT` automatically.
