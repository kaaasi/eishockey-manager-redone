# Nachprüfung PR #727 – Spur hok-export (Fixlauf B1–B5)

Reviewer: Claude Opus 5.5 (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · Vorreview
`docs/reviews/2026-11-10-hok-export.md` (Head `b84b94b`) · geprüft: Delta `b84b94b..ddb730d` und Merge `5e24636` · Head `5e24636`
(`git rev-parse HEAD` = Head, `git status --porcelain` leer) · Originaldaten und Bilder aus `kaaasi/eishockey-manager-referenz`
(Stand `bc1c203`).

## 1. Merge-Empfehlung

**Nein** für Head `5e24636`. B1–B5 sind behoben und mit Gegenproben belegt, und der Merge-Commit ist sauber. Es bleiben aber
zwei P1-Befunde:
- Die zweite DOSBox-Probe des Nutzers hat einen neuen Exportfehler gefunden (R727P-04, Namensreste). Er ist hier mit Bytes belegt.
- `tools/pruefen.sh --schnell` ist auf dem Head rot. Ein Test aus `main` scheitert erst zusammen mit dem Import-Fix aus B1.

Merge erst nach dem Fix von R727P-04 mit Regressionstest, einem grünen Lauf und der dritten Probe (Mannschaft).

## 2. Befunde

**N1 = R727P-04 (P1) Pool-Namen aus der Datenbank überschreiben das Feld, ohne den Rest zu leeren.**
[`OriginalExport.Squads.cs:154–155`](https://github.com/kaaasi/eishockey-manager-redone/blob/5e24636fa0c66859f35b7d25b8352288dc0a0023/src/EishockeyManager.Core/OriginalExport.Squads.cs#L154-L155)
kopiert `raw` nur bis zu dessen Endnull (`DatabasePlayerNames` schneidet am Nullbyte ab). Die übrigen Bytes von `e[..0x12]`
bleiben stehen. Der Zweig ohne Datenbanknamen (`OriginalSaveWriter.Name(…, 0x12)`) schreibt das ganze Feld.
Probe (eigener Python-Decoder, Pool far:8253 +0x00–0x11):
- FILE01: `J.SCHULZ 00…`, `MAYR 00…`, `N 99 TZEL 00…`
- EXPORT1B (= neu erzeugtes EXPORT01): Satz 197 `J.SCHULZV`, 196 `MAYRRSSON`, 189 `N 99 TZELD`, 192 `MENDENZ`, 194 `K]HNLD`
- Schon `emtool hok dump` zeigt diese Namen im Kader. Der Test `Umlauts_ExportedLikeDatabase` ist trotzdem grün.

Die Reste stammen von einem Namen, der vorher am selben Poolplatz stand. Die zweite Schleife (Managerkader, Zeile 63) schreibt
Plätze, die die erste Schleife (`TransferMarket.Pool`, Zeile 49) schon mit einem anderen Spieler belegt hat. **Verdacht (P3):**
Prüfen, ob dabei ein Poolspieler verloren geht. Das heißt nicht nur den Namen leeren, sondern auch klären, warum zwei Spieler
denselben Index bekommen.
Fehlender Test: Für einen neuen Stand und nach n Spieltagen muss jedes Namensfeld bis zur Endnull dem Remake-Namen entsprechen
und danach nur Nullbytes enthalten. Die Rundreise-Tests sehen das nicht, weil dort jeder Platz nur einmal beschrieben wird (siehe 3).

**N2 (P1) Roter Test auf dem Head: `TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` (aus #725).**
- Auf `main` `2f9cc99` grün (15/15 der Klasse).
- Auf `5e24636` rot: erwartet `A.SCHNEIDER, BELFOUR …`, ist `A.OTT, FÜRBACHER …`.
- Gegenprobe: [`OriginalImport.cs:376`](https://github.com/kaaasi/eishockey-manager-redone/blob/5e24636fa0c66859f35b7d25b8352288dc0a0023/src/EishockeyManager.Core/OriginalImport.cs#L376)
  in einer temporären Kopie auf die Absolutwerte zurückgesetzt → grün.

Die Ursache ist also der B1-Import, also #729 (Originalwert − Datenbankwert). Die Übergabe erklärt das schlüssig: Mit dem
geänderten Spielverlauf schlägt der Listenwechsel-Wurf (`GameState.DailyRun.cs`) bei Saat 4711 an. Nicht selbst nachgemessen
habe ich die Saaten 4705–4720. Der Import-Fix ist richtig. Der Test hängt aber an der Saat und ist deshalb hier nicht mehr
gültig. Fremde Datei: Die Leitstelle muss entscheiden, ob die Spur `tagesablauf` den Test ändert (feste Prüfung ohne den Wurf)
oder ob ihn #727 mit Begründung im PR anpasst. Bis dahin gilt `pruefen.sh` nicht als grün (`zusammenarbeit.md` §6).

**B1–B5 aus dem Vorreview: behoben.**
- **B1:** Neu erzeugt (Befehle aus dem Kommentar „zweiter Durchgang“): EXPORT01 SHA-256 `2f9e1010…509f06`, EXPORT02
  `7fe823c4…664fef`. Das ist gleich mit dem Kommentar und mit `EXPORT1B`/`EXPORT2B` im Referenz-Repo. (Σ 0x18–0x1D)/6, Sätze 0–35:
  EXPORT01 27–78, EXPORT02 19–80. Zum Vergleich FILE01 30–77, FILE05 30–78, FILE09 29–78; vorher 0 bzw. 15–24. Gegenprobe:
  Export wieder als Abweichung geschrieben → `ComputerClubStrength_AfterExport…` und `Import_ClubFieldValues_EqualHokBytes` rot (4 von 7).
- **B2:** Kopf +0x25 `K 99 LNER EC` in beiden Exporten. Im Pool NÖTZEL mit 0x99, aber mit Rest (N1). Gegenprobe: beide
  Namensregeln `far:8253 +0x00–0x11` auskommentiert → 50 von 56 Rundreise-Fällen rot. Alle Abweichungen sind erklärt: Reste hinter
  dem Nullbyte (`Export 0x00`) und umbenannte Poolspieler (wenige Paare). **Keine** Abweichung `0x99 → 0x5C` mehr.
- **B3:** `0178` und `965B+0x13` sind in allen 55 Original-Ständen gleich (FILE01/05 `06`, FILE09 `03 0A`). Verschieden sind sie
  nur im alten EXPORT01/02 (`0178` = 0, `+0x13` = 6), und dessen Bild `export01_1` zeigt Kopf 0. Ich habe alle Blöcke gesucht: Nur
  `0178[0]` ist in EXPORT01 0, in EXPORT1B, FILE01 und FILE05 6 und in FILE09 3. Kein anderes Byte erklärt alle Bilder. Der
  Kopf 6 in `export1b_1` bestätigt das. Belegt, `[ORIGINAL]` akzeptiert. Gegenprobe: `0178` nicht geschrieben → 53 von 67
  Export-Tests rot.
- **B4:** EXPORT01 ID −15122 / Füllbyte 0xEC / Schlüssel 0x66, EXPORT02 −20791 / 0xC7 / 0xB9. Zweimal erzeugt ergibt dieselbe
  Datei. Der Nutzer hat beide in einer Sitzung geladen, ohne Sperre. Gegenprobe: feste ID → `Export_DifferentStands_HaveDifferentSaveIds` rot.
- **B5:** Regeln haben jetzt `If`/`Records`, Trefferzahl je Regel (`AllowedRules_EachCoversADifference`, grün, keine Regel mit 0
  Treffern). `965B+0x13` und `0178` sind nicht mehr in der Liste, der Kopf wird byte-genau verglichen. Weiter pauschal: `AB7B` 979,
  `CED8` 1494, `CDB8` 338 Treffer. Das ist in der Übergabe offen benannt und bleibt P3.

**Merge-Commit `5e24636` (besonders prüfen): sauber.** Eltern `ddb730d` und `2f9cc99` (= aktuelles `main`). Ein mechanisches
`git merge 2f9cc99` auf `ddb730d` läuft ohne Konflikt durch und ergibt denselben Baum (`git diff` leer). Es gibt keine eigenen
Änderungen im Merge.

## 3. Ausgeführte Prüfungen, akzeptierte `[APPROX]`

`tools/pruefen.sh --schnell` (Head `5e24636`, SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2718 | 1 | 0 |

Fehlgeschlagen: N2. Die Zahlen stimmen mit der Übergabe überein.

**Weitere Befehle:** `emtool hok export --neu Joerg 5 … --kopf 6 [--spieltage 22]` und `emtool hok dump`. Dazu ein eigener
Decoder (Blockliste aus `hok dump`, `(c − k) XOR k`, `k += 0xD7`) über alle `original/spielstaende/*.HOK`. Den Fremdtest habe ich
zusätzlich im Worktree auf `2f9cc99` gebaut und ausgeführt. Alle Mutationen liefen in einer temporären Kopie und wurden danach mit
`git checkout` zurückgesetzt.

**Warum die Rundreise N1 nicht sieht:** Original → Import → Export schreibt jeden Poolplatz nur einmal, die Regel
`BehindPoolName` deckt dort nur Bytes hinter der Endnull ab. Im Weg `--neu` wird derselbe Platz zweimal beschrieben (N1).

**Erwartet offen – Stand:** Die zweite DOSBox-Probe (EXPORT1B/2B) steht nicht mehr aus. Laut PR-Kommentaren des Nutzers vom
10.11. bestätigt sie R727P-01, -03 und B4 als behoben und R727P-02 bis auf N1. Neu ist R727P-04 (= N1). Die dritte Probe (nur
Mannschaft) steht nach dem Fix aus. Anders als im Aufruf angenommen liegt eine Übergabe des Fixlaufs vor (PR-Kommentar 20:07 UTC).

**Akzeptierte `[APPROX]`:** ID, Füllbyte und Schlüssel aus SHA-256 über den Inhalt (im Bereich der Original-IDs, vom Nutzer
geladen); Spielraum ±10 im Stärketest; kleine Umlaute `{ | }` unbelegt. Die übrigen `[APPROX]` aus dem Vorreview bleiben akzeptiert.

**Annahmen:** Bildinhalte von FILE05/FILE09 und EXPORT1B/2B nach Doku und PR-Kommentaren (`export01_1` und `export1b_1` nicht
selbst ausgewertet). Saat-Erklärung zu N2 aus der Übergabe.
