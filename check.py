#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Scan ./src/Retinues/**.cs for "section banner" comment blocks and report non-conforming ones.

Checks two styles:

1) 60-char wide 3-line blocks (from '//' to ending '//' inclusive, indentation ignored):
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                        Overrides                       //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

Rules:
- Each of the 3 lines, after strip(), must be exactly 60 chars wide.
- The middle row's inner padding: left spaces == right spaces OR left spaces == right spaces + 1.

2) 30-char wide single-line blocks (from '/*' to '*/' inclusive, indentation ignored):
    /* ━━━━━━━━━ Test ━━━━━━━━━ */

Rules:
- The line, after strip(), must be exactly 30 chars wide.
- Count of '━' on left == right OR left == right + 1.

Output:
- Summary of all non-conforming blocks with filename + 1-based line number.
"""

from __future__ import annotations

import argparse
import os
import re
from dataclasses import dataclass
from pathlib import Path
from typing import List, Optional, Tuple


RE_60_BORDER = re.compile(r"^//\s*━+\s*//$")
RE_60_ANY = re.compile(r"^//.*//$")

RE_30 = re.compile(r"^/\*\s*(?P<l>━+)\s+(?P<t>.+?)\s+(?P<r>━+)\s*\*/$")


@dataclass
class Issue:
    file: str
    line: int  # 1-based
    kind: str
    message: str
    snippet: str


def _strip_line(s: str) -> str:
    # Ignore indentation + trailing whitespace for width checks per requirement
    return s.strip("\r\n").strip()


def _count_leading_spaces(s: str) -> int:
    i = 0
    while i < len(s) and s[i] == " ":
        i += 1
    return i


def _count_trailing_spaces(s: str) -> int:
    i = 0
    j = len(s) - 1
    while j >= 0 and s[j] == " ":
        i += 1
        j -= 1
    return i


def check_60_block(lines: List[str], i: int, file: Path) -> Optional[List[Issue]]:
    """
    If lines[i] looks like a 60-block border, try to validate the 3-line block.
    Return issues if it's a recognized block but non-conforming; otherwise None.
    """
    if i + 2 >= len(lines):
        return None

    l1 = _strip_line(lines[i])
    l2 = _strip_line(lines[i + 1])
    l3 = _strip_line(lines[i + 2])

    # Recognize the block by border lines pattern
    if not RE_60_BORDER.match(l1):
        return None
    if not RE_60_ANY.match(l2):
        return None
    if not RE_60_BORDER.match(l3):
        return None

    issues: List[Issue] = []

    # Width checks
    for off, l in enumerate((l1, l2, l3), start=0):
        if len(l) != 60:
            issues.append(
                Issue(
                    file=str(file),
                    line=i + 1 + off,
                    kind="60-block",
                    message=f"Line width is {len(l)} (expected 60) after strip().",
                    snippet=l,
                )
            )

    # Middle padding checks (only if it still looks like // ... //)
    if l2.startswith("//") and l2.endswith("//") and len(l2) >= 4:
        inner = l2[2:-2]  # content between the '//' pairs (keeps spaces)
        left_spaces = _count_leading_spaces(inner)
        right_spaces = _count_trailing_spaces(inner)
        title = inner.strip()

        if not title:
            issues.append(
                Issue(
                    file=str(file),
                    line=i + 2,
                    kind="60-block",
                    message="Middle line has an empty title (only spaces).",
                    snippet=l2,
                )
            )
        else:
            # Rule: left == right OR left == right + 1
            if not (left_spaces == right_spaces or left_spaces == right_spaces + 1):
                issues.append(
                    Issue(
                        file=str(file),
                        line=i + 2,
                        kind="60-block",
                        message=f"Middle padding invalid: left_spaces={left_spaces}, right_spaces={right_spaces} (expected equal or left=right+1).",
                        snippet=l2,
                    )
                )
    else:
        issues.append(
            Issue(
                file=str(file),
                line=i + 2,
                kind="60-block",
                message="Middle line does not start/end with '//' as expected.",
                snippet=l2,
            )
        )

    return issues if issues else []


def check_30_line(line: str, i: int, file: Path) -> Optional[Issue]:
    """
    If line looks like a 30-char /* ... */ header, validate it and return an Issue if non-conforming.
    Returns None if it doesn't look like that header or if it's conforming.
    """
    s = _strip_line(line)
    m = RE_30.match(s)
    if not m:
        return None

    # Width check
    if len(s) != 30:
        return Issue(
            file=str(file),
            line=i + 1,
            kind="30-block",
            message=f"Line width is {len(s)} (expected 30) after strip().",
            snippet=s,
        )

    lrun = m.group("l")
    rrun = m.group("r")
    title = m.group("t").strip()

    if not title:
        return Issue(
            file=str(file),
            line=i + 1,
            kind="30-block",
            message="Title is empty.",
            snippet=s,
        )

    # Rule: left == right OR left == right + 1
    if not (len(lrun) == len(rrun) or len(lrun) == len(rrun) + 1):
        return Issue(
            file=str(file),
            line=i + 1,
            kind="30-block",
            message=f"Unequal '━' runs: left={len(lrun)}, right={len(rrun)} (expected equal or left=right+1).",
            snippet=s,
        )

    return None


def scan(root: Path) -> List[Issue]:
    issues: List[Issue] = []

    if not root.exists():
        raise FileNotFoundError(f"Root folder not found: {root.resolve()}")

    for file in root.rglob("*.cs"):
        try:
            text = file.read_text(encoding="utf-8", errors="replace")
        except Exception as e:
            issues.append(
                Issue(
                    file=str(file),
                    line=1,
                    kind="io",
                    message=f"Failed to read file: {e}",
                    snippet="",
                )
            )
            continue

        lines = text.splitlines(True)

        i = 0
        while i < len(lines):
            # 60-block: only validate when we see a border line
            maybe = check_60_block(lines, i, file)
            if maybe is not None:
                # Recognized a 3-line block (conforming => [], nonconforming => issues)
                issues.extend(maybe)
                i += 3
                continue

            # 30-block: validate single-line header
            issue30 = check_30_line(lines[i], i, file)
            if issue30 is not None:
                issues.append(issue30)

            i += 1

    return issues


def print_report(issues: List[Issue], show_snippets: bool) -> int:
    if not issues:
        print("OK: No non-conforming blocks found.")
        return 0

    # Group by file
    by_file: dict[str, List[Issue]] = {}
    for it in issues:
        by_file.setdefault(it.file, []).append(it)

    total = len(issues)
    print(f"Found {total} non-conforming block issue(s) in {len(by_file)} file(s).\n")

    for fname in sorted(by_file.keys()):
        items = sorted(by_file[fname], key=lambda x: x.line)
        print(fname)
        for it in items:
            print(f"  L{it.line:>5} [{it.kind}] {it.message}")
            if show_snippets and it.snippet:
                print(f"        {it.snippet}")
        print()

    return 1


def main() -> int:
    ap = argparse.ArgumentParser(description="Validate Retinues comment banner blocks in .cs files.")
    ap.add_argument(
        "--root",
        default=os.path.join("src", "Retinues"),
        help="Root folder to scan (default: ./src/Retinues).",
    )
    ap.add_argument(
        "--no-snippets",
        default=True,
        action="store_false",
        help="Do not print the offending line text.",
    )

    args = ap.parse_args()
    root = Path(args.root)

    issues = scan(root)
    return print_report(issues, show_snippets=not args.no_snippets)


if __name__ == "__main__":
    raise SystemExit(main())
