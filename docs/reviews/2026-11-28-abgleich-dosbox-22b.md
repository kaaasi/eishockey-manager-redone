# Review PR #1083 – abgleich-dosbox-22b (Erstreview, unbeaufsichtigter Lauf)

Base `920a5c2`, Head `7df8104` (`git rev-parse HEAD` = Head, `git status --porcelain` leer). Reviewer: Claude (frische Sitzung, nicht an
der Spur beteiligt). Nur Doku/Python, kein Produktcode (`git diff --stat 920a5c2 7df8104 -- src tests` leer).

## 1. Merge-Empfehlung

**Ja** für `7df8104`. Alle Skripte reproduzieren die Berichtsausgaben, die Klassen stützen sich auf Zustandsvergleiche der Stände
und Nachbildungen über die Bytes von `em.exe`. Nur P3 (Wortlaut/Verweis), kein Befund blockiert. Einschränkung: Die Remake-Seite der
Proben (Saaten 1–8, FILE01/FILE21, `PlayFriendly`) konnte hier nicht laufen (kein `dotnet` im Lauf, Netz gesperrt) – nur Codelesen.

## 2. Befunde

| ID | Prio | Befund | Beleg |
|---|---|---|---|
| P3-1 | P3 | Bericht [Z. 283](https://github.com/kaaasi/eishockey-manager-redone/blob/7df8104/docs/analyse/abgleich-dosbox-22b.md#L283) nennt „A923, FILE75 in `archiv/analyse/abgleich-dosbox-22.md`“. Dort steht A923 nicht (der Eintrag „Computervereine führen keinen Kader“ ist A427); A923 ist in der CSV eine eigene E-Zeile. Die CSV-Zeile A259 zitiert korrekt A427. | `grep -c A923 docs/archiv/analyse/abgleich-dosbox-22.md` → 0; `grep -n "^A427\|^A923" docs/analyse/restliste-v1-klassen.csv` |
| P3-2 | P3 | A408-Klassentext, Bericht Z. 72–75 und Issue #1084 schreiben „Aufstellung 0x426BC … für alle 25 Plätze“. Das Skript zeigt 0x426BC 26× mit erster Stelle 0x07548 (Vertragsteil), die Aufstellung ist ein Aufruf (Datei 0x07C07, Bericht Z. 61 selbst). Belegt ist die Aufstellungsautomatik nur durch den Zustandsvergleich (`+0x0A`/`+0x15`, 4 Wechsel in FILE19 → FILE20); 0x426BC endet ohne „automatisch aufstellen“ sofort (A068), die Nachbildung prüft das Flag nicht. Klasse V bleibt tragfähig (Heilung 25×, Training 25×, Verletzte in FILE18 → FILE48). | `python3 -I …/a408_tageslauf_modus1.py` → „0x426bc 26x, erste Stelle 0x07548“, „Aufruffolge gleich: True“ |
| P3-3 | P3 | Bericht Z. 102f. „Listenwechsel im Modus 1 kann aus 25 Plätzen ziehen“ steht in Spannung zu A707 (Z. 366–369: Modus 1 überspringt den Hauptweg, 0x494EF nur beim Spielstart 0x43A9F). Gemeint ist offenbar der Start-Listenwechsel; für Issue #1085 präzisieren („beim Spielstart“). Die Probe selbst (7 von 8 Saaten) ist nicht reproduziert. | Bericht Z. 102–103 vs. 366–369 |

Keine P0–P2. Die Klassen sind je ID gedeckt (siehe 3).

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` entfällt (Auftrag). Stände und `em.exe` aus dem Referenz-Repo (`em.exe` SHA-256 = `SHA256SUMS`), Aufruf `python3 -I`:

| Prüfung | Ergebnis |
|---|---|
| `a408_tageslauf_modus1.py` | Rückgabe 0; Modus 1 und erzwungen 0: je 683 Aufrufe, „Aufruffolge gleich: True“; Training/Heilung je 25× |
| `kaderdiff.py FILE18 FILE48 0` / `FILE19 FILE20 0` | Satz 0 (Tag 217 → 219 / 231): Platz 17 verletzt (`+09` 0→2, `+17` 0→18), Platz 12 eingesetzt, Platz 16 `+15` 0→131; 4 Aufstellungswechsel, `+13` 13 Plätze −1/−2 – wie im Bericht |
| `kaderdiff.py FILE80 FILE81 0` / `5` | Tag 217 in beiden Ständen; Satz 5: 16 Gespielte `+08`/`+20` +1, Tore `+03`/`+22` bei 5 Plätzen, `+06`/`+1C` unverändert – A918 K gedeckt |
| `a411_pool_satz0.py` | FILE18/19/54/80: Satz 0 auf Pool 2–3, 22–31, 98–110, Besitzer {0: 25}, Markt 7/7/6/7 – wie im Bericht |
| `a899_tor.py` | alle 20 Fälle wie Tabelle; „im Spiel rechts Verteidiger Tor leer: aufgestellt: Reihe 4 Zelle 4“, Torhüter → Reihe 0 Zelle 3 |
| `a899_zaehler.py FILE89 FILE90` | MAYR Pokal 0→1, Verein Pokal 0→1, `+0x15` 0→1, Bit 7 0→1; HEISS nur `+0x0A` 1→0 (A899 X; A678 V gedeckt durch FILE90 „TW 4“) |
| `a599_mogelguru.py … 4` | Rahmen (0,0)–(319,53)/(1,1)/(2,2), Farbe 0x12 ↔ 0, Pause 6 Takte; „Ausgang 0x5d03c nein“, Schleifenende `eb c2` – A599 Endlosschleife belegt |
| `a259_annahme.py` | Ausgabe deckt den Berichtsauszug (Verkauf bei Besitz + Gebotsbit, keine Kaderprüfung; Pool +0x1A = 4 → kein Verkauf) |
| `a707_grundstaerken.py` | Teil A/B wie Bericht (Ersatzverein 24 bei +0x16 = 6; Satz 54: +0x15 81/85, +0x16 92/84) |
| `python3 tools/reverse-engineering/markerliste.py` | Rückgabe 0, `git status` danach sauber; V 3 (A408, A411, A678), D 15 (vorher 25), E 274, K 194, X 2, unklassifiziert 0 |
| `funktionskarte.py em.exe --csv` Base vs. Head | Spalten 1–5 (Adresse, Status …) identisch: umgesetzt 339, dokumentiert 110, genähert 3, unbekannt 227. Nur die „dokumentiert:“-Zeilenverweise (z. B. `restliste-v1.md:253` statt `:244`) wandern erwartungsgemäß |
| Klassen CSV (Python-Zählung) | V: A408, A411, A678 neu; D: genau 15 inkl. A629 (Leihende, offen) |

**Sitzung-1-IDs** gegen `docs/archiv/analyse/abgleich-dosbox-22.md`: A057 X ✓ (Zugfolge, FILE76), A065 X ✓ (Archiv V, umgesetzt, Test `File75_HighscoreBeforeFirstMatchDay…`), A117 K ✓ (Archiv K), A402 K ✓ (Archiv V S umgesetzt; Summenregel „nur gerechnet“ bleibt als Hinweis), A427 K ✓, A613 X ✓, A614 X ✓ (Rest „Folge“ ist als `[APPROX]` mit eigener ID A966 K geführt), A629 bleibt D, Export-Teil ergänzt ✓.

**`dosbox-aufnahme.md`:** Hunks nur bei Sitzung 2 Nr. 3 (S04-Fotos), Sitzung 3 Nr. 3 (A259 gestrichen), Sitzung 4 Nr. 2 (A707 gestrichen), Sitzung 6, Sitzung 7 Nr. 4/5/6-Teil-1. Sitzung 5 unverändert; Sitzung 7 Nr. 6 Teil 2 (FILE92/FILE93, A671) im Wortlaut unverändert, nur der Zeilenumbruch hinter dem Teil-1-Zusatz wandert. A259/A707/A910 gestrichen wie verlangt.

**P3 #1078:** P3-1 (alte Doku-Pfade A754/A002/F018 → `archiv/analyse/…`) in der CSV ✓; P3-2 `inventur.md` nennt jetzt `Strain_DeviatesAtMostSeeds_DriftGuard`, Methode existiert (`OriginalPairsTests.cs`, `grep`) ✓; P3-3 `transfer.md` Migrationshinweis ✓ (Datei `…TransferVertraegeKomplett5.cs` gibt es nicht mehr, `ls src/EishockeyManager.Core | grep -i Migration` leer).

**Erwartet offen (bestätigt):** #1084, #1085, #1086 existieren, offen, Label `befund`. A599 als E-Vorschlag, Entscheidung Nutzer offen – im Bericht und in der CSV so geführt.

**Akzeptierte `[APPROX]`:** keine neuen; Marker von A259, A707, A918 unverändert im Code (Dateigrenze). **Annahmen (nicht belegt):** Remake-Proben des Berichts (Saaten 1–8, FILE01/FILE21-Länderwerte, `PlayFriendly`-Zähler, Export Satz 5 ohne Zähler) – nur Codelesen, `EuropeGames` wird vom Export in `OriginalExport.Squads.cs:246–251` gelesen, für Satz 5 nicht nachgeprüft.
