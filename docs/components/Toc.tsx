"use client";

import { useLanguage } from "@/lib/LanguageContext";

export default function Toc({ activeId }: { activeId: string }) {
  const { content } = useLanguage();
  const { sections } = content;

  const activeSection = sections.find((s) => s.id === activeId);
  if (!activeSection) return <aside className="toc" id="toc" />;

  const siblings = sections.filter((s) => s.group === activeSection.group);

  return (
    <aside className="toc" id="toc">
      <p className="toc-heading">{content.misc.tocTitle}</p>
      <ul className="toc-list">
        {siblings.map((s) => (
          <li key={s.id}>
            <a
              href={`#${s.id}`}
              className={"toc-link" + (s.id === activeId ? " active" : "")}
            >
              {s.navLabel}
            </a>
          </li>
        ))}
      </ul>
    </aside>
  );
}
