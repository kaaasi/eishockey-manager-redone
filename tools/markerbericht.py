#!/usr/bin/env python3
"""Zählt wörtliche [ORIGINAL]/[APPROX]-Vorkommen in src/ als Markdown-Bericht.

Aufruf: python3 tools/markerbericht.py [quellordner]
Ohne Argument wird src/ relativ zu diesem Skript verwendet, unabhängig vom Arbeitsverzeichnis.
Mehrere Marker in einer Zeile zählen einzeln; Dateien ohne Marker werden nicht aufgelistet.
"""

import argparse
from dataclasses import dataclass
from html import escape
from pathlib import Path


@dataclass(frozen=True)
class MarkerCount:
    path: Path
    original: int
    approx: int


def collect_counts(source: Path) -> list[MarkerCount]:
    counts = []
    for path in sorted(source.rglob("*")):
        if not path.is_file():
            continue
        content = path.read_bytes()
        original = content.count(b"[ORIGINAL]")
        approx = content.count(b"[APPROX]")
        if original or approx:
            counts.append(MarkerCount(path.relative_to(source), original, approx))
    return counts


def code_cell(value: str) -> str:
    return "<code>" + escape(value).replace("|", "&#124;") + "</code>"


def render_report(source: Path, counts: list[MarkerCount]) -> str:
    projects: dict[str, tuple[int, int]] = {}
    for count in counts:
        project = count.path.parts[0] if len(count.path.parts) > 1 else "(direkt im Quellordner)"
        original, approx = projects.get(project, (0, 0))
        projects[project] = (original + count.original, approx + count.approx)

    lines = ["## Markerbericht", "", "Marker-Vorkommen sind keine Abnahme der Originaltreue.", "",
             "### Projekte", "", "| Projekt | [ORIGINAL] | [APPROX] |", "|---|---:|---:|"]
    for project, (original, approx) in sorted(projects.items()):
        lines.append(f"| {code_cell(project)} | {original} | {approx} |")
    lines.append(f"| Gesamt | {sum(c.original for c in counts)} | {sum(c.approx for c in counts)} |")
    lines.extend(["", "### Dateien mit Markern", "", "| Datei | [ORIGINAL] | [APPROX] |", "|---|---:|---:|"])
    for count in counts:
        name = (Path(source.name) / count.path).as_posix()
        lines.append(f"| {code_cell(name)} | {count.original} | {count.approx} |")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("quellordner", nargs="?", type=Path, default=Path(__file__).resolve().parent.parent / "src")
    source = parser.parse_args().quellordner.resolve()
    if not source.is_dir():
        parser.error(f"Quellordner fehlt oder ist kein Verzeichnis: {source}")
    try:
        report = render_report(source, collect_counts(source))
    except OSError as error:
        parser.error(f"Quelldateien konnten nicht vollständig gelesen werden: {error}")
    print(report, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
