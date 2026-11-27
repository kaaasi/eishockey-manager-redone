#!/usr/bin/env python3
"""Reihenlampen und Spielminute aus den Live-Bildern einer DOSBox-Aufnahme lesen (Spur abgleich-dosbox-19, Teil 1, #730).

Aufruf: python3 -I reihen19.py <präfix> [bilderordner]   z. B. file61, file63, file50 (Vorgabe ~/referenz/original/spielstaende/bilder)
Ausgabe: je Bild Name, Minute, Reihen Heim Abwehr/Sturm, Gast Abwehr/Sturm; danach je Teil die Ziffernfolge für Minute 1…60
(letztes vollständiges Bild je Minute, fehlende Minuten vom Vorgänger) wie in AbgleichDosboxLinesTests.
Minute: Siebensegment-Anzeige im Managerkasten unten links (Ziffern bei x 96 und 106, y 196…204); gegen die von Hand gelesenen
Minuten von file50 (lampen.py) geprüft: 84 von 84 gleich. Lampen wie lampen.py (Anzeigetafel 0x52BD6). Bilder nur im Referenz-Repo.
"""
import glob, os, sys
from PIL import Image

SEG = {(1, 1, 1, 0, 1, 1, 1): 0, (0, 0, 1, 0, 0, 1, 0): 1, (1, 0, 1, 1, 1, 0, 1): 2, (1, 0, 1, 1, 0, 1, 1): 3,
       (0, 1, 1, 1, 0, 1, 0): 4, (1, 1, 0, 1, 0, 1, 1): 5, (1, 1, 0, 1, 1, 1, 1): 6, (1, 0, 1, 0, 0, 1, 0): 7,
       (1, 1, 1, 1, 1, 1, 1): 8, (1, 1, 1, 1, 0, 1, 1): 9}
LIT = {(255, 255, 255), (195, 243, 227), (146, 178, 195)}   # Segmentfarben (hell, Kante, Schatten)
AN = (243, 0, 0)                                            # Lampe „an“

def ziffer(bild, x):
    an = lambda y, xx: bild.getpixel((xx, y)) in LIT
    s = (an(196, x + 2), an(198, x), an(198, x + 5), an(200, x + 2), an(202, x), an(202, x + 5), an(203, x + 2))
    return None if not any(s) else SEG.get(tuple(int(v) for v in s), '?')

def minute(bild):
    z, e = ziffer(bild, 96), ziffer(bild, 106)
    return None if e in (None, '?') or z == '?' else (z or 0) * 10 + e

def reihen(bild):
    r = []
    for x0 in (13, 240):            # Heim, Gast
        for t in (0, 1):            # Abwehr, Sturm
            an = [k for k in range(4) if bild.getpixel((x0 + 8 + 8 * t + 3, 15 + 7 * k + 3)) == AN]
            r.append(an[0] if len(an) == 1 else None)
    return r

praefix = sys.argv[1]
ordner = sys.argv[2] if len(sys.argv) > 2 else os.path.expanduser("~/referenz/original/spielstaende/bilder")
letzte = {}
for pfad in sorted(glob.glob(os.path.join(ordner, f"{praefix}_*.png"))):
    if "fenster" in pfad:
        continue
    bild = Image.open(pfad).convert("RGB")
    m, r = minute(bild), reihen(bild)
    print(os.path.basename(pfad), m, r)
    if m is not None and 1 <= m <= 60 and None not in r:
        letzte[m] = r
print("fehlende Minuten", [m for m in range(1, 61) if m not in letzte])
folge, aktuell = [], None
for m in range(1, 61):
    aktuell = letzte.get(m, aktuell)
    folge.append(aktuell)
for i, name in enumerate(("HomeDefense", "HomeAttack", "AwayDefense", "AwayAttack")):
    print(name, "".join(str(r[i]) for r in folge))
