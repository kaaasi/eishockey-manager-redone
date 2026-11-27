import os
OUTD = os.environ.get('OUT', '/tmp/belastung-staerke')
import sys,glob,re
# Köln games = last 3 END lines; home flags H A H
tag=sys.argv[1]; side=sys.argv[2] if len(sys.argv)>2 else 'HAH'
n=int(sys.argv[3]) if len(sys.argv)>3 else 3
tot=[];pts=[];wins=0;draws=0;loss=0
for f in sorted(glob.glob(f'{OUTD}/tr/{tag}_*.txt'), key=lambda x:int(re.findall(r'_(\d+)\.txt',x)[0])):
    ends=[l.split()[1] for l in open(f) if l.startswith('END')][-n:]
    gf=ga=p=0; s=[]
    for e,h in zip(ends,side):
        a,b=map(int,e.split(':'))
        if h=='A': a,b=b,a
        gf+=a; ga+=b
        r='W' if a>b else 'D' if a==b else 'L'
        wins+=r=='W'; draws+=r=='D'; loss+=r=='L'
        p+= 2 if r=='W' else 1 if r=='D' else 0
        s.append(f'{a}:{b}{r}')
    tot.append((gf,ga)); pts.append(p)
    print(f, ' '.join(s), f'{gf}:{ga}', p)
N=len(tot)
print('Mittel Tore', sum(a for a,b in tot)/N, sum(b for a,b in tot)/N, 'Punkte', sum(pts)/N, 'W/D/L', wins,draws,loss)
print('Punkte<=2:', sum(1 for p in pts if p<=2), 'von', N)
