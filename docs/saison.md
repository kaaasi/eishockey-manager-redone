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
