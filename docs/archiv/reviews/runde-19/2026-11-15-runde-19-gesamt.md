# Gesamtreview Runde 19

Codex, unabhängige Sitzung · Base `926aeab` · Head `c4fe55a730655b23156007edb84d10fbfd590ebb` · 15.11.2026

## 1. Merge-Empfehlung

**Ja für genau `c4fe55a`**. Keine neuen nachgewiesenen Produktfehler;
die folgenden Grenzen und die erwarteten offenen Punkte sind nicht blockierend.

## 2. Befunde

Keine neuen nachgewiesenen Produktfehler. **Testlücke:** Die FILE54-Probe unten prüft die Punkteberechnung nach
Fortsetzung und Platz 4, nicht die tatsächliche Darstellung und Eintragung der Bestenliste am Spielende.

## 3. Ausgeführte Prüfungen

- Checkout vor Prüfungen: `git rev-parse HEAD` = `c4fe55a730655b23156007edb84d10fbfd590ebb`, `git status --porcelain` leer;
  SDK 10.0.402. Originaldaten und FILE-Stände aus Referenz-Repo (Stand `36363bbb`, Kopie nach `/tmp`).
- #847 / #826: `dotnet test tests/EishockeyManager.Core.Tests --filter 'FullyQualifiedName~AbgleichDosboxNeuerManagerTests|FullyQualifiedName~HighscoreWeltkugelTests'` mit Originaldaten: 9 bestanden. FILE64→65 vergleicht Platz, Tabelle und Teamwerte nach gezielter Platzwahl; Highscore-Test Platz 4 → 5 synthetisch.
- #826 zusätzlich temporäre Probe: FILE54 mit `HokCompare.Import` laden, Saaten 1…30; je höchstens zehnmal `game.Continue()` bis `WorldCupRankings.Count > 0`; bei `WorldCupPlace(0, NationalTeams.Germany) == 4` `HighscoreRules.Entry(game, 0, 0).Score == 5`: bestanden. FILE54 selbst liegt vor dem Spiel um Platz 3, sofortige Wertung 1 ist kein Gegenbeweis.
- Torszenen: `dotnet test tests/EishockeyManager.UI.Tests --filter 'FullyQualifiedName~GoalSceneCall'`: 8 bestanden. Ganzer Spieltag mit Anzeige gegen `Continue()` bei gleicher Darstellung, gespeicherter Spielzustand inklusive Zufall gleich; `r` wiederholt dieselbe Szene höchstens dreimal. Temporäre Kopie von `Replay_AtMostThreeTimes_SameScene_EqualsWithoutDisplay` mit `d.Type("w")` statt `"r"`: 1 bestanden, gleicher Zustandsvergleich.
- Platztausch, Export und Aufrufer gelesen: Platzdaten bleiben am Index, Kennung und Spieler wandern; `NeuerManagerTests` prüft Laden/Speichern und vier Saisonwechsel, `NeuerManagerExportTests` Name, Logo und Farbe beider Plätze. Europapokal-Ausschluss separat abgesichert. Keine neue Mutation.
- #730: §4.5 und `AbgleichDosboxStrainHitsTests` geprüft: fünf Läufe ab FILE50, Mittel 58,4 gegenüber 60,9 ± 6,7 (400 Remake-Saaten, Bericht). „Im Bereich“ trägt die begrenzte Aussage; ein kleinerer Restunterschied und die abweichende Gesamtsumme bleiben offen, keine Gleichheit bewiesen.
- R19-D1…D3 und E1…E4 anhand der Arbeitspakete und betroffenen Änderungen geprüft: Spurfolge, Fallback und Auswertung dokumentiert; Wiederholung ohne neuen Zug umgesetzt. A093 weiter unbelegt; #871 und #872 auf GitHub offen, wie erwartet.
- `tools/pruefen.sh --schnell`: bestanden (SDK 10.0.402, Originaldaten, `EM_SAVEGAMES` auf Referenzkopie). Erster Versuch scheiterte beim Build an `NU1900` (NuGet-Sicherheitsdaten nicht erreichbar); Wiederholung mit `NuGetAudit=false`, Paket-Audit somit nicht geprüft. Der Skript-Commit ist der Review-Datei-Commit `9878a86`; der geprüfte Quellbaum entspricht `c4fe55a`.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3364 | 0 | 0 |

**Akzeptierte `[APPROX]`:** Torszenen: Speichermangel-Zweig, Dauer-Eisfeld, Bildtakt und Wiederholung ohne neuen Zug; ferner Taktikzeile beim Platztausch und Auswahl ohne festen Verein. **Annahmen/nicht geprüft:** kein eigener Bild- oder em.exe-Abgleich; Finanzen, Wettbewerbe und Bedienung außerhalb der genannten Übergänge nicht gesondert untersucht. Die Erstreviews sind keine eigenen Testläufe.
