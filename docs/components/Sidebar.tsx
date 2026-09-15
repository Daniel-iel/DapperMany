"use client";

import { useEffect, useRef, useState } from "react";
import { useLanguage } from "@/lib/LanguageContext";

export default function Sidebar({
  activeId,
  isOpen,
  onLinkClick,
}: {
  activeId: string;
  isOpen: boolean;
  onLinkClick: () => void;
}) {
  const { content } = useLanguage();
  const { groups, sections } = content;
  const navRef = useRef<HTMLElement>(null);
  const [indicator, setIndicator] = useState({ top: 0, height: 0 });

  useEffect(() => {
    const nav = navRef.current;
    if (!nav) return;
    const activeLink = nav.querySelector<HTMLElement>(
      `.side-link[data-section="${activeId}"]`,
    );
    if (activeLink) {
      setIndicator({ top: activeLink.offsetTop, height: activeLink.offsetHeight });
    }
  }, [activeId, content]);

  return (
    <nav
      className={"sidebar" + (isOpen ? " open" : "")}
      id="sidebar"
      ref={navRef}
    >
      <div
        className="sidebar-indicator"
        style={{ top: indicator.top, height: indicator.height }}
      />
      {groups.map((group) => (
        <div className="side-group" key={group.id}>
          <p className="side-heading">{group.label}</p>
          {sections
            .filter((s) => s.group === group.id)
            .map((s) => (
              <a
                key={s.id}
                href={`#${s.id}`}
                className={"side-link" + (s.id === activeId ? " active" : "")}
                data-section={s.id}
                onClick={onLinkClick}
              >
                {s.navLabel}
              </a>
            ))}
        </div>
      ))}
    </nav>
  );
}
