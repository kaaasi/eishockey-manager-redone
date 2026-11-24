"""B15/B16 (A286–A290, A495, A753): team info window of em.exe (0x128:0x2A = Datei 0x4D385) – face, goals per game,
record table and the competition code DGROUP 0xD0C2.

1. Face beside the flag (A287): 0x4D385 calls 0x51AAF(image kb\\33, record DGROUP 0x8257 + 0x50 · team, x 0x76, y 0x59,
   x 0xF9, y 0x59) (Datei 0x4D59E–0x4D5C6). The script runs 0x51AAF for result strings at record +7…+14 and records the
   blits 0x139C9 (source x/y of the 6 × 5 sprite, target x/y). Note: "0x110:0x5C" (Datei 0x4D588) is the rank curve
   0x445E0, which draws nothing for national teams (see b18), not the face.
2. "Tore / Spiel" (A288, A290): 0xE0:0x70 = Datei 0x2ECA0(buffer, team, page, 2), compared with the remake's
   `TeamInfoWindow.Average` (goals · 10 / games, truncated, "a.b"). Record fields found by the replay: games +0x21/+0x22,
   goals +0x19/+0x1A, goals against +0x1D/+0x1E (home/away), page 2 = sum.
3. Which competition codes DGROUP 0xD0C2 write the record table (A495, A753): result entry Datei 0x50283(home 52, away 53,
   3, 1) for codes 0…13; changed bytes of both records are listed.
4. Line "Historische Ergebnisse" and third button (A286): the window 0x4D385 with code 0 and 8 (texts at y ≥ 167, button
   calls 0x4C196 at x 0xF3).
Usage: python3 b15_gegnerfenster.py [em.exe]
"""
import random
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
WIN, DRAW, LOSS = ord("s"), ord("u"), ord("n")
SPRITE = {(19, 36): "rot (Niederlage)", (25, 41): "gelb (Unentschieden)", (25, 36): "grün (Sieg)"}


def cpu(extra=()):
    c = emu16.load(PATH)
    c.allowed = [(emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB954, 0xBAC8), *extra]
    c.int_hook = lambda cpu_, n: (cpu_.r.__setitem__("ax", 0), True)[1]
    return c


def part1():
    print("1. Gesicht 0x51AAF (Zeichen +7…+14, das jüngste zuletzt)")
    ov = next(r for r in emu16.load(PATH).regions if r.stub == 0x128)
    for seq in ("--------", "s-------", "ss------", "sss-----", "n-------", "nn------", "uuuuuuuu", "snsnsnsn",
                "ssssnnnn", "nnnnnsss", "nnnnnnnn", "ssssssss"):
        c = cpu([(0x51AAF, 0x51BEE), (0x4BD56, 0x4BD7E)])
        rec = 0x8257 + 0x50 * 52
        for i, ch in enumerate(seq):
            c.sdb(rec + 7 + i, ord(ch))
        blits = []

        def blit(cc):
            blits.append(((cc.arg(2), cc.arg(3)), (cc.arg(6), cc.arg(7))))
            return 0
        c.hooks[0x139C9] = blit
        c.sdw(0xF100, 0)
        c.sdw(0xF102, 0x1234)            # dummy image pointer
        c.call(0x51AAF, 0xF100, emu16.DGROUP, rec, emu16.DGROUP, 0x76, 0x59, 0xF9, 0x59)
        face = [SPRITE.get(s, s) for s, d in blits if d == (0xF9, 0x59)]
        print(f"   {seq}: {len(blits) - 1} Zeichen, Gesicht bei (249 | 89): {face[0] if face else '-'}")
    _ = ov


def part2():
    print("2. Tore / Spiel 0x2ECA0 (Seite 2 = Gesamt) gegen TeamInfoWindow.Average")
    c = cpu([(0x2ECA0, 0x2EF1B)])
    rec = 0x8257 + 0x50 * 52
    rnd = random.Random(15)
    bad = 0
    cases = [(0, 0, 0, 0, 0, 0), (1, 0, 2, 0, 3, 0), (2, 0, 0, 0, 1, 0), (5, 4, 3, 3, 3, 3), (60, 60, 2, 1, 4, 3)]
    cases += [tuple(rnd.randrange(0, 40) for _ in range(6)) for _ in range(200)]
    for n, (gh, ga, fh, fa, ah, aa) in enumerate(cases):
        for k, v in ((0x21, gh), (0x22, ga), (0x19, fh), (0x1A, fa), (0x1D, ah), (0x1E, aa)):
            c.sdb(rec + k, v)
        c.call(0x2ECA0, 0xF000, emu16.DGROUP, 52, 2, 2)
        orig = c.cstr(emu16.DGROUP, 0xF000)
        games, gf, gaa = gh + ga, fh + fa, ah + aa

        def avg(goals, g):
            t = 0 if g == 0 else goals * 10 // g
            return f"{t // 10}.{t % 10}"
        rem = avg(gf, games) + ":" + avg(gaa, games)
        bad += orig != rem
        if n < 5 or orig != rem:
            print(f"   Spiele {games:3} Tore {gf:3}:{gaa:3} → Original „{orig}“, Remake „{rem}“")
    print(f"   Abweichungen: {bad} von {len(cases)}")


def part3():
    print("3. Ergebnis 0x50283(52, 53, 3, 1): geänderte Bytes der Teamsätze je Wettbewerbscode 0xD0C2")
    ov = next(r for r in emu16.load(PATH).regions if r.stub == 0x128)
    o120 = next(r for r in emu16.load(PATH).regions if r.stub == 0x120)
    for code in range(14):
        c = cpu([(ov.start, ov.end), (0x4BD35, o120.end)])
        c.sdb(0xD0C2, code)
        lo = 0x14FD0 + 0x8257 + 0x50 * 52
        before = bytes(c.mem[lo:lo + 0xA0])
        c.call(0x50283, 52, 53, 3, 1)
        after = bytes(c.mem[lo:lo + 0xA0])
        ch = [f"{'H' if i < 0x50 else 'G'}+{i % 0x50:#04x}={after[i]}" for i in range(0xA0) if after[i] != before[i]]
        print(f"   Code {code:2}: {', '.join(ch) or 'nichts'}")


def part4():
    print("4. Fenster 0x4D385(52, 2, 0, 0): Zeile „Historische Ergebnisse“ und dritter Knopf je Code 0xD0C2")
    ov = next(r for r in emu16.load(PATH).regions if r.stub == 0x128)
    for code in (0, 7, 8, 13):
        c = cpu([(ov.start, ov.end), (0xB0E6, 0xB447), (0x4BDE6, 0x4BE17), (0x445E0, 0x44A48)])
        for i in range(50):
            c.sdw(0x5A57 + 10 * i, 0xFFFF)
        c.sdw(0x9657, 0)
        c.sdw(0x9659, emu16.FAR_HEAP_SEG)
        c.sdb(0xD0C2, code)
        texts, buttons = [], []
        c.hooks[0x0B651] = lambda cc: texts.append((cc.arg(4), cc.arg(5))) or 0
        c.hooks[0x4C196] = lambda cc: buttons.append((cc.arg(0), cc.arg(1))) or 0

        def stop(cc):
            raise emu16.Stop("event")
        c.hooks[0x0C068] = stop
        c.hooks[0x0C097] = stop
        try:
            c.call(0x4D385, 52, 2, 0, 0)
        except emu16.Stop:
            pass
        last = [t for t in texts if t[1] >= 167]
        print(f"   Code {code:2}: Texte ab y 167: {len(last)} {last[:2]}, Knöpfe {buttons}")


if __name__ == "__main__":
    part1()
    part2()
    part3()
    part4()
