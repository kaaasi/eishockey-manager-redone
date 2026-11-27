#!/usr/bin/env python3
"""Belastungstreffer je Spiel gegen die Eiszeit aus den Bildern (Spur abgleich-dosbox-19, Teil 1, #730).

Aufruf: python3 -I treffer19.py   (erwartet $OUT/FILE02.blk, FILE50.blk, FILE61.blk, FILE63.blk aus
docs/archiv/analyse/belastung-staerke/dump.sh; $OUT Vorgabe /tmp/belastung-staerke)
Je Spiel des Laufs FILE02→50→61→63 (DOSBox runde-17/-19, ohne Mausklick):
- Original: Treffer = Zuwachs + Erholung (Stürmer 1 je Training, Abwehr/Torhüter 0; wie hits.py), gekippte Spieler mit der
  Mindestzahl bis 128 (untere Schranke);
- erwartet nach em.exe Datei 0x8C53–0x8C82 (lrandom(0, 4 + (Kondition > Technik)) = 0 je Spieler und Minute auf dem Eis) mit
  den Minuten auf dem Eis aus den Bildern (Reihenfolgen wie AbgleichDosboxLinesTests; 0x87F7 läuft vor dem Reihenwechsel der
  Minute, „versetzt“ nimmt deshalb die Reihe des Vorbilds);
- Simulation des Managerteils von 0x87F7 mit diesen Reihen, einmal mit unabhängigen Zügen, einmal mit der echten lrandom-Folge
  (x = 16x + 167 mod 4.000.000, wie belastung-staerke/lrsim.py), je 400 Spiele.
"""
import os, random, statistics as st, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'belastung-staerke')); import hok
OUT = os.environ.get('OUT', '/tmp/belastung-staerke')
M = 4_000_000
# Stand vor dem Spiel, Stand danach, Köln Abwehr, Köln Sturm (Bilder file50, file61, file63)
SPIELE = [
    ('FILE02', 'FILE50', "000111121222200002222111100011111222200002222221110000011111",
                         "000111121222200002222111100011111222200001111100002222111122"),
    ('FILE50', 'FILE61', "000111112220000011111222221111100002222111100000111100001111",
                         "222000001110122220000011112222200010000111102222111100001111"),
    ('FILE61', 'FILE63', "000111122220000111222220000011112220000011112222111222220000",
                         "000111122220000111222220000011112220000022221111001222220000"),
]

class LR:
    def __init__(self, x): self.x = x
    def __call__(self, a, b):
        self.x = (16 * self.x + 167) % M
        return a + self.x * (b - a + 1) // M

class IND:
    def __init__(self, s): self.r = random.Random(s)
    def __call__(self, a, b): return self.r.randint(a, b)

def kader(bl):
    return {hok.name(bl, s): s for s in hok.slots(bl) if s[0x10]}

def simulation(sq, folge, art, n=400):
    tot = []; g = random.Random(5)
    for k in range(n):
        rnd = LR(g.randrange(M)) if art == 'lr' else IND(k)
        e = [min(101 - (s[0x13] >> 3), 91) - 1 for s in sq]; h = 0; mins = [0, 0]; vorher = [None, None]
        for m in range(60):
            if m in (20, 40): e = [min(x + 20, 90) for x in e]
            reihe = [int(folge[0][m]), int(folge[1][m])]
            for t in (0, 1):
                if reihe[t] != vorher[t]: mins[t] = 0
            vorher = reihe[:]; los = [0, 0]
            for i, s in enumerate(sq):
                t = s[0x15] >> 7
                if (s[0x15] & 0x7f) - 1 != reihe[t]:
                    if e[i] < 90: e[i] += 1 + (1 if rnd(0, 4) == 0 and s[0x10] > rnd(0, 100) else 0)
                    continue
                if not los[t]: mins[t] += 1; los[t] = 1
                if rnd(0, 4 + (s[0x10] > s[0x11])) == 0: h += 1
                d = (rnd(0, 60) + 75) // 40 + 1 + (4 if mins[t] >= rnd(0, 5) else 0) + (8 if mins[t] > rnd(5, 15) else 0)
                e[i] = max(1, min(99, e[i] - d))
            for _ in range(g.randint(20, 200)): rnd(0, 1)
        tot.append(h)
    return st.mean(tot), st.pstdev(tot)

summe = [0, 0.0, 0.0]
for A, B, D, S in SPIELE:
    a, b = kader(hok.blocks(f'{OUT}/{A}.blk')), kader(hok.blocks(f'{OUT}/{B}.blk'))
    idle = [a[n][0x13] - b[n][0x13] for n in b if n in a and b[n][6] == a[n][6] and a[n][0x13] > 60]
    trainings = max([d for d in idle if d < 10], default=0)
    sq = [a[n] for n in a if a[n][0xa] and a[n][0x15] & 0x7f]
    teile = {0: [0, 0.0, 0.0], 1: [0, 0.0, 0.0]}
    for n in a:
        s = a[n]
        if not (s[0xa] and s[0x15] & 0x7f): continue
        t = s[0x15] >> 7; reihe = (s[0x15] & 0x7f) - 1; folge = S if t else D
        p = 1 / (5 + (s[0x10] > s[0x11]))
        start, ende = s[0x13], b[n][0x13]
        h = 128 - start + (trainings if t else 0) if ende <= 60 < start else ende - start + (trainings if t else 0)
        teile[t][0] += h
        teile[t][1] += p * sum(int(c) == reihe for c in folge)
        teile[t][2] += p * sum(int(c) == reihe for c in folge[0] + folge[:-1])   # versetzt: Reihe vor dem Wechsel
    o = teile[0][0] + teile[1][0]; e = teile[0][1] + teile[1][1]; ev = teile[0][2] + teile[1][2]
    mi, si = simulation(sq, (D, S), 'ind'); ml, sl = simulation(sq, (D, S), 'lr')
    summe[0] += o; summe[1] += ml; summe[2] += sl * sl
    print(f'{A}→{B}: Trainings {trainings}; Original {o} (Abwehr {teile[0][0]}, Sturm {teile[1][0]}) | erwartet {e:.1f} '
          f'(Abwehr {teile[0][1]:.1f}, Sturm {teile[1][1]:.1f}; versetzt {ev:.1f}) | Simulation unabhängig {mi:.1f} ± {si:.1f}, '
          f'lrandom-Folge {ml:.1f} ± {sl:.1f} | z {(o - ml) / sl:+.2f}')
print(f'Lauf FILE02→63: Original {summe[0]} | Simulation lrandom-Folge {summe[1]:.1f} ± {summe[2] ** 0.5:.1f} | '
      f'z {(summe[0] - summe[1]) / summe[2] ** 0.5:+.2f}')
