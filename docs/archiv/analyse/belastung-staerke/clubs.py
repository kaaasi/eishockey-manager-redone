"""Vereinsfelder 0x26 (Einsatz), 0x2D (Grundwert), 0x2C Bits 4-6 (Angriff) der Computervereine 0-51 je Spielstand."""
import collections, os, sys
sys.path.insert(0, os.path.dirname(__file__)); import hok
OUT = os.environ.get('OUT', '/tmp/belastung-staerke')
for f in sys.argv[1:]:
    b = hok.blocks(f'{OUT}/{f}.blk'); c = collections.Counter()
    for i in range(52):
        r = hok.club(b, i)
        if r[0x16] == 0:
            c[(r[0x26], r[0x2d], (r[0x2c] >> 4) & 7)] += 1
    print(f, '(0x26, 0x2D, Angriff): Anzahl', sorted(c.items(), key=lambda x: -x[1]))
