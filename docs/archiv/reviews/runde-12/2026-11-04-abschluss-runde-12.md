# Review: Abschluss Runde 12 (PR #561)

## 1. Merge-Empfehlung

**Ja für `019bf8df2e9e1661705d9ee682345b133888db55`** (Base `f5d8a043dfc6669d2fcb611b65f8e974ed757ed6`), **nur Codelesen/Dokumentprüfung** – kein
Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 (reine Doku-Änderung, im Review-Lauf kein .NET-SDK; `tools/pruefen.sh` nicht ausgeführt).
Es gibt keinen P0–P2-Befund. Die P3 unten sind Nachbesserungen der Doku und blockieren den Merge nicht; P3-1 und P3-2 am besten noch im
PR beheben, weil das Archiv „unverändert“ sein soll und danach nur bei Bedarf gelesen wird.

Unabhängige Sitzung (Claude, geplanter Lauf), keine Mitarbeit am PR. Geprüft: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain`
leer (`gamedata` ist ein ignorierter Link auf `original/gamedata` des Referenz-Repos, `SHA256SUMS` stimmen).

## 2. Befunde

**P3-1 – Archiv enthält überholte Spurstatus** ([`docs/archiv/runde-12.md`](https://github.com/kaaasi/eishockey-manager-redone/blob/019bf8df2e9e1661705d9ee682345b133888db55/docs/archiv/runde-12.md), Abschnitte der Spuren).
Der Kopf verspricht „Inhalt unverändert bis auf …“, die Statuszeilen sind aber auf dem Stand der Teilläufe stehen geblieben und
widersprechen dem Abschnitt „Ergebnis“ und den PRs:
- `re-pruefung`: „Nachprüfung offen“ – Nachprüfung ist #507 (gemergt, im „Ergebnis“ genannt).
- `saisonwechsel` Teil 6: „fertig (PR folgt)“ – PR #512 (gemergt, in #519 integriert).
- `kader-transfer` Teil 4: „fertig (PR siehe Übergabe, Base `paket/kader-transfer-3`)“ – PR #509.
- `pixel-marker` Teil 2: „fertig“ ohne Nummer – PR #544; Teil 1 #543 und Teil 3 #542 stimmen.
- Spuren `geld` und `wettbewerbe-spiel`: Status nennt PRs, aber nicht die Integration (#505, #508); die Ergebniszeile hat sie.
Gegenbeispiel: `gh api repos/kaaasi/eishockey-manager-redone/pulls --jq '.[]|[.number,.title]'` (alle genannten PRs gemergt) neben den Statuszeilen lesen.

**P3-2 – Pixel-Stand G8 widerspricht der eigenen Nachmessung.** Inventur F155–F157 (`docs/analyse/inventur.md`, neue Stand-Zeilen) und
`docs/arbeitspakete.md` („Pixel offen: … G8 Zeilenhöhen (57ECFh)“) führen „Zeilenhöhen ±1–2 px offen“. `docs/finanzen-rekonstruktion.md`
(„Nachmessung G7/G8 mit DOSBox-Bildern“, Fortsetzung 1 von #490) stellt zustandsgleich fest: dx = dy = 0, die 1–2 px waren Videoversatz,
„G8 erledigt“. Der Archivtext („G8 ohne Abweichung“) stimmt damit; Inventur und Kandidatenliste nennen einen erledigten Punkt weiter
offen. Ebenso F091: „Texte ‚(Noch n Wochen)‘/‚(kein Bau...)‘ unverändert offen“ – „(Noch n Wochen)“ steht seit der Spur `geld` Teil 1 im Code
([`StadiumScreen.cs` Z. 319](https://github.com/kaaasi/eishockey-manager-redone/blob/019bf8df2e9e1661705d9ee682345b133888db55/src/EishockeyManager.UI/Screens/Manager/StadiumScreen.cs#L319), `[APPROX]`);
offen ist „(kein Bau...)“ (kein Treffer in `src/`) und die Belegung des Wortlauts. In die Kandidatenliste gehört außerdem der dort als
„Randzeile, nicht geändert“ geführte Hinweistext „… ausbauen“ (x = 213, y = 201).

**P3-3 – Inventur: Stand-Text in der falschen Spalte, A324/A326 ohne Aussage zur Zeile.** In A035, A194, A324 und A326 steht der
„Stand Runde 12“ in der Spalte *Kandidat* (nach „–“, „S-K6“, „G7“); die Konvention der übrigen Zeilen (A032, A033, A193, A327) ist die
Spalte *Beleg*. Bei A324/A326 steht derselbe Satz („linke Stadiontabelle exakt; rechter Block … angepasst“); er sagt nicht, ob
Platzzahl-Anzeige (A324) bzw. Baustellenanzeige (A326) damit belegt oder weiter `[APPROX]` sind (laut Code für A326 weiter `[APPROX]`, Z. 316).
Inhaltlich sind PR-Nummern und Integrationen richtig: A035 #487→#505, A194 #510→#519 (S-K6 = A), A324/A326/F091/F092/F155–F157 #490→#505.

**P3-4 – Verwaiste Prozessangaben.** PR-Beschreibung nennt Head `ae6b0b5`, tatsächlicher Head `019bf8d`. Issue #534 beschreibt Sitzung 2–4
noch als „offen“, obwohl abgelegt (Kommentare und Referenz-PRs #11–#14 sagen „vollständig“; Kandidatenliste/Archiv sind richtig).
`Fixes #553` ist gesetzt und passt (R12PMN-01 ist durch die Inventurzeilen erledigt, siehe P3-3 zur Qualität).

## 3. Ausgeführte Prüfungen, Abgleiche, `[APPROX]`

**Gegenbeispiele (Funktionskarte):** `python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --write` mehrfach am Head:
Rückgabe 0, `git status`/`git diff` jeweils leer (zweiter Lauf ohne Diff). `--csv` ohne `--write`: 679 Funktionen; erzeugter Block
`docs/re-abdeckung.md`: Spielcode **254 umgesetzt (90 %), 3 genähert, 106 dokumentiert, 1 unbekannt (0x575C3)**, 5097 Zuordnungen, 309525 Byte –
alles wie im Archiv und in der Aufgabe. Keine Abweichung. `ndisasm` vorhanden.

**Archiv gegen PRs/Reviews #480–#561:** alle genannten Nummern geprüft (`gh api …/pulls`): Spur-PRs #480–#515, Integrationen #505/#508/#519/#526/#535/#545,
Fixes #524/#525/#527/#551/#556/#559, Reviews #494/#506/#507/#514/#518/#520/#529/#536/#546/#552/#555/#557/#560, Vorlage #528 – Titel, Zuordnung
und „gemergt“ stimmen; #554 ist (wie im Archiv, D9) ein verworfener PR. Schema 32 → 36 gegen `SaveGame.SchemaVersion = 36`. Entscheidungen E4 = B, S-K6 = A
(Kommentar in #480/#489), D4 = A (#491), D6 = A (#504), D7 = D (#493), D8 = A (#537), D10 = A (#538/#556) stimmen mit den Kommentaren überein;
D1–D3, D5, D9 stammen aus Start-/Prozesskommentaren, mit dem Archivtext vereinbar, im Einzelnen nicht nachverfolgt. Referenz-PRs #9–#14 sind gemergt, FILE-Nummern
und Inventur-IDs wie im Archiv/Kandidatenliste. „Überschrift/Ergebnis“ ohne weitere Auffälligkeit außer P3-1.

**Kandidatenliste Runde 13:** alle erwarteten Punkte vorhanden und bestätigt offen: U15-Rest (#493, #533), DOSBox-Auswertung (#534), Restlücken
`ui-meldungen`, R-K5 (#549, Issue geschlossen), `[APPROX]` S-K6 (#510), P3-Issues #521–#523, #530–#533, #539–#541 (alle offen; #553 durch diesen PR), offene
Prüfbereiche aus #557, Pixel offen, `livespiel` zurückgestellt; #349 steht korrekt unter „Zurückgestellt“. Es fehlen: der Hinweistext „… ausbauen“ (P3-2) und
als Auswertungsnotiz der Dump-Auffälligkeit zu A189 (Kaderplatz-Feld „Reihe“ der Verletzten, Referenz-README Sitzung 3); zu streichen bzw. umzuformulieren: G8 und
„(Noch n Wochen)“ (P3-2).

**Links/Umfang:** relative Links in `archiv/runde-12.md`, `arbeitspakete.md`, `re-abdeckung.md` per Skript geprüft: keine toten. Änderungen nur in
`CLAUDE.md` und `docs/` (`git diff --stat`), kein Code, keine Originaldaten.

**Akzeptierte `[APPROX]`:** keine neuen. **Nicht geprüft (Annahme):** Tabelle von `tools/pruefen.sh` (kein SDK; Doku-PR), D1–D3/D5/D9 im Wortlaut.
