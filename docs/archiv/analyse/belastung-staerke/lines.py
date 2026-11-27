import os
OUTD = os.environ.get('OUT', '/tmp/belastung-staerke')
import sys,glob,re,collections
tag=sys.argv[1]; side=sys.argv[2]; n=int(sys.argv[3])
agg=collections.defaultdict(lambda: collections.Counter())
modes=collections.defaultdict(collections.Counter)
for f in glob.glob(f'{OUTD}/tr/{tag}_*.txt'):
    games=[];cur=[]
    for l in open(f):
        if l.startswith('M '): cur.append(l)
        elif l.startswith('END'): games.append(cur); cur=[]
    games=games[-n:]
    for gi,(g,h) in enumerate(zip(games,side)):
        for l in g:
            d=dict(kv.split('=',1) for kv in l.split()[2:])
            mine='h' if h=='H' else 'a'
            dl,al=map(int,d[mine+'L'].split('/'))
            agg[gi]['D%d'%dl]+=1; agg[gi]['S%d'%al]+=1
            modes[gi][d[mine+'Mode']]+=1
N=len(glob.glob(f'{OUTD}/tr/{tag}_*.txt'))
for gi in sorted(agg):
    c=agg[gi]
    print('Spiel',gi+1,'Sturm', [round(c['S%d'%i]/N,1) for i in range(4)], 'Abwehr',[round(c['D%d'%i]/N,1) for i in range(4)], 'Modus', {k:round(v/N,1) for k,v in sorted(modes[gi].items())})
