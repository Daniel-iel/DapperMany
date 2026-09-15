"use client";

import { useLanguage } from "@/lib/LanguageContext";
import { CODE } from "@/content/code";
import CodeBlock from "./CodeBlock";

export default function Hero() {
  const { content } = useLanguage();
  const { hero } = content;

  return (
    <section className="hero" id="top">
      <div className="hero-inner">
        <svg className="hero-chevrons" viewBox="0 0 520 300" aria-hidden="true">
          <polyline className="chev chev-1" points="40,40 130,150 40,260" />
          <polyline className="chev chev-2" points="150,40 240,150 150,260" />
          <polyline className="chev chev-3" points="250,40 340,150 250,260" />
          <polyline className="chev chev-4" points="350,40 440,150 350,260" />
          <polyline className="chev chev-5" points="440,40 500,150 440,260" />
        </svg>

        <p className="eyebrow-plain">{hero.eyebrow}</p>
        <h1 className="hero-title">{hero.title}</h1>
        <p
          className="hero-sub"
          dangerouslySetInnerHTML={{ __html: hero.sub }}
        />

        <div className="hero-actions">
          <a href="#quickstart" className="btn btn-primary">
            {hero.ctaPrimary}
          </a>
          <a href="#install" className="btn btn-ghost">
            {hero.ctaSecondary}
          </a>
        </div>

        <div className="hero-code">
          <CodeBlock
            code={CODE.heroUsage}
            lang="csharp"
            fileLabel="Program.cs"
          />
        </div>

        <div className="hero-stats">
          <div className="stat">
            <span className="stat-num">3</span>
            <span className="stat-label">{hero.stat1}</span>
          </div>
          <div className="stat">
            <span className="stat-num">0</span>
            <span className="stat-label">{hero.stat2}</span>
          </div>
          <div className="stat">
            <span className="stat-num">1</span>
            <span className="stat-label">{hero.stat3}</span>
          </div>
        </div>
      </div>
    </section>
  );
}
