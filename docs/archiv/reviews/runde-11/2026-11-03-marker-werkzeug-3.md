# Unabhängiges Review: PR #472 `marker-werkzeug` Teil 3 (Marker ohne Codeänderung, P3 Nr. 1)

Base `c3eb3377051c40504884f76f2b8ff1f759f2e88e` · Head `1870bec8537bd04ddcabd5113fc81447d32ac5f6` · Reviewer: Claude
(neue Sitzung, geplanter Lauf, ohne Beteiligung an Teil 1–3). Stand: Klon, `git rev-parse HEAD` = Head,
`git status --porcelain` leer; `gamedata/` verknüpft mit `~/referenz/original/gamedata` (SHA256SUMS ok); SDK 10.0.402.
Ausgeführt, nicht nur gelesen.

## 1. Merge-Empfehlung

**Ja** für genau Head `1870bec8537bd04ddcabd5113fc81447d32ac5f6`. In `src/` sind nur Kommentarzeilen geändert (Wortdiff
unten), jede auf `[ORIGINAL]` gestellte Fundstelle ist in `em.exe` nachvollziehbar, A264/A265 bleiben zu Recht `[APPROX]`,
`funktionskarte.py --write` und die R10N-01-Mutation sind ohne Diff, `tools/pruefen.sh --schnell` ist grün, R11I-01 ist wie
vorgeschlagen behoben. Offen sind nur P3 (Wortlaut/Stand der Inventur), nicht merge-hindernd. Merge nur durch
Leitstelle/Nutzer nach `zusammenarbeit.md` §6. Nur `--schnell` verlangt; die Läufe ohne Originaldaten übernimmt die CI.

## 2. Befunde

Keine P0–P2. Zwei P3:

| ID | Prio | Ort | Kurz |
|---|---|---|---|
| R11T3-01 | P3 | [`FinanceStadium.cs` Z. 359](https://github.com/kaaasi/eishockey-manager-redone/blob/1870bec8537bd04ddcabd5113fc81447d32ac5f6/src/EishockeyManager.Core/FinanceStadium.cs#L359) | Fundstelle „Datei 0x38:0x847“ ist keine Dateiadresse |
| R11T3-02 | P3 | [`inventur.md` Z. 742–743](https://github.com/kaaasi/eishockey-manager-redone/blob/1870bec8537bd04ddcabd5113fc81447d32ac5f6/docs/analyse/inventur.md#L742-L743) | F066/F067 laut Standvermerk „erledigt“, Zeilen aber unverändert |

**R11T3-01 (P3).** `0x38:0x847` ist Segment:Offset (so in `finanzen-rekonstruktion.md`); die Dateiadresse der Funktion ist
**0xC037** (`spielberechnung.md` Z. 9: „Datei 0xC037, `0x99F:0x847`“). Mit dem Präfix „Datei“ passt die Angabe nicht zur Schreibweise
des Repos und ist mit dem Werkzeug nicht auffindbar. Inhaltlich stimmt der Marker (siehe 3.1). Vorschlag: „Datei 0xC037
(0x38:0x847 in `finanzen-rekonstruktion.md`)“. Dasselbe in `inventur.md` A044 („0x38:0x847“ ohne „Datei“, dort unkritisch).

**R11T3-02 (P3).** Der Standvermerk in P3 Nr. 1 ([L166](https://github.com/kaaasi/eishockey-manager-redone/blob/1870bec8537bd04ddcabd5113fc81447d32ac5f6/docs/analyse/inventur.md#L166))
sagt „F066/F067 (Playoffs) erledigt“, die Tabellenzeilen F066/F067 (L742/L743) stehen aber noch als „vorhanden“ mit
„Heimrecht [APPROX]“ bzw. „[APPROX] → [ORIGINAL] laut Doku möglich“. Die Karte führt 0x31505 jetzt als „umgesetzt“
(`re-abdeckung.md`: umgesetzt 246 → 247). Vorschlag: beide Zeilen auf „Heimtausch/Siege [ORIGINAL]; Spiel 1 beim besser
Platzierten [APPROX]“ stellen (wie A140).

## 3. Ausgeführte Prüfungen

### 3.1 Gegenbeispiele je Marker (Disassembly `ndisasm -b 16`, Dateiadressen)

| Marker | Fundstelle | Befund |
|---|---|---|
| A139 `WinsNeeded = 3` | 0x315E9 | Siegzähler DGROUP 0xCCA4/0xCCA5 (`inc [bx-0x335c]` / `[bx-0x335b]`, Z. 0x3167A/0x31687). Ab 0x317D3: `cmp wins,2; ja` – bei mehr als 2 Siegen (= drei) wird für die übrigen Spiele (Spiel < 4) Ergebnis 0x64 = 100 eingetragen (Z. 0x31800), sonst Serie weiter (`[0xD0E5] = 1`, Z. 0x31833). **Stützt `[ORIGINAL]`.** |
| A140 Heimtausch | 0x31505 | Tauscht Heim/Gast `[0x5D3B]`↔`[0x5D3C]` sowie Siege `[0xCCA4]`↔`[0xCCA5]` und die bisherigen Spielergebnisse; Aufruf 0x316C9 im Pfad „Spiel gespielt“. **Stützt Heimtausch `[ORIGINAL]`.** `NextPairing` (`Games.Count % 2`) entspricht dem. Spiel 1: Setzung schreibt `[0x5D3B+2i] = 0x5CCB[i]` (besser Platzierter), `[0x5D3C+2i] = 0x5CCB[7−i]` (0x319AC–0x319D4); dass `0x5D3B` die Heimseite ist, steht in der Doku, nicht im gelesenen Code – `[APPROX]` ist dafür vorsichtig und richtig. |
| A141 Setzung / Runden / Meister | 0x31934, 0x31BB0, 0x31CBF | Setzung 1–8, 2–7, 3–6, 4–5 aus Tabelle 0x5CCB (0x31934–0x319D4). Paarungszahl `8 − 4·([0x7B0]>0) − 2·([0x7B0]>1)` (0x31BB0–0x31BDF) = 8 → 4 → 2, drei Runden. Meister in `[0xD0E6]` (0x31CBF). **Stützt `[ORIGINAL]`.** Halbfinal-Paarung (1–8 gegen 4–5) nicht gelesen → `[APPROX]` bleibt richtig. |
| A026 Bundestrainer-Menü Modus 1 | 0x54CEE, 0x555A3 | 0x54CEE: `cmp [0xD0B9],1` → Schleife über Tage (`[0x828+idx]`) bis Wert 0x10, dann Ernennung `[0xD0BA] = 1` (0x54D19). 0x555A3: Menüaufruf `0x110:0x66` am Tagesende, danach Zug 5 (`[0xA70D]=[0xA70E]=5`, 0x555B7). Stützt den Ablauf; „Menü ist das Hauptmenü“ kommt aus `dosbox-befunde.md`. **Stützt `[ORIGINAL]`** (Verhalten gleich, die frühere Näherung war unbeobachtbar). |
| A044 Zufall mit b < a | 0xC037 | `cmp di,si; jnl` – bei b < a werden beide Grenzen getauscht, dann `rand() mod (hi+1−lo) + lo`. **Stützt `[ORIGINAL]`**; Adressschreibweise siehe R11T3-01. Dass im Preisfaktor-Fall K < 0 tatsächlich b < a auftritt, ist Aussage der Doku, nicht neu geprüft. |
| A276 Rundenname nach Laden | Block 0x0903B (Spielstanddatei), `spielstand-original.md` Z. 164 | Tabelle: DGROUP 0x7A9, 8 Byte = 0x7A9–0x7B0, enthält 0x7AD. „Datei 0x0903B“ ist hier ein Offset im Spielstand, nicht in `em.exe`; die Tabelle ist die Quelle, nicht neu aus Spielständen nachgemessen. Wortlaut „sichert … also auch 0x7AD“ ist schlüssig. Als `[ORIGINAL]` akzeptiert. |
| A264 Eintrittspreis `[APPROX]` | 0x3DDB8–0x3DDC5 | `cmp [bp−9],9; jg` … `cmp [bp−9],0x1f; jl` → nur 10–30 DM werden übernommen, Patcher erlaubt 1–99. Weicht ab → `[APPROX]` bleibt richtig. |
| A265 Stadionstufen `[APPROX]` | 0x3DEB4 / re-geld.md | Original: höchste Stufe = Stufenzahl − 1 je Teil (3,3,3,3,5,3,4,4,2), Patcher 0–9 für alle → bleibt `[APPROX]`. Die Begründung im Inventur-Vermerk ist stimmig (Tabelle `re-geld.md` nicht neu gemessen). |

### 3.2 Nur Kommentare in `src/`

`git diff --stat c3eb337..1870bec`: 7 Dateien, 30 +/26 −; davon 4 Dateien in `src/` (`CupsNationalMode.cs`, `FinanceStadium.cs`,
`Playoffs.cs`, `DebCupGamesScreen.cs`). Wortdiff (`--word-diff=porcelain`) mit Filter auf Zeilen, die nicht mit `//`/`///`
beginnen: nur Fortsetzungswörter innerhalb von Kommentarblöcken, **kein Codewort**. Signaturen, Konstanten (`WinsNeeded = 3`),
`NextPairing`, `Rnd` unverändert. Die Funktionskarte zählt die neue Fundstelle 0x31505 (Playoffs.cs:66) – das ist die einzige
Statusänderung (228 B).

### 3.3 Werkzeug und Befehle

| Prüfung | Ergebnis |
|---|---|
| `funktionskarte.py gamedata/em.exe --repo . --write` am Head | **kein Diff** (`git status --porcelain` leer); 679 Funktionen, umgesetzt 247, dokumentiert 171, genähert 4, unbekannt 257 = Tabelle in `re-abdeckung.md` |
| R10N-01: `perl -pi -e 's/(0x[0-9A-Fa-f]{4,5}):(?=[^0-9A-Fa-fx]\|$)/$1 :/g'` auf alle `src/**/*.cs` einer Kopie, `--csv` gegen unveränderte Kopie | **0 Zeilen Diff** (680 Zeilen), kein Statuswechsel |
| R11I-01 | `inventur.md` A231/A232/A238 tragen jetzt „(für Managerkader; Konstanten der Computervereine bleiben `[APPROX]`)“ – wörtlich der Vorschlag aus dem Review #467; `Transfer.Squads.cs` selbst ist unverändert und war schon ehrlich. **Behoben.** (R11I-02/-03 betrafen nicht diesen PR.) |
| `tools/pruefen.sh --schnell` am Head | siehe Tabelle |

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`), Build Release mit Warnungen = Fehler | 1982 | 0 | 0 |

Commit 1870bec8537bd04ddcabd5113fc81447d32ac5f6, SDK 10.0.402, Ergebnis: bestanden. Die Läufe ohne Originaldaten sind für
diesen reinen Kommentar-/Doku-PR nicht verlangt (CI).

### 3.4 Akzeptierte `[APPROX]` und Annahmen

- **Akzeptiert (bleiben `[APPROX]`):** Spiel 1 der Serie beim besser Platzierten (`Playoffs.cs` `NextPairing`), Halbfinal-Paarung
  (1–8 gegen 4–5, 2–7 gegen 3–6), A264, A265. Alle drei Begründungen sind richtig und kurz.
- **Annahmen, nicht selbst geprüft:** Identität „Hauptmenü“ hinter `0x110:0x66` (A026, Doku), K < 0 ⇒ b < a (A044, Doku),
  Spielstandblock 0x0903B (A276, Tabelle), Stufentabelle DGROUP 0x102 (A265). Es wurde kein Spielstand und kein Video herangezogen.
- Keine Originaltreue wird ohne Beleg behauptet; die Aussagen der PR-Beschreibung wurden an den Fundstellen oben nachgeprüft,
  nicht übernommen.
