"""Probe (Runde 19, re-belastung): Managerteil von 0x87F7 (Datei 0x8B31–0x8DF8) mit der echten lrandom-Folge (Datei 0xBFD5)
gegen unabhängige Züge. Kader und Reihen wie Köln in FILE02 (nur Werte, keine Originaldaten); Reihenwechsel vereinfacht
(Modus 2), zwischen den Minuten 20–200 fremde Züge. Ausgabe: Treffer je Spiel und je Spieler (Mittel/Streuung)."""
import random, statistics as st, sys
M=4_000_000
class LR:
    def __init__(s,x): s.x=x
    def __call__(s,a,b):
        s.x=(16*s.x+167)%M
        return a+s.x*(b-a+1)//M
class IND:
    def __init__(s,x): s.r=random.Random(x)
    def __call__(s,a,b): return s.r.randint(a,b)
# Köln FILE02: (name, part, line, goalie, K, T) in Kaderplatz-Reihenfolge
SQ=[('CISSEWSKI',0,0,1,80,83),('KUEHN',0,1,0,83,81),('SCHIFFL',0,0,0,79,83),('MENDE',0,0,0,80,82),('KWASIGROCH',0,2,0,80,80),
    ('NOETZEL',0,2,0,79,80),('UVIRA',0,1,0,79,82),('BRANDL',1,2,0,80,82),('HOCK',1,1,0,82,81),('SIKORA',1,1,0,79,82),
    ('STEFAN',1,2,0,79,80),('DOROCHIN',1,0,0,83,83),('LIEBSCH',1,0,0,81,82),('HANEMANN',1,2,0,81,83),('CHAIDAROV',1,0,0,82,82),('LUPZIG',1,1,0,79,80)]
def game(rnd, gaprng, effort=60, gap=(20,200)):
    E=[88]*len(SQ); hits=[0]*len(SQ)
    line=[0,0]; mins=[0,0]
    for minute in range(1,61):
        if minute in (21,41): E=[min(e+20,90) for e in E]
        started=[False,False]; m=0
        for i,(n,part,ln,g,K,T) in enumerate(SQ):
            if ln!=line[part]:
                if E[i]<90:
                    b=1 if (rnd(0,4)==0 and K>rnd(0,100)) else 0
                    E[i]+=1+b
            else:
                if not started[part]:
                    mins[part]+=1; started[part]=True
                m=mins[part]
                if rnd(0,4+(1 if K>T else 0))==0: hits[i]+=1
                d=(rnd(0,effort)+75)//40+1
                d+=4 if m>=rnd(0,5) else 0
                d+=8 if m>rnd(5,15) else 0
                E[i]=max(1,min(99,E[i]-d))
        for _ in range(gaprng.randint(*gap)): rnd(0,1)
        for part in (0,1):
            if mins[part]>3:
                le=[]
                for l in range(3):
                    v=[E[i] for i,p in enumerate(SQ) if p[1]==part and p[2]==l and not p[3]]
                    le.append((sum(v)+(90 if part==0 else 0))//3)
                order=sorted(range(3),key=lambda l:-le[l])
                line[part]=order[1] if line[part]==order[0] else order[0]; mins[part]=0
    return hits
def run(kind,N=400):
    tot=[];per=[[] for _ in SQ]
    g=random.Random(7)
    for s in range(N):
        rnd = LR(g.randrange(M)) if kind=='lr' else IND(s)
        h=game(rnd,g)
        tot.append(sum(h))
        for i,x in enumerate(h): per[i].append(x)
    print(kind,'total mean %.1f sd %.2f min %d max %d'%(st.mean(tot),st.pstdev(tot),min(tot),max(tot)))
    print('  '+' '.join('%s %.1f/%.1f'%(SQ[i][0][:5],st.mean(p),st.pstdev(p)) for i,p in enumerate(per)))
    return tot
run('ind'); run('lr')
