"""Spur hok-patch-tabellen, Teil 1: who writes and who reads the table blocks of a save game (static, from the bytes of em.exe).

For every block of docs/analyse/hok-tabellen.md the script collects all instructions of the game code that use an address
inside the block (memory operands, and with immediate values also pointers such as `add dx,0xcd58`, Datei 0x30B75) and
groups them by function: W = writes (mov/inc/add/sub/or/and with the block as destination), R = reads, P = pointer
(immediate value; the access happens in the callee, e.g. 0x303D9 for the group points). Then it lists the calls of the
load function 0x5C3E3 and checks that none of the table functions (league booking/sorting 0x50B45, places 0x51049,
group draw 0x30427, group matchday 0x30852, season change 0x54C9F, club swap 0x36169) is called while a save game is
loaded: the loader reads the blocks and computes nothing (Datei 0x5C62E ff., calls of 0x5A9AE).
Usage: python3 -I bloecke.py [em.exe] [--cache <scratch>/an.pickle]
Prints addresses only.
"""
import re
import sys
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import disasm  # noqa: E402
import funktionskarte as fk  # noqa: E402

REPO = HERE.parents[2]
BLOCKS = [
    ("8257 Tabellensätze", 0x8257, 0x8257 + 5120 - 1),
    ("5CCB Platzliste der Ligen", 0x5CCB, 0x5CCB + 36 - 1),
    ("A0CB Ergebnisse", 0xA0CB, 0xA0CB + 1584 - 1),
    ("CD88 Gruppen-Platzliste", 0xCD88, 0xCD88 + 47),
    ("CD58 Gruppen-Punkte", 0xCD58, 0xCD58 + 47),
    ("CD28 Gruppen-Tore", 0xCD28, 0xCD28 + 47),
    ("CCF8 Gruppen-Gegentore", 0xCCF8, 0xCCF8 + 47),
    ("CED8 Gruppenpaarungen", 0xCED8, 0xCED8 + 287),
    ("CDB8 Gruppentore", 0xCDB8, 0xCDB8 + 287),
    ("CFF4 Gruppenteilnehmer (Block CFFC ab +8)", 0xCFF4, 0xCFF4 + 31),
    ("07A9 Spieltage/Termine", 0x07A9, 0x07B0),
    ("D0E6 Meister", 0xD0E6, 0xD0E6),
]
TABLE_FUNCS = {0x50B45: "Ligatag 0x50B45", 0x51049: "Plätze 0x51049", 0x30427: "Gruppenauslosung 0x30427",
               0x30852: "Gruppenspieltag 0x30852", 0x54C9F: "Tagesfunktion 0x54C9F", 0x36169: "Vereinstausch 0x36169"}
WRITE = re.compile(r"^(mov|inc|dec|add|sub|or|and|xor|adc|sbb|shl|shr|not|neg)( byte| word)? \[")
LOADER = 0x5C3E3


def scan(an, lo, hi):
    hits = defaultdict(set)
    for r in an.regions:
        if fk.is_system(r):
            continue
        for off, _raw, text in an.code[r.key]:
            kind = None
            for m in fk.MEM.finditer(text):
                v = int(m.group(2), 16)
                if m.group(1) == "-":
                    v = (-v) & 0xFFFF
                if lo <= v <= hi:
                    kind = "W" if WRITE.match(text) and text.index("[") < text.find(",") else "R"
            if kind is None and "[" not in text:
                m = re.search(r",0x([0-9a-f]+)$|^push word 0x([0-9a-f]+)$", text)
                if m and lo <= int(m.group(1) or m.group(2), 16) <= hi and lo >= 0x100:
                    kind = "P"
            if kind:
                f = an.func_at(r.start + off)
                hits[f.start if f else r.start + off].add(kind)
    return hits


def main():
    args = [a for a in sys.argv[1:]]
    cache = None
    if "--cache" in args:
        i = args.index("--cache")
        cache = Path(args[i + 1])
        del args[i:i + 2]
    exe = Path(args[0]) if args else Path.home() / "referenz/original/gamedata/em.exe"
    an = disasm.load(exe, REPO, cache)
    for title, lo, hi in BLOCKS:
        hits = scan(an, lo, hi)
        cells = [f"{a:#07x}:{''.join(sorted(k))}" for a, k in sorted(hits.items())]
        print(f"{title} (DGROUP {lo:#06x}-{hi:#06x}): {' '.join(cells)}")
    loader = next(f for f in an.functions if f.start == LOADER)
    region = an.region_of(LOADER)
    called = set()
    for off, _raw, text in an.code[region.key]:
        addr = region.start + off
        if not loader.start <= addr < loader.start + loader.size:
            continue
        m = fk.FAR_CALL.match(text)
        if m:
            t = an.resolve_far(region, int(m.group(2), 16), int(m.group(3), 16))
            if t is not None:
                called.add(t)
        m = fk.NEAR_CALL.match(text)
        if m:
            called.add(region.start + int(m.group(1), 16))
    print(f"Laden {LOADER:#07x} ruft {len(called)} Funktionen: {' '.join(f'{a:#07x}' for a in sorted(called))}")
    bad = [TABLE_FUNCS[a] for a in sorted(called) if a in TABLE_FUNCS]
    print(f"davon Tabellenfunktionen: {', '.join(bad) if bad else 'keine'}")


if __name__ == "__main__":
    main()
