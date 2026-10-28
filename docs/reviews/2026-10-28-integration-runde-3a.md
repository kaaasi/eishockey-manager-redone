# Unabhängiges Review: Integration Runde 3, Welle A

## 1. Merge-Empfehlung

**Noch nein für #54, Head `c622b0fe06b0000b74fe1f1f6ce91dbf365c068a`, Base `ee7ea0ed58570312e564a3568cb1fff5bcb9d712`.** Die beiden historischen Integrationsfehler sind reproduziert und am geprüften Head behoben. Die synthetischen Tests, exakten Pokalabrechnungsproben und zusätzliche Kader-/Transfer-/WM-Proben sind grün. Für die verlangte vollständige Freigabe fehlt der selbst ausgeführte Originaldatenlauf; außerdem benötigt der Head für das Prüfverfahren aus Teil 1 die Originaldaten-Unterstützung aus #45 (R3A-01).

Frische ChatGPT/Codex-Sitzung ohne Mitarbeit an den acht Einzel-PRs oder der Integration. Keine Implementierung und kein geprüfter PR wurden verändert. Zusätzliche Tests und Mutationen liefen ausschließlich in temporären Kopien.

## 2. Befunde

**R3A-01 · P2 · Der genaue Head unterstützt den Ohne-Daten-Schalter des angeforderten Prüfsystems noch nicht.**

[TestGameData:22–25](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/tests/EishockeyManager.UI.Tests/Support/GameDataFact.cs#L22-L25) ruft ungeachtet `EM_SKIP_GAMEDATA` den Daten-Locator auf. Dieselbe Datei ist in die anderen Testprojekte eingebunden. Mit vorhandenen Daten führt das Skript aus #50 deshalb auch den bezeichneten „Ohne Originaldaten“-Lauf mit Daten aus; sein Pflichtlauf simuliert dann keinen Datenentzug. Das ist eine Integrationsabhängigkeit zu #45, kein neuer Spielfehler.

Isolierte Gegenprobe mit dem unveränderten `TestGameData`-Klassenkörper und der echten Formats-Bibliothek: leeres `gamedata/kb` als reine Locator-Fixture anlegen, `EM_SKIP_GAMEDATA=1` setzen und `TestGameData.Available` abfragen. Head #54: **true**; Helfer aus #50 einschließlich #45: **false**. Keine Originaldateien wurden dafür erzeugt oder vorgetäuscht; die Probe prüft nur das Auffinden/Ignorieren eines Ordners. Dass der Locator bereits diesen Ordner akzeptiert, folgt aus `GameData` und wurde so ausgeführt.

Vor dem regulären Dreifachlauf mit vorhandenen Originaldaten #45/#50 einbeziehen und den neuen Prüfstand benennen. Alternativ Daten für die beiden Negativläufe tatsächlich aus **allen vom Locator durchsuchten Pfaden** entfernen. Ein bloßes Setzen von `EM_SKIP_GAMEDATA=1` reicht auf diesem Head nicht. Die PR-Beschreibung nennt den bisherigen manuellen Datenentzug; diese Aussage ersetzt die eigene Gegenprobe nicht.

**R3A-02 · P3 · Der Heimrecht-Test benutzt die Implementierung als Sollwert.**

[DebCupTests:124–127](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/tests/EishockeyManager.Core.Tests/DebCupTests.cs#L124-L127) vergleicht die Spiele mit `CupTie.Pairing`, also derselben Funktion, die sie erzeugt. Mutation in `Cups.cs`: in `Pairing` die Bedingung `game % 2 == 0` durch `true` ersetzen. Dann spielt immer `First` zu Hause. Der Test `EveryTie_IsBestOfThree_WithAlternatingHomeRight_OnCalendarDays` bleibt dennoch **grün (1/0/0)**. Unabhängige Erwartungen für Spiel 1/2/3 aus `First`/`Second` ergänzen. Der gegenwärtige Produktionscode wechselt das Heimrecht korrekt; der Befund betrifft den behaupteten Regressionsschutz.

**R3A-03 · P3 · Wiederholte Ernennung erzeugt erneut Etat und setzt den Nationalkader zurück.**

[AppointNationalCoach:58–65](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/src/EishockeyManager.Core/CupsNational.GameState.cs#L58-L65) verhindert keine zweite Ernennung innerhalb derselben Saison. Reproduziert mit synthetischem Spielstand:

```csharp
var m = game.Managers[0];                  // 400.000 DM
game.AppointNationalCoach(m);             // 900.000 DM
game.AutoNominate();                      // 29 Spieler
game.AppointNationalCoach(m);             // 1.400.000 DM, Kader wieder leer
```

Auch die Freundschaftsspielhistorie wird durch den neuen Coach ersetzt; so lässt sich über diese API die Acht-Spiele-Grenze zurücksetzen. Vor Freischaltung des Bundestrainer-Spielmodus Ernennung auf den vorgesehenen Saisonübergang begrenzen oder eine bereits erfolgte Ernennung ohne erneute Buchung behandeln. Der Modus ist laut #51 noch nicht im normalen Menü erreichbar; deshalb P3, kein behaupteter derzeitiger UI-Geldexploit. Dieser Befund ist getrennt von der erwarteten offenen **Kategorie** des Etats.

Die drei vom Auftrag erwarteten Restlücken sind bestätigt, ohne neue Einstufung:

| Restlücke | Bestätigung |
|---|---|
| Gehaltspauschale bei leerem Kader | `FinanceRules.MonthlyWages` verwendet bei `Players.Count == 0` weiter `FallbackWages`; `MonthlyFinances` bucht diesen Betrag. |
| Bundestrainer-Etat als „Prämien“ | `AppointNationalCoach` bucht `NationalBudget` über `FinanceCategory.Praemien`. |
| Finanzbalance ohne Endabnahme | `FinanceCalibrationTests` dokumentiert ausdrücklich vorläufige technische Kontogrenzen und keinen nachgewiesenen DOS-Balancevergleich. Die Originaldaten-Mehrsaisonprobe konnte hier nicht ausgeführt werden. |

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

**Identität und Umfang.** Review-Vorlage aus `prozess/sparsam`, AGENTS und Zusammenarbeit §§5/6 gelesen. Angefragten Head und historischen Gegenbeispielstände über Blob-Hashes, komplette Git-Trees und SHA-identisch rekonstruierte signierte Commitobjekte verifiziert. Head-Tree: `6db58a4618e2b1370beb3b1c8937117406fb9437`. Diff Base→Head: 51 Dateien; betroffene Aufrufer in Zeitleiste, Saisonabschluss, Finanzbuch, Transfermarkt, Menüregistrierung und Tests geprüft. GitHub-Vergleiche bestätigen alle folgenden Einzel-Heads als Vorfahren des geprüften #54-Heads (jeweils `merge_base_commit` gleich Einzel-Head):

| PR / Head | Inhaltlich geprüft, über die Integration hinaus |
|---|---|
| #39 · `79a0bc71650ccc7693236a3b3cf46f88d0e0ee64` | Vertragsankündigungen, Verhandlungssperren, Alterung/Leih-Rückgabe, Ruhestand, Free-Agent-Pool, Kaderauffüllen. Bestehende sechs Saisonwechsel plus zusätzliche gemeinsame WM-/Transferprobe. |
| #40 · `3114f26d687f2bcd76cfe9cbf32c8e752463f9ae` | 32 Teilnehmer, Manageraufnahme, Klassen-Heimrecht, Best-of-three, Kalender/Continue, Titel und Abrechnung; R3A-02. |
| #43 · `f50cda0be49dd58d75f5d811b8f82e43b542c879` | Deutsche Teilnehmer aus Vorjahrestiteln, Gegnergewichtung, Gruppen-/Endrundenplan, Siegeranzeige und Heimabrechnung. |
| #44 · `87c109592c3f32ad6c8d0a52361b5aab4cdb636f` | Länderwerte, zwei Sechsergruppen, Viertelfinalkreuzung, Weiterverteilung und eindeutige Plätze 1–12; Kalender nach Vereinssaison. |
| #48 · `ade477cd62279896ddd3b4ade9aac20abaab9629` | Auswahl von Abwehr/Sturm/Gesamt, Summen/Sortierung, Anzeigegrenzen und lesende Tabellenberechnung. |
| #49 · `db3c253413f08f317bf00dcabfcf5e1ee4cd6883` | Ewige Punkte/Titel, Managerbilanz/-historie, einmaliger Saisonabschluss und Menüregistrierung. |
| #51 · `e7c57dc327e03b66170738a4530680909a996329` | Kandidaten/Nominierung, Verletztenfilter, Kosten/Bonus/Kaps, Etat, Acht-Spiele-Grenze, Saisonreset und Deutschland-Neuwürfeln; R3A-03. |
| #41 · `c53ff3dad80da79c0b065f35ce01a0f6590d14b1` | Titel-/Aufstiegsprämien statt pauschaler Tabellenprämien, Buchung beim Saisonabschluss, Kalibrierungsproben und deren begrenzte Aussage. |

**Gegenbeispiele zuerst erneut ausgeführt.**

```bash
# 88b561503e2fc49dde0f6c7a74007ca8c181e6ff
dotnet build EishockeyManager.slnx -warnaserror -m:1 -p:UseSharedCompilation=false
# 9fb4a3b0c6771233bd6200b8cef77d66c06b1fb0
dotnet test tests/EishockeyManager.Core.Tests -m:1 -p:UseSharedCompilation=false \
  --filter FullyQualifiedName~SeasonPaymentTests.SeasonPremium_IsBookedOnce_InOwnCategory
```

Erster Stand: Exit 1, CS8600/CS8602 in `Scenarios/Wettbewerbe.cs:196/197`. Zweiter Stand: 0 bestanden, 1 fehlgeschlagen, „Keine Prämie gezahlt – Test prüft nichts“. Auf dem geprüften Head sind Build und dieser Test grün. Der Test prüft weiter tatsächliche Prämienbuchungen, Kontosaldo sowie Vorjahresübernahme; nur der Seed-Suchraum wurde erweitert.

**Dreifachprüfung mit `tools/pruefen.sh` aus #50.** SDK 10.0.402. Das Skript wurde als ungetracktes Werkzeug bereitgestellt, ohne den getesteten Code oder den Originaldatenhelfer zu ersetzen. Wegen einer Umgebungsgrenze bei parallelem MSBuild ergänzte ein transparenter lokaler Wrapper bei `build`/`test` ausschließlich `-m:1 -p:UseSharedCompilation=false`; `DOTNET_PROCESSOR_COUNT=2`. Release-Build mit `-warnaserror`: 0 Warnungen, 0 Fehler.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten | – | – | nicht ausführbar: Originaldaten fehlen |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`, tatsächlich keine Daten vorhanden) | 392 | 0 | 66 |
| Pflichtlauf ohne Daten (`EM_SKIP_GAMEDATA=1 EM_REQUIRE_GAMEDATA=1`) | 392 | 78 | 0 |

Die beiden ausführbaren Läufe wurden real ausgeführt; beim Pflichtlauf fehlen erwartungsgemäß die Originaldateien. Das Skript endet insgesamt mit Exit 0. Das ist **kein** Nachweis aller drei realen Läufe. Eigene TRX-Nachzählung des normalen Laufs: Audio 28/0/3, Core 260/0/7, Formats 90/0/5, UI 14/0/51, Summe 392/0/66. Die abweichende Pflicht-Gesamtzahl entsteht durch Theory-Datenzeilen. Die 470/0/0 aus der PR-Beschreibung wurden nicht übernommen.

**Gezielte Gegenproben.** Bestehende DEB-/Europapokaltests rechnen nach jedem Continue-Schritt mit kleinen, sicher ausgelasteten Stadien exakt die neu hinzugekommenen Heimspiele zum jeweils aktuellen Eintrittspreis ab. Mutationen „keine Pokalabrechnung“ und „doppelte Pokalabrechnung“ machen beide Abrechnungstests rot; die gemeinsame Saisonzahlprüfung wird beim Auslassen ebenfalls rot. Im regulären Continue-Ablauf kein Hinweis auf fehlende oder doppelte Heimabrechnung. WM-Spiele haben hier keinen Vereinsmanager als Kontoinhaber.

Zusätzliche Eigentumsprobe: je vier Seeds, aktiver/passiver Manager, fünf Saisons mit vier Wechseln; Nationalkader nominieren, beim aktiven Manager verfügbare deutsche Marktspieler auch aus dem Nationalkader kaufen/leihen und Verträge verlängern. Nach jedem Continue und Saisonwechsel: alle bisher bekannten Spieler weiterhin in genau einem Eigentumsbereich (Vereinskader, Free-Agent-Pool oder Ruhestand), keine Doppelbesetzung, neue Talente ergänzen die bekannte Menge, Nationalamt beim Saisonwechsel beendet. **2 parametrisierte Fälle grün.** Markt- und Nationalkader-Einträge sind dabei Referenzen, keine zusätzlichen Eigentümer; sie dürfen nicht fälschlich als duplizierte Spieler gezählt werden. Die Probe verwendet hohe Kontostände zur Isolierung von Kader- und Entlassungseffekten und ist kein Balance-Test.

**34 ausgeführte Mutationen: 33 erkannt, 1 überlebt (R3A-02).** Alle erkannten Fälle scheiterten an Testassertionen, nicht an Kompilierfehlern. Abgedeckt: Pokalabrechnung auslassen/verdoppeln; Siegschwelle und Klassen-Heimrecht; Gruppen-Rückspiel, Gegnergewichtung und Europatitel-Anzeige; WM-Werte, Mischung und Viertelfinalkreuzung; Statistikschutz, Punktbetrag und Bilanz; Wertebegrenzung; Vertragsankündigung, Kaderauffüllen und Absage; Freundschaftskosten, Erst-/Folgeetat, Bonus und Wertverteilung; Kadergröße, Verletztenfilter, Geldprüfung, Kostenbuchung, Saisonreset und Deutschland-Neuwürfeln; Meisterprämie; vier synthetische UI-Positions-/Textproben. Jeweils gezielt `dotnet test <Core- oder UI-Testprojekt> --filter FullyQualifiedName~<betroffener Test>` nach genau einer Mutation, anschließend Originaldatei wiederhergestellt.

Das ist keine vollständige Mutation pro neuem Test: weitere Mehrsaison-, Zustandsreinheits- und Originaldaten-/UI-Tests wurden durch den normalen Gesamtlauf bzw. nur statisch geprüft. Insbesondere sind Skips keine Gegenproben. Eine zusätzliche Annahmeprobe „leerer Bundestrainer wird am ersten WM-Termin automatisch nominiert“ bleibt rot. Da #51 den vollständigen Bundestrainer-Spielmodus ausdrücklich einem Folge-PR zuweist, wird diese Probe hier als offener Integrationsablauf dokumentiert, nicht als bewiesener Fehler des bereits freigeschalteten Spielmodus.

**Akzeptierte [APPROX] für diesen begrenzten Umfang:** Kader-KI einschließlich Alters-/Wahrscheinlichkeitsgrenzen; zusammengefasste Spielstärke und Penaltyentscheidungen; Form/Trend und Balkenhöhen der Statistik; erweiterte Nationalkandidaten, automatisch abgeleitete Nationalaufstellung und nicht übernommener Energie-Reset beim Öffnen. Die Zuordnung der zusätzlichen 100.000-DM-Titelprämien bleibt ausdrücklich Näherung. Diese Markierungen machen die Annahmen sichtbar; sie belegen keine Originaltreue oder fertige Finanzbalance.

**Grenzen/Annahmen:** Keine eigenen Originaldateien, Referenzvideos oder unabhängigen Disassembly-/Pixelvergleiche verfügbar; daher kein eigener Originaldatenlauf und keine visuelle Endabnahme. Kein uneingeschränktes Urteil über DOS-Belege allein aus den Codekommentaren. Die Eingliederung der Prozess-Änderungen zusammen mit #54 wurde zusätzlich auf den verifizierten Trees konfliktfrei simuliert; dafür ist nach Übernahme ein neuer gemeinsamer Testnachweis nötig. Die Testanzahl ist keine Merge-Freigabe.
