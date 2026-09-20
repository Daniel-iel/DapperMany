"use client";

import { useEffect, useState } from "react";

/**
 * Tracks which section id is currently "active" based on scroll position.
 * A section becomes active once its top crosses a fixed offset below the
 * sticky topbar, and stays active until the next section crosses it too.
 */
export function useActiveSection(sectionIds: string[], offset = 150) {
  const [activeId, setActiveId] = useState<string>(sectionIds[0] ?? "");

  useEffect(() => {
    if (sectionIds.length === 0) return;

    let ticking = false;

    function compute() {
      let current = sectionIds[0];
      for (const id of sectionIds) {
        const el = document.getElementById(id);
        if (!el) continue;
        const top = el.getBoundingClientRect().top;
        if (top - offset <= 0) {
          current = id;
        } else {
          break;
        }
      }
      setActiveId(current);
      ticking = false;
    }

    function onScroll() {
      if (!ticking) {
        window.requestAnimationFrame(compute);
        ticking = true;
      }
    }

    document.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll);
    compute();

    return () => {
      document.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sectionIds.join(","), offset]);

  return activeId;
}
