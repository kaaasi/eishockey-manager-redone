"""Spur hok-patch-tabellen, Teil 1: the promotion-round tables of a save game recomputed with em.exe's own matchday code.

The group tables of competition 1 (promotion rounds) are not computed from the results when a game is loaded or shown; the
group matchday 0x30852 (Stub 0xE8:0x48, called per play-off day from Datei 0x38691) adds each result to points DGROUP
0xCD58, goals 0xCD28 and goals against 0xCCF8 (index 12 * competition + 6 * group + participant, via 0x303D9) and then
re-sorts the place list 0xCD88 (same index, participant per place). This script replays a save game's promotion round from
the start: participants DGROUP 0xCFFC + 4 * group, pairing rows 0xCF20 + 0x24 * group + 6 * round (club slots), place list
0, 1, 2, 3 and zero points (draw 0x30427: the eleventh and twelfth of the upper league, then the first and second of the
lower league; 5CCB + 12 * league + place), and for every played round t it stores the save game's goal bytes
(0xCE00 + 0x24 * group + 6 * t, home/away, +30 per mark) as match results at DGROUP 0x5CAB + 4 * group + 2 * pair and runs
0x30852(1, 0, 4, t) from the bytes (0x50283, the series/form booking, is a stub; it writes table fields only on league days,
DGROUP 0xD0C2 = 7/8). The result is compared with the points, goals, goals against and place list stored in the save game.
Played rounds: T = min(2 * DGROUP 0x7B0 + (0xCCA3 > 0) + (0xCCA3 > 2), 6) (Datei 0x38652-0x38691; selector 0x3AAA4).
It also checks the participants against the league place list 0x5CCB at the draw and prints the consistency sums.
Usage: python3 -I gruppentabelle.py <save.HOK> [em.exe]     (numbers only, no names)
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "abweichungen"))
sys.path.insert(0, str(HERE))
import emu16  # noqa: E402
import hokread  # noqa: E402

GROUP_DAY = 0x30852


def played_rounds(s):
    return min(2 * s.b(0x7B0) + (s.b(0xCCA3) > 0) + (s.b(0xCCA3) > 2), 6)


def replay(s, exe, rounds):
    c = emu16.load(exe)
    c.max_steps = 3_000_000
    ov = next(r for r in c.regions if r.start <= GROUP_DAY < r.end)
    c.allowed = [(ov.start, ov.end)]
    c.sdw(0x9657, 0x100)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    cffc = s.block("CFFC")
    for i in range(8):
        c.sdb(0xCFFC + i, cffc[i])
    for g in range(2):
        for t in range(6):
            for i in range(6):
                c.sdb(0xCF20 + 0x24 * g + 6 * t + i, s.b(0xCF20 + 0x24 * g + 6 * t + i))
        for p in range(4):
            c.sdb(0xCD94 + 6 * g + p, p)
            for base in (0xCD64, 0xCD34, 0xCD04):
                c.sdb(base + 6 * g + p, 0)
    for t in range(rounds):
        for g in range(2):
            for k in range(2):
                for side in range(2):
                    c.sdb(0x5CAB + 4 * g + 2 * k + side, s.b(0xCE00 + 0x24 * g + 6 * t + 2 * k + side))
        c.call(GROUP_DAY, 1, 0, 4, t)
    return c


def main():
    path = sys.argv[1]
    exe = sys.argv[2] if len(sys.argv) > 2 else None
    s = hokread.Save(path)
    rounds = played_rounds(s)
    print(f"{Path(path).name}: Prüfsummen gültig {s.checksums_ok}; 0x7B0 = {s.b(0x7B0)}, 0xCCA3 = {s.b(0xCCA3)} "
          f"-> gespielte Termine T = {rounds}")
    c = replay(s, exe, rounds)
    places = list(s.block("5CCB"))
    all_ok = True
    for g in range(2):
        parts = list(s.block("CFFC")[4 * g:4 * g + 4])
        expect = [places[12 * g + 10], places[12 * g + 11], places[12 * g + 12], places[12 * g + 13]]
        names = (("Punkte", 0xCD64), ("Tore", 0xCD34), ("Gegentore", 0xCD04), ("Platzliste", 0xCD94))
        line = []
        for label, base in names:
            stored = list(s.dgroup(base + 6 * g, 4))
            got = [c.db(base + 6 * g + p) for p in range(4)]
            ok = stored == got
            all_ok &= ok
            line.append(f"{label} {got} {'=' if ok else '!='} {stored}")
        row0 = list(s.dgroup(0xCF20 + 0x24 * g, 4))
        pts = list(s.dgroup(0xCD64 + 6 * g, 4))
        gf, ga = sum(s.dgroup(0xCD34 + 6 * g, 4)), sum(s.dgroup(0xCD04 + 6 * g, 4))
        print(f"  Gruppe {g}: Teilnehmer {parts}, Zeile 0 {row0} ({'=' if row0 == parts else '!='} Teilnehmer), "
              f"Plätze 11/12 oben + 1/2 unten laut 5CCB {expect} ({'=' if sorted(expect) == sorted(parts) else '!='})")
        print(f"    Nachbildung 0x30852 über {rounds} Termine: " + "; ".join(line))
        print(f"    Summen: Punkte {sum(pts)} (4·T = {4 * rounds}), Tore {gf} = Gegentore {ga}: {gf == ga}")
    print(f"  Ergebnis: gespeicherte Gruppentabellen {'gleich' if all_ok else 'NICHT gleich'} der Nachbildung")


if __name__ == "__main__":
    main()
