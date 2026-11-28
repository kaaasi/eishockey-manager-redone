"""A667 (Spur hok-patch-tabellen, Teil 1): forced relegation and promotion-round exchange of em.exe replayed from its bytes.

Runs the season change of the day function 0x54C9F from Datei 0x55F00 (forced relegation loop per manager, Datei
0x55F17-0x560DB, insertion into the group table 0x55F93-0x560C5, clearing of the lists 0x560EB) through the exchange of
the three groups (Datei 0x56137-0x563A4) and stops at Datei 0x563AD (before the all-time table 0x5667A). The club swap
0x36169 runs from the bytes as well (flag 1: club records 0x3D, table records 0x50 at DGROUP 0x8257, pool +0x1D, manager
club DGROUP 0x966F); only the heap helpers 0x0C729/0x0C789 (alloc/free) and random 0x0C037 are hooked.

Prepared memory (no original data needed for the synthetic cases):
  DGROUP 0x386 managers; per manager m (stride 0x1AF): account 0x97EA (32 bit, thirds of DM), league 0x9746, status
  0x974E, club 0x966F; club record +0x16 = m + 1 (far 0x9657, 0x3D bytes, name field tagged with the slot number so the
  club can be followed through the swaps); place list DGROUP 0x5CCB (league * 12 + place, default slot = 12 * league +
  place); groups of competition 1 (promotion rounds): place list DGROUP 0xCD94 + 6 * g (participant per place) and the
  first pairing row DGROUP 0xCF20 + 0x24 * g (club slot per participant).
Constants from the image: 0x7B1 = 12 clubs per league, 0x7B9 = 2 pairs per group, 0x7C1 = 12/24 (first slot of the lower
league of group 0/1).

Modes:
  python3 -I zwangsabstieg.py [em.exe]            synthetic cases (A667 constellations, see CASES)
  python3 -I zwangsabstieg.py [em.exe] --hok F    replay with the group tables, place list and managers of save game F
                                                  (participants of the first row restored from DGROUP 0xCFFC)
        [--add slot:league:thirds ...]            with further managers appended (forecast for a template that gets
                                                  managers via the tile "Neuer"; --konto m:thirds overrides an account)
  python3 -I zwangsabstieg.py [em.exe] --check17  FILE17 check: FILE17 keeps the tables of the season change it went through
                                                  (FILE16A -> FILE17, DOSBox run of the user); the run on these tables with
                                                  the participants of 0xCFFC must reproduce the first row stored in FILE17
                                                  and the leagues the user saw (club of slot 23 to the Bundesliga, slot 9
                                                  to the Oberliga; README of the reference repo, FILE17).
Output: slot numbers, places and flags only; no names or other original data.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "abweichungen"))
sys.path.insert(0, str(HERE))
import emu16  # noqa: E402
import hokread  # noqa: E402

START, STOP = 0x55F00, 0x563AD
MGR = 0x1AF
CLUBS_OFF, PLAYERS_OFF, SCRATCH_OFF = 0x0100, 0x4000, 0xF000
LIMIT = -2_000_000  # thirds of DM, Datei 0x55F22-0x55F34


def league_of(slot: int) -> str:
    return "BL" if slot < 12 else "2L" if slot < 24 else "OL" if slot < 36 else "--"


class Run:
    """One replay. groups: [(participants[4] = slots, places[4] = participant index per place)] for group 0 and 1.
    managers: [(club slot, league 0/1/2, account in thirds, status byte)]."""

    def __init__(self, exe, groups, managers, places5ccb=None, randoms=(2, 4, 1, 3, 5, 0)):
        self.c = c = emu16.load(exe)
        self.groups, self.managers = groups, managers
        c.max_steps = 5_000_000
        c.sdb(0x386, len(managers))
        c.sdw(0x9657, CLUBS_OFF)
        c.sdw(0x9659, emu16.FAR_HEAP_SEG)
        c.sdw(0x8253, PLAYERS_OFF)
        c.sdw(0x8255, emu16.FAR_HEAP_SEG)
        for slot in range(64):
            tag = f"S{slot:02d}".encode()
            for i, ch in enumerate(tag):
                c.wb(emu16.FAR_HEAP_SEG, CLUBS_OFF + 0x3D * slot + i, ch)
        for p in range(36):
            c.sdb(0x5CCB + p, places5ccb[p] if places5ccb else p)
        for m, (club, league, account, status) in enumerate(managers):
            c.sdw(0x97EA + MGR * m, account & 0xFFFF)
            c.sdw(0x97EC + MGR * m, (account >> 16) & 0xFFFF)
            c.sdb(0x9746 + MGR * m, league)
            c.sdb(0x974E + MGR * m, status)
            c.sdb(0x966F + MGR * m, club)
            c.wb(emu16.FAR_HEAP_SEG, CLUBS_OFF + 0x3D * club + 0x16, m + 1)
        for g, (parts, places) in enumerate(groups):
            for i in range(4):
                c.sdb(0xCF20 + 0x24 * g + i, parts[i])
                c.sdb(0xCD94 + 6 * g + i, places[i])
        self.swaps, self.inserted = [], None
        seq = iter(list(randoms) * 50)

        def rnd(cc):
            lo, hi = cc.arg(0), cc.arg(1)
            return lo + next(seq) % (hi - lo + 1)

        def swap_log(cc):
            self.swaps.append((cc.arg(0) & 0xFF, cc.arg(1) & 0xFF, cc.arg(2) & 0xFF))
            if self.inserted is None:
                self.inserted = self.group_tables()
        c.hooks[0x0C037] = rnd
        c.hooks[0x0C729] = lambda cc: (SCRATCH_OFF, emu16.FAR_HEAP_SEG)
        c.hooks[0x0C789] = lambda cc: 0
        c.pre[0x36169] = swap_log  # log, then the swap itself runs from the bytes

    def group_tables(self):
        c = self.c
        return [[c.db(0xCF20 + 0x24 * g + c.db(0xCD94 + 6 * g + p)) for p in range(4)] for g in range(2)]

    def go(self):
        c = self.c
        cs, ip = c.at(START)
        c.push(0xFFFF)
        c.push(0xFFFF)
        c.push(0)
        c.r["bp"] = c.r["sp"]
        c.r["sp"] = (c.r["sp"] - 0x40) & 0xFFFF
        c.r["ax"] = 0  # Datei 0x55F00/0x55F03: counters [bp-0x17]/[bp-0x18] = al
        c.s["cs"], c.ip = cs, ip
        c.stop_at = {STOP}
        try:
            c.run()
        except emu16.Stop as e:
            if f"{STOP:#x}" not in str(e):
                raise
        if self.inserted is None:
            self.inserted = self.group_tables()
        return self

    def slot_of_club(self):
        """Original slot (tag) -> slot where that club stands after the swaps."""
        c, res = self.c, {}
        for slot in range(64):
            tag = bytes(c.rb(emu16.FAR_HEAP_SEG, CLUBS_OFF + 0x3D * slot + i) for i in range(3)).decode()
            res[int(tag[1:])] = slot
        return res

    def report(self, title, focus=()):
        c = self.c
        print(f"== {title}")
        for g, (parts, places) in enumerate(self.groups):
            before = [parts[places[p]] for p in range(4)]
            print(f"   Gruppe {g}: Plätze vorher {before} -> nach Einschub {self.inserted[g]}")
        print("   Tausche 0x36169(a, b, Schalter): " + ", ".join(f"({a},{b},{f})" for a, b, f in self.swaps))
        moved = self.slot_of_club()
        shown = sorted(set(focus) | {s for a, b, _ in self.swaps for s in (a, b) if s < 36})
        print("   Verein (Platz vorher) -> Platz nachher: " + ", ".join(
            f"{s}({league_of(s)})->{moved[s]}({league_of(moved[s])})" for s in shown if moved[s] != s or s in focus))
        for m in range(len(self.managers)):
            acc = c.dw(0x97EA + MGR * m) | (c.dw(0x97EC + MGR * m) << 16)
            acc = acc - (1 << 32) if acc & 0x80000000 else acc
            print(f"   Manager {m}: Status {c.db(0x974E + MGR * m):#04x}, Verein (Platz) {c.db(0x966F + MGR * m)}, "
                  f"Konto {acc} Drittel")
        return moved


# Group tables: participants = slots of the first pairing row, places = participant per place (identity: place = row order).
G0 = ([10, 11, 12, 13], [2, 3, 0, 1])        # places: 12, 13 (2L) on 1-2, 10, 11 (BL) on 3-4: normal exchange
G1 = ([22, 23, 24, 25], [2, 3, 0, 1])        # places: 24, 25 (OL) on 1-2, 22, 23 (2L) on 3-4
BROKE = LIMIT - 3
EMPTY = ([0, 0, 0, 0], [0, 0, 0, 0])         # mode 1: no promotion round is played, tables stay zero (FILE18)

CASES = [
    ("Fall 0: ohne Zwangsabstieg (Gruppen normal)", [G0, G1], [(5, 0, 0, 0)], ()),
    ("Fall 1: ein BL-Manager außerhalb der Gruppe (Platz 5) pleite", [G0, G1], [(5, 0, BROKE, 0)], (5, 10, 11)),
    ("Fall 2: BL-Manager auf Platz 4 der Gruppe, BL auf Platz 3 (Zwangsabsteiger doppelt: Platz 3 und 4)",
     [([10, 11, 12, 13], [2, 3, 0, 1]), G1], [(11, 0, BROKE, 0)], (10, 11, 12, 13)),
    ("Fall 3: wie Fall 2, dazu 2L-Manager auf Platz 2 der Gruppe 0 (Verein 13) pleite (A667)",
     [([10, 11, 12, 13], [2, 3, 0, 1]), G1], [(11, 0, BROKE, 0), (13, 1, BROKE, 0)], (10, 11, 12, 13, 22, 23)),
    ("Fall 4: wie Fall 3, aber 2L-Manager auf Platz 1 der Gruppe 0 (Verein 12)",
     [([10, 11, 12, 13], [2, 3, 0, 1]), G1], [(11, 0, BROKE, 0), (12, 1, BROKE, 0)], (10, 11, 12, 13, 22, 23)),
    ("Fall 5: zwei BL-Manager außerhalb der Gruppe pleite (doppelter Zwangsabstieg BL)",
     [G0, G1], [(4, 0, BROKE, 0), (5, 0, BROKE, 0)], (4, 5, 10, 11)),
    ("Fall 6: zwei BL-Manager pleite, einer auf Platz 4 der Gruppe (Verein 11), einer außerhalb (Verein 5)",
     [G0, G1], [(11, 0, BROKE, 0), (5, 0, BROKE, 0)], (5, 10, 11, 12, 13)),
    ("Fall 7: zwei BL-Manager pleite, einer auf Platz 1 der Gruppe (Verein 10 vor 12, 13), einer außerhalb",
     [([10, 11, 12, 13], [0, 2, 3, 1]), G1], [(10, 0, BROKE, 0), (5, 0, BROKE, 0)], (5, 10, 11, 12, 13)),
    ("Fall 8: wie Fall 3, Konto genau auf der Grenze (−2.000.000 Drittel, kein Zwangsabstieg)",
     [([10, 11, 12, 13], [2, 3, 0, 1]), G1], [(11, 0, LIMIT, 0), (13, 1, LIMIT, 0)], (10, 11, 12, 13)),
    ("Fall 9: wie Fall 3, BL-Manager Deutscher Meister (Statusbit 0x08)",
     [([10, 11, 12, 13], [2, 3, 0, 1]), G1], [(11, 0, BROKE, 0x08), (13, 1, BROKE, 0)], (10, 11, 12, 13, 22, 23)),
    ("Fall 10 (A671): Gruppen leer wie im Modus „nur Weltkugel“ (FILE18), Managerverein Oberliga-Letzter (Platz 35)",
     [EMPTY, EMPTY], [(35, 2, 1_500_000, 0)], (35,)),
    ("Fall 11 (A671): wie Fall 10, Managerverein Oberliga-Zehnter (Platz 33)",
     [EMPTY, EMPTY], [(33, 2, 1_500_000, 0)], (33,)),
]


def from_hok(path):
    s = hokread.Save(path)
    cffc = s.block("CFFC")
    groups = []
    for g in range(2):
        parts = list(cffc[4 * g:4 * g + 4])
        places = list(s.dgroup(0xCD94 + 6 * g, 4))
        groups.append((parts, places))
    managers = []
    for m in range(s.b(0x386)):
        acc = int.from_bytes(s.manager(m, 0x97EA, 4), "little", signed=True)
        managers.append((s.manager(m, 0x966F)[0], s.manager(m, 0x9746)[0], acc, s.manager(m, 0x974E)[0]))
    return s, groups, managers, list(s.block("5CCB"))


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    exe = args[0] if args and not args[0].upper().endswith(".HOK") else None
    if "--hok" in sys.argv:
        path = sys.argv[sys.argv.index("--hok") + 1]
        s, groups, managers, places = from_hok(path)
        argv = sys.argv
        for i, a in enumerate(argv):
            if a == "--add":
                slot, league, thirds = (int(x) for x in argv[i + 1].split(":"))
                managers.append((slot, league, thirds, 0))
            if a == "--konto":
                m, thirds = (int(x) for x in argv[i + 1].split(":"))
                managers[m] = (managers[m][0], managers[m][1], thirds, managers[m][3])
        print(f"Prüfsummen gültig: {s.checksums_ok}; Manager {len(managers)}: "
              + ", ".join(f"Verein {c} Liga {lg} Konto {a} Drittel ({'unter' if a < LIMIT else 'nicht unter'} der Grenze)"
                          for c, lg, a, _ in managers))
        Run(exe, groups, managers, places).go().report(f"Spielstand {Path(path).name}", [m[0] for m in managers])
        return
    if "--check17" in sys.argv:
        path = Path.home() / "referenz/original/spielstaende/FILE17.HOK"
        s, groups, _, _ = from_hok(path)
        stored = [list(s.dgroup(0xCF20 + 0x24 * g, 4)) for g in range(2)]
        # manager of FILE16A/FILE22: slot 23, 2. Liga; account on 29.04.1994 below the limit (README: -1.234.094 DM on 14.04.)
        r = Run(exe, groups, [(23, 1, -1_234_094 * 3, 0)]).go()
        moved = r.report("FILE17: Tabellen des durchlaufenen Saisonwechsels, Teilnehmer aus 0xCFFC", (8, 9, 13, 23))
        rows = [[r.c.db(0xCF20 + 0x24 * g + i) for i in range(4)] for g in range(2)]
        print(f"   erste Paarungszeile nach dem Lauf {rows}, in FILE17 gespeichert {stored}: "
              f"{'gleich' if rows == stored else 'VERSCHIEDEN'}")
        print(f"   Verein von Platz 23 -> {league_of(moved[23])}, Verein von Platz 9 -> {league_of(moved[9])} "
              f"(Nutzer: Bundesliga bzw. Oberliga)")
        return
    for title, groups, managers, focus in CASES:
        Run(exe, groups, managers).go().report(title, focus)


if __name__ == "__main__":
    main()
