#!/usr/bin/env python3
"""Reihenlampen der Anzeigetafel (0x52BD6) aus den Bildern file50_02…85 lesen (Spur abgleich-dosbox, Teil 1, #730).

Aufruf: python3 -I lampen.py [bilderordner]   (Vorgabe ~/referenz/original/spielstaende/bilder)
Ausgabe: je Bild Nummer, Minute, Reihen KÖLN Abwehr/Sturm, PREUSSEN Abwehr/Sturm; danach je Teil die Ziffernfolge
für Minute 1…60 (letztes vollständiges Bild je Minute, fehlende Minuten vom Vorgänger) wie in AbgleichDosboxLinesTests.
Die Minuten der Spieluhr unten links sind von Hand abgelesen (MINUTEN), die Bilder liegen nur im Referenz-Repo.
"""
import glob, os, sys
from PIL import Image

MINUTEN = ("01 02 03 03 04 04 05 06 07 08 09 10 10 11 12 13 14 15 16 16 18 18 19 19 20 20 20 21 21 22 22 24 24 24 25 26 26 "
           "27 28 29 29 29 30 31 32 33 33 33 34 35 36 36 38 38 38 39 40 41 41 41 42 42 44 44 44 45 46 47 48 48 49 50 51 51 "
           "52 53 54 55 56 56 57 58 59 59").split()
AN = (243, 0, 0)  # Lampe „an“ (kb\38 76|10), aus = Farbe 0x10

ordner = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser("~/referenz/original/spielstaende/bilder")
bilder = sorted(glob.glob(os.path.join(ordner, "file50_*_live_*.png")))
assert len(bilder) == len(MINUTEN), len(bilder)
letzte = {}
for pfad, minute in zip(bilder, MINUTEN):
    bild = Image.open(pfad).convert("RGB")
    reihen = []
    for x0 in (13, 240):            # Heim, Gast (y0 = 15 für die erste Paarung)
        for t in (0, 1):            # Abwehr, Sturm
            an = [k for k in range(4) if bild.getpixel((x0 + 8 + 8 * t + 3, 15 + 7 * k + 3)) == AN]
            reihen.append(an[0] if len(an) == 1 else None)   # None: Bild mitten im Neuzeichnen
    print(os.path.basename(pfad).split("_")[1], minute, reihen)
    if None not in reihen:
        letzte[int(minute)] = reihen
folge, aktuell = [], None
for m in range(1, 61):
    aktuell = letzte.get(m, aktuell)
    folge.append(aktuell)
for i, name in enumerate(("HomeDefense", "HomeAttack", "AwayDefense", "AwayAttack")):
    print(name, "".join(str(r[i]) for r in folge))
