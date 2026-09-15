"use client";

import { useLanguage } from "@/lib/LanguageContext";
import { LANGS } from "@/content";

export default function LangSwitch() {
  const { lang, setLang } = useLanguage();

  return (
    <div className="lang-switch" role="group" aria-label="Language">
      {LANGS.map((l) => (
        <button
          key={l}
          type="button"
          className={"lang-btn" + (l === lang ? " active" : "")}
          onClick={() => setLang(l)}
        >
          {l.toUpperCase()}
        </button>
      ))}
    </div>
  );
}
