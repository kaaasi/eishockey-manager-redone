# Belege aus em.exe für die Abweichungen der Runde 25 (Spur `re-abweichungen`)

Welle 0 der Runde 25 (R25-D3 = B, `docs/arbeitspakete.md`). Je Thema: Beleg als **Nachbildung des Originalablaufs** – die
Skripte in `tools/reverse-engineering/abweichungen/` führen den Code von `em.exe` mit dem Interpreter `emu16.py` aus (Speicher
vorbereitet, Grafik- und Eingabefunktionen durch Stubs ersetzt) und geben aus, was das Original berechnet bzw. wohin es
verzweigt (`zusammenarbeit.md` §5 Punkt 7). Urteil „Abweichung bestätigt“ oder „keine Abweichung“, Vorgabe für die
umsetzende Spur. Kein Produktcode.

Aufruf aller Belege: `python3 tools/reverse-engineering/abweichungen/<skript>.py [~/referenz/original/gamedata/em.exe]`
(ohne Pfad: `~/referenz/original/gamedata/em.exe`). Die Ausgaben unten sind wörtlich kopiert.

## Teil 1 – Bedienung/Anzeige

(in Arbeit)

## Teil 2 – Kern

(offen)
