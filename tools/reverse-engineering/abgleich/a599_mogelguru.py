"""A599: what em.exe does after the "Der kleine Mogelguru" screen (KAPUTT.HOK, wrong checksum).

Runs the tail of the loader 0x5C3E3 (Ov 0x140) from Datei 0x5CEFA with the error code [bp-0x1] = 5 that the checksum
compare sets (Datei 0x5CBD8–0x5CBE1, 0x5CDAF–0x5CDB8). Real code: Datei 0x5CEFA–0x5D04E. Stubbed and recorded: all
drawing calls; the frame routine 0x0B5A7 (x0, y0, x1, y1, colour) and the pause 0x130EE(n) (n / 3 BIOS ticks,
LiveClock.cs) are printed. The run ends after a fixed number of pauses; it also reports whether the normal exit
(Datei 0x5D03C: clear flags, return 1) or any address outside the blink loop was reached after the first pause.
Usage: python3 a599_mogelguru.py [em.exe] [pauses]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
PAUSES = int(sys.argv[2]) if len(sys.argv) > 2 else 6
LOOP = (0x5CFFE, 0x5D03C)   # blink loop: xor colour, three frames, pause, jmp back
EXIT = 0x5D03C


def main():
    c = emu16.load(PATH)
    c.allowed = [(0x5CEFA, 0x5D04F)]
    c.record = True
    c.visited = set()
    bp = 0xF000
    c.r["bp"] = bp
    c.r["sp"] = bp - 0x150
    c.wb(c.s["ss"], bp - 1, 5)      # error code 5 = wrong checksum
    c.wb(c.s["ss"], bp - 2, 0)      # initial colour, Datei 0x5C3F0
    log = []
    pauses = [0]
    after_first = set()

    def frame(cc):
        a = [cc.arg(i) for i in range(2, 7)]  # args 0/1 = far pointer to the screen buffer
        log.append(f"  Rahmen ({a[0]},{a[1]})–({a[2]},{a[3]}) Farbe {a[4] & 0xFF:#04x}")

    def pause(cc):
        pauses[0] += 1
        n = cc.arg(0)
        log.append(f"Pause 0x130EE({n}) = {n // 3} BIOS-Takte (Nr. {pauses[0]})")
        if pauses[0] >= PAUSES:
            raise emu16.Stop("pauses")
        return 0

    c.hooks[0x0B5A7] = frame
    c.hooks[0x130EE] = pause
    cs, ip = c.at(0x5CEFA)
    c.s["cs"], c.ip = cs, ip
    c._sentinel = (0xFFFF, 0xFFFF)
    try:
        while True:
            here = c.here()
            if pauses[0] >= 1:
                after_first.add(here)
            c.step()
            c.steps += 1
            if c.steps > 200000:
                raise emu16.Stop("step limit")
    except emu16.Stop as e:
        reason = str(e)
    print("\n".join(log))
    outside = sorted(a for a in after_first if not LOOP[0] <= a < LOOP[1])
    print(f"Ende: {reason}; Schritte {c.steps}")
    print(f"nach der ersten Pause erreicht: Ausgang Datei {EXIT:#07x} {'ja' if EXIT in after_first else 'nein'}; "
          f"Adressen außerhalb der Schleife {LOOP[0]:#07x}–{LOOP[1]:#07x}: {len(outside)}")
    # static check: every branch in the loop stays inside it (the loop ends with an unconditional jmp back)
    exe = c.exe
    print(f"Schleifenende Datei 0x5D03A: Befehl {exe[0x5D03A]:#04x} {exe[0x5D03B]:#04x} (eb = jmp short), "
          f"Ziel Datei {0x5D03C + ((exe[0x5D03B] ^ 0x80) - 0x80):#07x}")


if __name__ == "__main__":
    main()
