# Gesamtreview Runde 14 – Teil 6 (Abschluss)

## 1. Merge-Empfehlung

**Ja** für das geprüfte Rundendelta `401efa7` → `b13046422b285404ad6ddbe7b5f1b18e22069eea`, im risikoorientierten Zuschnitt der Nutzerentscheidung C vom 10.11.2026. In den gezielt geprüften Übergängen wurde kein neuer Produktfehler nachgewiesen. Die erwarteten offenen Punkte und die unten genannten Unsicherheiten bleiben bestehen; die Empfehlung ist keine Zusicherung vollständiger Originaltreue. `main` ist inzwischen weiter: Dies ist keine Prüfung seines späteren Deltas. Runde 14 ist mit diesem Teil abgeschlossen.

## 2. Befunde

**Keine neuen reproduzierten Produktfehler.** Die erwarteten offenen Issues [#620](https://github.com/kaaasi/eishockey-manager-redone/issues/620) (Rest), [#698](https://github.com/kaaasi/eishockey-manager-redone/issues/698), [#699](https://github.com/kaaasi/eishockey-manager-redone/issues/699) und [#703](https://github.com/kaaasi/eishockey-manager-redone/issues/703) sowie [#712](https://github.com/kaaasi/eishockey-manager-redone/issues/712) sind laut GitHub offen. Die bekannte `[APPROX]`-Differenz beim April-Abschluss beträgt 115.000 DM. Diese Punkte wurden auftragsgemäß nur bestätigt, nicht neu bewertet.

**Testlücken, keine nachgewiesenen Produktfehler:** Die P3-Befunde R14G2-01/02, R14G3-01–03, R14G4-01–08 und R14G5-01 aus [Teilen 2–5](https://github.com/kaaasi/eishockey-manager-redone/blob/c0894ff/docs/reviews/2026-11-09-runde-14-gesamt-teil5.md) bleiben offen. Insbesondere sichern die betroffenen Tests Ranghistorienimport, vier tatsächlich erfolgte Saisonwechsel und spätere WM-Termine nicht vollständig ab. Die dort dokumentierten Mutationen sind keine Fehler des unveränderten Produkts.

## 3. Ausgeführte Prüfungen und `[APPROX]`

**Prüfstand:** Unabhängige Sitzung, sauberer abgesetzter Git-Checkout exakt `b13046422b285404ad6ddbe7b5f1b18e22069eea`, SDK 10.0.402; Originaldaten und Spielstände aus Referenzcommit `779a1c8af8597adf51a8538d87cfb5a228179f79`, `em.exe`-Blob `88b1039b9ef9b5d952c1c7ac85368fc58ac91803`. Die Prüfkopie für den exakten Kontowert änderte nur die Testassertion, nicht den Produktcode. Originaldaten wurden nicht übernommen.

**Gegenbeispiele erneut ausgeführt:** `EM_REQUIRE_GAMEDATA=1 EM_SAVEGAMES=… dotnet test tests/EishockeyManager.Core.Tests -c Release --no-build --filter 'FullyQualifiedName~PlayoffSemifinalTests|FullyQualifiedName~TagestaktSommerpauseReviewTests|FullyQualifiedName~File44_ToNewSeason_AccountFollowsFile46|FullyQualifiedName~LoadQuirkTests'`: 26 bestanden, 0 fehlgeschlagen, 0 übersprungen.

- R688S-01: Normale Sommerpause 1. Mai bis 9. September, vier Monatsabschlüsse; kein regulärer Tagesabschluss am 30. April. Ausnahme: Nach Import des am Markierungstag gespeicherten FILE44 führt der Ladeversatz zum Aprilabschluss am 30. April.
- R688S-03: Die Schuldenfälle bleiben im Spiel, der Zwangsabstieg tritt ein. Im Kern keine Zuweisung `Dismissed = true` gefunden; alte gesetzte Kennzeichen bleiben bei Migration erhalten.
- FILE44 → FILE46: Original 18.027.183 → 15.887.669 DM, **−2.139.514 DM**. Zusätzliche exakte Assertion in einer Kopie: Remake **−2.106.011 DM**, 1/1 bestanden; Differenz 33.503 DM. Die Einzelursachen sind nicht vollständig belegt und die 115.000 DM Aprilabweichung wird teilweise durch andere Abweichungen kompensiert.
- R683: Halbfinale Sieger 1–8 gegen Sieger 3–6, Sieger 2–7 gegen Sieger 4–5; Import und Migration sind im Testfilter enthalten.

**Entscheidungen gegen den Code:**

| Entscheidung | Geprüfter Anschluss und Grenze |
|---|---|
| R14-D1 A, D3 A; E14-2 A, E14-3 A | Spur- und Aufnahmezuordnung in `arbeitspakete.md` und `re-runde-14.md`; vorliegende Teile und Integrationen sind dokumentiert. Das vorliegende Abschlussreview folgt der späteren Nutzerentscheidung C, nicht der ursprünglich verlangten Vollinventur. Modellwahl und tatsächliche Sitzungsidentität lassen sich aus dem Produktcode nicht verifizieren. |
| E14-1 A+C, R14-D2 B, E14-4 A | HOK-Laden über `SaveLoadScreen`/`OriginalImport`, Feldkarte und Restimporte vorhanden; Export zurückgestellt. Einzelne unklare Felder bleiben `[APPROX]` bzw. in #699; keine vollständige Feldinventur in diesem Teil. |
| R14-D4 A | `ManagerDayMinuteLoop` ist standardmäßig aus, im Optionsbildschirm schaltbar; `SeasonFlow` setzt den Spieltagsmerker nur bei aktiver Option und Managerpaarung. |
| E14-5 C | Keine angeforderte Nachaufnahme; verbleibende unbelegte Teilaspekte sind Näherungen, etwa A115 Managerleihe, A189 Sperre und A357 Europapokal. Bereits anderweitig belegte Teilaspekte sind getrennt markiert. |
| L1–L4 | Ein-Saison-/Ein-Manager-Sonderfall und Startliga-Filter in `NewGame`; Kader-/Tabellenfelder und Migration, Aufstellung und Taktik im Kern angeschlossen. Teil 4 enthält gezielte Gegenproben; nicht jede Bedienkombination wurde erneut geprüft. |
| L5–L7, E14-6 A | WM-Sperre für Transfer/Training nur im Bundestrainer-Menü (`NationalMenu`); Speicherdialog einschließlich des zusätzlich beauftragten Laufs und A012-Anschluss vorhanden, Dateigrenzen und Feldkarte dokumentiert. Dialog-Gegenproben in Teil 3; kein vollständiger Pixelvergleich hier. |
| L8–L10 | A009 über Zug- und Hauptmenüpfad im Kern; A362 als privates Bildschirmabbild; A115 unterscheidet Manager- und Computerverleiher, Managerleihe bleibt mangels Stand `[APPROX]`. |
| Keine Schuldenentlassung; Markierungstag C | Schuldengrenze führt zum Zwangsabstieg statt Entlassung. `SavedOnMarkerDay` erkennt FILE22/FILE44 und `ReopenMarkerDayAfterLoad` setzt Versatz und Tagesabschluss; andere solche Markierungstage werden ausdrücklich abgelehnt. Schema 44→45 ergänzt `DayOpened`, 45→46 entfernt `DebtDays`, 46→47 ergänzt Versatz 0; ältere bereits importierte Stände werden nicht rückwirkend korrigiert. |

**Originalbelege gezielt nachgelesen:** `objdump -D -b binary -m i8086 -Mintel` auf `em.exe`: `0x54D41–0x54D65` liest das Tagesbyte und verzweigt bei 0/255; `0x26F55` setzt Zähler 233, `0x564BC` erhöht vor der Sommer-Schleife, `0x56566–0x56595` zählt und prüft die Abbruchwerte. `0x555DD–0x555ED` prüft WM-Modus und Tagesbyte; `0x071BB–0x071CA` reduziert Marktbelastung über 56 um 10. `0x31BED–0x31C07` verwendet die Tabelle bei DGROUP `0x144` (Datei `0x16F14`, Beginn `09 18 0B 18 0A 18 0C 18`); `0x5494C–0x5499E` wählt Wettbewerbsindizes und erhöht Spielerzähler; `0x275FF` setzt das Besitzerbyte auf 6. Diese Ausschnitte stützen die jeweiligen Teilbehauptungen, nicht allein die vollständigen Aufrufketten. Für die Konten wurden die Originalstände über den Importtest gelesen; weitere Nachweise in Teilen 1–4.

**Akzeptierte `[APPROX]` und Annahmen:** Neben dem ausdrücklich erwarteten Aprilbetrag bleiben die im Code markierten Näherungen, insbesondere Heilung nicht verwalteter Spieler, Mindestbelastung 50 und WM-Tageslauf bei Bundestrainer sowie Managerleihe, bestehen. Akzeptiert für diese Empfehlung im Sinne des risikoorientierten Zuschnitts, nicht als Originalbeweis. Annahme: Die Referenzstände und die dokumentierte Aufnahmefolge bilden den angegebenen Originalablauf ab. Die Einzelprüfung aller Originalstellen, Sollwerte und Bildschirmzustände wurde nach Entscheidung C nicht vorgenommen; #698 bleibt eine relevante Unsicherheit für passive Langzeitspiele.

**Standardprüfung:** `PATH=/workspace/.dotnet:$PATH NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=2 EM_SAVEGAMES=… tools/pruefen.sh` auf dem sauberen Head: Build und Gesamtergebnis bestanden; NuGet-Audit nur für diesen Lauf deaktiviert.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten | 2625 | 0 | 0 |
| Ohne Originaldaten | 2076 | 0 | 452 |
| Pflichtlauf ohne Daten (wie gewollt) | 2092 | 533 | 0 |
