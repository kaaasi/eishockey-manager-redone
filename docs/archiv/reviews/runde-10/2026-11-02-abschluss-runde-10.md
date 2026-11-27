# Review PR #407 – Abschluss Runde 10 (Archiv, P3-Befunde, Inventur nach Review #400)

- **PR:** #407 · **Base:** `162e0d1724ea122468c0bd160b32bcae4e5372d9` · **Head:** `2260a7b38811e7b1c5ede4333c9ec22261e4d23c`
- **Reviewer:** Claude Opus 5.5, geplanter Lauf (frische Sitzung, nicht an der Umsetzung beteiligt)
- **Checkout:** Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`original/gamedata`, Zugriff per `git ls-remote … HEAD` geprüft: `fcfc610`).

Links: `https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/<pfad>#L<n>`.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Reiner Doku-PR (7 Dateien unter `docs/`, nichts in `src/`, `tests/`, `tools/`). Alle drei
Gegenbeispiele sind erfüllt, jede mit `Fixes` genannte Issue ist in der Sache behoben, das Archiv enthält den Abschnitt
der Runde 10 vollständig. Die Befunde unten sind P3 (Restwortlaut, kein Merge-Hindernis); R10A-01 kann mit #401 in
Runde 11 erledigt werden.

## 2. Befunde

### R10A-01 (P3) – Tabelle 2 und P3 Nr. 1 tragen noch die alte Erklärung von R10N-01

„Neue Befunde“ ([L190](https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/docs/analyse/inventur.md#L190))
ist richtig berichtigt, die Zeilen in Tabelle 2 aber nur für F030/F093/F157. Weiter mit der widerlegten Ursache:
F016 ([L681](https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/docs/analyse/inventur.md#L681),
„Marker als 0xA3F7 ohne Datei-Präfix“), F069/F070 ([L734–L735](https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/docs/analyse/inventur.md#L734-L735),
„ohne Datei-Präfix“), F080 (L745 ohne Hinweis). P3 Nr. 1
([L161](https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/docs/analyse/inventur.md#L161))
empfiehlt für alle „vorhanden“ mit P3 weiterhin „Fundstelle mit `Datei 0x…` schreiben“ – das betrifft F016/F069/F070 und
behebt dort nichts. Gegenbeispiel: die `perl -pi`-Mutation aus R10N-01 (nur Doppelpunkt entfernt, kein `Datei`
ergänzt) macht genau diese drei „umgesetzt“. Vorschlag: F016/F069/F070/F080 wie F030 als „vorhanden mit `[ORIGINAL]`
(Karte erkennt ihn nicht, R10N-01)“ führen, in P3 Nr. 1 den `Datei`-Hinweis auf echte Fälle ohne Marker beschränken.

### R10A-02 (P3) – Archiv: Kopfsatz und „Ergebnis“ ungenau

[runde-10.md L4](https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/docs/archiv/runde-10.md#L4)
sagt „Inhalt unverändert bis auf Überschrift, ‚Ergebnis‘ und ohne ‚Zurückgestellt‘“. Gegenprobe (Abschnitt aus
`arbeitspakete.md` an Base gegen das Archiv, `diff`): darüber hinaus geändert sind die zwei relativen Links (nötig,
richtig), der Status `re-meldungen` (L75, „– wartet auf Review“ entfernt, #392) und der Status `inventur` (L143–L144).
Sachlich richtig, aber der Kopfsatz sollte „und nachgeführte Status“ nennen. L10 zählt #398 und #401 zu den im Abschluss
erledigten P3-Befunden; #398 (README Referenz-Repo) und #401 (Code, zudem P2) bleiben offen, ebenso L143 „R10N-01 bis
-05 … behoben“ (Code-Teil von R10N-01 offen). Vorschlag: „#398, #401 Doku-Teil“.

### R10A-03 (P3) – Restwortlaut Platz/Platzfeld in `re-saisonwechsel.md`

[L162](https://github.com/kaaasi/eishockey-manager-redone/blob/2260a7b38811e7b1c5ede4333c9ec22261e4d23c/docs/analyse/re-saisonwechsel.md#L162):
„ein Nachrücker 12 − (alter Platz) und +3, wenn das alte Platzfeld 0 ist“ – im selben Satz, den #398/P3-01 auf
„Platzfeld“ umstellt, steht noch „Platz“. Vorschlag: „12 − (altes Platzfeld)“.

### Hinweis außerhalb des Diffs (kein Befund dieses PR)

- `re-abdeckung.md` „Einordnung bekannter offener Punkte“: L137 (#194, 0x2FC2F „unbekannt“) und L144 (Versicherung
  0x07089 „unbekannt“) – beide laut Karte am Head „umgesetzt“; widerspricht dem neuen „Ergebnis in Kürze“. #391 betraf nur
  „Ergebnis in Kürze“.
- `reverse-engineering.md` L937: „DOSBox-Beleg mit `FILE13.HOK` steht aus“ – seit #389 überholt.

### Gegenbeispiele (erneut ausgeführt)

- **R10N-01:** Mutation `perl -pi -e 's/(0x[0-9A-Fa-f]{4,5}):(?=[^0-9A-Fa-fx]|$)/$1 :/g'` auf alle `src/**/*.cs` einer Kopie
  des Heads, `funktionskarte.py --csv` vorher/nachher: genau 8 Statuswechsel dokumentiert → umgesetzt: 0x0A3F7, 0x0B954,
  0x346D5, 0x347EF, 0x350B6, 0x3688F, 0x3EB30, 0x581C2 = die 8 Adressen in `inventur.md` L190–L194 ✓ (Rest R10A-01).
- **R10N-02:** alle 39 Kennungen der Zuordnungstabelle (G1–G8, M-K1–M-K10, L1–L8, R-K1–R-K7, S-K1–S-K6) im Abschnitt
  „Runde 11: priorisierte Liste“ bis vor „Neue Befunde“ gesucht, Bereiche (`L1–L8`, `R-K2…R-K5`) aufgelöst → keine fehlt;
  L1–L8 ausdrücklich zurückgestellt (P2 Nr. 12), S-K3 P1 Nr. 8, S-K4 P2 Nr. 13, S-K5/S-K6 P3 Nr. 5/6; A039, A185, A186, A349
  in P2 Nr. 14–16 ✓.
- **R10P-01:** `funktionskarte.py em.exe --write` am Head → `git status --porcelain` leer, also Ergebnis =
  `docs/re-abdeckung.md` ✓. CSV: keine Funktion mit `arbeitspakete.md` oder `docs/archiv/` als Fundstelle ✓; unbekannt im
  Spielcode nur 0x575C3 (421 B) ✓; Spielcode 82/2/16/0 % ✓.

### Issues mit `Fixes`

| Issue | Ergebnis |
|---|---|
| #386 (N-1/N-2) | `leitstelle-start.md` Schritt 7: „dem Nutzer melden (Fehlermeldung von `add_repo` bzw. `ls-remote`)“ und „push belegt durch das `add_repo`-Ergebnis“ ✓ |
| #391 | „Ergebnis in Kürze“ mit Zahlen des erzeugten Teils (82/2/16 %, nur 0x575C3) ✓; Bereichsaussage „nichts mehr unbekannt“ stimmt mit CSV ✓ |
| #392 | Status `re-meldungen` im Archiv ohne „wartet auf Review“ ✓ |
| #393 | „bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7)“ ✓ |
| #399 | Kennungen eindeutig mit Präfix und Legende (`inventur.md` L5–L8), Zähleinheit Zeilen (L12–L15); Runde-10-Abschnitt nur noch im Archiv (historisch) ✓ |
| #402 | siehe R10N-02 ✓ |
| #403 | Ø 2, Summe 0 → 6, Randfall n ≤ 5; per `ndisasm -e 0x3F928` nachvollzogen (`jng` überspringt i ≤ 0, `div bx` durch n − max(n−5,0), 0x3F98E `mov di,6`) ✓ |
| #404 | Z. 8, P1 Nr. 8 und P2 Nr. 13 nennen #389 gemergt; kein „offen“ mehr ✓ |
| #405 | A244 auf Datei 0x1B121/0x1B13E; `strings -t x em.exe`: `1b121 M|chten Sie, da~ Ihr 'Spion'`, `1b13e f}r Sie auf Talentsuche geht?` ✓; auch unter „Texte nur in em.exe“ ✓ |

### Erwartet offen (bestätigt)

- **#398:** Doku-Teil erledigt (`re-saisonwechsel.md` P3-01–03, `finanzen-rekonstruktion.md` L194 Fair-Play mit Verweis
  auf Tabelle L243); README im Referenz-Repo (`original/spielstaende/README.md` L25, „Prämie für Platz 12“) unverändert,
  Issue offen ✓.
- **#401:** Doku-Teil erledigt (Rest R10A-01), `funktionskarte.py` unverändert (`BARE … (?![\w:])` L416), Issue offen ✓.
- **#406:** offen ✓; `inventur.md` ist am Head Fundstelle von 248 Funktionen (4112 Zuordnungen), Status unverändert.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git diff --stat 162e0d1..2260a7b` | 7 Dateien, alle unter `docs/` – reiner Doku-PR ✓ |
| Archiv gegen Base (`awk` Abschnitt „Aktuelle Runde: Runde 10“ bis vor „Zurückgestellt“, `diff`) | vollständig; Abweichungen nur Überschrift, Kopfsatz/„Ergebnis“, 2 Links, 2 Status (R10A-02) |
| Relative Links in allen 7 Dateien (Skript, `os.path.exists`) | keine kaputten; Pfade in Backticks im Archiv vorhanden |
| PR-Nummern im „Ergebnis“ (REST) | #366–#400 wie beschrieben, alle gemergt |
| Gegenbeispiele R10N-01, R10N-02, R10P-01 | siehe oben ✓ |
| Issues #386–#406 (REST) | alle noch offen (schließen erst beim Merge) |
| CI am Head (Check-Runs) | `build` success, `publish` skipped |

`tools/pruefen.sh` (alle drei Läufe, SDK 10.0.402, Commit 2260a7b):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

**Akzeptierte `[APPROX]`:** keine neuen (reiner Doku-PR). **Annahmen:** Inhalt der Übergabe an die Leitstelle Runde 11
(PR-Kommentar) nicht geprüft; in P2 Nr. 12 fehlen die `[APPROX]`-IDs A053 (L5) und A341 (L8), die Kandidaten sind aber
über ihre Kennung erfasst – kein Befund.
