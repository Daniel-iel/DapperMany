import type { Block, DocSection } from "@/content/types";
import CodeBlock from "./CodeBlock";
import CodeTabs from "./CodeTabs";

function Html({ html, as: Tag = "p" }: { html: string; as?: "p" | "h3" }) {
  return <Tag dangerouslySetInnerHTML={{ __html: html }} />;
}

function BlockRenderer({ block }: { block: Block }) {
  switch (block.type) {
    case "p":
      return <Html html={block.html} as="p" />;
    case "h3":
      return <Html html={block.html} as="h3" />;
    case "code":
      return <CodeBlock code={block.code} lang={block.lang} />;
    case "tabs":
      return <CodeTabs tabs={block.tabs} />;
    case "callout":
      return (
        <div className={`callout callout-${block.variant}`}>
          <p dangerouslySetInnerHTML={{ __html: block.html }} />
        </div>
      );
    case "ol":
      return (
        <ol className="flow-list numbered">
          {block.items.map((item, i) => (
            <li key={i} dangerouslySetInnerHTML={{ __html: item }} />
          ))}
        </ol>
      );
    case "ul":
      return (
        <ul className="plain-list">
          {block.items.map((item, i) => (
            <li key={i} dangerouslySetInnerHTML={{ __html: item }} />
          ))}
        </ul>
      );
    case "table":
      return (
        <div className="table-scroll">
          <table className="ref-table compare-table">
            <thead>
              <tr>
                {block.headers.map((h, i) => (
                  <th key={i}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {block.rows.map((row, r) => (
                <tr key={r}>
                  {row.map((cell, c) => (
                    <td key={c} dangerouslySetInnerHTML={{ __html: cell }} />
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    case "grid":
      return (
        <div
          className={
            "principle-grid" + (block.columns === 2 ? " two-col" : "")
          }
        >
          {block.cards.map((card, i) => (
            <div className="principle" key={i}>
              <span
                className="principle-mark"
                style={{ ["--c" as string]: card.color }}
              />
              <h3>{card.title}</h3>
              <p dangerouslySetInnerHTML={{ __html: card.body }} />
            </div>
          ))}
        </div>
      );
    default:
      return null;
  }
}

export default function Section({ section }: { section: DocSection }) {
  return (
    <section id={section.id} className="doc-section">
      <h2>{section.title}</h2>
      {section.blocks.map((block, i) => (
        <BlockRenderer block={block} key={i} />
      ))}
    </section>
  );
}
