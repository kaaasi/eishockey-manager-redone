# Review PR #1054: Abschluss Runde 26 (Archiv, Kopf Runde 27)

Head `e782b4ea9c0814f6c78be66cd86bdf1bd6db3c46`, Base `4531d1f` (= `main`). Claude Sonnet 5.5, frische Sitzung, geplanter Lauf.
Checkout verifiziert: `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. Nur Doku, `tools/pruefen.sh` entfällt
(nicht ausgeführt, kein SDK nötig); Originaldaten nicht geprüft.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Archiv wörtlich, Zahlen und Verweise stimmen mit GitHub, Generatorblock reproduzierbar,
Klassen- und Belegzeilen stimmen mit dem Code. Ein P3 (Doku-Nachzug), kein P0–P2.

## 2. Befunde

- **P3-1 – `abweichungen-v1.md` Z. 63 nachziehen.** Die S02-Zeile nennt „Reste mit Beleg als Marker A1013, A1014, A1025–A1027,
  A1029–A1031 (K/E)“. Nach #1054 sind A1027/A1031 Klasse X (kein Marker mehr), A1033/A1034 (K) sind neu.
  Beleg: `grep -n 'A1027\|A1031' docs/analyse/abweichungen-v1.md` → Z. 63; `restliste-v1-klassen.csv` führt A1027/A1031 = X.
  Folgenlos für Code und Generator; passt in das Aufräumen #936.
- **P3-2 (Hinweis) – A1029-Beleg.** „live-seiten-rest (PR #1042), torszene-dauer (PR #1033)“: laut `git blame` stammen
  `LivePageScreens.cs:40–42` (Markertext) aus #1042 (`3556c268`), Z. 43 (`rink`) aus #1033 (`25fc2e10`); der alte Beleg
  „live-verschraenkt (#1032, Teil 3)“ betraf nur den Ursprung (`27c88668`). Vertretbar, nur Hinweis.

## 3. Geprüft

| Prüfung | Befehl / Quelle | Ergebnis |
|---|---|---|
| Gegenbeispiel Generator | `python3 tools/reverse-engineering/markerliste.py >/dev/null` (ohne `--write`) auf dem Head; danach `git status --porcelain` | Rückgabe 0, Arbeitsbaum sauber → Generatorblock in `restliste-v1.md` unverändert |
| Zählung | Kopf des Blocks | 225 Dateien, 543 Markerzeilen, V 0 / D 25 / E 321 / K 197 / X 0 / – 0; 491 entfallene IDs (vorher 489 = A1027, A1031) |
| Archiv wörtlich | Skript: Zeilen 11–165 von `git show 4531d1f:docs/arbeitspakete.md` gegen `docs/archiv/runde-26.md` | 155 Zeilen, 17 fehlend: Überschrift (geändert), „Zurückgestellt“ und „Vormerkungen“ (bewusst in `arbeitspakete.md` geblieben, dort unverändert vorhanden). Rundenabschnitt sonst wörtlich |
| PRs #1030–#1053 | `list_pull_requests` (alle) | alle gemergt; Nummern und Rollen des Ergebnisabsatzes stimmen (Plan #1030/Review #1031, #1038/#1039, #1034/#1035 Ja, #1032/#1040 Ja, #1042/#1043 Ja, #1033/#1044 Nein, #1049/#1050 Nein, #1047/#1048 Ja, #1051 Gesamtreview, Fix #1053). #1036, #1037, #1041, #1045 sind Issues |
| Issues | `list_issues` | #1018, #1046, #1052 geschlossen; #1019, #1024, #978, #980, #936 offen (wie „Erwartet offen“) |
| Kopf „Runde 27“ | `docs/arbeitspakete.md` Z. 11–20 | vollständig: Abschlussdatum und Archivlink, Stand V 0/D 25/E 321/K 197, Abweichungsliste, Auftrag (#936 A1–A6 mit #978, #980, #1019, #1024 Rest P3-2), danach Abschlussprüfung, Modellwahl, Push-Hinweis; alle Verweisziele existieren; „Zurückgestellt“/„Vormerkungen“ unverändert |
| Klassen A1027, A1031 (X) | `LiveMatchScreen.cs`, `LivePenaltyScreen.cs` (#1049) | Marker für Darstellung 0 jetzt `[ORIGINAL]` (Z. 283 ff., 315); `LivePenaltyScreen.cs:23` ohne [APPROX]; Rest-APPROX Z. 20 ist A1030 (bleibt) |
| Klassen A1033, A1034 (K) | `LiveMatchScreen.cs:287`, `:315` | beide `[APPROX]` Darstellung 1/2 ohne `tore\`, Schlüssel und Stellen passen zur Liste |
| Beleg A1010 | `git blame -L 125,127 CupsGroup.cs` | `8b1b92e5` „live-verschraenkt Teil 2b“ → #1032 ✓ (vorher fälschlich #1042) |
| Beleg A1029 | `git blame -L 36,46 LivePageScreens.cs` | siehe P3-2 |

## 4. Akzeptierte `[APPROX]`

Keine neuen außer A1033/A1034 (K, begründet: ohne `tore\` mit vollständigen Originaldaten nicht erreichbar, Original-Ablauf im
Kommentar belegt). Das Entfallen von A1027/A1031 stützt sich auf den im Fixlauf #1049 geprüften Code, nicht auf eigene
Originalnachmessung.

Annahmen (nicht geprüft): Aussagen zu Spielinhalten der Runde (Review-Inhalte, D-Themen) wurden nur gegen Titel, Status und
Nummern auf GitHub abgeglichen, nicht gegen die Review-Dateien im Wortlaut.
