export type Lang = "en" | "pt" | "es";

export type Block =
  | { type: "p"; html: string }
  | { type: "h3"; html: string }
  | { type: "code"; lang: "csharp" | "bash"; code: string }
  | { type: "callout"; variant: "info" | "warn"; html: string }
  | { type: "table"; headers: string[]; rows: string[][] }
  | { type: "ol"; items: string[] }
  | { type: "ul"; items: string[] }
  | {
      type: "tabs";
      tabs: { label: string; lang: "bash" | "csharp"; code: string }[];
    }
  | {
      type: "grid";
      columns?: 2;
      cards: { title: string; body: string; color: string }[];
    };

export interface DocSection {
  id: string;
  group: string;
  navLabel: string;
  title: string;
  blocks: Block[];
}

export interface DocGroup {
  id: string;
  label: string;
}

export interface HeroContent {
  eyebrow: string;
  title: string;
  sub: string;
  ctaPrimary: string;
  ctaSecondary: string;
  stat1: string;
  stat2: string;
  stat3: string;
}

export interface TopbarContent {
  badge: string;
  install: string;
  githubAria: string;
}

export interface NavContent {
  toggleLabel: string;
  toggleAria: string;
}

export interface MiscContent {
  tocTitle: string;
  copyLabel: string;
  copiedLabel: string;
  footer: string;
  metaTitle: string;
  metaDescription: string;
}

export interface LocaleContent {
  hero: HeroContent;
  topbar: TopbarContent;
  nav: NavContent;
  misc: MiscContent;
  groups: DocGroup[];
  sections: DocSection[];
}
