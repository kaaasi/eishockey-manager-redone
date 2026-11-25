"""S01 (A442, A450, A454, A547, A554, A555, A766, A909, A954): replay of the minute loop 0x37A62 over all pairings of a day.

Runs the original code of 0x37A62 (Ov 0xF0, Datei 0x37A62–0x386D1) with the emulator `abweichungen/emu16.py` and logs, in
the order the original executes them, every call of the minute computation per pairing:
  0x38D96 intervention menu (only pairings in the row list, 0x52B91, and DGROUP 0x5A16 in {1, 3}),
  0x38B8F injuries (club field +0x16 of home or away ≠ 0), 0x09F5C chances, 0x0A484 shot (home 0 / away 1),
  0x0874D, 0x087F7 (row only), random(0, 1) 0x0C037 swap of the sides (row only), 0x080CB (odd minute), 0x08EF2 ×2,
  0x0939A ×2 (even minute), 0x35818 penalties ×2 (not in minute 60/70), 0x0BFD5 lrandom(0, 4) wink (display only),
  0x52BD6 scoreboard of all rows at the end of the minute, 0x2CD1C options (space key).
Everything else outside 0x37A62 is a stub. Prepared memory: pairing tables (league DGROUP 0x5D7B + 12 l, count 0x7B1[l],
result bytes 0xA0BF + 0x210 l + 12 · 0x7A9[l]; cup rows DGROUP 0x5CFB + 32 r, result bytes 0x5C4B + 32 r, first pairing
0xCFF8 + r), team table far pointer 0x9657 (stride 0x3D). Only numbers and addresses are printed (no original data).
Usage: python3 tools/reverse-engineering/live/minutenschleife.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
CLUB = 0x3D
NAMES = {
    0x38D96: "Eingriff", 0x38B8F: "Verletzung", 0x09F5C: "Chancen", 0x0A484: "Schuss", 0x0874D: "874D",
    0x087F7: "87F7", 0x0C037: "random", 0x080CB: "80CB", 0x08EF2: "8EF2", 0x0939A: "939A", 0x35818: "Strafe",
    0x0BFD5: "lrandom", 0x52BD6: "Tafel", 0x2CD1C: "Optionen", 0x36AAA: "Tor-Wertung",
}


def make(rows=frozenset(), managed=frozenset(), space_minute=None, chance=3, goal=(), speed=80):
    """rows: clubs in the row list (0x52B91 true); managed: club field +0x16 ≠ 0; goal: set of (minute, club) that score."""
    c = emu16.load(PATH)
    c.allowed = [(0x37A62, 0x386D1), (emu16.HEADER, emu16.HEADER + 0x46E * 16)]
    c.int_hook = lambda cpu_, n: (cpu_.r.__setitem__("ax", 0), True)[1]
    c.max_steps = 20_000_000
    c.sdw(0x9657, 0)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    for club in managed:
        c.wb(emu16.FAR_HEAP_SEG, club * CLUB + 0x16, 1)
    c.sdw(0xC147, 0x100)                       # window object of the mouse: +0 ≠ 0 (no options without the space key)
    c.sdw(0xC149, emu16.FAR_HEAP_SEG + 0x1000)
    c.wb(emu16.FAR_HEAP_SEG + 0x1000, 0x100, 1)
    c.sdw(0x5A16, 1)
    c.sdw(0x262, speed)                       # speed (≥ 70: no pause, no wink)
    c.sdb(0x1DB, 0)
    c.sdb(0x5A55, 0)
    log = []

    def rec(addr, *args):
        # DGROUP 0xE19E is the minute from Datei 0x37CD2 on; the wink roll 0x37C8C comes before it is set
        log.append((c.db(0xE19E) + (addr == 0x0BFD5), NAMES[addr], args))

    def hook(addr, nargs, ret=0, fn=None):
        def h(cc):
            args = tuple(cc.arg(i) & 0xFF for i in range(nargs))
            rec(addr, *args)
            return fn(cc, args) if fn else ret
        c.hooks[addr] = h

    hook(0x38D96, 8)
    hook(0x38B8F, 6)
    hook(0x09F5C, 2, chance)
    hook(0x0A484, 7, fn=lambda cc, a: int(goal is not None and (c.db(0xE19E), a[2] if a[4] == 0 else a[3]) in goal))
    for a in (0x0874D, 0x087F7):
        hook(a, 1)
    hook(0x0C037, 2)
    hook(0x080CB, 2)
    hook(0x08EF2, 2)
    hook(0x0939A, 1)
    hook(0x35818, 1)
    hook(0x0BFD5, 0, 1)
    hook(0x52BD6, 1)
    hook(0x2CD1C, 0)
    hook(0x36AAA, 5)
    c.hooks[0x52B91] = lambda cc: int((cc.arg(0) & 0xFF) in rows)
    c.hooks[0x1080E] = lambda cc: 0x20 if space_minute is not None and c.db(0xE19E) == space_minute else 0
    return c, log


def league_day(c, leagues=3, pairs=6):
    """Clubs 10 l + 2 p (home) and 10 l + 2 p + 1 (away) – club numbers only as labels."""
    for lg in range(leagues):
        c.sdb(0x7B1 + lg, 2 * pairs)
        c.sdb(0x7A9 + lg, 0)
        for p in range(pairs):
            c.sdb(0x5D7B + 12 * lg + 2 * p, 20 * lg + 2 * p)
            c.sdb(0x5D7C + 12 * lg + 2 * p, 20 * lg + 2 * p + 1)


def cup_rows(c, rows, pairs=4, marks=None):
    for r in rows:
        c.sdb(0xCFF8 + r, 0)
        for i in range(32):
            c.sdb(0x5CFB + 32 * r + i, 0xFF)
            c.sdb(0x5C4B + 32 * r + i, 0)
        for p in range(pairs):
            c.sdb(0x5CFB + 32 * r + 2 * p, 100 + 20 * r + 2 * p)
            c.sdb(0x5CFC + 32 * r + 2 * p, 100 + 20 * r + 2 * p + 1)
    c.sdb(0xD074, 0)
    c.sdb(0xCCA3, 0)
    for (r, p), (h, g) in (marks or {}).items():
        c.sdb(0x5C4B + 32 * r + 2 * p, h)
        c.sdb(0x5C4C + 32 * r + 2 * p, g)


def order(log, minute):
    """Pairings in the order their chances are rolled in a minute (home club of 0x09F5C)."""
    return [a[0] for m, n, a in log if m == minute and n == "Chancen"]


def show_minute(log, minute):
    print(f"   Minute {minute}:")
    for m, n, a in log:
        if m == minute:
            print(f"     {n}{a}")


def main():
    print("1. Ligaspieltag, Minuten 1–20 (0x37A62(Ligamaske 7, Pokal 0, 1, 20, Anzeige 1)), 3 Ligen × 6 Paarungen;")
    print("   Reihenliste: Verein 2 (Liga 0, Paarung 1) und Verein 41 (Liga 2, Paarung 0); Managervereine 2 und 41; Geschwindigkeit 0")
    c, log = make(rows={2, 3, 40, 41}, managed={2, 41}, speed=0)
    league_day(c)
    c.call(0x37A62, 7, 0, 1, 1, 20, 1, 0)
    print("   Reihenfolge der Paarungen (Heimverein) in Minute 1:", order(log, 1))
    print("   alle Minuten gleich:", all(order(log, m) == order(log, 1) for m in range(1, 21)))
    show_minute(log, 1)
    show_minute(log, 2)
    per_min = {m: sum(1 for x in log if x[0] == m and x[1] == "random") for m in (1, 2)}
    print("   random(0, 1) je Minute (nur Reihenpaarungen):", per_min)
    print("   Eingriff je Minute:", sum(1 for x in log if x[0] == 5 and x[1] == "Eingriff"),
          "(Reihenpaarungen ×1, vor deren Rechnung)")

    print("\n2. Leertaste in Minute 7 (Spielmodus-Optionen 0x2CD1C, Datei 0x37D0B–0x37D89)")
    c, log = make(rows={2, 3}, managed={2}, space_minute=7, speed=0)
    league_day(c, leagues=1)
    c.call(0x37A62, 1, 0, 1, 1, 20, 1, 0)
    seq = [(m, n) for m, n, a in log if m in (6, 7, 8) and n in ("Optionen", "Chancen", "Tafel", "lrandom")]
    print("   Ablauf Minute 6–8 (lrandom, Optionen, Chancen, Tafel):", seq)

    print("\n3. Pokalzweig, Minuten 1–20, Maske 0xC (Play-off Zeile 2 und WM-Endrunde Zeile 3, je 4 Paarungen)")
    c, log = make(rows={140, 141}, managed={140})
    cup_rows(c, (2, 3))
    c.call(0x37A62, 0, 0xC, 1, 1, 20, 1, 2)
    print("   Reihenfolge Minute 1:", order(log, 1))
    show_minute(log, 1)

    print("\n4. Verlängerung 61–65 (Maske 1, DEB-Pokal Zeile 0): Paarung 0 und 2 Gleichstand mit Marke (33:3),")
    print("   Paarung 1 entschieden (4:2) und Reihenpaarung (Verein 102); Tor Verein 104 in Minute 62")
    c, log = make(rows={102, 103}, managed={102}, goal={(62, 104)})
    cup_rows(c, (0,), marks={(0, 0): (33, 3), (0, 1): (4, 2), (0, 2): (33, 3), (0, 3): (2, 1)})
    c.call(0x37A62, 0, 1, 1, 61, 65, 1, 0)
    print("   Reihenfolge Minute 61:", order(log, 61), "– Eingriffe in 61–65:",
          sum(1 for x in log if x[1] == "Eingriff"))
    show_minute(log, 62)
    print("   Minute 63 Reihenfolge:", order(log, 63))

    print("   Tor-Wertung 0x36AAA(Schalter, Zeile, Paarung, Heim, Gast):", [a for m, n, a in log if n == "Tor-Wertung"])

    print("\n6. Verlängerung: beide Seiten treffen in Minute 62 (Paarung 0, 33:3)")
    c, log = make(goal={(62, 100), (62, 101)})
    cup_rows(c, (0,), pairs=1, marks={(0, 0): (33, 3)})
    c.call(0x37A62, 0, 1, 1, 61, 65, 0, 0)
    show_minute(log, 62)

    print("\n5. Liga ohne Anzeige (Anzeige 0): Würfe je Minute")
    c, log = make(rows=set(), managed=set())
    league_day(c)
    c.call(0x37A62, 7, 0, 1, 1, 20, 0, 0)
    print("   lrandom Minute 1:", sum(1 for x in log if x[0] == 1 and x[1] == "lrandom"),
          "– random(0, 1):", sum(1 for x in log if x[0] == 1 and x[1] == "random"),
          "– Tafel:", sum(1 for x in log if x[0] == 1 and x[1] == "Tafel"))


if __name__ == "__main__":
    main()
