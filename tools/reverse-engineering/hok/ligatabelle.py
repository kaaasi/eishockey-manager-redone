"""Spur hok-patch-tabellen, Teil 1: is the league place list of a save game stable under em.exe's own re-sort?

The main menu (Datei 0x44EFD) calls 0x50B45(league, 1, 1) for every league: with flag 1 it books no results (Datei
0x50C43-0x50C4A skips 0x50283) but sorts the place list DGROUP 0x5CCB + 12 * league from the table records DGROUP 0x825A +
0x50 * slot (Datei 0x50D68-0x50FEB). It returns at once after the regular season: DGROUP 0x7A9 + league - 1 > 0x7B6 - 1
(= 43, Datei 0x50B4D-0x50B71). This script runs 0x50B45(league, 1, 1) from the bytes on the table records and place list of
a save game (pairing generator 0x4E7C7, marker 0x4F325 and play-off setup 0x31934 are stubs) and reports whether the place
list stays as stored, for the stored 0x7A9 and once more with 0x7A9 = 1 (sorting forced); as a counter check the
reversed place list is sorted with 0x7A9 = 1 and must come back to the stored order (ties excepted). A patched place list must be such
a fixed point, otherwise the first main menu re-sorts it (as long as 0x7A9 <= 44).
Usage: python3 -I ligatabelle.py <save.HOK> [em.exe]     (slot numbers only)
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "abweichungen"))
sys.path.insert(0, str(HERE))
import emu16  # noqa: E402
import hokread  # noqa: E402

LEAGUE_DAY = 0x50B45


def resort(s, exe, league, day=None, order=None):
    c = emu16.load(exe)
    c.max_steps = 3_000_000
    c.allowed = [(LEAGUE_DAY, LEAGUE_DAY + 1284)]
    rec = s.block("8257")
    for i, v in enumerate(rec):
        c.sdb(0x8257 + i, v)
    for i, v in enumerate(s.block("5CCB")):
        c.sdb(0x5CCB + i, v)
    for i, v in enumerate(s.block("07A9")):
        c.sdb(0x07A9 + i, v)
    if day is not None:
        c.sdb(0x7A9 + league, day)
    if order is not None:
        for p, slot in enumerate(order):
            c.sdb(0x5CCB + 12 * league + p, slot)
    c.sdb(0x386, 0)  # no manager: the series loop 0x50CDF is not needed for the sort
    c.call(LEAGUE_DAY, league, 1, 1)
    return [c.db(0x5CCB + 12 * league + p) for p in range(12)]


def main():
    path = sys.argv[1]
    exe = sys.argv[2] if len(sys.argv) > 2 else None
    s = hokread.Save(path)
    print(f"{Path(path).name}: Prüfsummen gültig {s.checksums_ok}, 0x7A9.. = {list(s.block('07A9')[:3])}")
    for lg in range(3):
        stored = list(s.block("5CCB")[12 * lg:12 * lg + 12])
        got = resort(s, exe, lg)
        forced = resort(s, exe, lg, day=1)
        back = resort(s, exe, lg, day=1, order=stored[::-1])
        print(f"  Liga {lg}: gespeichert {stored}; nach 0x50B45 {'gleich' if got == stored else got}; "
              f"mit 0x7A9 = 1 {'gleich' if forced == stored else forced}; "
              f"Gegenprobe umgekehrt eingesetzt -> {'gespeicherte Folge' if back == stored else back}")


if __name__ == "__main__":
    main()
