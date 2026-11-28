"""Spur hok-patch-tabellen, Teil 2: counter check of the fixed-point test in `emtool hok patch ligaplaetze=…`.

The patcher (OriginalSavePatcher.LeagueSortFixedPoint) does not run em.exe; it rebuilds the exchange sort of 0x50B45
(Datei 0x50D6D-0x51011) from the table records DGROUP 0x825A + 0x50 * slot, overall view = home + away byte of each field:
points +0x00/+0x01, games +0x1E/+0x1F, goals +0x16/+0x17, goals against +0x1A/+0x1B (reverse-engineering.md
"Ligatabelle"). For every place i all later places j; j replaces i with more points, with equal points fewer games, then a
higher goal difference, then more goals. This script compares that model with 0x50B45(league, 1, 1) run from the bytes
(ligatabelle.resort, 0x7A9 = 1 so the sort is forced) for the stored order, the reversed order and random permutations of
every league of the given save games. Output: counts only (no names, no table values).
Usage: python3 -I fixpunkt.py <save.HOK> … [--exe em.exe] [--n 40]
"""
import argparse
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "abweichungen"))
sys.path.insert(0, str(HERE))
import hokread  # noqa: E402
import ligatabelle  # noqa: E402

TABLE = 0x825A - 0x8257
RECORD = 0x50


def key(rec, slot):
    r = rec[TABLE + RECORD * slot:TABLE + RECORD * slot + RECORD]
    return r[0x00] + r[0x01], r[0x1E] + r[0x1F], r[0x16] + r[0x17], r[0x1A] + r[0x1B]


def better(b, a):
    if b[0] != a[0]:
        return b[0] > a[0]
    if b[1] != a[1]:
        return b[1] < a[1]
    if b[2] - b[3] != a[2] - a[3]:
        return b[2] - b[3] > a[2] - a[3]
    return b[2] > a[2]


def model(rec, order):
    o = list(order)
    for i in range(len(o) - 1):
        for j in range(i + 1, len(o)):
            if better(key(rec, o[j]), key(rec, o[i])):
                o[i], o[j] = o[j], o[i]
    return o


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("saves", nargs="+")
    ap.add_argument("--exe")
    ap.add_argument("--n", type=int, default=40)
    a = ap.parse_args()
    total = same = 0
    for path in a.saves:
        s = hokread.Save(path)
        rec = s.block("8257")
        rng = random.Random(Path(path).name)
        line = []
        for lg in range(3):
            stored = list(s.block("5CCB")[12 * lg:12 * lg + 12])
            orders = [stored, stored[::-1]] + [rng.sample(stored, 12) for _ in range(a.n)]
            ok = 0
            for o in orders:
                emu = ligatabelle.resort(s, a.exe, lg, day=1, order=o)
                ok += emu == model(rec, o)
            fixed = model(rec, stored) == stored
            total += len(orders)
            same += ok
            line.append(f"Liga {lg}: {ok}/{len(orders)} gleich, gespeicherte Folge Fixpunkt {fixed}")
        print(f"{Path(path).name}: " + "; ".join(line))
    print(f"Gesamt: Modell = 0x50B45 in {same} von {total} Folgen")
    return 0 if same == total else 1


if __name__ == "__main__":
    sys.exit(main())
