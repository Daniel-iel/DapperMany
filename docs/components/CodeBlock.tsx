"use client";

import { useState } from "react";
import { highlight } from "@/lib/highlight";
import { useLanguage } from "@/lib/LanguageContext";

export default function CodeBlock({
  code,
  lang,
  fileLabel,
}: {
  code: string;
  lang: "csharp" | "bash";
  fileLabel?: string;
}) {
  const { content } = useLanguage();
  const [copied, setCopied] = useState(false);
  const html = highlight(code, lang);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      // clipboard unavailable — ignore
    }
  };

  return (
    <div className={fileLabel ? "code-window" : undefined}>
      {fileLabel && (
        <div className="code-dots">
          <span />
          <span />
          <span />
          <span className="code-label">{fileLabel}</span>
        </div>
      )}
      <pre>
        <code dangerouslySetInnerHTML={{ __html: html }} />
        <button
          type="button"
          className="copy-btn"
          onClick={handleCopy}
          aria-label={copied ? content.misc.copiedLabel : content.misc.copyLabel}
        >
          {copied ? content.misc.copiedLabel : content.misc.copyLabel}
        </button>
      </pre>
    </div>
  );
}
