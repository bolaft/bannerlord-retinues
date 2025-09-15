#!/usr/bin/env python3
"""
Extract localized strings from C# calls like:
    L.S("unlock_btn", "Unlock")
    L.T('confirm', 'Confirm')

- Keys are automatically prefixed with 'ret_' (unless already prefixed).
- If the same key has different fallbacks across files, a warning is printed and
  the first encountered value is kept.
- Output: ./loc/en/retinues_strings.xml (relative to this script).
"""

import sys
import re
from pathlib import Path

# ---------- config ----------
OUTPUT_REL = Path("loc/ret_strings.xml")  # relative to this script
KEY_PREFIX = "ret_"

# ---------- regexes ----------
# Double-quoted: L.S("key", "text")
RE_DQ = re.compile(
    r"""L\.(?:S|T)\(\s*"(?P<key>(?:\\.|[^"\\])*)"\s*,\s*"(?P<text>(?:\\.|[^"\\])*)"\s*\)""",
    re.DOTALL,
)

# Single-quoted: L.S('key', 'text')
RE_SQ = re.compile(
    r"""L\.(?:S|T)\(\s*'(?P<key>(?:\\.|[^'\\])*)'\s*,\s*'(?P<text>(?:\\.|[^'\\])*)'\s*\)""",
    re.DOTALL,
)

# Verbatim strings: L.S(@"key", @"text")
RE_VB = re.compile(
    r"""L\.(?:S|T)\(\s*@\"(?P<key>(?:\"\"|[^"])*)\"\s*,\s*@\"(?P<text>(?:\"\"|[^"])*)\"\s*\)""",
    re.DOTALL,
)

def unescape_regular(s: str) -> str:
    """Unescape C#-style regular string (very small subset)."""
    return (
        s.replace(r"\\", "\\")
         .replace(r"\"" , '"')
         .replace(r"\'" , "'")
    )

def unescape_verbatim(s: str) -> str:
    """Unescape C# verbatim string: only "" becomes "."""
    return s.replace('""', '"')

def xml_escape(s: str) -> str:
    # First map actual line breaks to entities
    s = s.replace("\r\n", "&#13;&#10;").replace("\r", "&#13;").replace("\n", "&#10;")
    # Then escape XML meta-characters
    return (s.replace("&", "&amp;")
             .replace("<", "&lt;")
             .replace(">", "&gt;")
             .replace('"', "&quot;")
             .replace("'", "&apos;"))

def scan_file(path: Path):
    text = path.read_text(encoding="utf-8", errors="ignore")
    hits = []

    for m in RE_DQ.finditer(text):
        key = unescape_regular(m.group("key"))
        val = unescape_regular(m.group("text"))
        hits.append((key, val, path))

    for m in RE_SQ.finditer(text):
        key = unescape_regular(m.group("key"))
        val = unescape_regular(m.group("text"))
        hits.append((key, val, path))

    for m in RE_VB.finditer(text):
        key = unescape_verbatim(m.group("key"))
        val = unescape_verbatim(m.group("text"))
        hits.append((key, val, path))

    return hits

def main():
    # Root to scan: argv[1] or current working dir
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path.cwd()

    # Output path is relative to this script file
    script_dir = Path(__file__).resolve().parent
    out_path = (script_dir / OUTPUT_REL).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)

    # Collect
    entries = {}  # id -> (text, first_path)
    warnings = []

    cs_files = list(root.rglob("*.cs"))
    for cs in cs_files:
        for key, text, src in scan_file(cs):
            # prefix
            full_id = key if key.startswith(KEY_PREFIX) else f"{KEY_PREFIX}{key}"

            if full_id in entries:
                existing_text, first_src = entries[full_id]
                if existing_text != text:
                    warnings.append(
                        f"[WARN] Key '{full_id}' has differing fallbacks:\n"
                        f"       First: '{existing_text}' (from {first_src})\n"
                        f"       New:   '{text}' (from {src})\n"
                        f"       Using the first."
                    )
                continue

            entries[full_id] = (text, src)

    # Write XML
    ids_sorted = sorted(entries.keys(), key=str.lower)
    with out_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n')
        f.write('<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">\n')
        f.write('  <tags>\n')
        f.write('    <tag language="English" />\n')
        f.write('  </tags>\n')
        f.write('  <strings>\n')
        for _id in ids_sorted:
            text, _ = entries[_id]
            f.write(f'    <string\n      id="{xml_escape(_id)}"\n      text="{xml_escape(text)}" />\n')
        f.write('  </strings>\n')
        f.write('</base>')

    # Report
    print(f"[OK] Extracted {len(ids_sorted)} strings from {len(cs_files)} .cs files.")
    print(f"[OK] Wrote: {out_path}")
    if warnings:
        print("\n".join(warnings), file=sys.stderr)

if __name__ == "__main__":
    main()
