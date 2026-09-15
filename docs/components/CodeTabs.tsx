"use client";

import { useState } from "react";
import CodeBlock from "./CodeBlock";

export default function CodeTabs({
  tabs,
}: {
  tabs: { label: string; lang: "bash" | "csharp"; code: string }[];
}) {
  const [active, setActive] = useState(0);

  return (
    <div className="tabs">
      <div className="tab-list" role="tablist">
        {tabs.map((tab, i) => (
          <button
            key={tab.label}
            type="button"
            role="tab"
            className={"tab-btn" + (i === active ? " active" : "")}
            aria-selected={i === active}
            onClick={() => setActive(i)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {tabs.map((tab, i) => (
        <div
          key={tab.label}
          className={"tab-panel" + (i === active ? " active" : "")}
        >
          {i === active && <CodeBlock code={tab.code} lang={tab.lang} />}
        </div>
      ))}
    </div>
  );
}
