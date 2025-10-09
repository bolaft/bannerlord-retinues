#!/usr/bin/env python3

"""
Localization string extractor and synchronizer:
- Scans .cs files for L.S("key", "text") calls
- Keeps strings.json and per-locale XML files in sync.
"""

import sys
import re
import json
from pathlib import Path


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #
#                        config                          #
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #

OUTPUT_REL = Path("Languages/ret_strings.xml")  # relative to this script (now in ./loc)
KEY_PREFIX = "ret_"
JSON_NAME = "strings.json"  # next to this script
LOCS_DIRNAME = "Languages"  # locales live under ./loc/Languages/<LOCALE>/


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #
#                        regexes                         #
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #

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
    return s.replace(r"\\", "\\").replace(r"\"", '"').replace(r"\'", "'")


def unescape_verbatim(s: str) -> str:
    """Unescape C# verbatim string: only "" becomes "."""
    return s.replace('""', '"')


def xml_escape(s: str) -> str:
    s = s.replace("\\n", "{newline}")
    # First, replace Python newlines with Bannerlord's {newline}
    s = (
        s.replace("\r\n", "{newline}")
        .replace("\r", "{newline}")
        .replace("\n", "{newline}")
    )
    # Then escape XML meta-characters
    return (
        s.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace('"', "&quot;")
        .replace("'", "&apos;")
    )


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


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #
#            helpers for JSON & per-locale XML           #
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #


def list_locale_codes(loc_root: Path) -> list[str]:
    """
    Return the list of locale codes from subfolders of ./Languages (relative to script),
    e.g. ["FR", "IT", "CNs", "CNt"]. Ignores files; only directories.
    """
    if not loc_root.exists():
        return []
    codes = []
    for p in sorted(loc_root.iterdir()):
        if p.is_dir():
            codes.append(p.name)
    return codes


def load_or_init_json(json_path: Path) -> list[dict]:
    if json_path.exists():
        try:
            data = json.loads(json_path.read_text(encoding="utf-8"))
            if not isinstance(data, list):
                raise ValueError("strings.json must contain a list")
            # Normalize entries to ensure an 'id' exists (for resilience if a manual edit removed it)
            normalized = []
            for item in data:
                if isinstance(item, dict) and "id" in item and "EN" in item:
                    normalized.append(item)
            return normalized
        except Exception:
            # If corrupted, back it up and re-init
            json_path.rename(json_path.with_suffix(".json.bak"))
    return []


def ensure_json_contains_all_keys(
    json_list: list[dict], ids_to_default: dict[str, str], locale_codes: list[str]
) -> list[dict]:
    """
    Merge scan results into the json list:
    - Add missing keys with EN + nulls per locale.
    - Refresh EN text from current scan.
    - Add any new locale codes with null.
    """
    # Index existing by id
    by_id = {d["id"]: d for d in json_list if isinstance(d, dict) and "id" in d}

    # Add/update current keys
    for _id, default_text in ids_to_default.items():
        if _id in by_id:
            entry = by_id[_id]
            entry["EN"] = default_text
        else:
            entry = {"id": _id, "EN": default_text}
            by_id[_id] = entry
        # Ensure locales present
        for lc in locale_codes:
            if lc not in entry:
                entry[lc] = None

    # Also ensure every entry has all locales (including legacy entries no longer found)
    for entry in by_id.values():
        for lc in locale_codes:
            if lc not in entry:
                entry[lc] = None

    # Return stable order by id
    return [by_id[k] for k in sorted(by_id.keys(), key=str.lower)]


def write_json(json_path: Path, json_list: list[dict]):
    json_path.write_text(
        json.dumps(json_list, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


def write_locale_xml(locale_dir: Path, locale_code: str, json_list: list[dict]):
    """
    Write ./loc/<LOCALE>/ret_strings.xml.
    Only include <string> elements where entry[locale_code] is not None.
    """
    locale_dir.mkdir(parents=True, exist_ok=True)
    out_path = locale_dir / "ret_strings.xml"
    strings = [
        (d["id"], d.get(locale_code))
        for d in json_list
        if isinstance(d, dict) and "id" in d
    ]
    # Filter nulls
    strings = [(sid, txt) for (sid, txt) in strings if txt is not None]

    with out_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n')
        f.write(
            '<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">\n'
        )
        f.write("  <tags>\n")
        f.write(f'    <tag language="{xml_escape(locale_code)}" />\n')
        f.write("  </tags>\n")
        f.write("  <strings>\n")
        for sid, txt in strings:
            f.write(
                f'    <string\n      id="{xml_escape(sid)}"\n      text="{xml_escape(str(txt))}" />\n'
            )
        f.write("  </strings>\n")
        f.write("</base>")


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #
#       main (kept original behavior + additions)        #
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #


def main():
    # Always scan ../src/Retinues relative to this script
    script_dir = Path(__file__).resolve().parent
    root = (script_dir.parent / "src" / "Retinues").resolve()

    # Output path is relative to this script file
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

    # Write DEFAULT XML (unchanged behavior)
    ids_sorted = sorted(entries.keys(), key=str.lower)
    with out_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n')
        f.write(
            '<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">\n'
        )
        f.write("  <tags>\n")
        f.write('    <tag language="English" />\n')
        f.write("  </tags>\n")
        f.write("  <strings>\n")
        for _id in ids_sorted:
            text, _ = entries[_id]
            f.write(
                f'    <string\n      id="{xml_escape(_id)}"\n      text="{xml_escape(text)}" />\n'
            )
        f.write("  </strings>\n")
        f.write("</base>")

    # Report for default
    print(f"[OK] Extracted {len(ids_sorted)} strings from {len(cs_files)} .cs files.")
    print(f"[OK] Wrote default XML: {out_path}")
    if warnings:
        print("\n".join(warnings), file=sys.stderr)

    # ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #
    #              JSON + per-locale generation              #
    # ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ #

    loc_root = script_dir / LOCS_DIRNAME
    locale_codes = list_locale_codes(loc_root)

    json_path = script_dir / JSON_NAME
    json_list = load_or_init_json(json_path)

    ids_to_default = {sid: entries[sid][0] for sid in ids_sorted}
    json_list = ensure_json_contains_all_keys(json_list, ids_to_default, locale_codes)
    write_json(json_path, json_list)
    print(f"[OK] Synced JSON: {json_path}")

    # For each locale subfolder, write a ret_strings.xml from JSON values
    for code in locale_codes:
        write_locale_xml(loc_root / code, code, json_list)
        print(f"[OK] Wrote locale XML: {(loc_root / code / 'ret_strings.xml')}")


if __name__ == "__main__":
    main()
