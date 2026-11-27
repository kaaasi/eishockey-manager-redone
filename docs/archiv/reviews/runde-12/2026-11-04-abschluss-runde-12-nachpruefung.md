# Review: Nachprüfung Abschluss Runde 12 (PR #563)

Nachprüfung von `docs/reviews/2026-11-04-abschluss-runde-12.md` (PR #562 zu #561, geprüfter Head `019bf8d`). Geprüft nur das Delta von
#563 (Base `489415d9175adf71742ab0dfa5e599a1446e0f0e`, Head `56f65d99dcb72ad432a366ca837f0b30d2d33f6b`; Head laut API gleich Auftrag) und die Befunde P3-1 bis P3-3.

## 1. Merge-Empfehlung

**Ja für `56f65d9`**, **nur Codelesen/Dokumentprüfung** – kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 (reine Doku-Änderung, im Lauf
kein .NET-SDK; `tools/pruefen.sh` nicht ausgeführt). Geändert sind nur `docs/analyse/inventur.md`, `docs/arbeitspakete.md`, `docs/archiv/runde-12.md`
(`git diff --name-only 489415d 56f65d9`). P3-1, P3-2 und P3-3 sind behoben; ein neuer P3 (A324) blockiert nicht.

Unabhängige Sitzung (Claude, geplanter Lauf), keine Mitarbeit am PR. Klon: `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer.

## 2. Befunde

Zuvor offen, jetzt behoben:

- **P3-1 behoben.** Statuszeilen im Archiv gegen die PRs (alle `merged: true`): `re-pruefung` → Nachprüfung #507 (Datei
  `2026-11-03-re-pruefung-nachpruefung.md`: „Ja für `534e06c`“); `saisonwechsel` Teil 6 → #512, integriert #519 (Review #520); `kader-transfer`
  Teil 4 → #509, integriert #526 mit #524/#525 (Review #529); `pixel-marker` Teil 2 → #544, integriert #545 mit #551 (Review #546, Nachprüfung
  #552; #551 vor #545 gemergt); `geld` „integriert #505“; `wettbewerbe-spiel` „integriert #508 (Review #514, Nachprüfung #518)“. Alle Nummern
  und Titel stimmen. Der Kopf des Archivs nennt die Nachführung jetzt ausdrücklich.
- **P3-2 behoben.** F155–F157 nennen „dx = dy = 0 … G8 erledigt“ wie `finanzen-rekonstruktion.md` („Nachmessung G7/G8 mit DOSBox-Bildern“, Z. 685–697);
  `arbeitspakete.md` führt G8 nicht mehr als offen. F091: „(Noch n Wochen)“ im Code (`StadiumScreen.cs` Z. 317–319, `[APPROX]`), „(kein Bau...)“ fehlt
  (`grep -rn "kein Bau" src tests` ohne Treffer zur Anzeige; nur Kommentare in `FinanceStadium.cs`). Kandidatenliste nennt „(kein Bau...)“, Wortlaut
  „(Noch n Wochen)“ und die Hinweiszeile „… ausbauen“ (x 213, y 201; wie Z. 695 der Rekonstruktion).
- **P3-3 behoben.** A035, A194, A324, A326: „Stand Runde 12“ steht in der Spalte *Beleg*, die Spalte *Kandidat* hat wieder „–“, „S-K6“, „G7“, „G7“.
  PR-Nummern und Integrationen richtig (A035 #487→#505, A194 #510→#519, A324/A326 #490→#505). A326 sagt jetzt, die Baustellenanzeige bleibe `[APPROX]`
  (stimmt mit Z. 317).

Neu:

**P3-4 – A324: Aussage zur Zeile widerspricht dem Code.** [`inventur.md` A324](https://github.com/kaaasi/eishockey-manager-redone/blob/56f65d99dcb72ad432a366ca837f0b30d2d33f6b/docs/analyse/inventur.md)
schreibt „die Platzzahl-Anzeige nach dem Ausbau selbst bleibt `[APPROX]`“. Im Code ist sie `[ORIGINAL]`:
[`StadiumScreen.cs` Z. 230](https://github.com/kaaasi/eishockey-manager-redone/blob/56f65d99dcb72ad432a366ca837f0b30d2d33f6b/src/EishockeyManager.UI/Screens/Manager/StadiumScreen.cs#L230)
(`TargetText`, „Kostenzeile Datei 0x3E52B: Platzzahl nach dem Bau“). Gegenbeispiel: `grep -n "APPROX" …/StadiumScreen.cs` – Treffer nur Z. 41, 54, 131, 268, 317, 339,
keiner an der Platzzahl. Offen bleibt allenfalls, ob die Pixel-Lage der Zahl gemessen ist; der Satz sollte das sagen oder die Zeile als belegt führen.
Der Zeilenanker `:200` der Zeile (Z. 200 ist `Step`) ist bereits vor dem PR veraltet; kein Mangel von #563.

## 3. Ausgeführte Prüfungen, Abgleiche, `[APPROX]`

| Prüfung | Ergebnis |
|---|---|
| `gh api …/pulls/563 --jq .head.sha` | `56f65d9`, gleich Auftrag |
| `git diff --name-only 489415d 56f65d9` | 3 Doku-Dateien, kein Code/Test/Original |
| `python3 tools/reverse-engineering/funktionskarte.py --write <em.exe>` zweimal | beide rc 0, `git status --porcelain` jeweils leer (kein Diff, auch nicht im ersten Lauf) |
| PR-Status #507, #512, #509, #544, #505, #508, #519, #526, #545 (+ #480, #490, #484, #487, #510, #542, #543, #551, #524, #525, #514, #518, #520, #529, #546, #552) | alle gemergt, Titel passen zu den Zeilen |
| `tools/pruefen.sh` | nicht ausgeführt (kein SDK, reine Doku; CI-Lauf nicht geprüft) |

Akzeptierte `[APPROX]`: keine neuen; die genannten bleiben im Code (Z. 41, 268, 317, 339). Annahme: Inhalte von Review-Dateien wurden nur auf die
im Archiv genannten Ergebnisse („Ja“) gelesen, nicht erneut ausgewertet. P3-4 des Vorgängerreviews (Issue #534) war nicht Auftrag; laut PR-Text erledigt, nicht geprüft.
