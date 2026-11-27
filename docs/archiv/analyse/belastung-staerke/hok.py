import re,sys
def blocks(path):
    out={}; cur=None; buf=bytearray()
    for line in open(path,encoding='utf-8',errors='replace'):
        m=re.match(r'Block (\S+) \(',line)
        if m:
            if cur: out[cur]=bytes(buf)
            cur=m.group(1); buf=bytearray(); continue
        m=re.match(r'^([0-9A-F]{4,})  ((?:[0-9A-F]{2} ){1,16})',line)
        if cur and m: buf+=bytes(int(x,16) for x in m.group(2).split())
    if cur: out[cur]=bytes(buf)
    return out
def slots(b, mgr=0):
    s=b['5DC3'][mgr*0x618:(mgr+1)*0x618]
    return [s[i*0x34:(i+1)*0x34] for i in range(30)]
def club(b, i): return b['far:9657'][i*0x3D:(i+1)*0x3D]
def pool(b,i): return b['far:8253'][i*33:(i+1)*33]
def name(bl, slot):
    """Spielername wie in `emtool abgleich` (Umlaute statt der DOS-Zeichen ] [ \\; Runde 19: ohne das fiel KÜHN aus pair.py)."""
    n = pool(bl, slot[0xf])[:12].split(b'\0')[0].decode('cp850')
    return n.replace(']', 'Ü').replace('[', 'Ä').replace(chr(92), 'Ö')
