# Nachprüfung PR #628 – `re-dosbox` Teil 1 (Delta nach Review #643)

Nachprüfung des Reviews `docs/reviews/2026-11-08-re-dosbox-r14-1.md` (PR #643) nach `docs/vorlagen/review-anfrage.md`, geplanter
Lauf Claude Opus 5.5 ohne Implementierungskontext. Bisher geprüfter Head `6568de7e2da3c174f3d142049e4bf403e442cf84`, jetzt Head
`80ae47b9474e4116d87444df1299fa8992cf45df`; nur Delta. Geprüft am Klon: `git rev-parse HEAD` = Head, `git status --porcelain` leer.
Referenz-Repo `kaaasi/eishockey-manager-referenz` Stand `779a1c8` (`git ls-remote … HEAD`).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle fünf Befunde R14RD1-01 bis -05 sind sachlich richtig eingearbeitet; die Gegenbeispiele lassen
sich am Head nicht mehr gegen die Datei wenden. Der Merge-Commit `c4cfef8` übernimmt nur `main` @ `889e729`, sonst nichts. Offen
bleiben drei kleine Doku-Punkte (P3, unten), die den Merge nicht hindern; am besten mit Teil 2 oder beim nächsten Planstand
nachziehen.

## 2. Befunde

### R14RD1N-01 (P3) – L7 steht nur in der Analysedatei, nicht im Plan; Nummer doppelt belegt

- [Frage 6, Z. 420–425](https://github.com/kaaasi/eishockey-manager-redone/blob/80ae47b9474e4116d87444df1299fa8992cf45df/docs/analyse/re-runde-14.md#L420-L425)
  erweitert Dateigrenzen von `kader-start` und `ui-rest` („entschieden als L7“). `docs/arbeitspakete.md`
  [Z. 172](https://github.com/kaaasi/eishockey-manager-redone/blob/80ae47b9474e4116d87444df1299fa8992cf45df/docs/arbeitspakete.md#L172)
  führt weiter nur „Leitstelle L1–L6“, die Spurabschnitte (Z. 113, 124) kennen die Erweiterung nicht. Nach AGENTS.md §2.5 gelten die
  Dateigrenzen „der Spur in `docs/arbeitspakete.md`“; eine Spur, die nur ihren Abschnitt liest (§7), sieht die Freigabe nicht.
  Vorschlag (Leitstelle, außerhalb der Dateigrenzen von `re-dosbox`): L7 in Z. 172 und in den Dateigrenzen von `kader-start`/`ui-rest`
  nachtragen.
- [Z. 255](https://github.com/kaaasi/eishockey-manager-redone/blob/80ae47b9474e4116d87444df1299fa8992cf45df/docs/analyse/re-runde-14.md#L255)
  (A167, schon am alten Head) meint mit „bisher offen, L7“ das L7 aus **Runde 13**, ebenso Z. 109 „L2“ (Runde 13). In derselben Datei
  heißt L7 jetzt die Entscheidung der Runde 14 (Z. 251, 406, 420). Eindeutig wäre „L7 (Runde 13)“ bzw. „L2 (Runde 13)“.

### R14RD1N-02 (P3) – Restwidersprüche zu L7 und zur Selbstprüfung

- [Z. 105 f.](https://github.com/kaaasi/eishockey-manager-redone/blob/80ae47b9474e4116d87444df1299fa8992cf45df/docs/analyse/re-runde-14.md#L105-L106)
  (U15-c): „ggf. `Highscore.cs` (nur Kommentar, Datei der Spur prüfen)“ – L7 ordnet `Core/Highscore.cs` jetzt `kader-start` zu
  (mit Änderung, nicht nur Kommentar). Der Hinweis „Datei der Spur prüfen“ ist überholt.
- [Z. 431](https://github.com/kaaasi/eishockey-manager-redone/blob/80ae47b9474e4116d87444df1299fa8992cf45df/docs/analyse/re-runde-14.md#L431)
  „Fundstelle in 20 Funktionen“: am Head sind es **19** (0x0659C und 0x07C7E entfallen, neu 0x44E22 über „aus 0x44E22“ im neuen
  Geltungsbereich von U15-c – eine richtige Zuordnung). Status unverändert, nur die Zahl veraltet.

### R14RD1N-03 (P3) – Hinweis A380 (Geisterplatz im Marktsatz) nicht übernommen

R14RD1-04 nannte als „erwähnenswert, Deutung offen“ den 11. Platz im Marktsatz FILE41 (Kopie von Platz 10 DAFFNER, Pool-Index 0).
Die Datei erwähnt ihn nicht; für den Feldbeleg „Markt-Kaderplatz bei Transferliste“ (`hok-import`, Z. 407) wäre ein Satz nützlich.
Kein Fehler der Datei; Entscheidung der Leitstelle.

## 3. Ausgeführte Prüfungen

### Umfang des Deltas

- `git diff --stat 6568de7 80ae47b`: nur `docs/analyse/re-runde-14.md` (Fix-Commit `80ae47b`: 43 Einfügungen, 20 Löschungen) und `docs/arbeitspakete.md`.
- Merge `c4cfef8` (Eltern `6568de7`, `889e729`): `git diff 889e729 80ae47b --stat` → nur dieselben zwei Dateien (re-runde-14.md neu,
  arbeitspakete.md 3 Zeilen = Statuspunkt). Merge sauber: `git diff 59f98cf 6568de7` = `git diff 889e729 c4cfef8` bis auf die
  `index`-Zeile; `git diff 6568de7 c4cfef8 -- docs/arbeitspakete.md` gleich `git diff 59f98cf 889e729` (Änderung von `main`).
- Fix-Commit `80ae47b`: nur Doku; in `arbeitspakete.md` nur der Statuspunkt `re-dosbox` (Z. 70 f.). Keine Originaldaten, Bilder oder
  Byteauszüge außer Messwerten.

### Gegenbeispiele erneut ausgeführt

| Befund | Prüfung | Ergebnis am Head |
|---|---|---|
| R14RD1-01 A373 | `disasm.py gamedata/em.exe` 0x484A1–0x484AD: `di` = `[es:bx]` = y, `[bp−0x8]` = x. 0x48783–0x487B8: Mitte = `[bp−0xBE][i]` + 2·(i<4) + 6; `cmp ax,di; jnl → 0` ⇒ `[bp−0xC]` = 1 genau bei y > Mitte (untere Hälfte). 0x487BB–0x487D0: (7 + 2·(i<4)) · `[bp−0xC]` = gedrückte Darstellung tiefer. 0x48BD2–0x48BFF: Schritt nur bei Wert > 0 (oben) bzw. Wert < `[0x5306+i]` − 1 (unten), sonst Sprung 0x48D58. 0x48C07–0x48C25: `[bp−0x10]` = (`[bp−0xC]`=1) − (`[bp−0xC]`=0) | Datei: „widerlegt“, unten = +1, kein Umlauf, Umsetzung `Step` umdrehen und `Cycle` begrenzen (Z. 29, 182–196) – **behoben**. `TacticsScreen.cs` Z. 101–122 nutzt `Step`/`Cycle` wie beschrieben |
| R14RD1-02 Dateigrenzen | Jede Zeile der Tabelle in R14RD1-02 gegen Frage 6 (Z. 420–425): A046 `FinanceStadium.cs`/`FinanceSponsorRating.cs` ✓, A082 `MatchEngine.Live.cs` ✓, A189 `StadiumStatistics.cs` ✓, A357 `HistoryScreen.cs` (`ui-rest`) ✓, A135 `Cups*.cs` außer `CupsNational*` und `MatchEngine*.cs` ✓, Feldbelege → `hok-import` ✓ (auch Z. 406), U15-c `Highscore.cs` ✓, A012 → `nationalteam` ✓ (Z. 250 f. und Z. 405 jetzt gleich). Übrige Dateien der A135-Umsetzung (`Player*.cs`, `UI/Screens/Team/PlayerDataWindow.cs`) liegen schon in `kader-start` (`arbeitspakete.md` Z. 48, 113) | **behoben**; Nachtrag im Plan R14RD1N-01, Rest Z. 105 R14RD1N-02 |
| R14RD1-03 U15-c | Eigener Entschlüssler (Verfahren `spielstand-original.md`, nur im Scratch): FILE29/30/31 HOK 0x08F8C (`5CF3`) = `01 01 00 03` → DGROUP 0x5CF4 = 1; HOK 0x08F2C (Zeile 1) = 148 75 9 110 138 125 95 114, danach 24 × 0; Zeile 2 (0x08F4C) 32 × 0, Zeile 3 (0x08F6C) 32 × 0xFF; Zeile 0 (0x08F0C) voll belegt. HOK 0x00F70 = 1 (FILE29). `disasm.py` 0x2819C–0x281A1 (`[bp−0x64]` = `[0x386]`), 0x29CC5–0x29CD5 (Aufruf 0x45CD0, `jnz` zurück), 0x29CFA–0x29D23 (`[0x5A14]` = 0x7CA, `[0x386]` = 1, `mov byte [bx−0x68BC],1` = DGROUP 0x9744) | Z. 84–92 „Zeile 1“, Werte und Geltungsbereich „neues Spiel“ wie gemessen – **behoben** |
| R14RD1-04 `file40_06` | Spielbereich (1 \| 26), Schritt 2: `file40_06` Zeilen 164 und 180 x 180–314 einfarbig (97 \| 0 \| 16), 132/148/196 (243 \| 195 \| 113); `file40_05` Zeilen 132/148 (97 \| 0 \| 16), 164/180 (243 \| 195 \| 113) | Z. 328 f. – **behoben** |
| R14RD1-04 Indizes | `kb\21.vga` (6 Bit, `Palette.DacToRgb`): (97 \| 0 \| 16) → 16, 96; (243 \| 195 \| 113) → 31, 63; (32 \| 48 \| 81) → 29, 147; (97 \| 65 \| 48) → nur 27 | Z. 12–14 – **behoben** |
| R14RD1-04 HOHENADL, Fundstellen | Z. 263 „Nr. 8 seit FILE37A, vorher 9“; Z. 96 und Z. 162 ohne `[bx−…]`-Klammer | **behoben**; A380-Hinweis R14RD1N-03 |
| R14RD1-05 Stand | Statuspunkt `arbeitspakete.md` Z. 70 f. und A115 (Z. 44, 391 f.) auf FILE46/Teil 2; Kopf Z. 4 „Sitzungen 3–7, FILE43–49A“ | **behoben** |

### Erwartet offen (bestätigt)

- Taktik 375h: Wirkung von 0x53C57 (Neuaufstellung nach Verletzung im Spiel) nicht gelesen – unverändert so in der Datei (Z. 206, 221, Frage 4 Z. 416).
- U15-c: Wirkung von DGROUP 0x9744 außerhalb des Highscores nicht gelesen (Z. 93).
- A357: Höhenformel nicht gelesen, nur zwei Messpunkte (Z. 384).
- U15-c: Ob der Zweig über Datei 0x45755 (aus 0x44E22) den Sonderfall bzw. den Filter erreicht, ist nicht gelesen (Z. 87–90,
  jetzt ausdrücklich so vermerkt; Umsetzung auf „neues Spiel“ begrenzt).

### Selbstprüfung `funktionskarte.py`

`python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo <stand> --csv <scratch>` ohne `--write` an `59f98cf`
(Base des PR), `6568de7` und `80ae47b`: je 679 Funktionen, umgesetzt 268, dokumentiert 152, genähert 2, unbekannt 257; kein Status
geändert. Fundstelle `re-runde-14.md`: am alten Head 20 Funktionen, am Head 19 – **0x0659C und 0x07C7E entfallen**, neu 0x44E22
(richtig, R14RD1N-02 nur zur Zahl in Z. 431). `git status --porcelain` danach leer.

### `tools/pruefen.sh` (alle drei Läufe)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2273 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1908 | 0 | 327 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1909 | 364 | 0 |

Commit `80ae47b9474e4116d87444df1299fa8992cf45df`, SDK 10.0.402, Ergebnis: bestanden. Doku-PR ohne Codeänderung; der Lauf belegt
nur, dass der Head baut und die Tests wie am alten Head grün sind.

### Akzeptierte `[APPROX]` / Annahmen

Keine `[APPROX]` im Code (Doku-PR). Annahmen unverändert wie im Review #643 (A046 Schreibzeitpunkt DGROUP 0x8288, A180 Anzeige vor
dem ersten Spiel abgeleitet). Neu akzeptiert: Geltungsbereich U15-c „neues Spiel“ als ausdrücklich offene Grenze.
