"""Spur tagesabschluss Teil 2 (R26-E1 = B): weekly stadium damage 0x3CFA0, called first in the stadium build 0x3D316.

Replays straight from the bytes of em.exe (emu16, no original data written). Stadium record of the manager
DGROUP 0x9771 + 0x1AF · m (re-geld.md „Datensatz“): level +2p (p = 0 … 8), places word +0x1D + 4j (j = 0 seats, 1 standing,
project p = 9 + j). Level index DGROUP 0xED8 (game level 1 … 5 → 0 … 4), league DGROUP 0x9746 + 0x1AF · m (0 = Bundesliga).

Every random call 0x0C037 (random(lo, hi), both bounds inclusive) is answered from a script and printed with caller and
project [bp-0x5]. The number to text call 0x0BAB2 is stubbed (DGROUP 0x38D = 0 during the call: thousands point, printed
as such); copy/append 0x0390B/0x0389C run for real; the message 0x4B2BA is stubbed and prints both lines and the kind.

Cases:
1. Gate: random(0, 2·[0xED8] + 8) ≠ 0 → nothing else (one roll a week).
2. Gate 0, every chance roll misses (answer = hi): order and ranges of all chance rolls, level 0 … 4 (p = 2, 5 only at 0).
3. Hit on feature p = 0 (chance roll 0, league roll 3): level − 1, message, return (at most one damage a week).
4. Hit on feature p = 3 (Sitzkomfort; own message).
5. Hit on seats p = 9 with 5000 seats: random(5000 / 20, 5000 / 6), damage ((r + 150) / 100) · 100.
6. Chance hit with league roll ≤ league (no damage, loop goes on) and a feature at level 0 (league roll still drawn).

Usage: python3 stadionschaeden.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
REC = 0x9771
SIZE = 0x1AF
LEAGUE = 0x9746
FUNC = (0x3CFA0, 0x3D316)
CRT = (emu16.HEADER, emu16.HEADER + 0x46E * 16)
NUMBUF = 0xF000


def run(level, league, levels, seats, standing, answers, manager=0):
    c = emu16.load(PATH)
    c.allowed = [FUNC, CRT]
    c.sdb(0xA70E, manager)
    c.sdb(0xED8, level)
    c.sdb(LEAGUE + SIZE * manager, league)
    base = REC + SIZE * manager
    for p, v in enumerate(levels):
        c.sdb(base + 2 * p, v)
    c.sdw(base + 0x1D, seats)
    c.sdw(base + 0x21, standing)
    log = []
    feed = list(answers)

    def project(cc):
        # frame of 0x3CFA0: the caller's bp is the current bp inside the random stub entry
        return cc.rb(cc.s["ss"], (cc.r["bp"] - 5) & 0xFFFF)

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        lo, hi = (lo - 0x10000 if lo & 0x8000 else lo), (hi - 0x10000 if hi & 0x8000 else hi)
        caller = cc.calls[-1][0] if cc.calls else 0
        v = feed.pop(0) if feed else hi
        v = {"lo": lo, "hi": hi}.get(v, v)
        log.append(f"random({lo}, {hi}) aus Datei {caller:#07x}, Projekt {project(cc)} → {v}")
        return v & 0xFFFF

    def number(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        v = (hi << 16 | lo)
        text = f"{v:,}".replace(",", ".") if cc.db(0x38D) == 0 else str(v)
        for i, ch in enumerate(text.encode("cp437") + b"\0"):
            c.wb(emu16.DGROUP, NUMBUF + i, ch)
        cc.r["dx"] = emu16.DGROUP
        log.append(f"Zahl 0x0BAB2({v}), [0x38D] = {cc.db(0x38D)} → „{text}“")
        return NUMBUF

    def message(cc):
        a = cc.cstr(cc.arg(1), cc.arg(0))
        b = cc.cstr(cc.arg(3), cc.arg(2))
        log.append(f"Meldung 0x4B2BA Art {cc.arg(4) & 0xFF}: „{a}“ / „{b}“")
        return 0
    c.hooks[0x0C037] = rnd
    c.hooks[0x0BAB2] = number
    c.hooks[0x4B2BA] = message
    c.call(0x3CFA0)
    after = [c.db(base + 2 * p) for p in range(9)]
    return log, after, c.dw(base + 0x1D), c.dw(base + 0x21)


def show(title, **kw):
    log, after, seats, standing = run(**kw)
    print(title)
    for line in log:
        print("   " + line)
    print(f"   Stufen danach {after}, Sitzplätze {seats}, Stehplätze {standing}")


def main():
    levels = [2, 1, 1, 2, 2, 1, 1, 1, 1]
    show("1. Sperre: erster Wurf ≠ 0 (Level-Index 2, Liga 0)",
         level=2, league=0, levels=levels, seats=3000, standing=4500, answers=[1])
    for lv in (0, 2, 4):
        show(f"2. Sperre 0, alle Teilwürfe verfehlt (Antwort = Obergrenze), Level-Index {lv}",
             level=lv, league=0, levels=levels, seats=3000, standing=4500, answers=[0])
    show("3. Treffer an Teil 0 (Überdachung): Teilwurf 0, Ligawurf 3",
         level=2, league=0, levels=levels, seats=3000, standing=4500, answers=[0, 0, 3])
    show("4. Treffer an Teil 3 (Sitzkomfort): Teile 0–1 verfehlt, Teil 3 Teilwurf 0, Ligawurf 3",
         level=2, league=0, levels=levels, seats=3000, standing=4500, answers=[0, "hi", "hi", 0, 3])
    show("5. Treffer an Sitzplätzen (Projekt 9, 5000 Plätze): Teile 0–8 verfehlt, dann 0, Ligawurf 3, Schaden lo",
         level=2, league=0, levels=levels, seats=5000, standing=4500,
         answers=[0] + ["hi"] * 7 + [0, 3, "lo"])
    show("5b. wie 5, Schaden hi", level=2, league=0, levels=levels, seats=5000, standing=4500,
         answers=[0] + ["hi"] * 7 + [0, 3, "hi"])
    show("6. Liga 1: Teil 0 Teilwurf 0, Ligawurf 1 (≤ Liga, kein Schaden); Teil 1 auf Stufe 0: Teilwurf 0, Ligawurf 3",
         level=2, league=1, levels=[2, 0, 1, 2, 2, 1, 1, 1, 1], seats=3000, standing=4500, answers=[0, 0, 1, 0, 3])
    show("7. Stehplätze (Projekt 10, 4500 Plätze), Teile 0–9 verfehlt, Level-Index 0, Liga 2",
         level=0, league=2, levels=levels, seats=3000, standing=4500,
         answers=[0] + ["hi"] * 10 + [0, 3, "lo"])
    print("8. Meldung je Projekt (Level-Index 0, Liga 0, Treffer am Projekt p, alle vorher verfehlt):")
    for p in range(11):
        answers = [0] + ["hi"] * p + [0, 3, "lo"]
        log, after, seats, standing = run(level=0, league=0, levels=levels, seats=3000, standing=4500, answers=answers)
        msg = [x for x in log if x.startswith("Meldung")]
        print(f"   Projekt {p}: {msg[0] if msg else '(keine)'}")


if __name__ == "__main__":
    main()
