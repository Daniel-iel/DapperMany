"use client";

import { useState } from "react";
import Image from "next/image";
import { useLanguage } from "@/lib/LanguageContext";
import { useActiveSection } from "@/lib/useActiveSection";
import Sidebar from "./Sidebar";
import Toc from "./Toc";
import Section from "./Section";

export default function DocsShell() {
  const { content } = useLanguage();
  const [navOpen, setNavOpen] = useState(false);
  const sectionIds = content.sections.map((s) => s.id);
  const activeId = useActiveSection(sectionIds);

  return (
    <div className="layout">
      <button
        type="button"
        className="nav-toggle"
        aria-label={content.nav.toggleAria}
        aria-expanded={navOpen}
        onClick={() => setNavOpen((v) => !v)}
      >
        <svg
          width="20"
          height="20"
          viewBox="0 0 20 20"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.8"
        >
          <path d="M2 5h16M2 10h16M2 15h16" />
        </svg>
        <span>{content.nav.toggleLabel}</span>
      </button>
      <div
        className={"nav-backdrop" + (navOpen ? " open" : "")}
        onClick={() => setNavOpen(false)}
      />

      <Sidebar
        activeId={activeId}
        isOpen={navOpen}
        onLinkClick={() => setNavOpen(false)}
      />

      <main className="content">
        {content.sections.map((section) => (
          <Section section={section} key={section.id} />
        ))}

        <footer className="doc-footer">
          <Image src="/mark.png" alt="" width={26} height={17} />
          <p>{content.misc.footer}</p>
        </footer>
      </main>

      <Toc activeId={activeId} />
    </div>
  );
}
