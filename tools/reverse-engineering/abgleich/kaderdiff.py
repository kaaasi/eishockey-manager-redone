"""Field-wise comparison of the squad slots of one manager record between two original saves (track abgleich-dosbox-22b).

Squad slot = DGROUP 0x5DC3 + 0x618 * record + 0x34 * place (docs/formate/spielstand-original.md). Prints day counter
(DGROUP 0x3B7) and mode (0xD0B9) of both saves, then per occupied place the pool index (+0x0F) and every changed field
as "+off: old->new", and finally how many places changed per field. Only numbers are printed (no names, no raw blocks).
Usage: python3 kaderdiff.py <A.HOK> <B.HOK> <record 0-5> [saves dir]
Examples: A408 FILE19 FILE20 0; A918 FILE80 FILE81 5 and FILE80 FILE81 0.
"""
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from hokraw import Hok  # noqa: E402


def main():
    a_name, b_name, record = sys.argv[1], sys.argv[2], int(sys.argv[3])
    folder = Path(sys.argv[4]) if len(sys.argv) > 4 else Path.home() / "referenz/original/spielstaende"
    a, b = Hok(folder / f"{a_name}.HOK"), Hok(folder / f"{b_name}.HOK")
    for name, h in ((a_name, a), (b_name, b)):
        print(f"{name}: Tageszähler {int.from_bytes(h.at(0x08EE7, 4), 'little')}, Modus 0xD0B9 = {h.at(0x00314)[0]}")
    count = Counter()
    for place in range(0x618 // 0x34):
        x, y = a.squad_slot(record, place), b.squad_slot(record, place)
        if x[0x0F] == 0 and y[0x0F] == 0:
            continue
        changed = [(i, x[i], y[i]) for i in range(0x34) if x[i] != y[i]]
        for i, _, _ in changed:
            count[i] += 1
        print(f"Platz {place:2d} Pool {x[0x0F]:3d}: " + " ".join(f"+{i:02X}:{p}->{q}" for i, p, q in changed))
    print(f"Satz {record}, geänderte Plätze je Feld: " + ", ".join(f"+{i:02X} {n}" for i, n in sorted(count.items())))


if __name__ == "__main__":
    main()
