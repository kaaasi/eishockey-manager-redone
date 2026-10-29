# Referenzvideos

Drei Let's-Play-Videos des Originals dienen als Vorlage für Optik und Ablauf. Die Videos selbst liegen nicht im Repo.
Einzelbilder (1 pro Sekunde, 320 × 240) und Übersichtsbögen liegen im privaten Repo `kaaasi/eishockey-manager-referenz`;
laden mit `tools/claude/fetch-reference.sh`. Dateiname = Zeitstempel, z. B. `oberflaeche_1405.png` = 14:05,
`bundestrainer_00745.png` = 0:07:45.

## „Die Oberfläche von EM – einmal alle Funktionen und Menüs“ (720p, 17:35)

Zeigt jeden Bildschirm einmal. Zeitangaben ca.:

| Zeit | Bildschirm |
|---|---|
| 0:20 | Titelbild (`kb\197`) |
| 1:00–2:40 | Spielvorbereitung (`kb\150`): Manager, Vereinsauswahl mit Logoliste, Spielmodus, Level, Dauer, Liga, Stärke |
| 1:00–1:10 | Laden-Dialog (Notizbuch der Spielvorbereitung): Dateiliste in drei Spalten, „VERZEICHNIS:“, „FILENAME :“, Laufwerksknöpfe (`oberflaeche_0100`–`_0103`) |
| 3:40 | DEB-Pokal-Auslosung |
| 4:00–5:20 | Hauptmenü mit den acht großen Kacheln |
| 5:40–6:00 | Finanzen / Bank und Börse (`kb\152`) |
| 6:20–7:40 | Werbung (`kb\151`) mit Firmen-Infos |
| 8:00–8:20 | Stadion (`kb\14`) |
| 8:40 | Highscore (`kb\195`) |
| 9:00–10:00 | Aufstellung und Taktik |
| 10:20–10:40 | Verträge mit Vertragsverhandlung |
| 11:00 | Transferliste |
| 11:20–12:40 | Training (`kb\5`) |
| 13:00 | DEB-Pokalspiele |
| 13:20, 16:00 | Stärken Oberliga |
| 14:00–15:20 | Hauptmenü mit Tabellenkurve, Hinweis zur Relegation |
| 15:40 | Saisonende |
| 16:20 | Vereinsstatistik (Serien, Über-/Unterzahl, Zuschauer, Finanzen) |
| 16:40 | Statistik „Früher“ (Europapokal, DEB-Pokal, Liga) |

## „MEISTER werden in 75 Minuten“ (360p, 1:19:27)

Komplette Karriere von der Oberliga zur Meisterschaft – Referenz für Spielablauf, Saisonrhythmus und Größenordnungen
(Finanzen, Tabellen). Wiederkehrend: Hauptmenü, Tabellen/Stärken, Werbung, Börse, Stadionausbau, Transfers.

**Hinweis:** Das Live-Spiel mit Anzeigetafel und die Torszenen sind in diesem Video nicht zu sehen; an den
Spieltagen erscheint nur der Bildschirm „Spiele laufen“ (z. B. bei 23:00, 35:00, 39:00, 47:00).
Anzeigetafel und Reporter zeigt das WM-Video (Reihe `bundestrainer`); Torszenen fehlen weiterhin.

## „#32 – WM: DEUTSCHLAND gewinnt gegen TOP-Mannschaft“ (720p, 8:09) – Reihe `bundestrainer`

Spielmodus „nur Weltkugel“: Bundestrainer nach dem Sprung zum 14. April 2008, fünf Freundschaftsspiele, die komplette
WM (Gruppe 14.–22. April, Endrunde 24.–28. April) mit Live-Spielen und Weltmeister-Bild. 464 Bilder
(0:00:26–0:08:09, Intro weggelassen), laden mit `tools/claude/fetch-reference.sh bundestrainer`.
Ausschnitt wie beim 720p-Video „Oberfläche“ (960 × 720 ab x = 320, Faktor 3, per Template-Matching mit `kb\20`,
`kb\22`, `kb\16` geprüft). Verdeckt: unten links die Schulter des Sprechers (ab y ≈ 170 bis x ≈ 40–65), damit meist
die erste kleine Kachel der Kachelreihe; bei 0:07:31 zusätzlich seine Hand. Nicht zu sehen: Spielvorbereitung,
Saisonbeginn, Saisonende, Torszenen, Kachelreihen Statistiken/Tabellen/Paarungen/Gemischtes.

Bildnummer = Zeitstempel `HMMSS` (`bundestrainer_<nummer>.png`):

| Bilder | Bildschirm |
|---|---|
| 00026–00041 | Hauptmenü des Bundestrainers ohne Kachelreihe, Infoleiste „Samstag, 14. April 2008“ (nach dem Sprung), Gegner Schweiz, Konto 500.000 DM |
| 00053–00055, 00057, 00100–00103, 00107, 00114–00115, 00132, 00140, 00211–00212, 00221–00223, 00257–00258, 00305–00306, 00336–00337, 00347–00348, 00404–00406 | Kachelreihe Mannschaft: Aufstellung (verdeckt), Taktik, gesperrt (Verträge), Transfer, Training, leer |
| 00108, 00113, 00136–00137, 00139, 00219, 00259, 00303–00304, 00338, 00413, 00604, 00608–00609, 00611 | Kachelreihe Stärken: drei gesperrt, „WM“ (Länderteams) offen, zwei leer |
| 00042, 00220, 00414–00416, 00418–00421 | Kachelreihe Manager: alle sechs gesperrt |
| 00044–00052 | „Spieler in WM-Kader“ |
| 00056 | Aufstellung Nationalteam |
| 00058–00059 | Taktik |
| 00104–00106, 00116–00119, 00131, 00141–00156, 00209–00210, 00224–00245, 00256, 00307–00323, 00335, 00349–00352, 00403 | „WM Vorbereitung“ (Gegnerwahl, Heim/Auswärts, Kosten, Kontostand) |
| 00109–00111, 00133–00135, 00214–00218, 00300–00302, 00339–00345, 00407–00411, 00605–00607 | „Stärken Länderteams – Gesamt“ |
| 00120, 00157, 00246, 00324, 00353 | Banner „Freundschaftsspiel“ |
| 00122–00129, 00159–00208, 00247–00255, 00326–00334, 00354–00402 | Live-Spiel mit Anzeigetafel und Reporter: Freundschaftsspiele gegen USA, Schweden, Tschechien, Schweiz, Russland (auswärts) |
| 00422, 00517, 00547, 00612–00613, 00633, 00706–00707, 00730 | Banner „Weltmeisterschaft“ |
| 00424–00436, 00453–00502, 00519–00528, 00548–00555, 00614–00621 | Live-Spiel WM-Gruppe: Schweiz, Italien, Tschechien, Österreich, Finnland |
| 00437–00445, 00504–00506, 00529–00534, 00557–00558, 00623–00624 | WM-Spieltag: Ergebnisse „Gruppe 1/2 60. Min.“ |
| 00446–00449, 00509–00513, 00535–00544, 00559–00602, 00625, 00627–00629 | Tabelle WM „Gruppe 1“ / „Gruppe 2“ (nach jedem Spieltag) |
| 00450, 00515–00516, 00545–00546, 00603, 00630–00632, 00655–00656, 00700–00705, 00726–00729 | Hauptmenü mit Infoleiste an den WM-Tagen 16.–28. April (Konto 0 DM) |
| 00417, 00610, 00657–00659 | Gegner-Fenster aus der Infoleiste (Schweiz, Finnland, Russland) |
| 00634–00641, 00708–00718, 00732–00743 | Live-Spiel WM-Endrunde: USA, Russland, Tschechien |
| 00642–00645, 00647–00653, 00719–00725, 00744 | „WM-Endrundenspiele“ (60./70. Minute, Endstand) |
| 00646 | „Spiele laufen“ |
| 00745–00809 | „Weltmeister“ (`kb\16`) |

Übergänge (Fenster baut sich auf, leeres Spielfeld): 00043, 00112, 00121, 00130, 00138, 00158, 00213, 00325, 00346, 00412, 00423, 00451–00452, 00503, 00507–00508, 00514, 00518, 00556, 00622, 00626, 00654, 00731.
