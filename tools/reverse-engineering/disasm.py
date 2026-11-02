#!/usr/bin/env python3
"""Annotiertes Disassembly eines Bereichs von em.exe und DGROUP-Querverweise.

Baut auf funktionskarte.py auf (gleiche Funktionsgrenzen und Overlay-Auflösung):

  python3 tools/reverse-engineering/disasm.py <em.exe> 0x51E90 0x57480 > <scratch>/ov130.asm
      Disassembly des Bereichs (Dateioffsets, eine Region) mit Funktionsköpfen (Größe, Status, Stub-Eintrag, Aufrufer),
      aufgelösten Fern- und Nahaufrufen (-> Dateioffset[Status]), Sprungzielen und DGROUP-Texten hinter Direktwerten.
  python3 tools/reverse-engineering/disasm.py <em.exe> --xref 0xA839 0xA846 [--imm]
      alle Befehle des Spielcodes, die Speicheroperanden (mit --imm auch Direktwerte) im DGROUP-Bereich benutzen –
      findet Schreiber und Leser einer Tabelle. Die Analyse wird in <scratch>/an.pickle zwischengespeichert.

Ausgaben gehören nicht ins Repo (Auszüge aus dem Original); der Status stammt aus den Fundstellen in src/ und docs/.
"""
import argparse
import pickle
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import funktionskarte as fk  # noqa: E402

JUMP = re.compile(r"^j\w+(?: short)? 0x([0-9a-f]+)$")


def load(exe_path: Path, repo: Path, cache: Path | None):
    if cache and cache.exists():
        return pickle.loads(cache.read_bytes())
    exe = exe_path.read_bytes()
    an = fk.Analyzer(exe)
    an.run()
    rc = fk.RefCollector(an)
    rc.scan_code(repo)
    rc.scan_docs(repo)
    if cache:
        cache.write_bytes(pickle.dumps(an))
    return an


def name(an, addr: int) -> str:
    f = an.func_at(addr)
    if f is None:
        return f"{addr:#07x}"
    tag = f"{f.start:#07x}"
    if f.start != addr:
        tag += f"+{addr - f.start:#x}"
    return tag + f"[{f.status[:3]}]"


def listing(an, lo: int, hi: int) -> None:
    region = an.region_of(lo)
    if region is None:
        sys.exit(f"{lo:#x} liegt in keinem Code-Bereich")
    starts = {f.start: f for f in an.functions}
    for off, raw, text in an.code[region.key]:
        addr = region.start + off
        if addr < lo or addr >= hi:
            continue
        if addr in starts:
            f = starts[addr]
            entry = f"{region.stub:#x}:{f.entry:#x}" if f.entry is not None and region.stub is not None else "-"
            callers = " ".join(f"{c:#x}" for c in sorted(f.callers))
            print(f"\n;===== {addr:#07x} Größe {f.size} Status {f.status} Stub {entry} Aufrufer {callers}")
        note = ""
        m = fk.FAR_CALL.match(text)
        if m:
            tgt = an.resolve_far(region, int(m.group(2), 16), int(m.group(3), 16))
            if tgt is not None:
                note = f"; -> {name(an, tgt)}"
        m = fk.NEAR_CALL.match(text)
        if m:
            note = f"; -> {name(an, region.start + int(m.group(1), 16))}"
        m = fk.IMM.match(text)
        if m:
            s = an.dgroup_string(int(m.group(2), 16))
            if s:
                note = f'; "{s}"'
        m = JUMP.match(text)
        if m:
            note = f"; -> {region.start + int(m.group(1), 16):#07x}"
        print(f"{addr:07X}  {raw.hex():<16} {text:<40} {note}")


def xref(an, lo: int, hi: int, imm: bool) -> None:
    for r in an.regions:
        if fk.is_system(r):
            continue
        for off, raw, text in an.code[r.key]:
            hit = False
            for m in fk.MEM.finditer(text):
                v = int(m.group(2), 16)
                if m.group(1) == "-":
                    v = (-v) & 0xFFFF
                hit |= lo <= v <= hi
            if imm and not hit and "[" not in text:
                m = re.search(r"0x([0-9a-f]+)$", text)
                hit = bool(m) and lo <= int(m.group(1), 16) <= hi
            if hit:
                addr = r.start + off
                print(f"{addr:07X} {name(an, addr):<22} {text}")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("exe", type=Path)
    ap.add_argument("lo", type=lambda s: int(s, 16))
    ap.add_argument("hi", type=lambda s: int(s, 16))
    ap.add_argument("--xref", action="store_true", help="DGROUP-Querverweise statt Disassembly")
    ap.add_argument("--imm", action="store_true", help="bei --xref auch Direktwerte zählen")
    ap.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    ap.add_argument("--cache", type=Path, help="Analyse als Pickle zwischenspeichern (außerhalb des Repos)")
    args = ap.parse_args()
    an = load(args.exe, args.repo, args.cache)
    if args.xref:
        xref(an, args.lo, args.hi, args.imm)
    else:
        listing(an, args.lo, args.hi)
    return 0


if __name__ == "__main__":
    sys.exit(main())
