"""B17 (A285, A419, A961): line values of the national team panels ("WM Vorbereitung", Datei 0x3B5F5–0x3B6C4).

The panel calls 0x977F(team, line, part, 0, 0) (Datei 0x3B65C) and shows (byte 2 + byte 1) / 2 of the result
(Datei 0x3B664–0x3B691). The script runs the original 0x977F for national teams without national coach (club field
+0x16 = 0) with random team fields (+0x18/+0x1A/+0x1C + part = condition/technique/form) for lines 0–2 and both parts,
decodes the packing (Datei 0x9CDE–0x9D13: byte 3 = field +0x1E…, byte 2 = condition, byte 1 = technique, byte 0 = form,
each clamped 0…100 by 0x4BD35) and compares with the rule in the remake (`OriginalMatch.LineValues`, inMatch = false:
value + 4 − 4 · line, clamped 0…100; display (C + T) / 2 = `GameState.NationalPanelLineValue`).
Not replayed: the national coach's own team (field +0x16 ≠ 0 → player path 0x98F8, Datei 0x97D6–0x97F0).
Usage: python3 b17_reihenwerte_national.py [em.exe]
"""
import random
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
TEAMS = emu16.FAR_HEAP_SEG


def remake(c, t, f, line):
    s = 4 - 4 * line
    return tuple(max(0, min(100, v + s)) for v in (c, t, f))


def main():
    cpu = emu16.load(PATH)
    cpu.allowed = [(emu16.HEADER, emu16.HEADER + 0x46E * 16), (0x977F, 0x9D1B), (0x4BD35, 0x4BD7E)]
    cpu.sdw(0x9657, 0)
    cpu.sdw(0x9659, TEAMS)
    rnd = random.Random(25)
    bad = 0
    rows = []
    for case in range(300):
        team = rnd.randrange(52, 64)
        base = team * 0x3D
        for k in range(0x3D):
            cpu.wb(TEAMS, base + k, 0)
        vals = [rnd.randrange(0, 110) for _ in range(6)]
        for part in (0, 1):
            cpu.wb(TEAMS, base + 0x18 + part, vals[0 + part])
            cpu.wb(TEAMS, base + 0x1A + part, vals[2 + part])
            cpu.wb(TEAMS, base + 0x1C + part, vals[4 + part])
        clamp = rnd.randrange(2)
        cpu.sdb(0x967D, clamp)
        line, part = rnd.randrange(3), rnd.randrange(2)
        cpu.call(0x977F, team, line, part, 0, 0)
        packed = (cpu.r["dx"] << 16) | cpu.r["ax"]
        k, t, f = (packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF
        shown = (k + t) // 2
        exp = remake(vals[part], vals[2 + part], vals[4 + part], line)
        ok = (k, t, f) == exp and shown == (exp[0] + exp[1]) // 2
        bad += not ok
        if case < 8 or not ok:
            rows.append(f"   Team {team} Reihe {line} {'Sturm' if part else 'Abwehr'}: Felder K {vals[part]:3} T {vals[2 + part]:3} "
                        f"F {vals[4 + part]:3} → 0x977F K {k:3} T {t:3} F {f:3}, angezeigt {shown:3}; Remake {exp} → "
                        f"{(exp[0] + exp[1]) // 2:3} {'gleich' if ok else 'ABWEICHUNG'}")
    print("0x977F(Team, Reihe, Teil, 0, 0) ohne Bundestrainer, 300 Zufallsfälle")
    print("\n".join(rows))
    print(f"   Abweichungen zur Regel des Remakes: {bad} von 300")


if __name__ == "__main__":
    main()
