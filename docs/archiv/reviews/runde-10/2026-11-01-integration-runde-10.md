# Review: Integration Runde 10 – Analyse em.exe (PR #380)

- PR [#380](https://github.com/kaaasi/eishockey-manager-redone/pull/380) · Base `664ff18f8675c12d63c5f6ad4075309d5975b829`
  (Merge-Base mit `main` ist `6904eba`, der Branch enthält `main` bis dort) · Head `94893d3244cb6dcd643d711973731c5792f9c7ce` ·
  Reviewer: Claude Opus 5.5 (geplanter Lauf, eigene Sitzung, an #380 und den Spur-PRs #375–#378, #381 nicht beteiligt).
- Checkout per Klon, `git fetch origin pull/380/head`; `git rev-parse HEAD` = Head, `git status --porcelain` leer. Die fünf
  Spurbranches (`paket/re-geld` d695cda, `re-meldungen` cfb727a, `re-saisonwechsel` a13c536, `re-livespiel` 46dbfe6,
  `re-rest` eec7d28) sind Vorfahren des Heads und entsprechen den Heads von #375–#378, #381.
- `em.exe` aus dem Referenz-Repo (`sha256sum -c SHA256SUMS`: OK).

Links: `https://github.com/kaaasi/eishockey-manager-redone/blob/94893d3244cb6dcd643d711973731c5792f9c7ce/<pfad>#L<n>`.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Reiner Doku-PR (19 Markdown-Dateien, kein `src/`, `tests/`, `tools/`). Die Karte in
`re-abdeckung.md` ist exakt die Ausgabe von `funktionskarte.py --write` am Head; alle Adresslisten der fünf Spuren sind in
der eigenen Analyse-Datei belegt; die Leitstellen-Berichtigungen (Teil 4, E2) stimmen mit Analyse und Disassembly überein;
R10P-01 und R10R-01 sind behoben. `tools/pruefen.sh` (alle drei Läufe) grün, CI grün. Drei P3-Befunde (veralteter Text),
keiner blockiert.

## 2. Befunde

### R10I-01 (P3) – „Ergebnis in Kürze“ in `re-abdeckung.md` widerspricht der neu erzeugten Karte

[`docs/re-abdeckung.md` L76–L81](https://github.com/kaaasi/eishockey-manager-redone/blob/94893d3244cb6dcd643d711973731c5792f9c7ce/docs/re-abdeckung.md#L76-L81)
(Handtext außerhalb der Marker): „rund 54 % umgesetzt, 2 % genähert, 9 % dokumentiert, 35 % unbekannt“, „am schwächsten
belegt: Overlay 0x118 (72 % unbekannt) …“. Der erzeugte Block
([L158–L164](https://github.com/kaaasi/eishockey-manager-redone/blob/94893d3244cb6dcd643d711973731c5792f9c7ce/docs/re-abdeckung.md#L158-L164))
sagt für den Spielcode 82/2/16/0 %, unbekannt nur 0x575C3. Schon vor der Runde veraltet (Stand Runde 8), mit dieser
Integration aber in voller Höhe falsch. Gegenbeispiel: `--csv` am Head, Bytes je Status ohne Systemsegmente summieren.
Vorschlag: den Absatz auf den Verweis „Zahlen im erzeugten Teil“ kürzen oder auf den Stand nach Runde 10 setzen.

### R10I-02 (P3) – Status `re-meldungen` „wartet auf Review“

[`docs/arbeitspakete.md` L73](https://github.com/kaaasi/eishockey-manager-redone/blob/94893d3244cb6dcd643d711973731c5792f9c7ce/docs/arbeitspakete.md#L73):
aus dem Spur-Commit übernommen; die anderen vier Spuren stehen auf „Analyse fertig“, die Integration ist das Review. Halbsatz
streichen.

### R10I-03 (P3) – „dort mitzuanalysieren“ bei 575C3h ist mehrdeutig

[`docs/arbeitspakete.md` L122–L124](https://github.com/kaaasi/eishockey-manager-redone/blob/94893d3244cb6dcd643d711973731c5792f9c7ce/docs/arbeitspakete.md#L122-L124):
„Analyse in `re-livespiel`. Noch unbekannt und dort mitzuanalysieren: 575C3h“ – „dort“ lässt sich auf die abgeschlossene Spur
`re-livespiel` beziehen. Gemeint ist laut Kontext die Zuständigkeit von `livespiel` (R10R-01 Vorschlag 2). Vorschlag:
„… mit `livespiel` zu analysieren (zusammen mit L7 `tore\`-Format)“.

### Gegenbeispiele erneut ausgeführt

- **R10P-01** (`funktionskarte.py … --csv` am Head): `docs/arbeitspakete.md` kommt in **keiner** Spalte `fundstellen` vor.
  Vergleich gegen `6904eba` (Merge-Base): 154 Statuswechsel, alle „unbekannt → dokumentiert“, jede neue Fundstelle stammt aus
  `docs/analyse/*.md` (re-rest 67, re-meldungen 48, re-livespiel 17, re-geld 7, re-saisonwechsel 1, Rest gemischt aus
  Analyse-Dateien); keine aus Plan, `reverse-engineering.md` oder Reviews. **Behoben.**
- **R10R-01**: 575C3h ist unter „Zurückgestellt“ bei `livespiel` mit Ov 138h, Lader `tore\`, Aufrufer 567C8h eingetragen
  (L122–L124) und aus der `re-rest`-Liste ausdrücklich ausgenommen (L104). Einzige unbekannte Spielcode-Funktion am Head
  (0x575C3, 421 B). Zuständigkeit vorhanden – **behoben** (Wortlaut: R10I-03). R10R-02 (L115–L116) ebenfalls behoben.
- **`tools/pruefen.sh`** alle drei Läufe: Tabelle unten, gleich der PR-Beschreibung.

### Erwartet offen

- **#166** offen bestätigt (PR nur „Refs #166“): Analyse fertig, Auswertung DOSBox (`FILE12`–`FILE14`, Referenz-Repo #7)
  steht aus – so auch Teil 4 L937 und `arbeitspakete.md` L82–L86.
- **#386** (P3, Schritt 7 `leitstelle-start.md`) offen bestätigt; Datei am Head gegenüber `main` unverändert.

## 3. Ausgeführte Prüfungen

**Besonders prüfen (1) – Karte und Adresslisten**

- `funktionskarte.py em.exe --repo . --write` in einem Worktree des Heads → `git status --porcelain` leer: `re-abdeckung.md`
  ist byte-gleich mit der Ausgabe.
- Skript über die fünf Adresslisten in `arbeitspakete.md` (Suffix `h`) gegen die CSV-Spalte `fundstellen`:

  | Spur | Adressen | eigene Analyse-Datei als Fundstelle | Rest |
  |---|---:|---:|---|
  | `re-geld` | 9 | 9 | – |
  | `re-meldungen` | 24 | 24 | – |
  | `re-saisonwechsel` | 2 | 1 | 562C6h liegt in 54C9Fh (umgesetzt); Analyse-Datei steht dort als Fundstelle |
  | `re-livespiel` | 19 (+54C9Fh/562C6h nur lesen) | 19 | wie oben |
  | `re-rest` | 53 (+575C3h ausgenommen) | 53 | 575C3h unbekannt, siehe R10R-01 |

**(2) – Leitstellen-Commits `90f9d78`, `ceee82c`, `94893d3`**

- Teil 4 `reverse-engineering.md` (L914–L937) gegen `analyse/re-saisonwechsel.md` (L11–L14, L99–L113) und `ndisasm -b16`:
  0x562C1 `or byte [bx-0x6a61],0x2`; 0x562C6 `cmp byte [bp-0x1],0x2` / `jc 0x562FA`; 0x562D8/0x562DE Konto = 0x0012:0x4F80
  (400.000 DM, gesetzt); 0x562F2 `call 0xf0:0x4d` (aus, ein, 1); 0x56334 `or …,0x1` für den Manager des nach dem ersten
  Tausch nachrückenden Vereins; 0x5636D derselbe Aufruf mit denselben Argumenten → zweiter Tausch hebt den ersten auf.
  Zwangsabstieg 0x55F22–0x55F34: Hochwort > 0xFFE1 → kein Abstieg, gleich → `jc` bei Tiefwort < 0x7B80 → Grenze echt
  kleiner; die Berichtigung „≤“ → „<“ stimmt.
- E2 `zusatzfunktionen.md` §4 (L28–L30) deckt sich mit `analyse/re-geld.md` „Entscheidungen Nutzer“ (Netzwerk: Anfrage mit
  Zustimmung; lokal: sofort wie im Original).
- Relative Links aller geänderten Dateien und `archiv/runde-{4,5,8,9}.md` gegen das Dateisystem: keine Fehltreffer (#374
  erledigt).

**(3) – Stichproben gegen `ndisasm`** (je Spur mindestens zwei, alle bestätigt)

| Spur | Stelle | Aussage | Ergebnis |
|---|---|---|---|
| re-geld | 0x3A13B | Platz < 2 und Liga > 0 → Liga − 1, neu = p + 10; p > 10 und Liga < 2 → Liga + 1, neu = p − 10 | ✓ |
| re-geld | 0x3E3C4–0x3E3DA | „Ja“ beim Ausbau: Konto (0x97EA + 0x1AF·m) −= Kosten direkt | ✓ |
| re-meldungen | 0x0B8FE → 0x10AA5 | Fernhülle (`call 0xbbf:0x30b5` = Datei 0x10AA5), wartet auf Port 0x3DA Bit 3 | ✓ |
| re-meldungen | 0x130EE | Wartezeit m / 3 Takte (Argument durch 3 über Laufzeit-Division, dann Taktschleife) | ✓ |
| re-livespiel | 0x534BC | Treffer in x…x+breite, y…y+6 → `(mx−x)·n/breite+1`, sonst 0 | ✓ |
| re-livespiel | 0x53508 | Treffer in 5 × 5 → 1, sonst 0 | ✓ |
| re-rest | 0x06C9C | 10 B, setzt Byte DGROUP 0x175 = 1 | ✓ |
| re-rest | 0x4B0F0 | DGROUP 0xE25E = 10.000 (Doppelwort 0x0000:0x2710) | ✓ |
| re-rest | 0x5AC9B | leere Funktion, 5 B | ✓ |
| re-saisonwechsel | 0x562C6, 0x55F22 | siehe (2) | ✓ |

- **Regel 2.1:** In `docs/analyse/*.md` nur Pseudocode, Formeln und eine Befehlszeile (`re-rest.md` L277, leere Funktion);
  keine Disassembly-Listen, Bytes oder Bilder. Diff enthält nur `.md`-Dateien.

**`tools/pruefen.sh`** (alle drei Läufe, Head 94893d3244cb6dcd643d711973731c5792f9c7ce, SDK 10.0.402, Ergebnis: bestanden)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

CI am Head: `build` success. Keine Gegenprobe per Mutation (keine neuen Tests).

**Akzeptierte `[APPROX]`:** keine neuen (kein Code). **Annahmen:** Inhalt der Kandidatenlisten (G/K/L) und die übrigen
Analyse-Aussagen nur stichprobenweise geprüft; der DOSBox-Beleg (#166) ist nicht ausgewertet.
