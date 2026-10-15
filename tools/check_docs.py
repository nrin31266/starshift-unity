#!/usr/bin/env python3
"""Kiểm tra Markdown ở root/docs/tools, hỗ trợ link và anchor ATX cơ bản.

Không kiểm tra URL ngoài repository hoặc parse HTML/custom anchors.
"""

import html
from pathlib import Path
import re
import sys
import unicodedata
from urllib.parse import unquote, urlsplit


ALLOWED_MARKERS = {"LOCKED", "TUNABLE", "TBD", "PROVISIONAL", "FUTURE"}
HEADING = re.compile(r"^ {0,3}#{1,6}[ \t]+(.+)$", re.MULTILINE)
NUMBERED_HEADING = re.compile(r"^\d+(?:\.\d+)*[.)]?(?:\s|$)")
INLINE_LINK = re.compile(
    r"!?\[[^\]\n]*\]\([ \t]*(<[^>\n]+>|[^\s)]+)"
    r"(?:[ \t]+[\"'][^\n]*?[\"'])?[ \t]*\)"
)
REFERENCE_LINK = re.compile(
    r"^ {0,3}\[(?!(?:LOCKED|TUNABLE|TBD|PROVISIONAL|FUTURE)\])"
    r"[^\]\n]+\]:[ \t]*(<[^>\n]+>|\S+)", re.MULTILINE
)
STATUS_MARKER = re.compile(r"\[([A-Z][A-Z0-9_]*(?:[ ,:-][^\]\n]*)?)\]")


def without_fences(text):
    lines = []
    fence = None
    for line in text.splitlines(keepends=True):
        opening = re.match(r"^ {0,3}(`{3,}|~{3,})", line)
        if fence:
            if re.fullmatch(r" {0,3}" + re.escape(fence[0]) +
                            r"{" + str(fence[1]) + r",}[ \t]*\n?", line):
                fence = None
            lines.append("\n")
        elif opening:
            fence = (opening[1][0], len(opening[1]))
            lines.append("\n")
        else:
            lines.append(line)
    return "".join(lines)


def heading_anchors(text):
    anchors = set()
    for match in HEADING.finditer(text):
        title = re.sub(r"[ \t]+#+[ \t]*$", "", match[1])
        title = INLINE_LINK.sub(lambda link: link[0].split("]", 1)[0].lstrip("!["), title)
        title = html.unescape(re.sub(r"<[^>]*>", "", title)).lower()
        slug = "".join(
            char for char in title
            if char in "-_ " or unicodedata.category(char)[0] in "LNM"
        ).replace(" ", "-")
        anchor = slug
        suffix = 1
        while anchor in anchors:
            anchor = f"{slug}-{suffix}"
            suffix += 1
        anchors.add(anchor)
    return anchors


def check_documents(root):
    documents = sorted({
        *root.glob("*.md"),
        *(root / "docs").rglob("*.md"),
        *(root / "tools").rglob("*.md"),
    })
    errors = []
    link_count = 0
    anchors = {}

    def report(path, text, offset, message):
        line = text.count("\n", 0, offset) + 1
        errors.append(f"{path.relative_to(root)}:{line}: {message}")

    for path in documents:
        raw = path.read_text(encoding="utf-8")
        text = without_fences(raw)
        canonical = path.is_relative_to(root / "docs") and path.name != "README.md"
        for match in re.finditer(r"MASTER_SPEC(?:\.md)?", raw):
            report(path, raw, match.start(), "stale master reference")
        if canonical:
            for match in HEADING.finditer(text):
                if NUMBERED_HEADING.match(match[1]):
                    report(path, text, match.start(), "manual numbered heading")

        # Nhãn của link không phải status marker; tag viết trong inline code vẫn được kiểm tra.
        status_text = REFERENCE_LINK.sub("", INLINE_LINK.sub("", text))
        for match in STATUS_MARKER.finditer(status_text):
            if match[1] not in ALLOWED_MARKERS:
                report(path, status_text, match.start(), f"unknown status marker [{match[1]}]")

        link_text = re.sub(r"(`+)[^\n]*?\1", "", text)
        for pattern in (INLINE_LINK, REFERENCE_LINK):
            for match in pattern.finditer(link_text):
                destination = html.unescape(match[1].strip("<>"))
                url = urlsplit(destination)
                if url.scheme or url.netloc:
                    continue
                link_count += 1
                target = (path.parent / unquote(url.path)).resolve() if url.path else path
                if not target.exists():
                    report(path, link_text, match.start(), f"missing local target: {destination}")
                elif url.fragment and target.suffix.lower() == ".md":
                    if target not in anchors:
                        anchors[target] = heading_anchors(
                            without_fences(target.read_text(encoding="utf-8"))
                        )
                    if unquote(url.fragment) not in anchors[target]:
                        report(path, link_text, match.start(), f"missing ATX anchor: {destination}")

    return errors, len(documents), link_count


def main():
    root = Path(__file__).resolve().parent.parent
    errors, document_count, link_count = check_documents(root)
    if errors:
        print("\n".join(errors), file=sys.stderr)
        print(f"FAIL: {len(errors)} lỗi tài liệu.", file=sys.stderr)
        return 1
    print(f"PASS: {document_count} tài liệu Markdown, {link_count} relative links; "
          "anchors, headings, master references và status markers hợp lệ.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
