"""A899: game counters of manager 0's squad in an original save pair (FILE89 → FILE90, Runde 22 Sitzung 7 Nr. 4).

Prints for every squad place of record 0 that changed: games per competition +0x06 + k (k = 0 league, 1 cup, 2 Europe),
games for the club +0x1C + 2k, dressed +0x0A, line byte +0x15 and flag "has played" +0x1A bit 7 (MatchEngine.LiveCredit.cs,
Squad.Lineup.InMatch.cs; Datei 0x415B7–0x41622). Names come from the pool (far DGROUP 0x8253 → HOK 0x0505F, 33 bytes,
name at +0) via +0x0F. Usage: python3 a899_zaehler.py <FILE89.HOK> <FILE90.HOK>
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hokraw  # noqa: E402

POOL = 0x0505F


def name(buf, pool_index):
    o = POOL + 0x21 * pool_index
    text = bytes(buf[o:o + 0x12]).split(b"\0")[0].decode("cp437", "replace")
    return text.translate(str.maketrans("{|}[\\]", "äöüÄÖÜ"))  # game font: brackets stand for umlauts


def fields(p):
    return {"Liga": p[0x06], "Pokal": p[0x07], "Europa": p[0x08], "Verein Liga": p[0x1C], "Verein Pokal": p[0x1E],
            "+0x0A": p[0x0A], "+0x15": p[0x15], "Bit7": p[0x1A] >> 7}


def main():
    ha, hb = hokraw.Hok(sys.argv[1]), hokraw.Hok(sys.argv[2])
    a, b = ha.data, hb.data
    for slot in range(0x618 // 0x34):
        p, q = ha.squad_slot(0, slot), hb.squad_slot(0, slot)
        if p[0x0F] == 0 and q[0x0F] == 0:
            continue
        fa, fb = fields(p), fields(q)
        diff = {k: f"{fa[k]}→{fb[k]}" for k in fa if fa[k] != fb[k]}
        if diff:
            print(f"Platz {slot + 1:2} {name(b, q[0x0F]):12} Rolle {q[0x0F] and b[POOL + 0x21 * q[0x0F] + 0x18]:3}: "
                  + ", ".join(f"{k} {v}" for k, v in diff.items()))


if __name__ == "__main__":
    main()
