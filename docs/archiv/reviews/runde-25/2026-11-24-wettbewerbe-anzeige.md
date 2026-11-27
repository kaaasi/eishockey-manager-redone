# Review PR #1003 – wettbewerbe-anzeige (B10, B15/B16, B17, C07)

Erstreview, Claude Opus 5.5 (frische Sitzung ohne Implementierungskontext, geplanter Lauf). Base `0c1811f`, Head `44b19a5`
(`git rev-parse HEAD` = `44b19a5ade47e321d455ee0e837d09fe1d5d6597`, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau Head `44b19a5`. Alle Vorgaben aus `docs/analyse/re-abweichungen.md` (B15/B16, B17, C07) sind umgesetzt, die
Nachbildungen `c07_marken.py`/`c07_klick.py` laufen und decken sich mit Code und Tests, die Rückschaltprobe macht alle neuen
und alle geänderten fremden Tests rot, die drei Läufe von `tools/pruefen.sh` sind grün. Offen sind nur P3-Testlücken.

## 2. Befunde

Kein P0–P2.

**P3-1 Testlücke: Knopf zum Zusehen und Gesicht nicht über die Zeichnung abgesichert.** Gegenprobe (temporäre Kopie des Heads):
(a) [`ClubInfoWindow.cs:309`](https://github.com/kaaasi/eishockey-manager-redone/blob/44b19a5ade47e321d455ee0e837d09fe1d5d6597/src/EishockeyManager.UI/Screens/Competitions/ClubInfoWindow.cs#L309)
`FormMood(form)` → `Widgets.Mood.Same` und
[`ClubInfoWindow.cs:321`](https://github.com/kaaasi/eishockey-manager-redone/blob/44b19a5ade47e321d455ee0e837d09fe1d5d6597/src/EishockeyManager.UI/Screens/Competitions/ClubInfoWindow.cs#L321)
`if (CompetitionCode.IsMainMenu(game))` → `if (true)`; (b)
[`TeamInfoWindow.cs:315`](https://github.com/kaaasi/eishockey-manager-redone/blob/44b19a5ade47e321d455ee0e837d09fe1d5d6597/src/EishockeyManager.UI/Screens/Competitions/TeamInfoWindow.cs#L315)
ebenso → `if (true)`. `dotnet test tests/EishockeyManager.UI.Tests -c Release` (mit Originaldaten): je Lauf **848 bestanden,
0 fehlgeschlagen** – beide Mutationen überleben. Abgesichert sind nur die Zeile „Historische Ergebnisse“ (über `Lines`), der
Klick auf den Knopf und das Gesicht im Länderfenster. Vorschlag: Pixelprobe des dritten Knopfs nach `FriendlyPlayed` (Länder-
und Vereinsfenster) und Gesicht im Vereinsfenster nach einem Ligaspiel.

**P3-2 Hinweis (kein Fehler): Kopfzeilenprüfung wirkungslos.** `y <= top + 16 + 10` in
[`DebCupGamesScreen.cs:201`](https://github.com/kaaasi/eishockey-manager-redone/blob/44b19a5ade47e321d455ee0e837d09fe1d5d6597/src/EishockeyManager.UI/Screens/Competitions/DebCupGamesScreen.cs#L201)
(ebenso `WorldCupKnockoutScreen.cs:141`): Mutation → `y <= top` überlebt (848/0), weil die erste Zeile frühestens bei
`top + 54` liegt (Band ab `top + 49`). Gleichwertige Mutation; die Prüfung bildet Datei 0x33AE4 nur nach. Keine Änderung nötig.

## 3. Ausgeführte Prüfungen

**Gegenbeispiele (Nachbildungen, em.exe aus `original/gamedata`):**

- `python3 tools/reverse-engineering/abweichungen/c07_marken.py`: wie in re-abweichungen C07 – DEB-Pokal, Play-off, WM-Endrunde je
  Verlängerungstor 4:3 / 3:4 **1 Marke**, Penaltyschießen 6:5 **2 Marken**, 4:2 nach 60 **0**. Deckt sich mit
  `CupMatch.FromReport` und `PlayoffSeries.MarksFromReport` (Code gelesen, Tests `WettbewerbeAnzeigeMarkenTests`).
- `python3 …/c07_klick.py`: DEB (Art 0) und Play-off (Art 2) wie dokumentiert (Linksklick auf Zeile → 0x4D385 links/rechts nach
  x ≤ 0x63, Bildschirm bleibt, Rechtsklick zurück, Grundlinie + 1 und Kopfzeile ohne Fenster). Remake: `ClubAt`/`TeamAt`
  mit Band `Grundlinie − 6 < y ≤ Grundlinie`, Grenze `x > 0x63` rechts; `RowLines` gleich der Zeichenrechnung in `Draw`
  (Zeilenabstand 7, zwischen Gruppen 15 – wie die Liste des Originals 73 → 88 → 95). Art 3 (WM) lässt sich mit dem Skript ohne
  weiteren Zustand nicht starten (Zeilenliste leer, Probe mit `for art in (3,)` in Kopie) – dort nur Codelesen.
- **C07 Marken:** Verlängerungstor 1 (`Cups.Tiebreak.cs` Zeile 72), Penaltyschießen und Zusatzwurf schneller Weg bei Gleichstand 2
  (`MaxMarks`, auch WM ohne `debCup`), RR-01-Zusatzwurf unverändert 1; Play-offs: Penalty/Zusatzwurf 2, Verlängerungstor 1,
  „Play-Off Spiele“ zeigt jetzt `MarksOf` der Serie. Klick ins Vereinsfenster 0x4D385: `InfoBarWindows.Show`, Rechtsklick schließt
  erst das Fenster, dann den Bildschirm (Test `DebCupGames_ClickOnClub_…`).
- **B16-Urteil bestätigt:** `EnterNationalResult` schiebt Bilanzzeichen, Serien und Rekorde nur bei `WorldCupGroup` (Code 8),
  Vereinsfenster rechnet aus den Ligaergebnissen (Code 7); Freundschafts-, Endrunden-, Pokal- und Play-off-Spiele schreiben nur
  `RecordMeeting` – entspricht `b15_gegnerfenster.py` Teil 3. Keine Abweichung.
- **B17:** `NationalWidgets.LineValue` = `GameState.NationalPanelLineValue`, Ausgabe dreistellig bei 100 (`Number(…, 2)` füllt nur auf).
- **Fremde Dateien minimal:** `EuropeanCupScreen.cs` nur die Kachelanmeldung (5 Zeilen, Konstruktor mit Ansicht bestand schon),
  `FriendliesScreen.cs` nur `case FriendlyResult.Played` (3 Zeilen, nach dem Spiel, nicht beim Öffnen).
- **Öffnen ändert keinen Spielzustand:** `EuropeanCupPages`/`CompetitionCode` halten nur Oberflächenzustand in
  `ConditionalWeakTable` (kein Spielstand); Tests prüfen `StateFingerprint` vor/nach (Europapokal-Kacheln, DEB-Pokalspiele).

**Rückschaltprobe** (temporäre Kopie des Heads, Verhalten auf Base zurück, neue APIs bleiben: Marken, `MarksFromReport`,
`ResultOf`, `LineValue`, Kachelanmeldungen, Gesicht/`MoodX` 250, `IsMainMenu` = true, Klickverhalten):

| Probe | Ergebnis |
|---|---|
| Core: `WettbewerbeAnzeigeMarkenTests`, `WettbewerbeSpiel1Tests`, `ReResteTests`, `WettbewerbeRegelnTests` | 12 rot, 25 grün (alle 6 neuen und alle 6 geänderten fremden Tests rot) |
| UI: `WettbewerbeAnzeigeTests` | 13 rot, 4 grün (grün nur `FormMood` mit erwartetem Gelb) |
| Mutationen P3-1, P3-2 | überleben (848/0) |

**`tools/pruefen.sh` (alle drei Läufe, Head `44b19a5`, SDK 10.0.402):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3875 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2738 | 0 | 627 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2739 | 1136 | 0 |

CI auf dem Head: `build` success.

**Akzeptierte `[APPROX]` (erwartet offen, bestätigt):** gemerkte Europapokal-Seite und Wettbewerbscode nach dem Laden auf
Startwert (`EuropeanCupPages.cs`, `CompetitionCode.cs`); Vereins-/Gegnerfenster aus „DEB-Pokalspiele“/„WM-Endrundenspiele“ mit
Seite 2 statt 0/1 und Taste als Rückweg; alte Spielstände behalten Marken alter Bedeutung (Leitstelle A1 = B, keine Migration);
`MoodX` 249 aus dem Bildziel 0xF9 gelesen, nicht am DOSBox-Bild nachgemessen.

**Annahmen:** WM-Klick (Art 3, Seite 2) nur nach Code und re-abweichungen C07 geprüft, nicht nachgebildet (siehe oben).
