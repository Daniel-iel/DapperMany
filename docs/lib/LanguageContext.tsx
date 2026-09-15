"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { ReactNode } from "react";
import { getContent } from "@/content";
import type { Lang, LocaleContent } from "@/content/types";

interface LanguageContextValue {
  lang: Lang;
  setLang: (lang: Lang) => void;
  content: LocaleContent;
}

const LanguageContext = createContext<LanguageContextValue | null>(null);

const STORAGE_KEY = "dm-lang";

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>("en");

  // Restore saved preference after mount (avoids SSR/client hydration mismatch).
  useEffect(() => {
    try {
      const saved = window.localStorage.getItem(STORAGE_KEY);
      if (saved === "en" || saved === "pt" || saved === "es") {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- one-time restore of a browser-only preference; cannot be read during SSR/initial render.
        setLangState(saved);
      }
    } catch {
      // localStorage unavailable — stay on default.
    }
  }, []);

  useEffect(() => {
    document.documentElement.lang =
      lang === "en" ? "en" : lang === "pt" ? "pt-BR" : "es";
    document.title = getContent(lang).misc.metaTitle;
  }, [lang]);

  const setLang = (next: Lang) => {
    setLangState(next);
    try {
      window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // ignore
    }
  };

  const content = useMemo(() => getContent(lang), [lang]);

  const value = useMemo(
    () => ({ lang, setLang, content }),
    [lang, content],
  );

  return (
    <LanguageContext.Provider value={value}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  const ctx = useContext(LanguageContext);
  if (!ctx) {
    throw new Error("useLanguage must be used within a LanguageProvider");
  }
  return ctx;
}
