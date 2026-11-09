# Gesamtreview Runde 14 – Zwischenstand

## 1. Merge-Empfehlung

**Nein** für Head `b13046422b285404ad6ddbe7b5f1b18e22069eea` (Base `401efa7`). Die verlangte Vollprüfung ist in dieser Sitzung nicht vollständig durchgeführt; die offenen Prüfbereiche unten schließen eine Freigabe ausdrücklich aus. Dies ist ein Zwischenbericht, keine Feststellung, dass die nicht geprüften Bereiche fehlerfrei sind. Das Delta umfasst 256 Dateien, darunter 104 Testdateien mit 9.191 hinzugefügten Zeilen. Eine Fortsetzung muss die noch fehlenden Einzelprüfungen ausführen und als neue Datei `docs/reviews/2026-11-09-runde-14-gesamt-teil2.md` auf einem neuen Branch ablegen.

## 2. Befunde

**Noch keine neuen reproduzierten Produktbefunde.** Die hier erfolgten Prüfungen reichen nicht aus, um weitere Befunde auszuschließen. `git diff --check 401efa7 b130464` meldet ein nachgestelltes Leerzeichen in einer älteren Reviewdatei; kein Produktbefund.

Erwartet offen: GitHub-Suche nach offenen Issues bestätigt [#620](https://github.com/kaaasi/eishockey-manager-redone/issues/620) (Rest), [#698](https://github.com/kaaasi/eishockey-manager-redone/issues/698), [#699](https://github.com/kaaasi/eishockey-manager-redone/issues/699) und [#703](https://github.com/kaaasi/eishockey-manager-redone/issues/703). Der `[APPROX]` April-Abschluss mit 115.000 DM Abweichung ist im [Testkommentar](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.Core.Tests/TagestaktSommerpauseReviewTests.cs#L63) als offen dokumentiert; Das Referenzbild `file46_2` zeigt 17.523.876 DM und 100.000 DM Prämie; die Kategoriendifferenz und der genaue Remake-Aprilbetrag wurden hier nicht unabhängig zerlegt.

## 3. Ausgeführte Prüfungen und offene Prüfbereiche

### Prüfstand und Gegenbeispiele

- Frische unabhängige Sitzung; `git fetch origin`, anschließend sauberer Checkout `b13046422b285404ad6ddbe7b5f1b18e22069eea`, `git status --porcelain` leer. `origin/main` entsprach beim Abruf dem Head. SDK `/workspace/.dotnet/dotnet --version`: `10.0.402`. `gamedata` verweist auf lokale Originaldaten; die aktuelle Referenz `origin/main` wurde nach `/tmp/r14-referenz/original/spielstaende` gelesen, `EM_SAVEGAMES` darauf gesetzt. Keine Originaldateien im Commit.
- Vor dem Nachladen fehlte FILE44 in der älteren lokalen Referenzkopie; der erste gezielte Lauf scheiterte daher mit neun `FileNotFoundException` und ist **kein Produktbefund**. Nach Bereitstellung der aktuellen Spielstände Wiederholung am sauberen Head: `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests -c Release --no-build --filter 'FullyQualifiedName~PlayoffSemifinalTests|FullyQualifiedName~TagestaktSommerpauseReviewTests|FullyQualifiedName~File44_ToNewSeason_AccountFollowsFile46'`: **20 bestanden, 0 fehlgeschlagen, 0 übersprungen**.
- R683: Der synthetische Test zeigt Halbfinale Sieger 1–8 gegen Sieger 3–6 sowie Sieger 2–7 gegen Sieger 4–5; auch die Tests für Import und Schema-43-Migration wurden im gefilterten Lauf ausgeführt. R688S-01: Sommerzähler 234–365 entsprechen 1. Mai bis 9. September, vier Monatsabschlüsse. Zusätzlich bestätigte ein temporärer Test in der unveränderten Produktkopie bei einem normalen Neuspiel nach `Continue()` bis zum Saisonende `FinanceDay` = 29. April und `LoadCounterOffset` = 0 (1 bestanden). R688S-03: Schuldenläufe einschließlich vier Saisonwechseln und Zwangsabstieg grün; das ist keine vollständige Widerlegung aller möglichen Entlassungspfade.
- FILE44 → FILE46: Originalstände 18.027.183 → 15.887.669 DM, Differenz **−2.139.514 DM**. In einer isolierten Kopie wurde die Toleranzbehauptung des Tests durch `Assert.Equal(-2_106_011, me.Money - start)` ersetzt: **1 bestanden**, Saat 4711. Remake also **−2.106.011 DM**; Differenz zum Original 33.503 DM. Der Testausgang ist kein Nachweis für die einzelnen Buchungskategorien.
- Das Referenzbild `file46_2` wurde selbst angesehen: Saisonende 30. April, Konto 17.523.876 DM, Prämie 100.000 DM. Gegen FILE44 ergibt dies −603.307 DM vor den weiteren Saisonwechselbuchungen.
- em.exe selbst: `objdump -D -b binary -m i8086 -Mintel` ab Datei `0x26F55` ergibt `mov word ptr ds:0x3b7,0xe9`, ab `0x564BC` `add word ptr ds:0x3b7,0x1`. Diese zwei Einzelstellen stützen das Setzen von 233 und das Weiterzählen auf 234; sie belegen für sich genommen weder die ganze Schleife noch alle neuen Fundstellen. Das projektinterne `disasm.py` scheiterte, weil `ndisasm` fehlt; `objdump` ist für einzelne bekannte Instruktionsgrenzen verfügbar.

### Gegenproben neuer und geänderter Tests

Grüne Originalläufe und die folgenden gezielten Rückmutationen liefen getrennt vom unveränderten Prüfstand in `/tmp/r14-pruefkopie`. Die ersten zwei Mutationen greifen in verschiedene Funktionen ein; die dritte wurde nach Rücksetzen dieser Änderungen separat ausgeführt. **Diese Tabelle ist ein Teilstand, nicht die geforderte vollständige Inventur.**

| Test | Gegenprobe in der Kopie | Ergebnis |
|---|---|---|
| `PlayoffSemifinalTests.Playoffs_NextRound_PairsByTable0x144_HomeAtSmallerSlot` | Paarung `k` gegen `k + half` zu `2*k` gegen `2*k + 1` | Rot: erwartet `(108,103)`, erhalten `(108,102)`; Head grün |
| `TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | Sommerbeginn `SeasonEndCounter + 1` zu `SeasonEndCounter` | Rot: erwartet 234, erhalten 233; Head grün |
| `LoadQuirkTests.File44_AfterImport_SecondDayChange_FifteenthOfApril` | `LoadCounterOffset = 1` zu `0` | Rot: erwartet 1, erhalten 0; Head grün (separater Lauf) |
| `TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | dieselbe Mutation, in der Kopie zusätzlich auf den exakten Wert geprüft | Rot: erwartet −2.106.011, erhalten −1.623.204 DM; Head grün. Auch die ursprüngliche ±100.000-DM-Spanne wäre verfehlt. |

**Offener Prüfbereich – Testvollständigkeit:** Alle übrigen neuen oder geänderten Tests einschließlich geänderter bestehender Tests und Theorie-Datenzeilen müssen aus dem Gesamtdiff einzeln inventarisiert, grün ausgeführt und je Test per Base oder gezielter Mutation rot gemacht und tabelliert werden. Die grobe Diffzählung liefert 263 hinzugefügte öffentliche Testmethoden-Signaturen, erfasst aber geänderte bestehende Methoden, Hilfsmethoden und Attribute nicht zuverlässig und ist keine vollständige Testliste. Grund: Umfang der Runde, in dieser Sitzung nicht vollständig abgearbeitet.

**Offener Prüfbereich – Sollwerte, Fingerabdrücke und Saaten:** Die geforderte vollständige Einzelinventur und Rückmutation jeder verursachenden Regel fehlen. Der genaue FILE44-Kontowert oben ist eine Ergebniskontrolle, keine abgeschlossene Einzelrückführung sämtlicher Werte. Grund: Umfang und noch nicht ausgeführte Mutationen.

### Entscheidungen

| Entscheidung | Ergebnis dieser Sitzung |
|---|---|
| Nutzer 09.11.: keine Entlassung wegen Schulden; Zwangsabstieg | Gefilterte Tests `DeepDebts_NoDismissal_GameContinues` und `PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead` für beide Saaten grün; vollständige Prüfung aller Aufrufer und Originalstellen offen. |
| Nutzer 09.11.: Ladeeigenheit des Markierungstags, C | [Import](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/src/EishockeyManager.Core/OriginalImport.cs#L89) erkennt den Versatz; [Spielkern](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/src/EishockeyManager.Core/GameState.LoadQuirk.cs#L48) setzt ihn und führt den Tagesabschluss aus. FILE44-Gegenbeispiel grün; vollständige Prüfung aller Fälle und Aufrufer offen. |
| R14-D4 = A | `GameOptions.ManagerDayMinuteLoop` ist standardmäßig `false`, der Optionsbildschirm schaltet ihn, `SeasonFlow` setzt den Tagesmerker nur bei aktiver Option und Managerpaarung. Vollständige Prüfung aller Wettbewerbspfade und Gegenproben offen. |
| R14-D1 = A | Offen: Spuren und Modelle gegen die tatsächlichen Abläufe prüfen. |
| R14-D2 = B | Offen: HOK-Feldkarte und Import gegen Abgrenzung des Exports prüfen. |
| R14-D3 = A | Offen: Aufnahme und Zuordnung der Folgeumsetzungen prüfen. |
| E14-1 = A + C | Offen: Restpunkte, HOK-Import und Zurückstellung Livespiel prüfen. |
| E14-2 = A | Offen: Ablauf der Auswertung und späterer Aufnahmen prüfen. |
| E14-3 = A | Offen: geforderte Gesamtreview-Vollprüfung ist nicht abgeschlossen. |
| E14-4 = A | Offen: Restfelder des Imports einzeln prüfen. |
| E14-5 = C | Offen: fünf zurückgestellte Aufnahmefälle und `[APPROX]`-Markierungen prüfen. |
| L1 | Offen: U15-c und Filter prüfen. |
| L2 | Offen: Felder A046/A135/A380 und Migration prüfen. |
| L3 | Offen: Aufstellung/Reihenlogik und Dateigrenzen prüfen. |
| L4 | Offen: Taktik 375h und Dateigrenzen prüfen. |
| L5 | Offen: WM-Sperre nur im Bundestrainer-Menü prüfen. |
| L6 | Offen: Speichern-Dialog A338/A339 prüfen. |
| L7 | Offen: Dateigrenzen, A012 und Feldbelege prüfen. |
| L8 | Offen: A009 im Nationalteam prüfen. |
| L9 | Offen: A362 als Abbild ohne Schemaänderung prüfen. |
| L10 | Offen: A115 gemäß Originalcode prüfen. |
| Weitere Entscheidungen aus den in „Aktuelle Runde“ genannten PR-Kommentaren | Kommentare #627, #628 und #649 gelesen; übrige genannte Kommentare und weitere Einzelentscheidungen noch nicht vollständig erhoben und gegen den Code geprüft. |

**Offener Prüfbereich – Entscheidungen:** Die obige Tabelle ist keine vollständige Entscheidungsliste und ersetzt keine Prüfung je Entscheidung. Grund: PR-Kommentare und die vollständigen Codepfade wurden in dieser Sitzung noch nicht ausgewertet.

**Offener Prüfbereich – Originalbelege:** Nur die zwei genannten Instruktionen wurden selbst nachgelesen. Im Quelldiff stehen 249 hinzugefügte Zeilen mit `[ORIGINAL]` (Zeilen sind nicht gleich Fundstellen); sämtliche weiteren neuen bzw. geänderten Fundstellen in Code, Tests und Dokumentation müssen einzeln erfasst und in `em.exe` bzw. Spielständen nachgelesen werden. `ndisasm` fehlt; `objdump` steht als Alternative zur Verfügung. Grund: Umfang und fehlende vollständige Zuordnung.

**Offener Prüfbereich – Bildschirme und Hauptmenü:** Kein vollständiges Inventar und keine Prüfung jedes Bildschirms und jedes Hauptmenü-Ereignisses gegen AGENTS.md §4. Die grünen Tests ersetzen die Prüfung der Aufrufer und Zustandsänderungen nicht. Grund: Umfang, in dieser Sitzung nicht abgearbeitet.

### Standardprüfung

Der vorgeschriebene vollständige Lauf `PATH=/workspace/.dotnet:$PATH NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=2 EM_SAVEGAMES=/tmp/r14-referenz/original/spielstaende tools/pruefen.sh` wurde auf dem sauberen Head gestartet. Build und Lauf mit Originaldaten sind beendet; die beiden weiteren Läufe laufen noch. 

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten | 2625 | 0 | 0 |
| Ohne Originaldaten | läuft | – | – |
| Pflichtlauf ohne Daten | ausstehend | – | – |

Der Socket-Test scheiterte zunächst in der Sandbox mit `PermissionError`; die tatsächlich ausgeführten Testläufe nutzten die vorgesehene Freigabe für den lokalen Socket. `NuGetAudit=false` wurde nur für diesen Lauf gesetzt.

### Akzeptierte `[APPROX]` und Annahmen

- Als vom Nutzer erwartet offen nur bestätigt: April-Abschluss 115.000 DM (siehe oben); hier keine neue Abnahme weiterer `[APPROX]`.
- Die Interpretation der Referenzdaten und die Aussage, dass diese Stände den beschriebenen Originalablauf abbilden, bleiben für die noch ausstehende Originalprüfung zu verifizieren.
