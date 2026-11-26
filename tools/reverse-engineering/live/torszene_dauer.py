"""S02 / A472 (Spur torszene-dauer, Teil 1): replay of display mode 2 "Dauer-Eisfeld" from the bytes of em.exe.

Evidence for docs/analyse/torszene-dauer.md. The interpreter emu16 (tools/reverse-engineering/abweichungen) runs the
original code; everything outside the code under test is stubbed and logged. Flags: DGROUP 0x5A56 = live screen for
this match day, 0x5A55 = Dauer-Eisfeld active, 0x226 = display mode 0/1/2 (option 9), 0x386 = number of managers.

Parts (each prints the logged calls in order):
  A  match day 0x36B5D, section Datei 0x3710B-0x372FE (gate, setup, three periods, overtime, teardown), entered with
     the original stack frame; 0x37A62 and 0x32AA3 are hooked (arguments logged, 0x32AA3 sets 0x5A56 like Datei 0x32AAD
     / 0x32DD8 for a watched tie).
  B  minute loop 0x37A62 for one period without pairings: what runs per minute with live screen, with Dauer-Eisfeld,
     and with neither (pauses 50h:19Eh, minute line 0x570E8, clock 0x51E90, live screen 0x5353C, click 38h:118h).
  B2 the same at minute 60 with league 0 in the mask: league page 7 of the page selector 0x3A88A and the click.
  C  scene player 0x567C8 modes 1 (set up), 2 (scene only) and 3 (tear down), including the hall level rule
     Datei 0x568A9-0x568B6 and the memory check Datei 0x56816-0x56871; C2 a found scene in mode 2 against mode 0.
  D  penalty shoot-out 0x3901B with a watched club: teardown of the overtime ice, own setup for display 1 and 2,
     shots 0x53C57, final teardown.
  H  (A1027) the same shoot-out with display 0 and with a failed setup, the goal scene 0x53C57 run as well: live screen
     0x5353C, random draws, pauses, score 0x51FEA per goal with the shoot-out tally, other called targets.
  E  goal scene 0x53C57 with 0x5A56 = 0 / 0x5A55 = 1: which parts run (counters, fit-player check 120h:66h,
     reporter 0x52520, intervention 0x5434C or automatic line-up 108h:4Dh, scoreboard 0x52BD6).
  G  page selector 0x3A88A per page with and without Dauer-Eisfeld.
  F  display helpers 0x52BD6, 0x52520, 0x51FEA, 0x522EC per flag state (number of distinct executed instructions).

Usage: python3 tools/reverse-engineering/live/torszene_dauer.py [em.exe]
Output contains addresses and numbers only (no original data).
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None

NAMES = {
    0x567C8: "Abspieler 0x567C8", 0x37A62: "Minutenschleife 0x37A62", 0x32AA3: "K.-o.-Prüfung 0x32AA3",
    0x45D46: "Hintergrund/Ausblenden 110h:7Ah", 0x0C5CE: "Palette 38h:DDEh", 0x5353C: "Live-Bildschirm 0x5353C",
    0x3A88A: "Seitenwähler 0x3A88A", 0x570E8: "Minutenzeile 0x570E8", 0x51E90: "Spieluhr 0x51E90",
    0x130EE: "Pause 50h:19Eh", 0x0B908: "Mausklick 38h:118h", 0x522EC: "Zwinkern 0x522EC", 0x52BD6: "Anzeigetafel 0x52BD6",
    0x10AA5: "Strahlrücklauf 48h:30B5h", 0x2CD1C: "Optionen 0x2CD1C", 0x56EDD: "Kopfzeile 0x56EDD",
    0x0BF64: "Bild laden 38h:774h", 0x0C789: "Bild freigeben 38h:F99h", 0x575C3: "Szenenwahl 0x575C3",
    0x4BCCC: "Meldung 120h:4Dh", 0x53C57: "Torszene 0x53C57", 0x0B6A5: "Text 30h:885h", 0x4BE17: "fitte Spieler 120h:66h",
    0x52520: "Reporter 0x52520", 0x5434C: "Eingriff 0x5434C", 0x426BC: "auto. Aufstellung 108h:4Dh",
    0x05B1A: "Verletzung 18h:199Ah", 0x51FEA: "Spielstand 0x51FEA", 0x0B8FE: "Farbe 38h:10Eh",
}
SHOW = set(NAMES)


def cpu(allowed):
    c = emu16.load(PATH)
    c.allowed = allowed
    c.int_hook = lambda cpu_, n: (cpu_.r.__setitem__("ax", 0), True)[1]
    c.sdw(0x9657, 0)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)  # team table at far heap, all fields 0
    c.max_steps = 20_000_000
    return c


def logger(c, log, extra=None):
    """Hook every named function outside the allowed ranges; log name and the first word arguments."""
    for addr in SHOW:
        if any(lo <= addr < hi for lo, hi in c.allowed):
            continue

        def make(a):
            def h(cc):
                args = [cc.arg(i) & 0xFFFF for i in range(16)]
                log.append((a, args))
                if extra and a in extra:
                    return extra[a](cc)
                return None
            return h
        c.hooks[addr] = make(addr)


def fmt(log, skip=()):
    out, last, n = [], None, 0
    for a, args in log:
        if a in skip:
            continue
        key = NAMES.get(a, hex(a))
        if key == last:
            n += 1
            continue
        if last is not None:
            out.append(last + (f" ×{n}" if n > 1 else ""))
        last, n = key, 1
    if last is not None:
        out.append(last + (f" ×{n}" if n > 1 else ""))
    return out


# ---------------------------------------------------------------- part A
def part_a():
    print("A  Spieltag 0x36B5D, Ausschnitt Datei 0x3710B–0x372FE")
    cases = [
        ("Live-Anzeige, Darstellung 2, kein K.-o.", 1, 2, 0, None, 1),
        ("Live-Anzeige, Darstellung 1, kein K.-o.", 1, 1, 0, None, 1),
        ("Live-Anzeige, Darstellung 2, Aufbau scheitert (Rückgabe 0)", 1, 2, 0, None, 0),
        ("ohne Live-Anzeige, Darstellung 2", 0, 2, 0, None, 1),
        ("Live-Anzeige, Darstellung 2, K.-o., unentschieden nach 60 und 70", 1, 2, 1, (1, 1), 1),
        ("Live-Anzeige, Darstellung 1, K.-o., unentschieden nach 60 und 70", 1, 1, 1, (1, 1), 1),
        ("Live-Anzeige, Darstellung 1, im 3. Drittel per Optionen auf 2, K.-o., unentschieden", 1, 1, 1, (1, 1), 1),
    ]
    for label, live, mode, ko, ties, setup_ok in cases:
        c = cpu([(0x36B5D, 0x378C5)])
        log = []
        c.sdb(0x5A56, live)
        c.sdb(0x5A55, 0)
        c.sdb(0x226, mode)
        c.sdb(0xE2AA, 52)
        c.sdb(0xE2A9, 53)
        c.sdb(0xE2A8, 1)
        ko_calls = iter(ties or ())

        def ko_hook(cc):
            r = next(ko_calls, 0)
            cc.sdb(0x5A56, 1 if r and live else 0)  # Datei 0x32AAD / 0x32DD8: watched tie
            return r

        def player(cc):
            return setup_ok if cc.arg(13) & 0xFF == 1 else 0

        def minute(cc):
            if "auf 2" in label and cc.arg(4) & 0xFF == 60:
                cc.sdb(0x226, 2)  # options 0x2CD1C during the period (Datei 0x37D3C), saved in 0xE19C
            log[-1] = (0x37A62, [cc.arg(3) & 0xFF, cc.arg(4) & 0xFF, cc.arg(5) & 0xFF, cc.db(0x5A55)])
            return None

        logger(c, log, {0x567C8: player, 0x32AA3: ko_hook, 0x37A62: minute})
        # stack frame of 0x36B5D: args (league mask, cup mask, K.-o., 0xC, row) at bp+6 … bp+0xE
        cs, ip = c.at(0x3710B)
        for a in reversed([1, 1, ko, 0, 0]):
            c.push(a)
        c.push(0xFFFF)
        c.push(0xFFFF)
        c.push(0)
        c.r["bp"] = c.r["sp"]
        c.r["sp"] -= 0x20
        c.s["cs"], c.ip = cs, ip
        c.stop_at = {0x372FE}
        try:
            c.run()
        except emu16.Stop:
            pass
        print(f"  {label}:")
        for a, args in log:
            if a == 0x567C8:
                print(f"     {NAMES[a]}: Modus {args[13] & 0xFF}, Heim/Gast {args[0] & 0xFF}/{args[1] & 0xFF}, Minute {args[10] & 0xFF}, Hallenstufe {args[14] & 0xFF}")
            elif a == 0x37A62:
                print(f"     {NAMES[a]}: Minuten {args[0]}–{args[1]}, Anzeige-Argument {args[2]}, [0x5A55] = {args[3]}")
            elif a == 0x3A88A:
                print(f"     {NAMES[a]}: Seite {args[0] & 0xFF}, Minute {args[2]}")
            elif a in (0x32AA3, 0x45D46, 0x0C5CE):
                print(f"     {NAMES[a]}")
        print(f"     Ende: [0x5A55] = {c.db(0x5A55)}, [0x5A56] = {c.db(0x5A56)}, [0x541C] = {c.db(0x541C)}")


# ---------------------------------------------------------------- part B
def part_b():
    print("B  Minutenschleife 0x37A62(0, 0, 0, 41, 60, Anzeige, 0) ohne Paarungen, Geschwindigkeit 0")
    for label, disp, dauer in (("Live-Anzeige ([bp+0x10] = 1, [0x5A55] = 0)", 1, 0),
                               ("Dauer-Eisfeld ([bp+0x10] = 0, [0x5A55] = 1)", 0, 1),
                               ("ohne beides ([bp+0x10] = 0, [0x5A55] = 0)", 0, 0)):
        c = cpu([(0x37A62, 0x386D1), (0x369C7, 0x36B5D)])
        log = []
        c.sdb(0x5A55, dauer)
        c.sdb(0x5A56, disp)
        c.sdb(0x226, 2 if dauer else 1)
        c.sdw(0x262, 0)
        c.sdb(0x1DB, 0)
        logger(c, log)
        c.hooks[0x0BFD5] = lambda cc: (1, 0)  # lrandom(0, 4) != 0: no wink
        c.hooks[0x1080E] = lambda cc: 0       # no key
        c.sdw(0xC147, 0)
        c.sdw(0xC149, emu16.FAR_HEAP_SEG)
        c.ww(emu16.FAR_HEAP_SEG, 0, 1)        # window object 0xC147 +0 != 0: no mouse
        c.call(0x37A62, 0, 0, 0, 41, 60, disp, 0)
        per = {}
        for a, args in log:
            per[a] = per.get(a, 0) + 1
        keys = [0x5353C, 0x51E90, 0x130EE, 0x10AA5, 0x570E8, 0x52BD6, 0x0B908, 0x45D46, 0x0C5CE, 0x3A88A]
        print(f"  {label}: " + ", ".join(f"{NAMES[k]} ×{per.get(k, 0)}" for k in keys))


def part_b2():
    print("B2 Minutenschleife 0x37A62(Liga 1, 0, 0, 60, 60, Anzeige, 0), Liga 0 ohne Paarungen: Seiten nach dem Drittel")
    for label, disp, dauer in (("Live-Anzeige", 1, 0), ("Dauer-Eisfeld", 0, 1)):
        c = cpu([(0x37A62, 0x386D1), (0x369C7, 0x36B5D)])
        log = []
        c.sdb(0x5A55, dauer)
        c.sdb(0x5A56, disp)
        c.sdb(0x226, 2)
        c.sdw(0x262, 0)
        logger(c, log)
        c.hooks[0x0BFD5] = lambda cc: (1, 0)
        c.hooks[0x1080E] = lambda cc: 0
        c.sdw(0xC147, 0)
        c.sdw(0xC149, emu16.FAR_HEAP_SEG)
        c.ww(emu16.FAR_HEAP_SEG, 0, 1)
        for i in range(0x5CFB, 0x5CFB + 0x80):
            c.sdb(i, 0xFF)
        c.call(0x37A62, 1, 0, 0, 60, 60, disp, 0)
        tables = sorted({t for _, t in c.calls if t in (0x50B45, 0x51049, 0x052CF)})
        shown = [f"{NAMES[a]} Seite {g[0] & 0xFF} Minute {g[2]}" if a == 0x3A88A else NAMES[a]
                 for a, g in log if a in (0x0B908, 0x45D46, 0x3A88A)]
        print(f"  {label}: {', '.join(shown) or 'keine Seite, kein Klick'}; Tabellen gerufen: "
              + ", ".join(hex(t) for t in tables))


# ---------------------------------------------------------------- part C
def part_c():
    print("C  Abspieler 0x567C8(Heim 52, Gast 53, …, Minute 33, …, Modus, Hallenstufe, Text)")

    def run(mode, managers, hall, mem=(0, 8), label=""):
        c = cpu([(0x567C8, 0x56EDD)])
        log = []
        c.sdb(0x386, managers)
        c.sdb(0x226, 2)
        c.sdb(0xE19C, 2)
        c.sdb(0x49A9, 0)
        logger(c, log, {0x575C3: lambda cc: 0xFFFF})
        c.hooks[0x13024] = lambda cc: mem                        # free memory 50h:D4h (dx:ax)
        c.hooks[0x0C729] = lambda cc: (0x10, 0x1000)             # buffer 38h:F39h
        c.hooks[0x0BF64] = lambda cc: (log.append((0x0BF64, [cc.arg(0) & 0xFF] * 16)), (0x10, 0x1000))[1]
        r = c.call(0x567C8, 52, 53, 1, 2, 0, 0, 0, 0, 0, 0, 33, 0, 0, mode, hall, 0, 0) & 0xFFFF
        imgs = [g[0] for a, g in log if a == 0x0BF64]
        other = fmt([(a, g) for a, g in log if a != 0x0BF64])
        free = sum(1 for a, _ in log if a == 0x0C789)
        hdr = [g for a, g in log if a == 0x56EDD]
        print(f"  {label}Modus {mode}, [0x386] = {managers}, Stufe-Argument {hall}, freier Speicher {mem[1] * 65536 + mem[0]:#x}:"
              f" Rückgabe {r}, [0x226] = {c.db(0x226)}, [0xE19C] = {c.db(0xE19C)}, [0x49A9] = {c.db(0x49A9)}")
        if imgs:
            print(f"     geladen kb\\{imgs[0]} … : {imgs}")
        if hdr:
            print(f"     Kopfzeile 0x56EDD(Modus {hdr[0][0] & 0xFF}, …, Tore {hdr[0][5] & 0xFF}:{hdr[0][6] & 0xFF},"
                  f" Minute {hdr[0][7] & 0xFF})")
        print(f"     Aufrufe: {', '.join(o for o in other if 'freigeben' not in o)}"
              + (f"; Bilder freigegeben ×{free}" if free else ""))

    run(1, 1, 1)
    run(1, 2, 1)
    run(1, 1, 0)
    run(1, 1, 1, mem=(0x1000, 4), label="zu wenig Speicher: ")
    run(2, 1, 1, label="Szenenwahl −1 (50 Fehlversuche): ")
    run(3, 1, 1)

    print("C2 Abspieler mit gefundener Szene (Bildfolge sofort 0xAAAA), Tor der Heimseite, Tore vorher 1:2")
    for mode in (2, 0):
        c = cpu([(0x567C8, 0x56EDD)])
        log = []
        c.sdb(0x386, 1)
        c.sdb(0x226, 2)
        logger(c, log, {0x575C3: lambda cc: 0})
        c.hooks[0x13024] = lambda cc: (0, 8)
        c.hooks[0x0C729] = lambda cc: (0x10, 0x1000)
        c.hooks[0x0BF64] = lambda cc: (log.append((0x0BF64, [cc.arg(0) & 0xFF] * 16)), (0x10, 0x1000))[1]
        c.sdw(0xE2E5, 0x2000)
        c.sdw(0xE2E7, emu16.FAR_HEAP_SEG)
        c.ww(emu16.FAR_HEAP_SEG, 0x2000 + 0x54, 0xAAAA)    # end of the frame list
        r = c.call(0x567C8, 52, 53, 1, 2, 0, 0, 0, 0, 0, 1, 33, 0, 0, mode, 1, 0, 0) & 0xFFFF
        hdr = [f"0x56EDD(Modus {g[0] & 0xFF}, Tore {g[5] & 0xFF}:{g[6] & 0xFF}, Minute {g[7] & 0xFF})"
               for a, g in log if a == 0x56EDD]
        loads = sum(1 for a, _ in log if a == 0x0BF64)
        free = sum(1 for a, _ in log if a == 0x0C789)
        pauses = [g[0] for a, g in log if a == 0x130EE]
        live = sum(1 for a, _ in log if a == 0x5353C)
        print(f"  Modus {mode}: Rückgabe {r}; Kopfzeilen {', '.join(hdr)}; Bilder geladen ×{loads}, freigegeben ×{free},"
              f" Pausen {pauses}, Live-Bildschirm ×{live}")


# ---------------------------------------------------------------- part D
def part_d():
    print("D  Penaltyschießen 0x3901B(Zeile 0, Paarung 0, 1, 0), Heim 52 zugesehen (Vereinsfeld 0x34 Bit 7), Heim trifft immer")
    for label, mode, ice in (("Darstellung 2, Eisfeld der Verlängerung an ([0x5A55] = 1)", 2, 1),
                             ("Darstellung 1, [0x5A55] = 0", 1, 0),
                             ("Darstellung 0, [0x5A55] = 0", 0, 0),
                             ("Darstellung 2, Aufbau scheitert", 2, 0)):
        c = cpu([(0x3901B, 0x397BB)])
        log = []
        c.sdb(0x226, mode)
        c.sdb(0x5A55, ice)
        c.sdb(0x5A56, 1)
        c.sdb(0x1DB, 0)
        c.wb(emu16.FAR_HEAP_SEG, 52 * 0x3D + 0x34, 0x80)
        c.sdb(0x5CFB, 52)
        c.sdb(0x5CFC, 53)
        c.sdb(0x5C4B, 33)
        c.sdb(0x5C4C, 3)

        def rnd(cc):
            a, b = cc.arg(0) & 0xFFFF, cc.arg(1) & 0xFFFF
            if (a, b) == (0, 10):
                return 10 if c.mem[0] == 0 else 0
            return 0

        def scene(cc):
            c.mem[0] ^= 1                                       # alternate: home shot (goal), away shot (miss)
            return 0
        fail = "scheitert" in label
        logger(c, log, {0x567C8: lambda cc: 0 if fail else 1, 0x53C57: scene})
        c.hooks[0x0C037] = rnd
        c.mem[0] = 0
        c.call(0x3901B, 0, 0, 1, 0)
        out = []
        for a, g in log:
            if a == 0x567C8:
                out.append(f"Abspieler Modus {g[13] & 0xFF} (Heim/Gast {g[0] & 0xFF}/{g[1] & 0xFF}, Minute {g[10] & 0xFF},"
                           f" Stufe {g[14] & 0xFF})")
            elif a in (0x53C57, 0x5353C, 0x51FEA, 0x45D46, 0x0B908):
                out.append(NAMES[a])
        print(f"  {label}:")
        last, n = None, 0
        for o in out + [None]:
            if o == last:
                n += 1
                continue
            if last is not None:
                print(f"     {last}" + (f" ×{n}" if n > 1 else ""))
            last, n = o, 1
        print(f"     Ende: [0x5A55] = {c.db(0x5A55)}, [0x5A56] = {c.db(0x5A56)}, Stand Heim/Gast {c.db(0x5C4B)}/{c.db(0x5C4C)}")


# ---------------------------------------------------------------- part H
def part_h():
    """A1027 (fix/penalty-darstellung-0): shots of a watched shoot-out with display 0, including the inside of 0x53C57."""
    print("H  Penaltyschießen 0x3901B(Zeile 0, Paarung 0, 1, 0) mit Darstellung 0, Torszene 0x53C57 mitgerechnet;"
          " Heim 52 zugesehen, Zug random(0, 10) abwechselnd 10/0 (Heim trifft, Gast nicht)")
    for label, mode, ice, fail in (("Darstellung 0", 0, 0, False), ("Darstellung 2, Aufbau scheitert", 2, 0, True)):
        c = cpu([(0x3901B, 0x397BB), (0x53C57, 0x541D9)])
        log = []
        c.sdb(0x226, mode)
        c.sdb(0x5A55, ice)
        c.sdb(0x5A56, 1)
        c.sdb(0x1DB, 0)
        c.sdb(0x386, 1)
        c.wb(emu16.FAR_HEAP_SEG, 52 * 0x3D + 0x34, 0x80)
        c.sdb(0x5CFB, 52)
        c.sdb(0x5CFC, 53)
        c.sdb(0x5C4B, 33)
        c.sdb(0x5C4C, 3)
        state = {"shot": 0}
        events = []

        def rnd(cc):
            a, b = cc.arg(0) & 0xFFFF, cc.arg(1) & 0xFFFF
            v = 0
            if (a, b) == (0, 10):
                v = 10 if state["shot"] % 2 == 0 else 0
                state["shot"] += 1
            events.append(f"random({a}, {b}) = {v}")
            return v

        def pause(cc):
            events.append(f"Pause {cc.arg(0) & 0xFFFF}")
            return 0

        def score(cc):
            events.append(f"Spielstand 0x51FEA(Reihe {cc.arg(0) & 0xFF}, {cc.arg(1) & 0xFF}): Stand {c.db(0x5C4B)}:{c.db(0x5C4C)}")
            return 0

        def live(cc):
            events.append(f"Live-Bildschirm 0x5353C({cc.arg(0) & 0xFF}, {cc.arg(1) & 0xFF}): Stand {c.db(0x5C4B)}:{c.db(0x5C4C)}")
            return 0

        def scene(cc):
            events.append(f"Abspieler 0x567C8 Modus {cc.arg(13) & 0xFF}")
            return 0 if fail else 1

        logger(c, log)
        c.hooks[0x0C037] = rnd
        c.hooks[0x130EE] = pause
        c.hooks[0x51FEA] = score
        c.hooks[0x5353C] = live
        c.hooks[0x567C8] = scene
        c.hooks[0x0B908] = lambda cc: (events.append("Mausklick 38h:118h"), 0)[1]
        c.hooks[0x45D46] = lambda cc: (events.append("Hintergrund 110h:7Ah"), 0)[1]
        c.hooks[0x4BE17] = lambda cc: (events.append("fitte Spieler 120h:66h"), 1)[1]
        c.call(0x3901B, 0, 0, 1, 0)
        others = sorted({t for _, t in c.calls if t not in (0x0C037, 0x130EE, 0x51FEA, 0x5353C, 0x567C8, 0x0B908,
                                                             0x45D46, 0x4BE17)})
        print(f"  {label}:")
        last, n = None, 0
        for o in events + [None]:
            if o == last:
                n += 1
                continue
            if last is not None:
                print(f"     {last}" + (f" ×{n}" if n > 1 else ""))
            last, n = o, 1
        print(f"     sonstige gerufene Ziele: {', '.join(NAMES.get(t, hex(t)) for t in others) or 'keine'}")
        print(f"     Ende: [0x5A55] = {c.db(0x5A55)}, [0x5A56] = {c.db(0x5A56)}, Stand Heim/Gast {c.db(0x5C4B)}/{c.db(0x5C4C)}")


# ---------------------------------------------------------------- part E
def part_e():
    print("E  Torszene 0x53C57 (Art, Liga, Paarung, Verein 52, Gegner 53, Seite 0, Minute 33, Manager 0, 0, 0)")
    for label, live, dauer in (("Live-Anzeige ([0x5A56] = 1)", 1, 0), ("Dauer-Eisfeld ([0x5A56] = 0, [0x5A55] = 1)", 0, 1)):
        for art in (1, 4):
            c = cpu([(0x53C57, 0x541D9)])
            log = []
            c.sdb(0x5A56, live)
            c.sdb(0x5A55, dauer)
            c.sdb(0x226, 2)
            c.sdb(0x1DB, 0)
            c.sdb(0x386, 1)
            c.sdb(0x966F, 52)                                   # manager 0 coaches club 52 (DGROUP 0x966F + 0x1AF · m)
            c.sdb(0xA88D, 52)                                   # row 0 of the live screen
            logger(c, log, {0x4BE17: lambda cc: 1, 0x567C8: lambda cc: 0, 0x1080E: lambda cc: 0})  # >= 4 fit players
            c.hooks[0x1080E] = lambda cc: 0
            c.call(0x53C57, art, 0, 0, 52, 53, 0, 33, 0, 0, 0)
            cnt = c.db(0xA839) + c.db(0xA83F)
            seq = [o for o in fmt(log) if any(k in o for k in ("0x567C8", "120h:66h", "Reporter", "Eingriff",
                                                                    "Aufstellung", "Anzeigetafel", "Verletzung"))]
            mode = [g[13] & 0xFF for a, g in log if a == 0x567C8]
            print(f"  {label}, Art {art}: {', '.join(seq)}" + (f" (Modus {mode[0]})" if mode else "")
                  + f"; Drittelzähler Reihe 0: {cnt}")


# ---------------------------------------------------------------- part F
def part_f():
    print("F  Anzeigehilfen: verschiedene ausgeführte Befehle je Schalterstand [0x5A56]/[0x5A55] (klein = sofortige Rückkehr)")
    for addr, args, name in ((0x52BD6, (0xFF, 0), "Anzeigetafel 0x52BD6(0xFF, 0)"),
                             (0x52520, (52, 1, 0, 0, 0), "Reporter 0x52520(52, 1, 0, 0, 0)"),
                             (0x51FEA, (0, 0), "Spielstand 0x51FEA(0, 0)"),
                             (0x522EC, (), "Zwinkern 0x522EC()")):
        row = []
        for live, dauer in ((1, 0), (0, 1), (0, 0)):
            c = cpu([(addr, addr + 1)])
            c.allowed = [(addr, addr + 0x700)]
            c.sdb(0x5A56, live)
            c.sdb(0x5A55, dauer)
            c.sdb(0xA762, 1)
            c.sdb(0xA75C, 53)
            c.sdw(0x262, 0)
            c.record = True
            c.call(addr, *args)
            row.append(f"{live}/{dauer}: {len(c.visited)}")
        print(f"  {name}: " + ", ".join(row))


# ---------------------------------------------------------------- part G
def part_g():
    print("G  Seitenwähler 0x3A88A(Seite, Wettbewerb 0, Minute 60) mit allen Spielstand-Optionen an (DGROUP 0x21D–0x229)")
    names = {0x32E23: "Übersicht 0x32E23", 0x3183C: "Zwischenstand 0x3183C", 0x30852: "Gruppentabellen 0x30852",
             0x2FC2F: "WM-Paarungen 0x2FC2F", 0x45D46: "Ausblenden 110h:7Ah"}
    for page in (1, 2, 3, 7, 8):
        row = []
        for dauer in (0, 1):
            c = cpu([(0x3A88A, 0x3ACE3)])
            for o in range(0x21D, 0x22A):
                c.sdb(o, 1)
            c.sdb(0x5A55, dauer)
            for i in range(0x5D3B, 0x5D3B + 0x40):
                c.sdb(i, 0xFF)
            c.call(0x3A88A, page, 0, 60)
            hit = [names.get(t, hex(t)) for _, t in c.calls]
            row.append(f"[0x5A55] = {dauer}: {', '.join(hit) or '–'}")
        print(f"  Seite {page}: " + "; ".join(row))


# ---------------------------------------------------------------- main
def main():
    part_a()
    part_b()
    part_b2()
    part_c()
    part_d()
    part_h()
    part_e()
    part_f()
    part_g()


if __name__ == "__main__":
    main()
