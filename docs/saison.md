# Saisonablauf

Arbeitspaket `saison`. Quellen: Kalendertabelle in `em.exe`, Video „MEISTER werden in 75 Minuten“
(drei Saisonenden bei ca. 11:50, 18:36 und 25:52).

## Kalender

`em.exe` enthält ab Dateioffset `0x175F9` eine Tabelle mit **einem Byte je Tag** ab dem 10. September
(Tag 0) bis zum Saisonende (Tag 231). Die Werte sind Bitmasken:

| Wert | Bedeutung | Termine |
|---|---|---|
| 7 | Ligaspieltag (alle drei Ligen) | 44 – meist So/Mi, dazu einige Freitage; auch Heiligabend |
| 8 | DEB-Pokal | 15 (22.9. … 7.4.) |
| 32 | Europapokal | 12 (4.11. … 30.11., Di/Do/Sa) |
| 64 | Play-Off / Aufstiegsrunden | 15 (6.2. … 5.4.) |
| 16 | Weltmeisterschaft | 8 (14.4. … 28.4.) |
| 255 | Ende der Vereinssaison (13.4.) bzw. Saisonende (29.4.) | 2 |

Die Zuordnung der Werte ist an den Videos belegt: DEB-Symbol in der Infoleiste am Freitag, 22. Sept.;
„Play-Off / Aufstiegsrunden“ nach dem 44. Spieltag; Europacupsieger zwischen 15.11. und 3.12.

Das Original kennt **keinen 29. Februar** („Donnerstag, 22. März“, „Samstag, 14. April“ in allen Jahren).
Am Saisonende steht „Montag, 29. April“, obwohl der Kalender einen Sonntag ergibt – wird so nachgebildet. Hinweis (#688,
R688S-01): Das Saisonende 0x26F20 setzt den Tageszähler auf 233 (Datei 0x26F55), das ist der 30. April = Montag im Kalender
1989/90; der DOSBox-Stand `file46_2` zeigt im Fenster „Saisonende“ „Montag, 30. April“. Ob das Video wirklich „29. April“ zeigt, ist
offen (A158).

## Infoleiste

Datum und „n.Spieltag“ beziehen sich auf den **nächsten** Termin (Beleg: am „Sonntag, 4. Februar“ mit
„44.Spieltag“ werden drei bisherige Begegnungen mit dem Gegner des 44. Spieltags gezeigt).

## Ablauf einer Saison (`Core/SeasonFlow.cs`)

„Tag weiter“ (Klick aufs Vereinslogo der Infoleiste, `GameState.Continue()`) spielt den aktuellen Tag und läuft Tag für Tag weiter – auch über
spielfreie Tage –, bis wieder ein Verein eines Managers spielt. Belegt: 1995/96 springt das Hauptmenü vom
44. Spieltag (4.2.) direkt auf „Samstag, 14. April – Saisonende“, weil BK Oldenburg nicht mehr spielt.

1. **Hauptrunde**: 44 Spieltage (4 Runden à 11).
2. **Play-Off / Aufstiegsrunden** (15 Termine 6.2.–5.4.):
   - Play-Offs der Bundesliga, Plätze 1–8 → Deutscher Meister. `[APPROX]` Format: drei Runden „best of five“
     (15 Termine = 3 × 5), Heimrecht beim besser Platzierten, Unentschieden → Penaltyschießen.
   - Aufstiegsrunden (Video): die zwei Letzten der oberen und die zwei Ersten der unteren Liga spielen je
     zweimal gegeneinander; die beiden Ersten spielen oben. Belegt für 2. Liga/Oberliga (1994/95 steigen
     BK Oldenburg und EC Harz Braunlage auf, SC Riessersee und SC Memmingen ab). Zwischen Bundesliga und
     2. Liga wechseln laut Saisonende-Bildschirmen mal zwei, mal ein Verein – daher dasselbe Verfahren.
     Gruppenspieltage `[APPROX]`: Play-Off-Termine 1, 3, 6, 8, 10, 11 (belegt: 6.2., 10.2., 20.2., 25.2., …, nach 22.3.).
3. **13. April**: Ende der Vereinssaison, Auf- und Abstieg stehen fest. Die beiden Letzten der Oberliga
   verlassen die Liga, zwei Vereine ohne Liga (Datenbank 36–47) rücken nach `[APPROX]` (Manager-Vereine ausgenommen).
4. **14.–28. April**: WM-Termine (Paket `wettbewerbe`).
5. **29. April**: Saisonende – Titel, Prämien (`[APPROX]`, belegt nur 1. Platz Oberliga = 400.000 DM),
   Ereignisse je Manager, dann neue Saison am 10. September oder Spielende nach der gewählten Dauer.

**Keine Entlassung wegen Schulden** (#688, R688S-03; `SeasonDismissal.cs`): em.exe vergleicht das Konto nur beim Stadionauftrag,
beim Kauf, beim Zwangsabstieg am 29. April (< −2.000.000 Drittel, `SeasonFlow.Zwangsabstieg.cs`) und mit der Kontogrenze
±500.000.000 Drittel im Monatsabschluss (Datei 0x3F10E–0x3F165); das Spielende hängt nur am Endjahr. Die frühere Remake-Regel
„8 Wochen unter −1,5 Mio. DM“ ist entfernt. Ausgeschiedene Manager älterer Spielstände (`Manager.Dismissed`) werden im Hot-Seat
übersprungen.

**Erweiterung**: DEB-Pokal, Europapokal und WM melden sich über `ISeasonCompetition` in `GameState.Competitions`
an und werden an ihren Kalendertagen aufgerufen (Infoleiste, Anhalten von „Tag weiter“, Titel am Saisonende).

## Statusbyte und Ereignissymbole (Befund #125)

Das Original führt je Manager ein **Statusbyte** (DGROUP `0x974E` + `0x1AF` · Manager). Das Fenster „Ereignisse“ zeigt für jedes
gesetzte Bit 0–6 ein Symbol aus `kb\13` (Schleife Datei `0x2714F`, Tabelle DGROUP `0x4B2E` = 2, 1, 0, 5, 4, 6, 8):

| Bit | Symbol | Remake (`SeasonEvent`) | Beleg |
|---|---:|---|---|
| 0 (0x01) | 2 Pfeil hoch | `Promotion` | Tabelle 0x4B2E |
| 1 (0x02) | 1 Pfeil runter | `Relegation` | Tabelle 0x4B2E |
| 2 (0x04) | 0 | – (nicht abgebildet, Setzen bei 0x55F87) | Tabelle 0x4B2E |
| 3 (0x08) | 5 | `GermanChampion` – `[APPROX]` Zelle 3 (Puck) statt 5 | Setzen bei 0x55C09 |
| 4 (0x10) | 4 Fahne | `CupWinner` | Setzen bei 0x55D12 (Prämie 1.500.000 Drittel) |
| 5 (0x20) | 6 Landkarte mit Pokal | `EuropeanCupWinner` | Setzen bei 0x55DAD |
| 6 (0x40) | 8 Weltkugel | abgeleitet: `GameState.HasTopFiveStatus` (kein `SeasonEvent`, nicht gespeichert) | Setzen bei 0x553ED |

**Symbolnummer → Zelle** (Zeichenroutine Datei `0x6CA6` + `0x39`, `while (n > 5) { n -= 6; y += 21; }`, `x = n · 24`): sechs Zellen
je Zeile, `n % 6` Spalte, `n / 6` Zeile, 24 × 21 Pixel. Symbol 8 liegt damit in Spalte 2, Zeile 1 (die Weltkugel).

**Bit 0x40 „unter den ersten fünf der 1. Liga“** (Datei `0x553A2`–`0x553F2`, Aufruf bei `0x553A9`): An Tagen mit Markierung 255
(13. und 29. April) ruft das Original `0xE8:0x93(0x7F)` (Datei `0x34769`), löscht danach das Statusbyte jedes Managers und setzt Bit
0x40, wenn `0xE8:0x93(Manager)` wahr ist. Die Funktion liest die Plätze 1–5 der Tabelle DGROUP `0x5CCB` (Liga · 12 + Platz, Liga 0 =
Bundesliga) und prüft, ob der Verein den Manager hat (Vereinsfeld `0x16` = Manager + 1). Das gilt **für jeden Manager** in den
ersten fünf, nicht nur für den höchstplatzierten (der wird Bundestrainer, siehe `wettbewerbe.md`). **Nicht im Spielmodus 2**
(„nur Fahne“, `[0xD0B9] == 2`, Datei `0x553DB`): ohne WM kein Weltkugel-Symbol. Das Symbol bringt keine Prämie.

**Löschen des Statusbytes** (Datei `0x553C8`): Es geschieht an beiden Markertagen unmittelbar vor dem Setzen von Bit 0x40; die Bits
0x08/0x10/0x20 setzen andere Tagespfade (Titel). Das Remake hält kein Statusbyte: Auf-/Abstieg und Titel leitet es am 29. April aus dem Saisonergebnis
ab (`SeasonFlow.FinishSeason`, gespeichert in `ManagerSeasonResult.Events`), Bit 0x40 leitet es erst bei der Anzeige aus der
gespeicherten Abschlusstabelle, dem Verein und dem Modus ab (`GameState.HasTopFiveStatus`). Es ist deshalb **kein neuer Zustand**:
ein neues `SeasonEvent` hätte den Schemaabdruck geändert (er enthält die Namen aller Enum-Mitglieder) und eine Schemaversion samt
Migration erzwungen. Gleichwertig zum Original, solange sich die Tabellen zwischen 13. und 29. April nicht ändern (Ligaspiele ruhen). Der einzige Leser außer der Anzeige ist `0x55F53` (Maske 0x28, also nur Bit 3 und 5), Bit 0x40 wirkt
dort nicht.

Das Original liest 0x5CCB, die an Ort und Stelle sortierte Platzliste; sie steht beim Lesen immer in der Gesamtordnung (die
Tabellenansicht sortiert für „Heim“/„Auswärts“ nur vorübergehend und beim Verlassen wieder nach „Gesamt“, 0x50256). Das Remake nimmt
die Abschlusstabelle „Gesamt“; `[APPROX]` nur die Reihenfolge vollständig gleicher Vereine kann abweichen. Im Modus „nur Weltkugel“ gibt es keine Vereinssaison und keine Ergebnisse je Manager.

## Tabelle (Spur `tabellen-original`)

`LeagueTable.Compute` sortiert wie em.exe 0x50B45 (Belege in [reverse-engineering.md](reverse-engineering.md#ligatabelle-spur-tabellen-original)):
nach jedem Spieltag Tauschsortierung der Vortagsreihenfolge nach Punkten, weniger Spielen, Tordifferenz und Toren; voller
Gleichstand behält die Reihenfolge des Vortags (nicht den Vereinsindex). Heim- und Auswärtsansicht sortieren die
Gesamtordnung mit den Heim- bzw. Auswärtsfeldern. Die Bilanz zeigt in jeder Ansicht alle Spiele, Heimspiele klein.
Ab der zweiten Saison beginnt die Liste mit der Platzliste der Vorsaison, Auf- und Absteiger auf dem Platz ihres
Tauschpartners (Spur `wettbewerbe-komplett`, `LeagueTable.SuccessorOrder`); `[APPROX]` angesehene Heim-/Auswärtstabellen
ändern die Ausgangsreihenfolge nicht (Bildschirme sind zustandsrein).

**Teil 2 (Spur `tabellen-original-2`):**
- Spielbeginn: In der 1. Liga stehen Düsseldorfer EG (Deutscher Meister) und Eisbären Berlin getauscht (0x43A08), so im Video
  13:07; ab der zweiten Saison die Platzliste der Vorsaison nach Auf- und Abstieg (Teil 1 `wettbewerbe-komplett`).
- Gruppentabellen (Europapokal-Gruppen und -Endrunde, WM-Gruppen, Aufstiegsrunden) nach 0x30852: je Spieltag Punkte (2/1/0) und
  Tore eintragen, dann Tauschsortierung der Platzliste der Gruppe nach Punkten, Tordifferenz, Toren – **voller Gleichstand
  tauscht** (anders als die Liga), Spiele zählen nicht; Ausgangsreihenfolge = Plätze der Auslosung bzw. 11., 12. der oberen und
  1., 2. der unteren Liga (`CupGroup.Standings`).
- Ansicht 0x4F78D: Titel „1.Spieltag“ vor dem ersten Spiel, Bilanz mit „-“ auf acht Zeichen aufgefüllt, Zahlen als ein Text,
  „`“ (±) bei Tordifferenz 0, Markierung 1. Liga nur Platz 1. Belege: [reverse-engineering.md](reverse-engineering.md#ligatabelle-spur-tabellen-original).

## Oberfläche

| Bildschirm | Datei | Vorlage (Video) |
|---|---|---|
| „Tag weiter“ (Vereinslogo): Banner („Ligaspiel“, „Play-Off / Aufstiegsrunden“ …), „Spiele laufen“ | `MatchDayScreen.cs` | 10:43, 17:27 |
| Meisterfoto kb\16 („Deutscher Meister“, „DEB Pokalsieger“, „Europacupsieger“) | `SeasonEndTrophyScreen.cs` | 10:45, 17:35 |
| Infoleiste nach der Vereinssaison: Zielflagge, „Saisonende“ | `MainMenuScreen.DrawInfoBar` (minimal) | 10:50, 24:50 |
| Saisonende: Titelträger, neue Vereine je Liga (rot = Absteiger, grün = Aufsteiger, blau = Meister) | `SeasonEndScreen.cs` | 11:50, 18:36, 25:52 |
| Fenster „Ereignisse“ mit Platzierung und Prämie je Manager | `SeasonEndSummaryScreen.cs` | 11:53, 18:38 |
| Spielende (Entlassungstext nur für ausgeschiedene Manager älterer Spielstände) | `SeasonEndGameOverScreen.cs` | – (`[APPROX]`) |

Bildschirmfotos: `emtool screens gamedata <ziel> saison saison_aufstieg`.

Die Pfeile vor den Vereinsnamen sind Zeichen des Mini-Fonts: `&` = Pfeil nach unten, `@` = Pfeil nach oben,
`*` = Kreis. Die Fenster am Saisonende sind dunkler gefüllt (Palettenindex 4) als die übrigen grau-blauen Fenster.

## Offen

- Pokal auf dem Meisterfoto: das Original setzt je Titel einen Pokal aus den Einzelteilen in kb\16 (rechts oben)
  zusammen; hier bleibt der Pokal des Fotos.
- Ereignissymbole: Bit 2 (Symbol 0) fehlt; Meister (Bit 3) zeigt im Original das Symbol 5, das Remake den Puck (siehe Tabelle oben).
  Prämien außer 1. Platz Oberliga.
- Play-Off-Format und Termine der Aufstiegsrunden genauer am Disassembly prüfen.
- Bisherige Begegnungen in der Infoleiste reichen im Original über Saisongrenzen hinweg (nicht Teil dieses Pakets).

## Spielstand-Optionen und Kalenderfunktionen (Spur `optionen-kalender-2`)

Die Optionen aus em.exe 0x2CD1C (Matrix Drittelpause/Ergebnisse/Tabelle je Liga, DEB-Pokal, Europapokal, WM-Spiele) wirken auf Meldungen
im Live-Ablauf und sind im Remake nur lesend angezeigt (Einstellungen → „Spielstand“); keine Zeit, kein Zufall. Der Datumstext
(0x43B1F) und die Termin-Funktionen 0x43C38/0x43E21/0x36169 ändern den Saisonablauf nicht; Einordnung in `docs/reverse-engineering.md`
(„Spielmodus-Optionen … Teil 2“). Ein Terminbildschirm ist ohne Videobeleg nicht gebaut.

