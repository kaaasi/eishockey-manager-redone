"""Belastungstreffer je Reihe: Original gegen die Saaten des Remakes (Runde 19, Spur re-belastung).

Aufruf: hits.py "A B" ["A B" …] – erwartet $OUT/<A>.blk, $OUT/<B>.blk (dump.sh) und $OUT/s_<A>_<B>.txt (strain.sh).
Treffer = Zuwachs der Belastung + Erholung (Stürmer 1 je Training, Abwehr 0; Zahl der Trainings aus den Spielern ohne
Einsatz). Gekippte Spieler (Belastung ≤ 60 nach > 60) zählen mit der Mindestzahl bis 128 („k“). Reihen aus Stand B.
"""
import collections, math, os, statistics as st, sys
sys.path.insert(0, os.path.dirname(__file__)); import hok
OUT = os.environ.get('OUT', '/tmp/belastung-staerke')
def squad(bl):
    return {hok.name(bl, s): s for s in hok.slots(bl) if s[0x10]}
Z = []; P = []
for A, B in [x.split() for x in sys.argv[1:]]:
    a = squad(hok.blocks(f'{OUT}/{A}.blk')); b = squad(hok.blocks(f'{OUT}/{B}.blk'))
    rem = collections.defaultdict(dict)
    for l in open(f'{OUT}/s_{A}_{B}.txt'):
        sd, n, o, r = l.rsplit(None, 3); rem[sd][n] = int(r)
    idle = [a[n][0x13] - b[n][0x13] for n in b if n in a and b[n][6] == a[n][6] and a[n][0x13] > 60]
    trainings = max([d for d in idle if d < 10], default=0)   # gekippte Spieler ohne Einsatz (Δ > 70) zählen nicht
    names = [n for n in b if n in a and b[n][0xa] and b[n][0x15]]
    missing = [n for n in names if any(n not in v for v in rem.values())]
    assert not missing, f'{A}→{B}: im Remake fehlen {missing} (Namensabgleich?)'
    def hits(n, val):
        start = a[n][0x13]; r = trainings if b[n][0x15] & 0x80 else 0
        if val <= 60 and start > 60: return 'k', 128 + r - start
        return '', val - start + r
    lines = collections.defaultdict(list)
    for n in names: lines[('S' if b[n][0x15] & 0x80 else 'A') + str(b[n][0x15] & 0x7f)].append(n)
    print(f'{A}→{B}: {len(rem)} Saaten, Trainings {trainings}')
    for g in sorted(lines):
        o = [hits(n, b[n][0x13]) for n in lines[g]]; osum = sum(h for _, h in o)
        rs = [sum(hits(n, v[n])[1] for n in lines[g]) for v in rem.values()]
        z = (osum - st.mean(rs)) / max(st.pstdev(rs), 0.1); Z.append((f'{A}→{B}', g, round(z, 2)))
        print(f"  {g} {','.join(lines[g]):34s} Original {'+'.join(str(h) + k for k, h in o):12s} Σ {osum:3d} | Remake {st.mean(rs):5.1f} ± {st.pstdev(rs):4.1f}"
              f" ({min(rs)}–{max(rs)}) | Saaten ≤/≥ Original {sum(x <= osum for x in rs)}/{sum(x >= osum for x in rs)} | z {z:+.2f}")
    for n in names:
        k, h = hits(n, b[n][0x13]); rs = [hits(n, v[n])[1] for v in rem.values()]
        P.append((round((h - st.mean(rs)) / max(st.pstdev(rs), 0.5), 2), f'{A}→{B}', n + k))
    tot = sum(hits(n, b[n][0x13])[1] for n in names); rt = [sum(hits(n, v[n])[1] for n in names) for v in rem.values()]
    print(f'  Summe Original {tot} | Remake {st.mean(rt):.1f} ± {st.pstdev(rt):.1f} ({min(rt)}–{max(rt)})')
print(f'Σ z² = {sum(z * z for _, _, z in Z):.1f} bei {len(Z)} Reihen; größte |z|:', sorted(Z, key=lambda x: -abs(x[2]))[:6])
print(f'je Spieler: Σ z² = {sum(z * z for z, _, _ in P):.1f} bei {len(P)} Spielern; größte |z|:', sorted(P, key=lambda x: -abs(x[0]))[:8])
