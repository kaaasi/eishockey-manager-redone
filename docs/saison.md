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
Am Saisonende steht „Montag, 29. April“, obwohl der Kalender einen Sonntag ergibt – wird so nachgebildet.

## Infoleiste

Datum und „n.Spieltag“ beziehen sich auf den **nächsten** Termin (Beleg: am „Sonntag, 4. Februar“ mit
„44.Spieltag“ werden drei bisherige Begegnungen mit dem Gegner des 44. Spieltags gezeigt).

## Ablauf einer Saison (`Core/SeasonFlow.cs`)

„und tschüß…“ (`GameState.Continue()`) spielt den aktuellen Tag und läuft Tag für Tag weiter – auch über
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

**Entlassung**: 8 Wochen am Stück unter −1,5 Mio. DM; im Hot-Seat spielen die übrigen Manager weiter.

**Erweiterung**: DEB-Pokal, Europapokal und WM melden sich über `ISeasonCompetition` in `GameState.Competitions`
an und werden an ihren Kalendertagen aufgerufen (Infoleiste, Anhalten von „und tschüß…“, Titel am Saisonende).
