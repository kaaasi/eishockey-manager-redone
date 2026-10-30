# Referenzvideos

Drei Let's-Play-Videos, die 29-teilige Reihe „Eishockey Manager EM“ (Reihen `folge01` … `folge29`) und die Videos des
Releases `videos-roh-2` (Reihen `eisbaeren_f01` … `eisbaeren_f65`, `duell_s2a` … `duell_s6d`, `speedrun_t1`/`_t2`, unten)
dienen als Vorlage für Optik und Ablauf. Die Videos selbst liegen nicht im Repo.
Einzelbilder (1 pro Sekunde, 320 × 240) und Übersichtsbögen liegen im privaten Repo `kaaasi/eishockey-manager-referenz`;
laden mit `tools/claude/fetch-reference.sh`. Dateiname = Zeitstempel, z. B. `oberflaeche_1405.png` = 14:05,
`bundestrainer_00745.png` = 0:07:45, `folge06_01256.png` = Folge 6, 0:12:56, `eisbaeren_f24_01048.png` = Eisbären-Folge 24, 0:10:48.

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

## Reihe „Eishockey Manager EM“ (360p, 29 Folgen, zusammen 14:31 h) – Reihen `folge01` … `folge29`

Let's Play der deutschen DOS-Version im Vereinsmodus (Berliner SC, Start in der Oberliga, bis in die Bundesliga mit
Play-Offs, DEB-Pokal und Europapokal; Folge 8 beginnt ein neues Spiel). Quelle: Release `videos-roh-1` des Referenz-Repos.
Laden: `tools/claude/fetch-reference.sh folge06` (einzelne Folge, 5–25 MB) bzw. `folgen` (alle, ca. 0,4 GB).

**Anders als bei den anderen Reihen entdoppelt und ausgedünnt:** Von aufeinanderfolgenden, bis auf Mauszeiger und
Einzelzeichen gleichen Bildern bleibt nur das erste (Zeitstempel im Namen). Damit das Referenz-Repo unter 5 GB bleibt,
sind außerdem häufige Bildschirme (Hauptmenü, Spieltag, Live-Spiel, Transferliste, Verträge, Werbung, Finanzen,
Aufstellung, Tabelle Liga, Stadion …) nur abgelegt, wenn sie sich von allen schon abgelegten derselben Folge deutlich
unterscheiden (4.221 von 25.638 Bildern); seltene Bildschirme und alle hier genannten Bilder sind vollständig da. Die
Zeiten in den Tabellen beziehen sich auf das Video – nicht jede genannte Sekunde hat eine Datei.
Schwellwert, Ausschnitt, Versatz und Verdeckung je Folge: README des Referenz-Repos. Vollständiger Kapitelindex je Folge
(alle Bildschirme mit Zeiten): `uebersicht/kapitel.md` im Referenz-Repo; Kontaktbögen `uebersicht/folgeNN_ab_*.png`.

Die Bildschirme wurden automatisch zugeordnet (Gruppierung, von Hand benannt, geprüft per Template-Matching gegen
Hintergründe, Titelleiste und Kachelsymbole des Originals und gegen `emtool screens`). In allen Folgen sind die WM-Kacheln
(Tabellen/Paarungen Platz 6, Stärken „Länderteams“, Statistiken „WM-Statistik“) und „Highscore“ gesperrt.

| Reihe | Länge | Bilder (behalten / roh) | Ausschnitt (360p) | Versatz `kb\22` |
|---|---|---|---|---|
| `folge01` | 0:29:04 | 831 / 1744 | 478 × 358 ab x = 82 | (-0.7 \| +0.1) |
| `folge02` | 0:26:42 | 761 / 1602 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge03` | 0:27:22 | 799 / 1642 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge04` | 0:31:06 | 938 / 1866 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge05` | 0:30:18 | 859 / 1818 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge06` | 0:30:05 | 810 / 1805 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge07` | 0:37:13 | 1073 / 2233 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge08` | 0:30:10 | 872 / 1810 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge09` | 0:30:39 | 915 / 1839 | 478 × 358 ab x = 82 | (-0.6 \| +0.1) |
| `folge10` | 0:26:56 | 870 / 1616 | 480 × 360 ab x = 82 | (-0.5 \| +0.1) |
| `folge11` | 0:30:39 | 836 / 1839 | 480 × 360 ab x = 82 | (-0.5 \| +0.1) |
| `folge12` | 0:30:06 | 1019 / 1806 | 480 × 360 ab x = 82 | (+0.8 \| +0.1) |
| `folge13` | 0:30:30 | 957 / 1830 | 480 × 360 ab x = 82 | (-0.5 \| +0.1) |
| `folge14` | 0:31:17 | 989 / 1877 | 480 × 360 ab x = 82 | (-0.5 \| +0.1) |
| `folge15` | 0:31:11 | 869 / 1871 | 480 × 360 ab x = 82 | (-0.5 \| +0.1) |
| `folge16` | 0:31:19 | 896 / 1879 | 480 × 360 ab x = 82 | (-0.5 \| +0.1) |
| `folge17` | 0:30:54 | 922 / 1854 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge18` | 0:14:05 | 456 / 845 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge19` | 0:31:15 | 898 / 1875 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge20` | 0:29:09 | 847 / 1749 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge21` | 0:30:11 | 922 / 1811 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge22` | 0:31:02 | 943 / 1862 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge23` | 0:28:49 | 903 / 1729 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge24` | 0:29:46 | 936 / 1786 | 480 × 360 ab x = 80 | (+0.2 \| +0.1) |
| `folge25` | 0:31:24 | 941 / 1884 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge26` | 0:35:40 | 1017 / 2140 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge27` | 0:29:55 | 847 / 1795 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge28` | 0:33:29 | 896 / 2009 | 480 × 360 ab x = 80 | (-0.3 \| +0.3) |
| `folge29` | 0:30:45 | 816 / 1845 | 480 × 360 ab x = 0 | (+0.2 \| +0.2) |

### Fundliste (gezielt gesucht)

| Gesucht | Bilder (`folgeNN_HMMSS`) |
|---|---|
| DEB-Pokal-Auslosung mit Dialog „An Alle: Sind Ihre Nerven stark genug, um die DEB-Pokal-Auslosung live zu verfolgen?“ | `folge01_00217`, `folge08_00102` (Dialog über der leeren Ziehung) |
| Übersicht „DEB-Pokalspiele“ | Erste Runde vor dem Spiel `folge08_00607`, mit Ergebnissen `folge13_02545` (29.9.), Achtelfinale `folge26_00919`, `folge28_00339`, Viertelfinale `folge17_00226`, `folge19_00213`, Halbfinale `folge22_02923`, `folge22_03011`, Endspiel `folge07_03600`, `folge15_03026`; alle Stellen unten |
| Paarungen 1./2. Liga/Oberliga (Spieltag mit Ergebnissen) | Oberliga `folge06_01256`, `folge12_00424`; Zweite Liga `folge17_02406`; Bundesliga `folge21_02149`, `folge29_00152`; Kachelreihe Paarungen z. B. `folge06_01251`, `folge12_00423` |
| Vereinstaktik | `folge01_02643`, `folge02_00341`, `folge10_00300`–`00328`, `folge27_00749`–`00802`; alle Stellen unten |
| Vereinsaufstellung | durchgehend (1.249 Bilder), z. B. `folge01_00528`–`00550`, `folge01_01000`–`01052` |
| Spielmodus „Liga + Weltkugel“ (Bundestrainer) | **nicht gefunden** – keine offene WM-Kachel, kein Bildschirm des Nationalteams |
| Monatsausgaben zu Spielbeginn | Vereinsstatistik mit „Einnahmen/Ausgaben / Monat“ vor dem ersten Spieltag `folge01_00522`; Finanzen (Bank) zu Spielbeginn `folge01_00508`–`00510` |
| Europapokal mit Ergebnissen | Gruppe 1/2 `folge04_01241`, `folge04_01245`, `folge20_02316`, `folge20_02318`; Endrunde `folge04_01243`, `folge04_01247` |
| WM-Endrunde | **nicht gefunden** |
| Kachelreihen Statistiken / Tabellen / Gemischtes | Statistiken `folge01_00526`, `folge01_01018`; Tabellen `folge03_00018`, `folge04_01301`; Gemischtes `folge01_00600`, `folge04_01238` (Laden, Sichern, Einst., drei leere Plätze) |
| Torszenen | **nicht gefunden** (Suche in den Hallenbildern `torszenen/halle_*`) |
| Live-Spiel mit Vereinen | durchgehend (3.329 Bilder), z. B. `folge01_00615`–`00724`, `folge17_00644`; Endergebnis/Penaltyschießen `folge09_01150`, `folge17_00746` |
| Highscore am Spielende | **nicht gefunden** (Kachel „Highscore“ gesperrt, kein Spielende) |

Außerdem neu belegt: Spielvorbereitung mit Logoliste und Kopfwahl (`folge01_00000`–`00151`, `folge08_00000`–`00052`),
Infoleiste „Vor dem ersten Spieltag“ (`folge01_00221`), Aufstiegsrunde, Play-Off-Spiele, Meisterfotos „DEB Pokalsieger“
und „Europacupsieger“, Saisonende, Ewige Tabelle, Spielerstatistik, Fairplay, Jugendteams, Passwortabfrage.

### Seltene Bildschirme über alle Folgen

`F6 0:12:57` = `folge06_01257.png`. Bereiche fassen behaltene Bilder mit höchstens 30 s Abstand zusammen. Nicht
aufgeführt, weil in jeder Folge vielfach: Hauptmenü (mit Kachelreihe Manager/Mannschaft), Spieltag („Spiele laufen“),
Banner, Live-Spiel, Transferliste, Verträge, Werbung, Finanzen, Aufstellung, Tabelle Liga, Stadion.

| Bildschirm | Stellen |
|---|---|
| Aufstiegsrunde (Paarungen und Tabelle) | F6 0:12:57; F7 0:33:07–0:33:10, 0:36:02; F12 0:04:25–0:04:47; F17 0:24:07 |
| DEB-Pokal-Auslosung | F1 0:02:17; F8 0:01:02 |
| DEB-Pokalspiele | F7 0:36:00; F8 0:06:07; F13 0:25:45; F15 0:30:26–0:30:31; F17 0:02:26–0:02:30, 0:05:25; F18 0:00:25, 0:13:01; F19 0:02:13, 0:12:47–0:12:51, 0:24:33; F21 0:01:25; F22 0:29:23, 0:30:11–0:30:16; F24 0:28:02; F25 0:00:12–0:00:24; F26 0:09:19; F27 0:03:36, 0:21:58; F28 0:03:39 |
| Einstellungen | F1 0:05:54; F7 0:08:20–0:08:21; F8 0:04:33 |
| Endergebnis / Penaltyschießen | F9 0:11:50–0:12:14; F10 0:22:00–0:22:18; F17 0:07:46–0:08:04; F19 0:19:34; F21 0:29:08; F24 0:03:52; F28 0:05:00 |
| Europapokal Endrunde | F4 0:12:43–0:12:47 |
| Europapokal Gruppe | F4 0:12:41–0:12:45; F20 0:23:16–0:23:18 |
| Ewige Tabelle | F6 0:26:36–0:26:42; F10 0:16:44–0:16:48; F15 0:00:36–0:00:40; F16 0:10:18; F19 0:28:10–0:28:14, 0:30:10–0:30:16; F21 0:24:36–0:24:45; F23 0:26:19–0:26:27 |
| Fairplay | F8 0:21:38; F9 0:24:00; F12 0:20:50; F14 0:21:56; F15 0:10:01; F25 0:07:16 |
| Hauptmenü, Kachelreihe Gemischtes | F1 0:06:00; F4 0:12:38; F6 0:03:41; F7 0:33:03, 0:36:59; F8 0:04:28–0:04:40; F13 0:00:00; F15 0:00:04; F19 0:18:27; F23 0:00:08–0:00:09; F26 0:07:33; F29 0:30:18–0:30:32 |
| Hauptmenü, Kachelreihe Paarungen | F4 0:12:40; F6 0:12:51–0:12:55; F7 0:33:05, 0:36:01; F8 0:06:06; F12 0:04:23; F15 0:30:25; F17 0:05:24, 0:24:04; F18 0:00:08–0:00:09, 0:12:59; F19 0:02:12, 0:25:07; F20 0:23:15; F21 0:21:48; F22 0:29:22, 0:30:10; F23 0:20:13; F24 0:28:01; F25 0:00:11; F26 0:09:18; F27 0:03:35, 0:21:57; F29 0:01:51, 0:07:00 |
| Hauptmenü, Kachelreihe Statistiken | F1 0:05:26, 0:10:18–0:10:23, 0:15:41–0:15:47, 0:17:34–0:17:38, 0:25:28; F2 0:22:26; F3 0:14:57; F4 0:12:37; F6 0:26:30–0:26:43; F8 0:21:36–0:21:42; F9 0:18:48–0:19:09, 0:23:59; F10 0:16:41–0:16:53; F12 0:20:49–0:20:56; F14 0:21:55–0:22:01; F15 0:00:12–0:00:34, 0:10:00–0:10:04, 0:11:12, 0:30:24; F16 0:10:23, 0:30:29–0:30:47; F17 0:05:10, 0:17:49, 0:19:34, 0:20:25; F18 0:01:10, 0:07:21; F19 0:12:55–0:13:18, 0:28:06–0:28:16, 0:30:09; F20 0:03:57, 0:04:56–0:05:04; F21 0:00:37–0:00:48, 0:24:33–0:24:48, 0:26:34–0:26:38; F23 0:14:31, 0:22:22, 0:26:17–0:26:35; F24 0:05:37–0:05:42, 0:12:03–0:12:10; F25 0:05:54–0:06:01, 0:07:15–0:07:22, 0:10:13, 0:11:38–0:11:47; F26 0:07:53, 0:08:49; F28 0:05:10 |
| Hauptmenü, Kachelreihe Stärken | F1 0:05:44–0:05:48, 0:11:50–0:11:54, 0:15:37; F2 0:00:00, 0:08:28; F3 0:06:38, 0:19:54; F4 0:12:10; F5 0:25:45; F6 0:20:47, 0:22:13–0:22:19, 0:27:24; F7 0:05:55–0:06:01, 0:36:48; F8 0:04:46, 0:17:30–0:17:43; F9 0:05:26–0:05:37, 0:23:03; F10 0:00:16, 0:01:15, 0:05:47, 0:06:19, 0:16:30–0:16:35; F11 0:29:02; F12 0:08:00–0:08:05, 0:18:15, 0:23:43; F13 0:02:32, 0:20:38–0:20:46, 0:29:58; F14 0:00:22–0:00:46, 0:06:22–0:06:32, 0:14:11, 0:22:22; F15 0:00:13, 0:00:46–0:00:58, 0:05:45, 0:07:28–0:08:05, 0:18:22, 0:20:12; F16 0:30:48; F17 0:01:18, 0:27:40; F18 0:08:18; F19 0:30:23–0:30:33; F20 0:02:38–0:02:42; F21 0:23:22; F22 0:15:43, 0:30:52; F24 0:07:09–0:07:17; F25 0:09:50, 0:27:39; F26 0:02:59–0:03:04, 0:03:37–0:03:48; F27 0:01:40, 0:28:28–0:28:41; F28 0:23:39–0:23:54 |
| Hauptmenü, Kachelreihe Tabellen | F3 0:00:18–0:00:24; F4 0:13:01; F8 0:06:12, 0:28:58; F13 0:02:26, 0:25:48; F14 0:14:23, 0:26:24; F17 0:02:33; F18 0:00:27–0:00:31, 0:12:49; F20 0:23:25; F22 0:30:20; F24 0:28:08; F25 0:09:51; F27 0:01:31–0:01:49 |
| Hauptmenü, Kachelreihe Tabellen/Paarungen | F4 0:04:38; F6 0:00:46; F7 0:33:12, 0:36:06; F10 0:24:23; F11 0:25:35; F12 0:04:48, 0:19:21–0:19:25; F13 0:00:17; F15 0:30:33; F17 0:05:28; F19 0:02:17, 0:07:05, 0:10:36, 0:18:26–0:18:30, 0:24:34, 0:25:14; F21 0:01:28, 0:05:36, 0:22:02; F22 0:00:08; F23 0:00:16, 0:17:54; F24 0:22:06, 0:25:21; F25 0:00:25, 0:09:59; F26 0:17:41; F28 0:03:40; F29 0:01:56–0:02:00 |
| Jugendteams | F4 0:28:25 |
| Meisterfoto (Meister, DEB-Pokal-, Europacupsieger) | F1 0:24:27; F2 0:09:28–0:09:32; F3 0:16:06, 0:16:39; F4 0:27:57–0:28:01; F5 0:19:34; F6 0:14:24–0:14:27; F7 0:15:04, 0:36:29–0:36:33; F8 0:20:39; F9 0:03:21–0:03:25; F10 0:13:49–0:13:53; F12 0:03:55–0:03:59, 0:27:21; F13 0:14:11–0:14:15; F14 0:12:36, 0:27:43–0:27:48; F15 0:18:48; F16 0:06:33, 0:07:08; F17 0:24:30–0:24:32; F19 0:27:20–0:27:32; F21 0:21:10–0:21:14; F23 0:20:04, 0:21:33–0:21:39; F25 0:21:19, 0:21:51; F27 0:10:57, 0:11:44; F29 0:10:13–0:10:24 |
| Paarungen Liga (Spieltag mit Ergebnissen) | F6 0:12:56; F12 0:04:24; F17 0:24:06; F21 0:21:49; F29 0:01:52 |
| Passwortabfrage | F23 0:23:08–0:23:15 |
| Play-Off-Spiele | F19 0:25:10; F21 0:21:50–0:22:00; F23 0:20:17–0:20:24; F29 0:07:02 |
| Saisonende | F2 0:09:55–0:10:04; F3 0:18:07–0:18:41; F4 0:28:31–0:28:54; F6 0:19:24–0:19:46; F9 0:03:48–0:04:05; F10 0:14:35–0:14:41; F12 0:04:53–0:05:06; F13 0:15:56–0:16:12; F14 0:28:50–0:29:01; F16 0:08:17–0:08:31; F17 0:24:50–0:25:02; F19 0:28:22–0:28:37; F21 0:22:09–0:22:20; F23 0:22:59–0:23:19; F25 0:22:57–0:23:10; F27 0:15:00–0:15:18; F29 0:11:10–0:11:32 |
| Speichern | F7 0:37:01; F8 0:04:29; F29 0:30:20 |
| Spielerstatistik | F9 0:18:51; F15 0:10:07–0:11:08; F17 0:17:52–0:18:07, 0:19:37–0:19:44, 0:20:18 |
| Spielvorbereitung | F1 0:00:00–0:01:51; F8 0:00:00–0:00:52 |
| Statistik „Früher“ | F3 0:14:58; F6 0:26:33; F15 0:00:30–0:00:31; F17 0:05:11; F19 0:12:56–0:12:58; F21 0:00:40; F23 0:14:32, 0:22:23–0:22:29; F25 0:11:39–0:11:45; F28 0:05:11–0:05:17 |
| Stärken | F1 0:05:45, 0:11:47–0:11:51, 0:15:35; F2 0:08:24, 0:10:48–0:10:50; F3 0:06:40–0:06:43, 0:19:55; F4 0:12:06–0:12:09; F6 0:00:48, 0:20:45, 0:27:20–0:27:23, 0:28:37–0:28:41; F7 0:02:45, 0:05:56; F8 0:17:31–0:17:41; F9 0:05:32, 0:23:04; F10 0:00:18–0:00:56, 0:05:48, 0:16:31; F11 0:22:08–0:22:09, 0:28:56–0:28:59; F12 0:08:04, 0:23:46–0:23:47; F13 0:02:35, 0:20:40–0:20:45, 0:29:59; F14 0:00:25, 0:06:24, 0:14:12–0:14:19, 0:22:18; F15 0:00:14, 0:00:47–0:00:56, 0:05:41–0:05:44, 0:07:30, 0:08:02, 0:16:24–0:16:29, 0:18:23, 0:20:09; F16 0:30:49; F17 0:01:17, 0:27:42; F18 0:08:14; F19 0:30:24–0:30:30; F20 0:02:40; F24 0:07:10; F25 0:08:56, 0:09:38, 0:27:40; F26 0:02:56, 0:03:39; F27 0:01:34, 0:28:31–0:28:35; F28 0:23:41–0:23:48 |
| Taktik | F1 0:26:43–0:26:45, 0:27:30; F2 0:03:41–0:03:42, 0:08:15, 0:17:28–0:17:29, 0:21:00, 0:23:31, 0:26:24; F8 0:04:26; F9 0:15:01; F10 0:03:00–0:03:28, 0:12:14; F11 0:29:59; F12 0:00:40, 0:13:10; F14 0:02:24; F17 0:22:23–0:22:48; F23 0:00:26, 0:02:14, 0:05:18, 0:16:47, 0:18:04, 0:21:15; F24 0:08:09, 0:25:04–0:25:25, 0:29:33; F25 0:02:15, 0:14:59; F26 0:28:23; F27 0:07:49–0:08:02, 0:14:24, 0:18:56–0:19:14; F28 0:01:07, 0:13:50–0:14:07; F29 0:23:42 |
| Training | F1 0:02:31–0:03:56; F8 0:01:07–0:02:14; F14 0:05:31; F20 0:22:04; F21 0:10:00; F23 0:03:12, 0:20:50; F24 0:08:49; F25 0:28:37 |
| Vereinsstatistik (Serien, Zuschauer, Einnahmen/Ausgaben je Monat) | F1 0:05:22, 0:10:20–0:10:22, 0:15:42, 0:17:36, 0:25:29; F2 0:10:57, 0:22:23; F16 0:30:30; F18 0:01:12, 0:07:23; F19 0:13:20; F20 0:03:58, 0:04:57; F21 0:00:35, 0:26:36; F24 0:05:39, 0:12:04; F25 0:05:55, 0:10:10; F26 0:07:47, 0:08:46 |

## Release `videos-roh-2` (360p, 83 Videos, zusammen 50:21 h)

Quelle: Release `videos-roh-2` des Referenz-Repos. Laden z. B. `tools/claude/fetch-reference.sh eisbaeren_f24` (eine Reihe,
5–35 MB) oder die Gruppen `eisbaeren` (ca. 1,3 GB), `duell` (ca. 0,3 GB), `speedrun`. Vollständiger Kapitelindex mit allen
Bildschirmen und Zeiten: `uebersicht/kapitel2.md` im Referenz-Repo (⚠ = Fundstelle durch den YouTuber verdeckt);
Kontaktbögen `uebersicht/<reihe>_ab_*.jpg`; Ausschnitt, Versatz, Verdeckung und Bildzahl je Reihe im README dort.

- **`eisbaeren_f01` … `eisbaeren_f65`** (ohne Folge 41: Podcast ohne Spielbild): Eisbären Berlin von der Oberliga an im
  Spielmodus **Liga mit Weltkugel** – der Spieler ist zugleich Bundestrainer, WM-Kacheln offen; WM-Folgen 24, 29, 32, 35, 38,
  44, 47, 50, 53, 56.
- **`duell_s2a` … `duell_s6d`**: „Das Duell“, zwei menschliche Manager in einem Spiel (Saisons 2, 4, 6); Live-Spiel mit zwei
  Partien gleichzeitig, Torszenen eingeschaltet.
- **`speedrun_t1`, `speedrun_t2`**: neues Spiel mit Spielvorbereitung bis zum Triple.
- Nicht verarbeitet: fünf Erklärvideos (Sprecher im Vollbild, Spielausschnitte nur klein eingeblendet).

Entdoppelt und ausgedünnt wie `folge01` … (79.476 → 18.569 Bilder); beim Entdoppeln
zählt nur die Fläche außerhalb der Verdeckung, eine Bewegung des YouTubers allein ergibt kein neues Bild.

**Verdeckung:** Der YouTuber sitzt links neben dem Spielbild; seine Schulter ragt an der **linken Kante unten** hinein
(Eisbären typisch bis x ≈ 23–49, ab y ≈ 170–203; Speedrun bis x ≈ 74, Folge 65 bis x ≈ 89 ab y ≈ 146). Betroffen ist vor allem die erste
kleine Kachel der Kachelreihe und der Anfang von Listen unten links. „Das Duell“ und Folge 59 sind unverdeckt.

| Reihe | Länge | Bilder (abgelegt / entdoppelt) | Ausschnitt (360p) | Verdeckung typisch (320 × 240) | ⚠ |
|---|---|---|---|---|---:|
| `duell_s2a` | 0:41:27 | 202 / 872 | 480 × 360 ab x = 0 | – |  |
| `duell_s2b` | 0:41:29 | 442 / 1041 | 480 × 360 ab x = 0 | – |  |
| `duell_s2c` | 0:36:12 | 296 / 808 | 480 × 360 ab x = 0 | – |  |
| `duell_s4a` | 0:40:30 | 303 / 895 | 480 × 360 ab x = 0 | – |  |
| `duell_s4b` | 0:40:10 | 520 / 1040 | 480 × 360 ab x = 0 | – |  |
| `duell_s4c` | 0:34:29 | 303 / 788 | 480 × 360 ab x = 0 | – |  |
| `duell_s4d` | 0:42:12 | 526 / 1064 | 480 × 360 ab x = 0 | – |  |
| `duell_s6a` | 0:40:15 | 190 / 857 | 480 × 360 ab x = 0 | – |  |
| `duell_s6b` | 0:41:00 | 207 / 879 | 480 × 360 ab x = 0 | – |  |
| `duell_s6c` | 0:33:48 | 170 / 699 | 480 × 360 ab x = 0 | – |  |
| `duell_s6d` | 0:32:26 | 215 / 741 | 480 × 360 ab x = 0 | – |  |
| `eisbaeren_f01` | 0:40:53 | 308 / 880 | 480 × 360 ab x = 160 | x ≤ 22 (90 %: 25), y ≥ 179 |  |
| `eisbaeren_f02` | 0:22:24 | 114 / 405 | 480 × 360 ab x = 160 | x ≤ 22 (90 %: 23), y ≥ 178 |  |
| `eisbaeren_f03` | 0:46:02 | 248 / 1117 | 480 × 360 ab x = 160 | x ≤ 23 (90 %: 53), y ≥ 178 |  |
| `eisbaeren_f04` | 0:56:56 | 218 / 1813 | 480 × 360 ab x = 160 | x ≤ 25 (90 %: 31), y ≥ 174 |  |
| `eisbaeren_f05` | 1:02:31 | 224 / 2178 | 480 × 360 ab x = 160 | x ≤ 24 (90 %: 34), y ≥ 176 |  |
| `eisbaeren_f06` | 0:59:37 | 204 / 1581 | 480 × 360 ab x = 160 | x ≤ 66 (90 %: 66), y ≥ 166 |  |
| `eisbaeren_f07` | 0:44:21 | 210 / 1281 | 480 × 360 ab x = 160 | x ≤ 32 (90 %: 59), y ≥ 211 |  |
| `eisbaeren_f08` | 0:38:37 | 183 / 1082 | 480 × 360 ab x = 160 | x ≤ 19 (90 %: 69), y ≥ 183 |  |
| `eisbaeren_f09` | 0:36:38 | 204 / 1082 | 480 × 360 ab x = 160 | x ≤ 31 (90 %: 40), y ≥ 218 |  |
| `eisbaeren_f10` | 0:39:54 | 239 / 1233 | 480 × 360 ab x = 160 | x ≤ 19 (90 %: 77), y ≥ 182 |  |
| `eisbaeren_f11` | 0:29:43 | 188 / 753 | 480 × 360 ab x = 160 | x ≤ 21 (90 %: 32), y ≥ 180 |  |
| `eisbaeren_f12` | 0:34:33 | 183 / 1034 | 480 × 360 ab x = 160 | x ≤ 38 (90 %: 42), y ≥ 185 |  |
| `eisbaeren_f13` | 0:33:28 | 212 / 1002 | 480 × 360 ab x = 160 | x ≤ 36 (90 %: 44), y ≥ 186 |  |
| `eisbaeren_f14` | 0:34:58 | 184 / 952 | 480 × 360 ab x = 160 | x ≤ 36 (90 %: 39), y ≥ 189 |  |
| `eisbaeren_f15` | 0:51:47 | 238 / 1353 | 480 × 360 ab x = 160 | x ≤ 40 (90 %: 50), y ≥ 189 |  |
| `eisbaeren_f16` | 0:43:07 | 234 / 1271 | 480 × 360 ab x = 160 | x ≤ 29 (90 %: 35), y ≥ 222 |  |
| `eisbaeren_f17` | 0:40:30 | 214 / 1210 | 480 × 360 ab x = 160 | x ≤ 19 (90 %: 36), y ≥ 217 |  |
| `eisbaeren_f18` | 0:30:05 | 206 / 971 | 480 × 360 ab x = 160 | x ≤ 23 (90 %: 45), y ≥ 199 |  |
| `eisbaeren_f19` | 0:59:24 | 283 / 1834 | 480 × 360 ab x = 160 | x ≤ 33 (90 %: 44), y ≥ 192 |  |
| `eisbaeren_f20` | 0:46:51 | 253 / 1238 | 480 × 360 ab x = 160 | x ≤ 42 (90 %: 77), y ≥ 171 |  |
| `eisbaeren_f21` | 0:39:12 | 216 / 994 | 480 × 360 ab x = 160 | x ≤ 39 (90 %: 77), y ≥ 184 | 2 |
| `eisbaeren_f22` | 0:36:59 | 200 / 989 | 480 × 360 ab x = 160 | x ≤ 40 (90 %: 77), y ≥ 171 |  |
| `eisbaeren_f23` | 0:54:28 | 299 / 1572 | 480 × 360 ab x = 160 | x ≤ 39 (90 %: 62), y ≥ 173 |  |
| `eisbaeren_f24` | 0:18:08 | 152 / 547 | 480 × 360 ab x = 160 | x ≤ 42 (90 %: 49), y ≥ 170 | 1 |
| `eisbaeren_f25` | 0:34:32 | 229 / 975 | 480 × 360 ab x = 160 | x ≤ 30 (90 %: 37), y ≥ 188 |  |
| `eisbaeren_f26` | 0:40:32 | 225 / 1065 | 480 × 360 ab x = 160 | x ≤ 31 (90 %: 37), y ≥ 189 |  |
| `eisbaeren_f27` | 0:34:46 | 260 / 1054 | 480 × 360 ab x = 160 | x ≤ 33 (90 %: 38), y ≥ 187 | 1 |
| `eisbaeren_f28` | 0:44:07 | 262 / 1187 | 480 × 360 ab x = 160 | x ≤ 32 (90 %: 43), y ≥ 188 | 1 |
| `eisbaeren_f29` | 0:12:33 | 115 / 376 | 480 × 360 ab x = 160 | x ≤ 30 (90 %: 36), y ≥ 188 |  |
| `eisbaeren_f30` | 0:36:11 | 214 / 916 | 480 × 360 ab x = 160 | x ≤ 42 (90 %: 51), y ≥ 170 |  |
| `eisbaeren_f31` | 0:38:46 | 235 / 977 | 480 × 360 ab x = 160 | x ≤ 40 (90 %: 61), y ≥ 170 |  |
| `eisbaeren_f32` | 0:08:10 | 86 / 206 | 480 × 360 ab x = 160 | x ≤ 39 (90 %: 62), y ≥ 172 | 1 |
| `eisbaeren_f33` | 0:29:58 | 206 / 779 | 480 × 360 ab x = 160 | x ≤ 43 (90 %: 66), y ≥ 168 |  |
| `eisbaeren_f34` | 0:32:39 | 235 / 863 | 480 × 360 ab x = 160 | x ≤ 45 (90 %: 67), y ≥ 170 |  |
| `eisbaeren_f35` | 0:08:32 | 89 / 199 | 480 × 360 ab x = 160 | x ≤ 43 (90 %: 53), y ≥ 170 |  |
| `eisbaeren_f36` | 0:37:14 | 200 / 869 | 480 × 360 ab x = 160 | x ≤ 34 (90 %: 71), y ≥ 180 |  |
| `eisbaeren_f37` | 0:40:44 | 244 / 1156 | 480 × 360 ab x = 160 | x ≤ 33 (90 %: 52), y ≥ 184 |  |
| `eisbaeren_f38` | 0:08:20 | 74 / 184 | 480 × 360 ab x = 160 | x ≤ 33 (90 %: 38), y ≥ 181 |  |
| `eisbaeren_f39` | 0:46:59 | 295 / 1272 | 480 × 360 ab x = 160 | x ≤ 32 (90 %: 99), y ≥ 182 | 71 |
| `eisbaeren_f40` | 1:04:53 | 286 / 1773 | 480 × 360 ab x = 160 | x ≤ 32 (90 %: 131), y ≥ 182 | 46 |
| `eisbaeren_f42` | 0:43:35 | 269 / 1147 | 480 × 360 ab x = 160 | x ≤ 32 (90 %: 38), y ≥ 186 |  |
| `eisbaeren_f43` | 0:56:16 | 324 / 1583 | 480 × 360 ab x = 160 | x ≤ 31 (90 %: 37), y ≥ 187 | 1 |
| `eisbaeren_f44` | 0:14:27 | 118 / 394 | 480 × 360 ab x = 160 | x ≤ 31 (90 %: 36), y ≥ 186 | 1 |
| `eisbaeren_f45` | 0:58:21 | 339 / 1684 | 480 × 360 ab x = 160 | x ≤ 35 (90 %: 77), y ≥ 184 |  |
| `eisbaeren_f46` | 0:45:40 | 254 / 1297 | 480 × 360 ab x = 160 | x ≤ 33 (90 %: 67), y ≥ 184 |  |
| `eisbaeren_f47` | 0:16:53 | 125 / 492 | 480 × 360 ab x = 160 | x ≤ 37 (90 %: 58), y ≥ 183 |  |
| `eisbaeren_f48` | 0:46:04 | 261 / 1265 | 480 × 360 ab x = 160 | x ≤ 37 (90 %: 77), y ≥ 184 |  |
| `eisbaeren_f49` | 0:51:41 | 291 / 1486 | 480 × 360 ab x = 160 | x ≤ 41 (90 %: 66), y ≥ 183 | 1 |
| `eisbaeren_f50` | 0:12:36 | 100 / 343 | 480 × 360 ab x = 160 | x ≤ 39 (90 %: 60), y ≥ 182 | 1 |
| `eisbaeren_f51` | 0:47:38 | 306 / 1212 | 480 × 360 ab x = 160 | x ≤ 23 (90 %: 28), y ≥ 204 |  |
| `eisbaeren_f52` | 0:39:26 | 249 / 1055 | 480 × 360 ab x = 160 | x ≤ 24 (90 %: 28), y ≥ 204 | 1 |
| `eisbaeren_f53` | 0:10:28 | 111 / 327 | 480 × 360 ab x = 160 | x ≤ 24 (90 %: 30), y ≥ 204 |  |
| `eisbaeren_f54` | 0:41:05 | 260 / 1161 | 480 × 360 ab x = 160 | x ≤ 24 (90 %: 30), y ≥ 200 |  |
| `eisbaeren_f55` | 0:42:54 | 243 / 1151 | 480 × 360 ab x = 160 | x ≤ 26 (90 %: 35), y ≥ 198 |  |
| `eisbaeren_f56` | 0:12:14 | 120 / 376 | 480 × 360 ab x = 160 | x ≤ 33 (90 %: 41), y ≥ 193 |  |
| `eisbaeren_f57` | 0:36:32 | 297 / 981 | 480 × 360 ab x = 160 | x ≤ 36 (90 %: 44), y ≥ 189 |  |
| `eisbaeren_f58` | 0:40:29 | 308 / 1053 | 480 × 360 ab x = 160 | x ≤ 36 (90 %: 40), y ≥ 191 | 2 |
| `eisbaeren_f59` | 0:46:40 | 308 / 1343 | 344 × 258 ab x = 164, y = 44 | – |  |
| `eisbaeren_f60` | 0:41:04 | 321 / 1225 | 480 × 360 ab x = 160 | x ≤ 53 (90 %: 54), y ≥ 180 |  |
| `eisbaeren_f61` | 0:38:36 | 297 / 1149 | 480 × 360 ab x = 160 | x ≤ 53 (90 %: 54), y ≥ 180 |  |
| `eisbaeren_f62` | 0:25:47 | 270 / 887 | 480 × 360 ab x = 160 | x ≤ 50 (90 %: 53), y ≥ 181 |  |
| `eisbaeren_f63` | 0:29:25 | 305 / 1131 | 480 × 360 ab x = 160 | x ≤ 53 (90 %: 71), y ≥ 144 | 1 |
| `eisbaeren_f64` | 0:32:01 | 286 / 1071 | 480 × 360 ab x = 160 | x ≤ 53 (90 %: 54), y ≥ 181 |  |
| `eisbaeren_f65` | 0:45:26 | 252 / 1182 | 576 × 432 ab x = 64 | x ≤ 89 (90 %: 89), y ≥ 149 | 127 |
| `speedrun_t1` | 0:37:56 | 352 / 1291 | 486 × 366 ab x = 154, y = -2 | x ≤ 74 (90 %: 74), y ≥ 186 |  |
| `speedrun_t2` | 0:42:22 | 360 / 1283 | 486 × 366 ab x = 154, y = -2 | x ≤ 74 (90 %: 74), y ≥ 165 |  |

Folge 59: kleineres DOSBox-Fenster (Faktor 1,076, unschärfer). Folge 65: vergrößert (Faktor 1,8), nur die oberen 200 Zeilen
des Spielbilds sind im Video.

### Fundliste `videos-roh-2` (zuerst die bei roh-1 offenen Punkte)

| Gesucht | Bilder (`<reihe>_HMMSS`) |
|---|---|
| **Spielmodus „Liga + Weltkugel“ (Bundestrainer)** – bei roh-1 offen | Eisbären-Reihe durchgehend. Kachelreihen mit offenen WM-Kacheln: Tabellen/Paarungen mit „WM“ `eisbaeren_f01_01431`, Stärken mit „Länderteams“ `eisbaeren_f01_01430`, Statistiken mit „WM-Statistik“ `eisbaeren_f01_01011`. Hauptmenü des Bundestrainers (14. April, Gegner Frankreich, Fahne statt Vereinswappen) `eisbaeren_f24_00115`, Gegner-Fenster `eisbaeren_f24_00046`, „Spieler in WM-Kader“ `eisbaeren_f24_00129`, „WM Vorbereitung“ `eisbaeren_f24_00721`, Aufstellung/Taktik Nationalteam `eisbaeren_f24_00328`, `eisbaeren_f24_01615`, Banner „Weltmeisterschaft“ `eisbaeren_f24_01338`, Live-Länderspiel `eisbaeren_f24_00925`, Ergebnisse „Gruppe 2 60. Min.“ `eisbaeren_f24_01048`, Tabelle WM `eisbaeren_f35_00531`, Weltmeister `eisbaeren_f29_01214` |
| **WM-Endrunde** – bei roh-1 offen | „WM-Endrundenspiele“ `eisbaeren_f35_00646` (Viertelfinale), `eisbaeren_f35_00803`, `eisbaeren_f44_01330` (bis Spiel um Platz 7), `eisbaeren_f56_00915`, `eisbaeren_f29_01033`; in 22 Reihen |
| **Torszenen** – bei roh-1 offen | Eisbären `eisbaeren_f01_01149`–`01234` (EC Ratingen, 6. Min.), außerdem Folge 39, 40, 65; Duell `duell_s2b_00503`, `duell_s4c_02052` und fast jedes Spiel in `duell_s2b` … `duell_s4d`; Einstellungen mit Schalter „Torszenen“ `speedrun_t2_04024` |
| **WM-Statistik** – bei roh-1 offen | `eisbaeren_f07_03843`, `eisbaeren_f56_01126` (in 33 Reihen) |
| Highscore am Spielende | **nicht gefunden** (Kachel „Highscore“ offen, z. B. Kachelreihe Manager `eisbaeren_f01_00421`, aber nie geöffnet; kein Spielende) |
| DEB-Pokal-Auslosung mit Dialog „Sind Ihre Nerven stark genug …“ | `duell_s2a_02651`, `eisbaeren_f28_00706`, `eisbaeren_f51_01451`, `speedrun_t1_00110` (in 45 Reihen); Ziehung aufgedeckt `eisbaeren_f30_01719`, `eisbaeren_f52_03358` |
| Übersicht „DEB-Pokalspiele“ | 22.9. mit Ergebnissen `duell_s2a_01217`, Achtelfinale `duell_s6a_03100`, Endspiel `eisbaeren_f60_02758` (in 54 Reihen) |
| Paarungen 1./2. Liga/Oberliga | Oberliga `duell_s2a_01138`, Zweite Liga `eisbaeren_f14_01518`, Bundesliga 60. Minute `eisbaeren_f40_01833` (in 66 Reihen) |
| Vereinstaktik | `duell_s2a_02108`, `eisbaeren_f16_03225`, `eisbaeren_f45_03727` |
| Monatsausgaben | Vereinsstatistik „Einnahmen/Ausgaben / Monat“ kurz nach Spielbeginn `speedrun_t1_00456`, sonst `duell_s2b_02107`, `eisbaeren_f10_01657`; „vor dem ersten Spieltag“ weiter nur `folge01_00522` |
| Europapokal mit Ergebnissen | Gruppe 1 vor und nach Spielen `eisbaeren_f19_05806`, `eisbaeren_f20_02950`, Gruppe 2 `eisbaeren_f51_03346`, Endrunde `eisbaeren_f58_01323` (in 17 Reihen) |
| Kachelreihen Statistiken / Tabellen / Gemischtes | siehe Weltkugel; Duell mit gesperrter WM-Kachel: Tabellen `duell_s2a_01203`, Gemischtes `duell_s2a_00135`; Statistiken `eisbaeren_f22_00634` |
| Live-Spiel mit Vereinen | Duell mit zwei Partien `duell_s2a_01126`, Eisbären `eisbaeren_f10_00608` |

Außerdem neu: Spielvorbereitung mit Kopfwahl `speedrun_t1_00056`, `eisbaeren_f01_00204`; Speichern-Dialog
`speedrun_t2_04033`; Statistik „Früher“ mit Europacup/DEB-Pokal-Verlauf `eisbaeren_f58_03424`; Jugendteams in vielen Reihen.
