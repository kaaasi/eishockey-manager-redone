#!/usr/bin/env python3
"""Marker list of all [APPROX] lines in src/ with stable IDs (track `inventur-v1`, round 20).

Usage:
    python3 tools/reverse-engineering/markerliste.py [--repo .] [--write] [--abweichungen]

Without --write the generated Markdown block is printed to stdout. With --write the block between the
markers in docs/analyse/restliste-v1.md is replaced (hand-written text outside the markers stays).
--abweichungen prints the deviation list for the user before v1.0 (R20-D3 addition) instead of the block.

Exit code: 0 = every marker line has exactly one valid class, 1 = unclassified lines or invalid classes
(listed on stderr), 2 = error (e.g. old commits missing for the first seeding).

IDs and matching:
 - Every line under src/ (bin/ and obj/ skipped) that contains "[APPROX]" is one entry, exactly like
   `grep -rc '\\[APPROX\\]' src/`.
 - The key of a line is its file and its wording: SHA-1 of the normalised text (comment tokens and
   surrounding whitespace removed, inner whitespace collapsed), first 10 hex digits. Line numbers are
   never part of the key, so a moved line keeps its ID; a reworded line gets a new ID and the old ID is
   listed as "entfallen".
 - Memory: the previous generated block in restliste-v1.md (ID, file, key). Lines are paired with
   previous rows of the same file and key in line order; then with a unique key in another file
   (file moved). IDs not in the block are seeded once from table 1 of docs/analyse/inventur.md: the
   wording is read from git at the stand of the table row ("Stelle" = file:line at that commit).
 - Unmatched lines get new IDs from A482 on (max known ID + 1), in order of file and line.

Classification is hand-maintained in docs/analyse/restliste-v1-klassen.csv (key: ID; columns ID, Klasse,
Bereich, Funktion/Bildschirm, Abweichung, Beleg, Verdacht, Aufwand). Classes: V, D, E, K, X (see
docs/arbeitspakete.md, track inventur-v1). Rows with an F-ID are functions of the function map (no marker
line); they are listed in their own table.
"""

from __future__ import annotations

import argparse
import csv
import difflib
import hashlib
import io
import re
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

DOC = Path("docs/analyse/restliste-v1.md")
CSV_FILE = Path("docs/analyse/restliste-v1-klassen.csv")
INVENTUR = Path("docs/analyse/inventur.md")
CSV_COLUMNS = ["ID", "Klasse", "Bereich", "Funktion/Bildschirm", "Abweichung", "Beleg", "Verdacht", "Aufwand"]
CLASSES = {
    "V": "v1.0-Blocker",
    "D": "nur DOSBox/Video",
    "E": "bewusst",
    "K": "kosmetisch",
    "X": "Marker veraltet",
}
AUFWAND = {"", "S", "M", "L"}
FIRST_NEW_ID = 482
BEGIN = "<!-- BEGIN generiert: tools/reverse-engineering/markerliste.py – nicht von Hand ändern -->"
END = "<!-- END generiert -->"
MARK = "[APPROX]"

# Stand (commit) of the column "Stelle" in table 1 of inventur.md, per ID range (texts "Zeilen Stand …",
# "neue Zeilen: Stand …" of each round). If the line there holds no [APPROX], the other stands are tried.
INVENTUR_STANDS = [
    (1, 381, "2d0b081"),
    (382, 383, "7fbf9b6"),
    (384, 407, "7fbf9b6"),
    (408, 417, "0803e91"),
    (418, 437, "e947eb5"),
    (438, 459, "01ed8a5"),
    (460, 481, "5dbe9f2"),
]
FALLBACK_STANDS = ["2d0b081", "3a8070c", "e947eb5", "7fbf9b6", "0803e91", "81a358c", "01ed8a5", "5dbe9f2"]


def normalise(line: str) -> str:
    s = line.strip()
    s = re.sub(r"^(///|//|/\*\*|/\*|\*/|\*|#)\s?", "", s)
    s = re.sub(r"\s*\*/$", "", s)
    return re.sub(r"\s+", " ", s).strip()


def key_of(line: str) -> str:
    return hashlib.sha1(normalise(line).encode("utf-8")).hexdigest()[:10]


def id_num(ident: str) -> int:
    return int(ident[1:])


@dataclass
class Marker:
    path: str  # relative to src/EishockeyManager. (like inventur.md), e.g. Core/CupsDeb.cs
    line: int
    text: str
    key: str
    ident: str = ""
    origin: str = ""  # hint for new IDs, e.g. "ähnlich A123"
    inventur: str = ""


@dataclass
class Gone:
    ident: str
    last: str  # last known place
    key: str
    note: str


@dataclass
class PrevRow:
    ident: str
    path: str
    key: str
    origin: str = ""
    inventur: str = ""
    stelle: str = ""


def short_path(rel: Path) -> str:
    s = rel.as_posix()
    return s[len("EishockeyManager."):] if s.startswith("EishockeyManager.") else s


def scan(repo: Path) -> list[Marker]:
    src = repo / "src"
    out: list[Marker] = []
    for p in sorted(src.rglob("*"), key=lambda q: q.as_posix()):
        if not p.is_file():
            continue
        rel = p.relative_to(src)
        if any(part in ("bin", "obj") for part in rel.parts[:-1]):
            continue
        data = p.read_bytes()
        if MARK.encode() not in data:
            continue
        text = data.decode("utf-8", errors="replace")
        for i, line in enumerate(text.split("\n"), 1):
            if MARK in line:
                out.append(Marker(short_path(rel), i, line, key_of(line)))
    return out


def md_cell(s: str, limit: int = 0) -> str:
    s = re.sub(r"\s+", " ", s).strip()
    if limit and len(s) > limit:
        s = s[: limit - 1].rstrip() + "…"
    return s.replace("\\", "\\\\").replace("|", "\\|").replace("<", "&lt;").replace(">", "&gt;")


def md_uncell(s: str) -> str:
    return (s.strip().replace("&gt;", ">").replace("&lt;", "<").replace("\\|", "|").replace("\\\\", "\\"))


# ---------------------------------------------------------------- previous block

def split_row(line: str) -> list[str]:
    cells, cur, i = [], "", 1
    body = line.strip()
    while i < len(body):
        c = body[i]
        if c == "\\" and i + 1 < len(body):
            cur += body[i : i + 2]
            i += 2
            continue
        if c == "|":
            cells.append(cur)
            cur = ""
        else:
            cur += c
        i += 1
    return [md_uncell(x) for x in cells]


def read_previous(doc: Path) -> tuple[list[PrevRow], list[Gone]]:
    if not doc.exists():
        return [], []
    text = doc.read_text(encoding="utf-8")
    m = re.search(re.escape(BEGIN) + r"(.*?)" + re.escape(END), text, flags=re.S)
    if not m:
        return [], []
    rows: list[PrevRow] = []
    gone: list[Gone] = []
    section = ""
    for line in m.group(1).split("\n"):
        if line.startswith("### "):
            section = line
            continue
        if not line.startswith("| A"):
            continue
        c = split_row(line)
        if section.startswith("### Marker"):
            # | ID | Stelle | Schlüssel | Wortlaut | Herkunft | Inventur | ...
            stelle = c[1].strip("`")
            rows.append(PrevRow(c[0], stelle.rsplit(":", 1)[0], c[2].strip("`"), c[4], c[5], stelle))
        elif section.startswith("### Entfallen"):
            # | ID | zuletzt | Schlüssel | Hinweis | Klasse |
            gone.append(Gone(c[0], c[1].strip("`"), c[2].strip("`"), c[3]))
    return rows, gone


# ---------------------------------------------------------------- seeding from inventur.md

@dataclass
class Seed:
    ident: str
    path: str
    line: int
    inventur: str
    last_state: str
    text: str = ""
    key: str = ""
    stand: str = ""


def read_inventur(repo: Path) -> list[Seed]:
    p = repo / INVENTUR
    if not p.exists():
        return []
    seeds = []
    in_table1 = False
    for line in p.read_text(encoding="utf-8").split("\n"):
        if line.startswith("## Tabelle 1"):
            in_table1 = True
        elif line.startswith("## ") and in_table1:
            break
        if not in_table1 or not re.match(r"^\| A\d{3} ", line):
            continue
        c = [x.strip() for x in line.strip().strip("|").split(" | ")]
        ident, stelle, gr, thema, beleg = c[0], c[1].strip("`"), c[2], c[3], c[4]
        prio = c[6] if len(c) > 6 else ""
        path, _, ln = stelle.rpartition(":")
        states = re.findall(r"\*\*(Stand Runde \d+[^*]*)\*\*", beleg)
        last_state = states[-1] if states else ""
        info = f"{gr} · {prio} · {thema}" if prio not in ("", "–") else f"{gr} · {thema}"
        seeds.append(Seed(ident, path, int(ln), info, last_state))
    return seeds


def git_show(repo: Path, rev: str, path: str, cache: dict) -> list[str] | None:
    k = (rev, path)
    if k not in cache:
        r = subprocess.run(["git", "-C", str(repo), "show", f"{rev}:src/EishockeyManager.{path}"],
                           capture_output=True)
        cache[k] = r.stdout.decode("utf-8", errors="replace").split("\n") if r.returncode == 0 else None
    return cache[k]


def resolve_seeds(repo: Path, seeds: list[Seed]) -> None:
    for rev in FALLBACK_STANDS:
        r = subprocess.run(["git", "-C", str(repo), "cat-file", "-e", rev + "^{commit}"], capture_output=True)
        if r.returncode != 0:
            raise SystemExit(f"error: commit {rev} missing (shallow clone?) - run `git fetch --unshallow origin`")
    cache: dict = {}
    for s in seeds:
        n = id_num(s.ident)
        first = next((st for lo, hi, st in INVENTUR_STANDS if lo <= n <= hi), FALLBACK_STANDS[0])
        for rev in [first] + [x for x in FALLBACK_STANDS if x != first]:
            lines = git_show(repo, rev, s.path, cache)
            if lines and 0 < s.line <= len(lines) and MARK in lines[s.line - 1]:
                s.text, s.key, s.stand = lines[s.line - 1], key_of(lines[s.line - 1]), rev
                break


# ---------------------------------------------------------------- matching

def pair(markers: list[Marker], candidates: list[tuple[str, str, str]], assign) -> list[tuple[str, str, str]]:
    """candidates: (ident, path, key) in preferred order. Pairs same file + key in line order, then a key
    that is unique among the remaining markers and candidates (moved file). Returns the unpaired ones."""
    free: dict[tuple[str, str], list[Marker]] = defaultdict(list)
    for m in markers:
        if not m.ident:
            free[(m.path, m.key)].append(m)
    rest = []
    for c in candidates:
        lst = free.get((c[1], c[2]))
        if lst:
            assign(lst.pop(0), c)
        else:
            rest.append(c)
    by_key: dict[str, list[Marker]] = defaultdict(list)
    for m in markers:
        if not m.ident:
            by_key[m.key].append(m)
    cand_keys = defaultdict(int)
    for c in rest:
        cand_keys[c[2]] += 1
    still = []
    for c in rest:
        lst = by_key.get(c[2])
        if lst and len(lst) == 1 and cand_keys[c[2]] == 1:
            assign(lst.pop(), c)
        else:
            still.append(c)
    return still


def build(repo: Path):
    markers = scan(repo)
    prev_rows, prev_gone = read_previous(repo / DOC)
    known = {r.ident for r in prev_rows} | {g.ident for g in prev_gone}
    prev_by_id = {r.ident: r for r in prev_rows}

    def assign_prev(m: Marker, c):
        r = prev_by_id[c[0]]
        m.ident, m.origin, m.inventur = r.ident, r.origin, r.inventur

    # 1) previous block (active rows first, then IDs that were gone and whose wording came back)
    unpaired_prev = pair(markers, [(r.ident, r.path, r.key) for r in prev_rows], assign_prev)
    gone_by_id = {g.ident: g for g in prev_gone}

    def assign_gone(m: Marker, c):
        g = gone_by_id.pop(c[0])
        m.ident, m.origin = g.ident, "wieder da"

    pair(markers, [(g.ident, re.match(r"[^\s:]*", g.last.strip("`")).group(0), g.key) for g in prev_gone if g.key],
         assign_gone)
    gone: list[Gone] = list(gone_by_id.values())
    for c in unpaired_prev:
        r = prev_by_id[c[0]]
        gone.append(Gone(r.ident, f"{r.stelle} (zuletzt im Block)", r.key,
                         r.inventur or f"Wortlaut geändert oder entfernt (Herkunft {r.origin})"))

    # 2) seeds from inventur.md for IDs the block does not know yet
    seeds = [s for s in read_inventur(repo) if s.ident not in known]
    if seeds:
        resolve_seeds(repo, seeds)
    seed_by_id = {s.ident: s for s in seeds}

    def assign_seed(m: Marker, c):
        s = seed_by_id[c[0]]
        m.ident, m.origin, m.inventur = s.ident, "inventur", s.inventur

    unpaired_seed = pair(markers, [(s.ident, s.path, s.key) for s in seeds if s.key], assign_seed)
    lost = {c[0] for c in unpaired_seed} | {s.ident for s in seeds if not s.key}
    for s in seeds:
        if s.ident in lost:
            where = f"{s.path}:{s.line} @ {s.stand}" if s.stand else f"{s.path}:{s.line} (Stelle ohne [APPROX])"
            gone.append(Gone(s.ident, where, s.key, (s.last_state or s.inventur)))

    # 3) new IDs
    all_ids = [id_num(x) for x in known | set(seed_by_id)] + [id_num(m.ident) for m in markers if m.ident]
    nxt = max([FIRST_NEW_ID - 1] + all_ids) + 1
    lost_seeds = [s for s in seeds if s.ident in lost and s.text]
    for m in markers:
        if m.ident:
            continue
        m.ident = f"A{nxt:03d}"
        nxt += 1
        # hint: most similar lost inventur line in the same file
        best, score = None, 0.0
        nm = normalise(m.text)
        for s in lost_seeds:
            if s.path == m.path:
                r = difflib.SequenceMatcher(None, normalise(s.text), nm).ratio()
                if r > score:
                    best, score = s, r
        if best and score >= 0.75:
            m.origin = f"ähnlich {best.ident}"
            m.inventur = best.inventur
        else:
            m.origin = "neu"
    gone.sort(key=lambda g: id_num(g.ident))
    return markers, gone


# ---------------------------------------------------------------- classes

def read_classes(repo: Path) -> dict[str, dict[str, str]]:
    p = repo / CSV_FILE
    if not p.exists():
        return {}
    rows = {}
    with p.open(encoding="utf-8", newline="") as f:
        for row in csv.DictReader(f):
            ident = (row.get("ID") or "").strip()
            if ident:
                rows[ident] = {k: (row.get(k) or "").strip() for k in CSV_COLUMNS}
    return rows


def render(markers: list[Marker], gone: list[Gone], classes: dict, problems: list[str]) -> str:
    out = []
    w = out.append
    files = len({m.path for m in markers})
    counts = defaultdict(int)
    for m in markers:
        k = classes.get(m.ident, {}).get("Klasse", "")
        counts[k if k in CLASSES else ("?" if k else "")] += 1
    w(BEGIN)
    w("")
    w(f"**Zählung:** {files} Dateien, {len(markers)} Markerzeilen (= `grep -rc '\\[APPROX\\]' src/ | "
      f"awk -F: '$2>0{{n++;s+=$2}}END{{print n,s}}'`); {len(gone)} IDs entfallen.")
    w("")
    w("| Klasse | Bedeutung | Stellen |")
    w("|---|---|---:|")
    for k, v in CLASSES.items():
        w(f"| {k} | {v} | {counts[k]} |")
    w(f"| – | unklassifiziert | {counts['']} |")
    if counts["?"]:
        w(f"| ? | ungültige Klasse | {counts['?']} |")
    w("")
    w("### Marker")
    w("")
    w("Stelle relativ zu `src/EishockeyManager.`; Schlüssel = Datei + Wortlaut (`markerliste.py`, Docstring). "
      "Herkunft: Vorgänger-ID aus `inventur.md` bzw. „neu“; Inventur: Gruppe, Prio und Thema der alten Tabelle 1.")
    w("")
    w("| ID | Stelle | Schlüssel | Wortlaut | Herkunft | Inventur | " + " | ".join(CSV_COLUMNS[1:]) + " |")
    w("|" + "---|" * (6 + len(CSV_COLUMNS) - 1))
    for m in sorted(markers, key=lambda x: (x.path, x.line)):
        c = classes.get(m.ident, {})
        cells = [m.ident, f"`{m.path}:{m.line}`", f"`{m.key}`", md_cell(normalise(m.text), 110),
                 md_cell(m.origin), md_cell(m.inventur, 90)] + [md_cell(c.get(k, "")) for k in CSV_COLUMNS[1:]]
        w("| " + " | ".join(cells) + " |")
    all_funcs = sorted((i for i in classes if i.startswith("F")), key=id_num)
    # Klasse X (Code belegt gleich, „vorhanden ohne Marker“) ist keine Abweichung: ausgeblendet, die Zeilen und ihr Beleg bleiben in der CSV.
    funcs = [i for i in all_funcs if classes[i].get("Klasse") != "X"]
    hidden = len(all_funcs) - len(funcs)
    w("")
    w("### Funktionen")
    w("")
    w("Funktionen „dokumentiert“ und „genähert“ der Funktionskarte (ohne Markerzeile), nur aus `restliste-v1-klassen.csv`."
      + (f" Ausgeblendet: {hidden} Funktionen der Klasse X (Code belegt gleich, Beleg in der CSV)." if hidden else ""))
    w("")
    w("| ID | " + " | ".join(CSV_COLUMNS[1:]) + " |")
    w("|" + "---|" * len(CSV_COLUMNS))
    for i in funcs:
        w("| " + " | ".join([i] + [md_cell(classes[i].get(k, "")) for k in CSV_COLUMNS[1:]]) + " |")
    w("")
    w("### Entfallen")
    w("")
    w("IDs ohne Markerzeile: aus `inventur.md` (Stelle @ Stand, letzter Eintrag „Stand Runde n“) bzw. aus dem vorigen Block.")
    w("")
    w("| ID | zuletzt | Schlüssel | Hinweis | Klasse |")
    w("|---|---|---|---|---|")
    for g in gone:
        w(f"| {g.ident} | `{md_cell(g.last)}` | {f'`{g.key}`' if g.key else ''} | {md_cell(g.note, 120)} | "
          f"{md_cell(classes.get(g.ident, {}).get('Klasse', ''))} |")
    if problems:
        w("")
        w("### Meldungen")
        w("")
        for p in problems:
            w(f"- {md_cell(p)}")
    w("")
    w(END)
    return "\n".join(out)


def render_deviations(markers: list[Marker], classes: dict) -> str:
    """List for the user before v1.0 (R20-D3 addition): every entry except class X, by area.

    Functions without a marker whose reason is "nicht nötig" (runtime/platform routines, storage helpers without
    game effect) are no deviation for the user and are only counted (review #884 P3-3).
    """
    by_area: dict[str, list[tuple[str, str, str, str]]] = defaultdict(list)
    items = [(m.ident, m.path) for m in markers] + [(i, "") for i in classes if i.startswith("F")]
    skipped = 0
    for ident, path in items:
        c = classes.get(ident, {})
        k = c.get("Klasse", "")
        if k in ("", "X"):
            continue
        if ident.startswith("F") and c.get("Abweichung") == "kein Marker" and c.get("Beleg", "").startswith("nicht nötig"):
            skipped += 1
            continue
        by_area[c.get("Bereich") or "(ohne Bereich)"].append(
            (c.get("Funktion/Bildschirm") or path, c.get("Abweichung", ""), k, ident))
    out = ["# Abweichungen vom Original (vor v1.0)", ""]
    for area in sorted(by_area):
        out.append(f"## {area}")
        out.append("")
        out.append("| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |")
        out.append("|---|---|---|---|")
        for fn, dev, k, ident in sorted(by_area[area], key=lambda t: ("VDEK".find(t[2]), t[0], id_num(t[3]))):
            out.append(f"| {md_cell(fn)} | {md_cell(dev)} | {k} | {ident} |")
        out.append("")
    out.append(f"Nicht aufgeführt: {skipped} Funktionen ohne Marker mit Grund „nicht nötig“ (keine Spielwirkung).")
    return "\n".join(out)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    ap.add_argument("--write", action="store_true", help=f"update {DOC} between the markers")
    ap.add_argument("--abweichungen", action="store_true", help="print the deviation list (R20-D3) instead")
    args = ap.parse_args()
    repo = args.repo.resolve()
    markers, gone = build(repo)
    classes = read_classes(repo)
    active = {m.ident for m in markers}
    problems = []
    unclassified = [m for m in markers if not classes.get(m.ident, {}).get("Klasse")]
    for i, c in sorted(classes.items(), key=lambda t: (t[0][0], id_num(t[0]))):
        if c["Klasse"] and c["Klasse"] not in CLASSES:
            problems.append(f"{i}: ungültige Klasse „{c['Klasse']}“")
        if c["Aufwand"] not in AUFWAND:
            problems.append(f"{i}: ungültiger Aufwand „{c['Aufwand']}“")
        if i.startswith("A") and i not in active:
            problems.append(f"{i}: Klasse ohne Marker (entfallen)")
    if args.abweichungen:
        print(render_deviations(markers, classes))
    else:
        text = render(markers, gone, classes, problems)
        if args.write:
            doc = repo / DOC
            old = doc.read_text(encoding="utf-8") if doc.exists() else ""
            if BEGIN in old:
                new = re.sub(re.escape(BEGIN) + r".*?" + re.escape(END), lambda _: text, old, flags=re.S)
            else:
                new = old.rstrip() + ("\n\n" if old.strip() else "") + text + "\n"
            doc.write_text(new, encoding="utf-8")
        else:
            print(text)
    for p in problems:
        print(f"markerliste: {p}", file=sys.stderr)
    if unclassified:
        print(f"markerliste: {len(unclassified)} unklassifiziert (erste: "
              f"{', '.join(m.ident for m in unclassified[:10])})", file=sys.stderr)
    bad = [p for p in problems if "ungültig" in p]
    return 1 if unclassified or bad else 0


if __name__ == "__main__":
    sys.exit(main())
