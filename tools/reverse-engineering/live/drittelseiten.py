"""S04 (A292, A436, F068): replay of the pages after each period of a live match day.

Runs the original code of the minute loop 0x37A62 (Ov 0xF0, Datei 0x37A62–0x386D1) for the last minute of a period and the
page selector 0x3A88A (Stub 0xF8:0x3E, Datei 0x3A88A–0x3ACE3) with the emulator `abweichungen/emu16.py`. The minute
computation is stubbed exactly as in `minutenschleife.py`; the end of 0x37A62 (Datei 0x3855F–0x386CD) and the filter of
0x3A88A run from the bytes. Logged (in the order the original calls them): the page functions
  0x4E8A3 league pairings (league, flags, minute, 1), 0x32E23 cup games (competition, minute),
  0x31CD5 promotion round / European cup tables (7 args), 0x2FC2F World Cup group pairings (group, minute, stage),
  0x2F5B6 World Cup group table (group, minute), 0x3183C live goals into the group tables (competition, start, stride, day),
  0x30852 group table at minute 60, 0x369C7 running scores into the result table (2, cup),
  0x50B45/0x51049/0x052CF league tables at minute 60.
Every page returns 0 (= no further page). Prepared memory: match speed 80 (no click wait), DGROUP 0x5A55 (permanent rink),
option block DGROUP 0x21D…0x229, competition code DGROUP 0xD0C2, World Cup stage 0x7AE, play-off game 0xCCA3.
Only numbers and addresses are printed (no original data).
Usage: python3 tools/reverse-engineering/live/drittelseiten.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import minutenschleife as ms  # noqa: E402

PAGES = {
    0x4E8A3: ("Ligapaarungen", 4), 0x32E23: ("Pokalspiele", 2), 0x31CD5: ("Gruppentabelle", 7),
    0x2FC2F: ("WM-Gruppenpaarungen", 3), 0x2F5B6: ("WM-Gruppentabelle", 2), 0x3183C: ("Live-Tore", 4),
    0x30852: ("Tabelle 60", 4), 0x369C7: ("Stände", 2), 0x50B45: ("Ligatabelle", 3), 0x51049: ("Ligatabelle2", 1),
    0x052CF: ("Ligatabelle3", 2),
}
QUIET = {0x45D46: 0, 0x0C5CE: 3, 0x0B447: 1, 0x0B908: 2}
OPTION = 0x21D       # 0..8 matrix (league · 3 + row: 0 Drittelpause, 1 Ergebnisse, 2 Tabelle), 10 DEB, 11 EC, 12 WM


def make(options=(), code=0, stage=5, game=0, rink=0):
    c, log = ms.make()
    c.allowed.append((0x3A88A, 0x3ACE3))
    for i in range(16):
        c.sdb(OPTION + i, 0)
    for i in options:
        c.sdb(OPTION + i, 1)
    c.sdb(0xD0C2, code)
    c.sdb(0x7AE, stage)
    c.sdb(0xCCA3, game)
    c.sdb(0x5A55, rink)
    c.sdb(0x7AF, 2)
    c.sdb(0x7B0, 1)
    c.sdb(0xCFFA, 0)
    pages = []
    for addr, (name, n) in PAGES.items():
        def h(cc, name=name, n=n):
            pages.append((name, tuple(cc.arg(i) & 0xFF for i in range(n))))
            return 0
        c.hooks[addr] = h
    for addr in QUIET:
        c.hooks[addr] = lambda cc: 0
    return c, pages


def period(minute, league_mask=0, cup_mask=0, row=0, **kw):
    """Last minute of a period: 0x37A62(league mask, cup mask, 1, minute, minute, display 1, row)."""
    c, pages = make(**kw)
    ms.league_day(c, leagues=3, pairs=1)
    ms.cup_rows(c, (row,), pairs=1)
    c.sdb(0xCCA3, kw.get("game", 0))          # cup_rows clears the play-off game
    c.call(0x37A62, league_mask, cup_mask, 1, minute, minute, 1, row)
    return [p for p in pages]


def shown(pages):
    return [p for p in pages if p[0] not in ("Stände", "Ligatabelle", "Ligatabelle2", "Ligatabelle3")]


def main():
    print("1. Ligaspieltag (Ligamaske 7), Seiten nach dem Drittel (0x3A88A Seite 7 → 0x4E8A3):")
    for label, opts in (("alle Optionen aus", ()), ("Drittelpause 1.Liga (Index 0)", (0,)),
                        ("Ergebnisse 2.Liga (Index 4)", (4,)), ("Tabelle 1.Liga (Index 2)", (2,)),
                        ("Drittelpause und Ergebnisse aller Ligen", (0, 1, 3, 4, 6, 7))):
        res = {m: shown(period(m, league_mask=7, options=opts)) for m in (20, 40, 60)}
        print(f"   {label}: {res}")
    print("   Minute 60, Optionen aus – alle Aufrufe:", period(60, league_mask=7))
    print("   Darstellung 2 (DGROUP 0x5A55 = 1), alle Ligaoptionen an:",
          {m: shown(period(m, league_mask=7, options=range(9), rink=1)) for m in (20, 60)})

    print("\n2. Pokaltermine (Pokalmaske ≠ 0), Seite nach Wettbewerbscode DGROUP 0xD0C2:")
    cases = (
        ("DEB-Pokal (Code 6, Zeile 0)", dict(cup_mask=1, row=0, code=6)),
        ("Play-off (Code 1, Spiel 1, Zeile 2)", dict(cup_mask=4, row=2, code=1, game=1)),
        ("Play-off und Aufstiegsrunde (Code 1, Spiel 0)", dict(cup_mask=4, row=2, code=1, game=0)),
        ("Europapokal (Code 3, Zeile 1)", dict(cup_mask=2, row=1, code=3)),
        ("WM-Gruppe (Code 8, Zeile 3, Stufe 2)", dict(cup_mask=8, row=3, code=8, stage=2)),
        ("WM-Endrunde (Code 10, Zeile 3, Stufe 5)", dict(cup_mask=8, row=3, code=10, stage=5)),
        ("Freundschaftsspiel (Code 13)", dict(cup_mask=1, row=0, code=13)),
    )
    option_sets = (("Anfangswerte (nur DEB-Pokal 10)", (10,)), ("alles aus", ()),
                   ("Drittelpause 1.Liga (0)", (0,)), ("Ergebnisse 1.Liga (1)", (1,)),
                   ("DEB-Pokal 10 + Drittelpause 1.Liga 0", (0, 10)), ("Europapokal 11", (11,)),
                   ("Europapokal 11 + Drittelpause 0", (0, 11)), ("WM 12", (12,)), ("WM 12 + Drittelpause 0", (0, 12)),
                   ("Drittelpause/Ergebnisse 2.Liga (3, 4)", (3, 4)))
    for label, kw in cases:
        print(f"   {label}:")
        for olabel, opts in option_sets:
            res = {m: shown(period(m, options=opts, **kw)) for m in (20, 40, 60, 65, 70)}
            print(f"     {olabel}: {res}")

    print("\n3. Nach dem Penaltyschießen (Datei 0x37297–0x372AC: 0x3A88A(Code, 0, 70)):")
    for label, code, opts in (("DEB-Pokal, Anfangswerte", 6, (10,)), ("DEB-Pokal, alles aus", 6, ()),
                              ("Play-off, Ergebnisse 1.Liga", 1, (1,)), ("Play-off, Drittelpause 1.Liga", 1, (0,)),
                              ("WM-Endrunde, WM 12", 10, (12,))):
        c, pages = make(options=opts, code=code)
        ms.cup_rows(c, (0, 1, 2, 3), pairs=1)
        c.call(0x3A88A, code, 0, 70)
        print(f"   {label}: {shown(pages)}")

    print("\n4. Darstellung 2 (DGROUP 0x5A55 = 1), Pokal, alle Optionen an:")
    for label, kw in cases[:6]:
        res = {m: period(m, options=range(13), rink=1, **kw) for m in (20, 60)}
        print(f"   {label}: {res}")


if __name__ == "__main__":
    main()
