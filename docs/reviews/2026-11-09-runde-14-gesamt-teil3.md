# Gesamtreview Runde 14 – Fortsetzung, Teil 3

## 1. Merge-Empfehlung

**Nein** für `b13046422b285404ad6ddbe7b5f1b18e22069eea` (Base `401efa7`); die geforderte Vollprüfung ist noch nicht abgeschlossen. Dieser Bericht ergänzt Teil 1 (#704) und Teil 2 (#705).

## 2. Befunde

### R14G3-01 (P3): Ranghistorien-Import kann entfallen, ohne dass die bestehenden Rangtests rot werden

`Import_RankSlotsMatchTable` ([OriginalImportRestTests.cs:134](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.Core.Tests/OriginalImportRestTests.cs#L134)) vergleicht für FILE03 berechnete Platzfelder mit der Tabelle. `Import_SeriesRecordsAndTableFields` prüft für FILE36 außerdem feste Werte. Beide bleiben **grün**, wenn in der Prüfkopie ausschließlich die Zuweisung `target.StoreRankSlot(...)` im HOK-Import ([OriginalImport.Rest.cs:318](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/src/EishockeyManager.Core/OriginalImport.Rest.cs#L318)) entfernt wird: Das vorherige Nachspielen liefert für diese Stände passende Werte.

**Gegenprobe:** An FILE03 im eingelesenen HOK-Objekt nur das niedrige Halbbyte des Tabellenfelds `0x828B` für Satz 0, Spieltag 3 von 9 auf 10 ändern und danach importieren. Ein zusätzlicher Test erwartet `StoredRankSlot(2) == 10`. Unveränderter Produktcode besteht; ohne die Zuweisung meldet er **9 statt 10**, während die beiden bestehenden Tests grün bleiben. Die Eingabe wird lediglich in der temporären Kopie im Speicher verändert; Originaldateien werden nicht verändert. Eine verdrehte Halbbyte-Regel macht den bestehenden `Import_RankSlotsMatchTable` zwar rot (I22), aber das Fehlen des Imports selbst wird von den beiden vorhandenen Vergleichen nicht erkannt. Ein Test mit abweichendem, gezielt gesetztem HOK-Wert würde den Anschluss absichern. Dies ist ein Testbefund; im geprüften Code ist die Zuweisung vorhanden.

### R14G3-02 (P3): Passive Mehrsaisonprobe kann ohne Saisonwechsel bestehen

[OriginalImportTests.cs:171](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.Core.Tests/OriginalImportTests.cs#L171) soll laut Testkommentar vier Saisonwechsel für aktive und passive Manager nachweisen. Die Prüfung `start + 4` gilt jedoch nur bei `active == true`; bei passiv genügt schon `Phase == GameOver`, auch ohne einen einzigen Wechsel.

**Gegenprobe Q01:** In der Prüfkopie `StartNextSeason()` bei `SeasonOver` sofort `Phase = GameOver` setzen und zurückkehren lassen. Beide passiven Fälle (FILE05 und FILE16A) bleiben **grün**, obwohl kein Saisonwechsel ausgeführt wird; die aktiven Fälle werden rot. Der verschärfte Test mit `start + 4` für alle vier Datenzeilen besteht am unveränderten Produktcode (4/4). Das ist eine Lücke des Mehrsaisonnachweises, kein Nachweis, dass der unveränderte Spielablauf vorzeitig endet. Vorschlag: Für jeden Fall vier tatsächlich erfolgte Wechsel fordern und gegebenenfalls Spielzeitbegrenzung in den Testdaten ausdrücklich aufheben.

### R14G3-03 (P3): Zu-kurz-Test prüft gleichzeitig eine falsche Kennung

[OriginalSaveHeaderTests.cs:58](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.Formats.Tests/OriginalSaveHeaderTests.cs#L58) verwendet für den kurzen Kopf ausschließlich `new byte[Length - 1]`. Dieses Bytefeld trägt keine gültige Kennung, sodass es bereits aus einem anderen Grund abgelehnt wird.

**Gegenprobe F06:** Mindestlänge im Leser von `Length` auf `Length - 1` senken. Der bestehende Test bleibt **grün**, obwohl ein gültig beginnender, um ein Byte verkürzter Kopf mit null Managern angenommen wird. Ein zusätzlicher Fall `Header(1993, 0, 1).AsSpan(0, Length - 1)` wird unter dieser Mutation **rot** und besteht mit unverändertem Produktcode. Der bestehende Code hat die korrekte Längenprüfung; der Befund betrifft deren unabhängige Testabdeckung.

## 3. Ausgeführte Prüfungen und offene Prüfbereiche

### Prüfstand

Die Produktkopien unter `/tmp/r14-teil2`, `/tmp/r14-rank` und `/tmp/r14-ui3` stammen aus dem genannten Head. Alle Mutationen liegen ausschließlich in diesen Kopien, einzeln und mit Rücksetzen; Originaldaten und Spielstände wie in Teil 1. SDK 10.0.402, Tests mit `EM_REQUIRE_GAMEDATA=1` und `EM_SAVEGAMES=/tmp/r14-referenz/original/spielstaende`. Der Standardlauf mit allen drei Ergebnistabellen wurde bereits in Teil 1 am gleichen unveränderten Head ausgeführt. Der unveränderte Speichern-Dialog-Testlauf ergab 16/0/0.

### Selbst nachgelesene Originalstellen

- `em.exe` Datei `0x5E252–0x5E266`: Anfang `si = 0`, je Schritt `si += 0x50`, Vergleich `si + 0x7A` gegen Maus-x und Sprung nur bei `<`. Dies bestätigt die Spaltengrenzen 122/123 und 202/203 für den Test.
- `0x5E3A2–0x5E3B5`: Werte 1, 169, 174, 169, 150 werden vor dem Pfeilpaar-Aufruf `0x120:0x3E` auf den Stack gelegt; das stützt den Aufruf `(150,169,174,169,1)` im Dialog.
- `0x5B92E–0x5B947`: Der Eintrag wird mit Abstand `0x28` adressiert; das Byte bei `bp-0xA7 + 0x28*i` wird geprüft, bei ungleich 0 verzweigt der Code hinter die Ligaausgabe. Die Identität als Kopfbyte +1 und die übrige Leserichtung sind noch gesondert zu prüfen.
- `0x071BB–0x071C8`: Vergleich von `es:[bx+0x13]` mit `0x38`; bei größerem Wert Addition `0xF6` (−10) und Rückschreiben. Die Remake-Untergrenze 50 ist eine gesonderte `[APPROX]`-Entscheidung, keine aus dieser Stelle belegte Originalregel.
- `0x5561A–0x55670`: zwei Aufrufe des Helfers `0:048B` mit 7 und dem Tageszähler bzw. Tageszähler + 3; die Rückgaben werden jeweils auf null geprüft, bevor der Aufruf bei `0x5565C` erfolgt. Anschließend erhöht `0x55670` den Zähler. Die Bedeutung des Helfers als Modulo und die Identität des Overlay-Aufrufs wurden hier nicht separat nachgelesen; alle übrigen WM-Zweige bleiben offen.
- `0x55204–0x55230`: Zähler +1, Managerindex auf 0, Vergleich mit Managerzahl und zwei Aufrufe pro Manager. Die Zuordnung der Overlay-Stubs zu den beiden Finanzfunktionen ist damit allein noch nicht geprüft.

### Testgegenproben

Jede folgende Zeile ist eine tatsächlich rot gewordene Testausführung; Datenzeilen sind einzeln aufgeführt. Die Kürzel bezeichnen jeweils eine isolierte Produktmutation. Ein roter Build zählt nicht als roter Test. Grüne Gegenproben und ihre Bedeutung stehen bei den Befunden.

### Einzelne Fingerabdrücke und Sollwerte: Rückmutation Sommerbeginn

V01 setzt in `GameState.SummerBreakCounters()` den Start von `SeasonEndCounter + 1` auf `SeasonEndCounter` zurück. Die acht unten einzeln aufgeführten, im Rundendelta geänderten Fingerabdrücke wurden jeweils im unveränderten Head-Lauf aus Teil 1 erreicht; unter V01 werden sie **jeweils rot** mit folgendem tatsächlichem Wert. Diese Gegenprobe belegt die Abhängigkeit vom Sommerbeginn, nicht, dass ausschließlich diese Regel alle Änderungen seit Base verursacht. Insbesondere wurde das entfernte `DebtDays`-Feld nicht zurückgebaut; die Werte müssen daher nicht den Base-Fingerabdrücken entsprechen.

| Testfall | Soll Head | Ist unter V01 |
|---|---|---|
| RNG, synthetisch, passiv, Saat 3 | `71C440ACDC165036` | `4098B11C4DF78C46` |
| RNG, synthetisch, aktiv, Saat 3 | `F647215D0B088A26` | `CB55B3EF90833AD7` |
| RNG, synthetisch, passiv, Saat 11 | `82605FA6E5525270` | `21968076D5AA07A6` |
| RNG, synthetisch, aktiv, Saat 11 | `3705607C914AE2C0` | `C45DB98750B9446A` |
| RNG, Originaldatenbank, passiv | `8A64633B9AF694CD` | `5161DE79EB94834E` |
| RNG, Originaldatenbank, aktiv | `18C9B0A5398CF96B` | `13646D28DB1942F2` |
| Spielerzähler, Originaldatenbank, passiv | `7643FD5C4D33C359` | `E9325BB01EF20CA8` |
| Spielerzähler, Originaldatenbank, aktiv | `3BFBFB82C212FDB8` | `E210C3DB7F41FC8C` |

Auch beide Jugend-Verläufe werden rot: passiv unterscheidet sich unter anderem Saison 1, Schülerstärke `52 → 57`; aktiv `67 → 71` und Ligaplatz `2/6 → 1/11`. Der Testläufer kürzt diese langen Zeichenketten im Fehlertext; eine Rückführung jedes einzelnen Zahlenbestandteils ist damit noch nicht erbracht. Die beiden Saaten 3 und 11 sind durch die vier synthetischen Zeilen jeweils separat gegen den geänderten Sommerbeginn geprüft, nicht gegen jede andere Regel des Rundendeltas.

### Einzelne Grenzwerte des Speichern-Dialogs

Die Sollwerte `Marker_ColumnBoundaries_LikeTheOriginal` sind einzeln gegen die in `DrawMarker` implementierte Treffer- und Spaltenregel geprüft. Der unveränderte Lauf bestand alle sechs Datenzeilen. Bei jeder folgenden Rückmutation wurde die genannte Datenzeile rot; andere Datenzeilen konnten grün bleiben und werden nicht als Beleg für diese Zeile verwendet.

| Maus-x | Soll-Spalte | Rückmutation | Ergebnis |
|---:|---:|---|---|
| 43 | 0 | U13: untere Treffergrenze von `x <= 42` auf `x <= 43` | rot: Marker fehlt |
| 122 | 0 | U05: Spaltenschleife von `< x` auf `<= x` | rot: vorzeitiger Spaltenwechsel |
| 123 | 1 | U12: Spaltenschwelle `0x7A` auf `0x7B` | rot: Spaltenwechsel zu spät |
| 202 | 1 | U05: Spaltenschleife von `< x` auf `<= x` | rot: vorzeitiger Spaltenwechsel |
| 203 | 2 | U12: Spaltenschwelle `0x7A` auf `0x7B` | rot: Spaltenwechsel zu spät |
| 265 | 2 | U14: obere Treffergrenze von `x >= 266` auf `x >= 265` | rot: Marker fehlt |

Die Grenzen 122/123 und 202/203 folgen dem oben selbst gelesenen Originalvergleich `0x5E252–0x5E266`. Die äußeren Grenzen folgen `0x5E221–0x5E231`: Vergleich x > 42 und x < 266; diese Stelle wurde ebenfalls selbst in `em.exe` gelesen.

V02 setzt ausschließlich `TransferMarket.StartSeason` in der Prüfkopie auf die Implementierung von Base `401efa7` zurück. `FourSeasonChanges_WorldOnly` wird rot und liefert den früheren, im Diff entfernten Wert `WorldChampion=52|500000|WorldChampion=52|500000|WorldChampion=54|500000|WorldChampion=53|500000|WorldChampion=63|500000|R982137502 C211999367` statt des neuen Werts `WorldChampion=52|500000|WorldChampion=52|500000|WorldChampion=52|500000|WorldChampion=54|500000|WorldChampion=54|500000|R1374974225 C782029941`. Damit ist diese vollständige Änderung auf die geänderte Behandlung der Transferliste beim Saisonwechsel zurückgeführt. Dieselbe Rückmutation macht die beiden Listen-Erhaltungstests und `Mode4_ReferenceValues_Unchanged` rot; für letzteres wird der vollständige veränderte Wert gesondert protokolliert.

### Entscheidungen L5 und L6

| Entscheidung | Code- und Belegprüfung |
|---|---|
| L5: WM-Sperre nur im Bundestrainer-Menü | `NationalMenu.IsLocked` verwendet `NationalSquadLocked` für „Transfer“ und „Training“ nur innerhalb `game.NationalCoachMenu`; außerhalb werden die WM-Stufe und dieser Merker nicht herangezogen. `rg` findet keine weiteren Leser von `NationalSquadLocked`. Selbst gelesen: `0x44FBD–0x44FCB` setzt den lokalen Menütyp aus `0xA70E == 5`; `0x4504E–0x450A4` verzweigt danach zwischen Vereins- und Bundestrainerzweig und schreibt nur im letzteren `0x7AE` in `0x518C/0x518D`. Die Mutation des Bundestrainerzweigs und dessen Testergebnis werden gesondert protokolliert. |
| L6: Speichern-Dialog A338/A339 | `SaveLoadScreen.Update` blättert über `ArrowPair`, nimmt dafür keine Rad-/Pfeiltasten und verwirft die Info nach dem Loslassen; `DrawPanel` enthält kein Seitenlabel. Der unveränderte Dialogtestsatz besteht 16/16. U01–U16 prüfen die betroffenen Tests und alle sechs Spaltengrenzen; `0x5E221–0x5E266` und `0x5E3A2–0x5E3B5` wurden selbst gelesen. `0x5B9A8–0x5B9C5` wiederholt die Eingabeabfrage bis Ereignis 2 oder 4; die Bedeutung der Ereigniscodes wurde an dieser Stelle nicht erneut selbst aufgelöst. Die Entscheidung ist im geprüften Code umgesetzt; die Vollprüfung sämtlicher Originalstellen und aller visuellen Details bleibt gesondert offen. |


### Protokollverlust bei den Einzelgegenproben

Die temporäre Prüfumgebung wurde zwischen den Sitzungen ersetzt; `/tmp/r14-inventur` mit den Einzelprotokollen und Mutationsskripten ist nicht mehr vorhanden. Daher kann die geplante vollständige Ergebnistabelle nicht aus diesen Protokollen rekonstruiert werden. Die oben konkret dokumentierten Gegenproben und Werte bleiben nachvollziehbar; weitere Gegenproben müssen für eine vollständige Tabelle erneut ausgeführt und dauerhaft abgelegt werden.

### Offene Prüfbereiche

- Wegen des Verlusts der temporären Einzelprotokolle müssen weitere Einzelgegenproben erneut ausgeführt und dauerhaft dokumentiert werden.
- Die in Teil 2 inventarisierten 334 neuen oder geänderten Testmethoden sind noch nicht alle einzeln durch eine gültige Mutation rot nachgewiesen. Änderungen an Testhilfen und Datenquellen müssen zusätzlich vollständig zugeordnet werden. Die obige Tabelle ergänzt die Ergebnisse aus Teil 2.
- Nicht jeder neue oder geänderte Sollwert, Fingerabdruck und jede geänderte Saat ist einzeln auf eine Regel zurückgeführt. Insbesondere sind die langen Jugend- und Modus-4-Referenzen weiter aufzuschlüsseln; mehrere Regeländerungen wirken zusammen. V09 und V10 verändern den Pokalsieger der ersten Saison, der geänderte Weltmeister der ersten Saison ist noch nicht kausal isoliert.
- Nicht alle Entscheidungen aus Rundenliste und PR-Kommentaren sind einzeln gegen den gesamten betroffenen Code geprüft. L5 und L6 sind oben teilweise belegt; die restlichen Entscheidungen bleiben offen.
- Nicht alle neu hinzugekommenen [ORIGINAL]-Fundstellen wurden selbst in em.exe oder Spielständen nachgelesen. Oben stehen die zusätzlich tatsächlich gelesenen Stellen.
- Nicht jedes Bildschirm- und Hauptmenüereignis ist nach AGENTS.md § 4 geprüft; insbesondere fehlen die zwei Saisonende-Bildschirme in der dynamischen Öffnungsprüfung aus Teil 2 sowie die vollständige Ereignisprüfung.

Diese offenen Bereiche sind noch nicht bearbeitet beziehungsweise nicht vollständig belegt; deshalb bleibt die Merge-Empfehlung **Nein**. Eine Fortsetzung muss auf diesem Prüfstand aufbauen.
