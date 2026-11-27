# Werkzeuge zu `../belastung-staerke.md`

Nur Auswertung, keine Originaldaten: Die Skripte lesen Spielstände aus `~/referenz/original/spielstaende` (Referenz-Repo)
und schreiben nach `$OUT` (Vorgabe `/tmp/belastung-staerke`, nie ins Repo). Voraussetzung: `source ~/claude-tools/env.sh`,
`dotnet build src/EishockeyManager.Tools -c Release`, Python 3.

| Datei | Zweck |
|---|---|
| `dump.sh FILE02 FILE03 …` | `emtool hok dump` der Blöcke Kaderplätze (5DC3), Vereine (far:9657), Pool (far:8253) nach `$OUT/<f>.blk` |
| `hok.py` | Leser für diese Dumps (Kaderplatz 0x34 Byte, Verein 0x3D Byte, Pool 33 Byte) |
| `clubs.py FILE03 …` | Vereinsfelder 0x26/0x2D/0x2C der Computervereine (Abschnitt 4) |
| `strain.sh A B "KÖLNER EC" <saaten>` | `emtool abgleich --alle`, Belastung je Spieler Original/Remake je Saat |
| `pair.py A B <strain-datei> [b]` | Belastungszuwachs je Reihe (Reihen aus a bzw. mit `b` aus b), Original gegen Verteilung der Saaten |
| `hits.py "A B" …` | Runde 19: Treffer (Zuwachs + Erholung) je Reihe und Spieler, Original gegen alle Saaten in `$OUT/s_A_B.txt` (Ausgabe von `strain.sh`), z-Werte; bricht ab, wenn ein Spieler im Remake fehlt |
| `lrsim.py` | Runde 19: Managerteil von 0x87F7 mit der echten lrandom-Folge gegen unabhängige Züge (Treffer je Spiel und Spieler) |
| `trace.patch` | **temporäre** Ausgabe je Minute der Managerpaarung (`EM_TRACE=<datei>`), Zwangsmodus `EM_FORCE_MODE=1…3`; nur lokal anwenden (`git apply`), nie committen |
| `run.sh A B tag <saaten>` | Läufe mit `trace.patch` nach `$OUT/tr/<tag>_<saat>.txt` |
| `res.py tag HAH` / `lines.py tag HAH 3` | Ergebnisse bzw. Eiszeit je Reihe und Modus der letzten drei Managerspiele (H/A = Managerverein Heim/Gast) |
| `writers.sh` | Teil 1c: schreibende Befehle auf Reihe/Modus/Minuten (0x2C, 0x2E–0x31) und Kaderplatz +0x15 in em.exe (ndisasm), Blöcke A701/CC43 der Stände (`FILES="…"`) |
