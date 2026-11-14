import sys,collections,statistics as st,subprocess
import os; sys.path.insert(0, os.path.dirname(__file__)); import hok
OUT = os.environ.get('OUT', '/tmp/belastung-staerke')
A,B,strainfile=sys.argv[1],sys.argv[2],sys.argv[3]
a=hok.blocks(f'{OUT}/{A}.blk'); b=hok.blocks(f'{OUT}/{B}.blk')
def m(bl):
  d={}
  for s in hok.slots(bl):
    if s[0x10]==0: continue
    n=hok.name(bl,s)
    d[n]=s
  return d
SA=m(a); SB=m(b)
groups=collections.defaultdict(list)
LINE = SB if len(sys.argv)>4 and sys.argv[4]=="b" else SA
for n,s in LINE.items():
  pos=s[0x15]
  if s[0xa] and pos: groups[('S' if pos&0x80 else 'A')+str(pos&0x7f)].append(n)
rem=collections.defaultdict(dict)
for l in open(strainfile):
  sd,n,o,r=l.split(); rem[sd][n]=int(r)
print(A,'->',B, 'Saaten', len(rem))
for g in sorted(groups):
  names=groups[g]
  od=[(SB[n][0x13]-SA[n][0x13]) if n in SB else None for n in names]
  oflip=sum(1 for n in names if n in SB and SB[n][0x13]<=60 and SA[n][0x13]>60)
  osum=sum(SB[n][0x13]-SA[n][0x13] for n in names if n in SB and not (SB[n][0x13]<=60 and SA[n][0x13]>60))
  vals=[];fl=[]
  for sd,v in rem.items():
    vals.append(sum(v[n]-SA[n][0x13] for n in names if n in v and not (v[n]<=60 and SA[n][0x13]>60)))
    fl.append(sum(1 for n in names if n in v and v[n]<=60 and SA[n][0x13]>60))
  print(f" {g}: {','.join(names)} | Orig Δ {od} Σ{osum:+d} gekippt {oflip} | Remake Σ mittel {st.mean(vals):5.1f} sd {st.pstdev(vals):4.1f} gekippt mittel {st.mean(fl):.2f} | Remake Σ≤Orig {sum(1 for x in vals if x<=osum)}/{len(vals)}")
