import en from "./en";
import pt from "./pt";
import es from "./es";
import type { Lang, LocaleContent } from "./types";

export const CONTENT: Record<Lang, LocaleContent> = { en, pt, es };

export const LANGS: Lang[] = ["en", "pt", "es"];

export function getContent(lang: Lang): LocaleContent {
  return CONTENT[lang] ?? CONTENT.en;
}

export * from "./types";
