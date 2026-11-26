"""Spur tagesabschluss (#1018, SG-01): day change of the main path and of the world-cup loop, per manager.

Three replays straight from the bytes of em.exe (emu16, no original data written):

1. Main path Datei 0x55204–0x552D5 (inside 0x54C9F) with two managers (DGROUP 0x386 = 2). Day counter DGROUP 0x3B7 = 6
   before the run, so 0x55204 makes it 7 (≡ 0 mod 7: talent search, small drift, 0x07169 and stadium build all run).
   Stubs, printed in call order with the manager in DGROUP 0xA70D/0xA70E: date step 0x367B1, day close 0x3ED01, sponsor
   rating 0x3F833, talent search 0x06FA5, small drift 0x09D1B, daily run 0x07169, stadium build 0x3D316. The long
   modulo 0x0228B (C runtime) runs for real.
2. World-cup loop Datei 0x55670–0x55724 (same function), same setup; healing 0x07C12 per squad place (place counter
   0x4BE17 stubbed: 2 places per manager).
3. Day close 0x3ED01 itself for manager 0, then manager 1, on a mid-month offer day (month index DGROUP 0x5A1C = 0,
   day DGROUP 0x5A20 = month length / 2 from the table DGROUP 0x393): every random call 0x0C037 with caller and range,
   plus the sub-calls offers 0x3FB47 and share day 0x5784A (stubbed). The random function returns 1 (no flag cleared;
   the talent-price coin 0x3EEB2 = 1 draws the price roll 0x3EEC7 too).

Usage: python3 hauptweg.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
MAIN = (0x54C9F, 0x5667A)
CRT = (emu16.HEADER, emu16.HEADER + 0x46E * 16)


def frame(c, start):
    cs, ip = c.at(start)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x40) & 0xFFFF
    c.s["cs"], c.ip = cs, ip


def logger(log, name, who=0xA70E):
    def fn(cc):
        log.append(f"Manager {cc.db(who)}: {name}" if who else name)
        return 0
    return fn


def run_loop(start, stop, extra):
    c = emu16.load(PATH)
    c.allowed = [MAIN, CRT]
    c.sdb(0x386, 2)
    c.sdw(0x3B7, 6)
    c.sdw(0x3B9, 0)
    log = []
    c.hooks[0x367B1] = logger(log, f"0x367B1 Datum weiter", who=None)
    c.hooks[0x3ED01] = logger(log, "Tagesabschluss 0x3ED01", who=0xA70D)
    c.hooks[0x3F833] = logger(log, "Werbebewertung 0x3F833", who=0xA70D)
    c.hooks[0x3D316] = logger(log, "Stadionbau 0x3D316")
    for addr, hook in extra(log).items():
        c.hooks[addr] = hook
    frame(c, start)
    c.stop_at = {stop}
    try:
        c.run()
    except emu16.Stop:
        pass
    return c, log


def main_path():
    def extra(log):
        return {
            0x06FA5: logger(log, "Talentsuche 0x06FA5", who=None),
            0x09D1B: logger(log, "kleine Wanderung 0x09D1B", who=None),
            0x07169: logger(log, "Tageslauf 0x07169"),
        }
    c, log = run_loop(0x55204, 0x552D5, extra)
    print(f"1. Hauptweg Datei 0x55204–0x552D5, zwei Manager, Zähler 0x3B7 danach {c.dw(0x3B7)}:")
    for line in log:
        print("   " + line)


def world_cup_loop():
    def extra(log):
        places = {"n": 0}

        def heal(cc):
            log.append(f"Manager {cc.db(0xA70E)}: Heilung 0x07C12 Platz {(cc.arg(0) - 0x5DC3) % 0x618 // 0x34}")
            return 0

        def count(cc):
            places["n"] += 1
            return 2
        return {0x07C12: heal, 0x4BE17: count}
    c, log = run_loop(0x55670, 0x55724, extra)
    print(f"2. WM-Schleife Datei 0x55670–0x55724, zwei Manager, Zähler 0x3B7 danach {c.dw(0x3B7)}:")
    for line in log:
        print("   " + line)


def day_close():
    c = emu16.load(PATH)
    c.allowed = [(0x3ED01, 0x3F185), CRT]
    month = 0
    length = c.db(0x393 + month)
    c.sdw(0x5A1C, month)
    c.sdw(0x5A20, length // 2)
    c.sdw(0x5A22, 0)
    print(f"3. Tagesabschluss 0x3ED01, Monat {month} (Länge {length}), Tag {length // 2} (Angebotstag Monatsmitte):")
    for m in range(2):
        log = []

        def rnd(cc):
            caller = cc.calls[-1][0] if cc.calls else 0
            log.append(f"random({cc.arg(0)}, {cc.arg(1)}) aus Datei {caller:#07x}")
            return 1
        c.hooks[0x0C037] = rnd
        c.hooks[0x3FB47] = logger(log, "Werbeangebote 0x3FB47", who=None)
        c.hooks[0x5784A] = logger(log, "Börsentag 0x5784A", who=None)
        c.sdb(0xA70E, m)
        c.sdb(0xA70D, m)
        c.calls.clear()
        c.call(0x3ED01)
        print(f"   Manager {m}: {len([x for x in log if x.startswith('random')])} Würfe")
        # compress the eleven builder rolls into one line
        builders = [x for x in log if "0x3ed20" in x]
        rest = [x for x in log if "0x3ed20" not in x]
        print(f"      {len(builders)} × random(0, 10) aus Datei 0x3ed20 (Baufirmen)")
        for line in rest:
            print("      " + line)


def main():
    main_path()
    world_cup_loop()
    day_close()


if __name__ == "__main__":
    main()
