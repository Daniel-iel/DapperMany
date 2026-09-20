const CS_KEYWORDS = new Set([
  "public", "private", "internal", "protected", "static", "async", "await",
  "class", "void", "var", "new", "using", "return", "if", "else", "for",
  "foreach", "in", "null", "true", "false", "this", "get", "set", "readonly",
  "sealed", "record", "namespace", "interface", "override", "virtual", "out",
  "ref", "try", "catch", "finally", "throw",
]);

const CS_TYPES = new Set([
  "Task", "IDbConnection", "IDbTransaction", "IEnumerable", "List", "string",
  "int", "object", "DateTime", "DatabaseGeneratedOption",
  "ConcurrentDictionary", "Lazy", "Type", "PropertyInfo", "Delegate",
]);

function escapeHtml(s: string): string {
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

export function highlightCsharp(code: string): string {
  return code
    .split("\n")
    .map((line) => {
      const commentIdx = line.indexOf("//");
      const codePart = commentIdx >= 0 ? line.slice(0, commentIdx) : line;
      const commentPart = commentIdx >= 0 ? line.slice(commentIdx) : "";

      const regex =
        /("([^"\\]|\\.)*")|(\[[A-Za-z_][A-Za-z0-9_]*(\([^)]*\))?\])|(\b\d+\b)|([A-Za-z_][A-Za-z0-9_]*)|(\s+)|([^\sA-Za-z0-9_])/g;
      let m: RegExpExecArray | null;
      let result = "";
      while ((m = regex.exec(codePart)) !== null) {
        const tok = m[0];
        if (m[1]) {
          result += `<span class="tok-str">${escapeHtml(tok)}</span>`;
        } else if (m[3]) {
          result += `<span class="tok-attr">${escapeHtml(tok)}</span>`;
        } else if (m[5]) {
          result += `<span class="tok-num">${escapeHtml(tok)}</span>`;
        } else if (m[6]) {
          if (CS_KEYWORDS.has(tok)) {
            result += `<span class="tok-kw">${escapeHtml(tok)}</span>`;
          } else if (CS_TYPES.has(tok) || /^[A-Z]/.test(tok)) {
            result += `<span class="tok-type">${escapeHtml(tok)}</span>`;
          } else {
            result += escapeHtml(tok);
          }
        } else {
          result += escapeHtml(tok);
        }
      }
      if (commentPart) {
        result += `<span class="tok-com">${escapeHtml(commentPart)}</span>`;
      }
      return result;
    })
    .join("\n");
}

export function highlightBash(code: string): string {
  return code
    .split("\n")
    .map((line) => {
      const m = line.match(/^(\s*)([a-zA-Z0-9_.-]+)(.*)$/);
      if (!m) return escapeHtml(line);
      return (
        escapeHtml(m[1]) +
        `<span class="tok-fn">${escapeHtml(m[2])}</span>` +
        escapeHtml(m[3])
      );
    })
    .join("\n");
}

export function highlight(code: string, lang: "csharp" | "bash"): string {
  return lang === "bash" ? highlightBash(code) : highlightCsharp(code);
}
