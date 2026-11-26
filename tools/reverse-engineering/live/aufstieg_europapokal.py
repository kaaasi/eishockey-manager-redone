"""S04 rest (R26-E2 = C): promotion round (page 2) and European cup (pages 3-5) via 0x31CD5 with and without a minute.

The page selector 0x3A88A (`drittelseiten.py`) calls the group screen 0x31CD5 (Stub 0xE8:0x6B) with seven arguments
(Datei 0x3AB05-0x3ABCE): competition (1 promotion round, 2 European cup groups, 3 final round), group, flag "in game"
[bp+0xA] = (minute != 0), count [bp+0xC], 0 [bp+0xE], calendar mask [bp+0x10] (0x40 play-offs, 0x20 European cup) and the
intermission flag [bp+0x12] = (minute 20 or 40, selector [bp-4]). Count:
  promotion round  min(2 * DGROUP 0x7B0 + (0xCCA3 > 0) + (0xCCA3 > 2), 7) + (minute in {0, 60}) - 1   (Datei 0x3AAA4-0x3AB27)
  European groups  min(DGROUP 0x7AF, 6) + (minute in {0, 60}) - 1                                       (Datei 0x3AB43-0x3AB71)
  final round      (0x7AF - 6 if 0x7AF > 5 else 0x7AF) + (minute in {0, 60}) - 1                         (Datei 0x3AB8A-0x3ABB9)
This script
  1. evaluates these formulas from the selector bytes (the selector runs in the emulator, 0x31CD5 is a stub logging its
     arguments) for minute 0 (main menu) and 20/40/60/65/70, and
  2. runs 0x31CD5 itself from the bytes of em.exe (overlay 0xE8) for one group with prepared pairings and results and
     logs every text drawn by 0x0B651 (left: x, y, colour), so the rule "result or date" per game g = round + 3 * leg is
     visible: result if g < count + intermission, or g = count + intermission with "in game" (Datei 0x324C7-0x32515);
     date only in the first line of a round, without "in game" and for g >= count + intermission (Datei 0x3228D-0x322C6), and
  3. runs the selector as the main menu calls it (0x3A88A(3, 0, 0), Datei 0x45885) together with 0x31CD5 for DGROUP 0x7AF = 0
     and 3: in the main menu the open games of the first lines get their date (PW-04).
Club names and dates are not read (empty tables); results appear as numbers only. No original data is printed.
Usage: python3 -I tools/reverse-engineering/live/aufstieg_europapokal.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
GROUP_SCREEN = 0x31CD5
SELECTOR = 0x3A88A


def base_cpu():
    c = emu16.load(PATH)
    c.sdw(0xC147, 0)
    c.sdw(0xC149, emu16.FAR_HEAP_SEG)
    c.sdw(0x9657, 0x100)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    c.max_steps = 20_000_000
    return c


def selector_args(page, sub, minute, d7af=0, d7b0=0, cca3=0):
    """Arguments of 0x31CD5 as the selector computes them (all option switches on, no permanent rink)."""
    c = base_cpu()
    sel = next(r for r in c.regions if r.start <= SELECTOR < r.end)
    c.allowed = [(sel.start, sel.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16)]
    for i in range(13):
        c.sdb(0x21D + i, 1)
    c.sdb(0x5A55, 0)
    c.sdb(0x7AF, d7af)
    c.sdb(0x7B0, d7b0)
    c.sdb(0xCCA3, cca3)
    c.sdb(0xCFFA, 0)
    calls = []

    def stub(n, name):
        def h(cc):
            calls.append((name, tuple(cc.arg(i) & 0xFF for i in range(n))))
            return 0
        return h
    c.hooks[GROUP_SCREEN] = stub(7, "0x31CD5")
    c.hooks[0x3183C] = stub(4, "0x3183C")
    for quiet in (0x45D46, 0x0C5CE, 0x0B447):
        c.hooks[quiet] = lambda cc: 0
    try:
        c.call(SELECTOR, page, sub, minute)
    except emu16.Stop:
        pass
    return calls


def group_screen(comp, in_game, count, intermission, mask, group=0, d7af=12):
    """Texts drawn by 0x31CD5 for one group: game g has the result (g + 1, 10 + g) stored for slot (home, away)."""
    c = base_cpu()
    ov = next(r for r in c.regions if r.stub == 0xE8)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0x3800, 0x3C00), (0xB954, 0xBAC8)]
    c.sdb(0x7AF, d7af)
    c.sdb(0xCFFA, 0)
    c.sdb(0xA70E, 0x7F)
    base = comp * 0x48 + group * 0x24
    for j in range(4):
        c.sdb(0xCFF4 + comp * 8 + group * 4 + j, 20 + j)
    rounds = ((0, 1, 2, 3), (0, 2, 1, 3), (0, 3, 1, 2))
    for g in range(6):
        r, leg = g % 3, g // 3
        a, b, x, y = rounds[r]
        pairs = (a, b, x, y) if leg == 0 else (b, a, y, x)
        for k in range(4):
            c.sdb(0xCED8 + base + 6 * g + k, 20 + pairs[k])
            c.sdb(0xCDB8 + base + 6 * g + k, (g + 1) if k % 2 == 0 else 10 + g)
        c.sdb(0xCED8 + base + 6 * g + 4, 0xFF)
        c.sdb(0xCED8 + base + 6 * g + 5, 0xFF)
    out = []

    def date(cc):
        out.append(("Termin", cc.arg(0)))
        c.sdw(0x5A1C, 0)
        c.sdw(0x5A1E, 0)
        c.sdw(0x5A20, 0)
        c.sdw(0x5A22, 0)
        return 0
    c.hooks[0x367B1] = date

    def left(cc):
        out.append(("links", cc.arg(4), cc.arg(5), cc.arg(6), cc.cstr(cc.arg(3), cc.arg(2))))
        return 0
    c.hooks[0x0B651] = left
    c.hooks[0x0B6A5] = lambda cc: 0
    c.hooks[0x0B155] = lambda cc: 0

    def event(cc):
        raise emu16.Stop("Ereignis")
    c.hooks[0x0C068] = event
    try:
        c.call(GROUP_SCREEN, comp, group, in_game, count, 0, mask, intermission)
    except emu16.Stop:
        pass
    return out


def main_menu_page(d7af):
    """Selector 0x3A88A(3, 0, 0) as from the main menu (Datei 0x45885) with 0x31CD5 running from the bytes: date calls and
    result/date columns of the first lines (PW-04)."""
    c = base_cpu()
    ov = next(r for r in c.regions if r.stub == 0xE8)
    sel = next(r for r in c.regions if r.start <= SELECTOR < r.end)
    c.allowed = [(ov.start, ov.end), (sel.start, sel.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0x3800, 0x3C00),
                 (0xB954, 0xBAC8)]
    for i in range(13):
        c.sdb(0x21D + i, 1)
    c.sdb(0x7AF, d7af)
    c.sdb(0xCFFA, 0)
    c.sdb(0xA70E, 0x7F)
    for j in range(4):
        c.sdb(0xCFF4 + 2 * 8 + j, 20 + j)
    for g in range(6):
        for k in range(4):
            c.sdb(0xCED8 + 2 * 0x48 + 6 * g + k, 20 + k)
    out = []
    c.hooks[0x367B1] = lambda cc: (out.append(("Termin", cc.arg(0))), 0)[1]
    c.hooks[0x0B651] = lambda cc: (out.append(("links", cc.arg(4), cc.arg(5), cc.arg(6), "")), 0)[1]
    for quiet in (0x0B6A5, 0x0B155, 0x45D46, 0x0C5CE, 0x0B447):
        c.hooks[quiet] = lambda cc: 0

    def event(cc):
        raise emu16.Stop("Ereignis")
    c.hooks[0x0C068] = event
    try:
        c.call(SELECTOR, 3, 0, 0)
    except emu16.Stop:
        pass
    return out


def games(texts):
    """Per pairing line (y) the result or date columns: x 226/272 result, x 230/276 date (0x0B651 calls)."""
    lines = {}
    dates = [t for t in texts if t[0] == "Termin"]
    for t in texts:
        if t[0] == "links" and t[1] in (226, 230, 272, 276) and t[2] < 130:
            kind = "Ergebnis" if t[1] in (226, 272) else "Termin"
            lines.setdefault(t[2], []).append((t[1], kind, t[4]))
    return lines, len(dates)


def main():
    print("1. Argumente von 0x31CD5 aus dem Seitenwähler (alle Schalter an):")
    for page, sub, name, kw in ((2, 1, "Aufstiegsrunde Gruppe 0 (Seite 2, 0x7B0 = 1, 0xCCA3 = 0)", dict(d7b0=1, cca3=0)),
                                (2, 2, "Aufstiegsrunde Gruppe 1 (Seite 2, 0x7B0 = 1, 0xCCA3 = 2)", dict(d7b0=1, cca3=2)),
                                (3, 0, "Europapokal Gruppe 1 (Seite 3, 0x7AF = 2)", dict(d7af=2)),
                                (4, 0, "Europapokal Gruppe 2 (Seite 4, 0x7AF = 2)", dict(d7af=2)),
                                (5, 0, "Europapokal Endrunde (Seite 5, 0x7AF = 8)", dict(d7af=8))):
        print("  ", name)
        for minute in (0, 20, 40, 60, 65, 70):
            calls = [a for n, a in selector_args(page, sub, minute, **kw) if n == "0x31CD5"]
            live = [a for n, a in selector_args(page, sub, minute, **kw) if n == "0x3183C"]
            print(f"      Minute {minute:2}: 0x31CD5{calls}  0x3183C{live}")
    print()
    print("2. 0x31CD5 selbst (Europapokal-Gruppe, Wettbewerb 2): Spalten je Paarungszeile (x, Art, Text)")
    for name, args in (("Hauptmenü, 2 Termine gespielt (im Spiel 0, Zahl 2, Pause 0)", (0, 2, 0)),
                       ("Hauptmenü, vor dem ersten Termin (0, 0, 0)", (0, 0, 0)),
                       ("3. Termin, Minute 20 (im Spiel 1, Zahl 1, Pause 1)", (1, 1, 1)),
                       ("3. Termin, Minute 60 (1, 2, 0)", (1, 2, 0)),
                       ("3. Termin, Minute 65 (1, 1, 0)", (1, 1, 0))):
        lines, n = games(group_screen(2, *args, 0x20))
        print("  ", name, f"– Datumsaufrufe 0x367B1: {n}")
        for y in sorted(lines):
            print(f"      y {y}: {lines[y]}")


def main_menu():
    print()
    print("3. Hauptmenü: Seitenwähler 0x3A88A(3, 0, Minute 0) mit 0x31CD5 aus den Bytes (PW-04)")
    for d7af in (0, 3):
        lines, n = games(main_menu_page(d7af))
        print(f"   DGROUP 0x7AF = {d7af}: Datumsaufrufe 0x367B1: {n}")
        for y in sorted(lines):
            print(f"      y {y}: {[(x, kind) for x, kind, _ in lines[y]]}")


if __name__ == "__main__":
    main()
    main_menu()
