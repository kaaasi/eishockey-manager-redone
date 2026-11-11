# Review PR #758 – Spur pruefung-karte Teil 2 (Inventur Runde 15, Funktionskarte, #755)

Geprüft: Head `becbccc9aa813cde7a7c13156e7ef064e590528c` (`git rev-parse` am PR-Head), Base `7fbf9b621854fd092fbdddcca4c6c37691368a5e`
(= `main`), Arbeitsbaum sauber. Reviewer: Claude Code (Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext.
Mutationen nur in einer temporären Kopie, danach verworfen.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün; beide Gegenbeispiele werden rot; die Inventur deckt alle
73 in Runde 15 hinzugekommenen/umgeschriebenen `[APPROX]`-Zeilen ab (bis auf die drei im Text begründeten Ausnahmen), die IDs
A384–A407 sind lückenlos und eindeutig; die Funktionskarte ist reproduzierbar. Einziger Befund P3 (Einordnung der Export-Zeilen
nach bestandener DOSBox-Probe) blockiert nicht.

## 2. Befunde

**R758-01 (P3) – A399–A405 nennen R15-D5 noch als ausstehende Abnahme, die Probe ist bestanden.**
[inventur.md:579](https://github.com/kaaasi/eishockey-manager-redone/blob/becbccc9aa813cde7a7c13156e7ef064e590528c/docs/analyse/inventur.md#L579)
„Abnahme R15-D5 = D (DOSBox-Probe des Nutzers)“, PR-Text „Restlücken“: „A399–A405 Export, Abnahme R15-D5 = D“. Beleg:
`grep -n "bestanden\|#27\|EXPORT1C" docs/analyse/inventur.md` → kein Treffer; dagegen #727, Kommentar 6779386980: „Dritte
DOSBox-Probe EXPORT1C … bestanden“ (Kopf, Konto, alle 25 Kadernamen, Werte/Aufstellung; Bilder Referenz-PR #27, gemergt), und
Merge-Prüfung #727 Punkt 5. Vorschlag: in A399 „R15-D5 = D bestanden (EXPORT1C, Referenz #27)“; in A404 (Poolplatz der
Managerspieler, Namen) und A405 (Spielstand-ID aus dem Inhalt – Stand lädt) „im Original geladen“ ergänzen. Einordnung „d“/P3
bleibt richtig: die Probe belegt Laden und Anzeige, nicht die einzelnen Vorgabefelder (N/U der Feldkarte, A400/A401/A403). Zu
vermerken als Restlücke: EXPORT2C wurde im dritten Durchgang nicht angesehen, und die Stände aus Head `97c372a` (geänderte SHA-256,
Konto 325.691) sind nicht erneut im Original geprüft.

**Hinweise (kein Befund):**
- A390 nennt zusätzlich Symbol 7 (0x2726D), der Codemarker `FinanceTalent.cs:177` nur Symbol 9; Quelle ist
  `docs/reviews/2026-11-10-kader.md:92` – sachlich richtig, der Marker könnte bei Gelegenheit nachgezogen werden.
- `docs/arbeitspakete.md:99–102` (Spur `hok-export`) steht noch auf „wartet auf … dritte Probe“ – fremder Abschnitt, Leitstelle
  beim Rundenabschluss.
- R15-E6 ist im Repo nicht belegt (`grep -rn R15-E6 docs` leer); die Zuordnung der offenen Issues zu Runde 16 bitte festhalten.

**Erwartet offen – bestätigt:** #730, #732, #734, #743, #757 offen (Label `befund`); #755 schließt dieser PR (`Fixes #755`).

## 3. Ausgeführte Prüfungen

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2920 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2150 | 0 | 492 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2151 | 769 | 0 |

`tools/pruefen.sh` (alle drei Läufe), Commit `becbccc`, SDK 10.0.402, Ergebnis: bestanden.

Gegenbeispiele und Gegenproben (temporäre Kopie):
- `FinanceSponsorRating.cs:125` `10 * (2 - tier)` → `10 * tier`: `SponsorBewertungTests` 10 rot / 16 grün, rot genau Liga 0 und 2
  aller fünf S-Werte. Hand-Sollwerte gegen `ndisasm` Datei 0x3FAC5–0x3FB1D nachgerechnet (S als Wort ohne Vorzeichen, Langdivision
  0x47C durch 12, + 10 · (2 − [0x9746+0x1AF·m]), `imul 0x55` nur AX; „/ 100, Kappe 100“ hinter 0x3FB1F nicht nachgelesen); alle 15 Werte stimmen, auch S = −12 (5480 · 85 mod 65536).
- R688-08: `MainPathDailyRun(day)` hinter `MainPathEventDayLineups(day)` in `GameState.Integration.cs` (`OpenClubSeasonDay`):
  `Day_MainPath_TickTrainingBeforeEventDay952C` 2/2 rot (Saaten 3, 11), übrige `NationalteamRunde14Tests` 18 grün. Zusätzlich
  Vereinsteil abgeschaltet (`if (false && …)`, Mindestzahl nur Nationalteam): weiterhin 2/2 rot. Unverändert: 2/2 grün.
- Inventur: `grep -rc '\[APPROX\]' src/ | awk …` → `191 566` (= PR). Hinzugekommene Markerzeilen `git diff -U0 7ea49a5 7fbf9b6
  -- src` → 73; je Zeile Datei + Zeilennummer in einer „Stand Runde 15“-Zeile gesucht: fehlen nur `GameState.Manager.cs:164`,
  `Squad.Lineup.cs:402` (Rückblicke „vorher [APPROX]“) und `OriginalExport.cs:190` (Verweis auf den Lader) – genau die im Text
  genannten Ausnahmen. Höchste ID am Base A383; neu A384–A407 lückenlos; keine doppelte ID in der Datei (`uniq -d` leer). Die
  „Stelle“ jeder neuen Zeile enthält am Head ein `[APPROX]` mit passendem Inhalt (24/24 gelesen). F123 gegen `ndisasm` 0x4BD56:
  `mov al,[es:bx]; cbw`, Klemme auf [cx, si] – Berichtigung richtig.
- Funktionskarte: `funktionskarte.py --csv <tmp> gamedata/em.exe` ohne `--write` → Ausgabe zeilengleich mit dem Block in
  `docs/re-abdeckung.md` (nur Marker abweichend), Arbeitsbaum danach sauber; CSV: 275/3/144/257, 0x5AA34 „umgesetzt“ (108 Byte,
  = Differenz 246062 → 246170).

Die zwei neuen Tests sind synthetisch (`TestGames.CreateDatabase`, Hand-Sollwerte), keine Originaldaten nötig – passend zu #755.

**Akzeptierte `[APPROX]`:** keine neuen im Code. **Annahmen:** Beschreibungen der übertragenen Zeilen (A009 … F163) nur gegen
Codemarker und PR-Texte geprüft, nicht gegen jede Fundstelle in em.exe.
