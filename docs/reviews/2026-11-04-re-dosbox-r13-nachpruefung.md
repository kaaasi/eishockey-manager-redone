# Nachprüfung Review PR #567 (Spur `re-dosbox`, Runde 13)

Base `4b8e83db9852953050186957ae2a13d7f702cec6` · Head `bffbdb9e23cd4d40201c4078fa752bfffc5ae059` · Vorheriges Review
[`2026-11-04-re-dosbox-r13.md`](https://github.com/kaaasi/eishockey-manager-redone/pull/576) (Head `b25d246`). Reviewer: Claude Opus 5.5,
geplanter Lauf, neue Sitzung ohne Implementierungsbeteiligung. Geprüft: Delta `b25d246..bffbdb9` (Korrektur-Commit `f99f3c3`
plus Merge von `main`) und R13RD-01 bis R13RD-06.

## 1. Merge-Empfehlung

**Ja für `bffbdb9`.** R13RD-01 ist behoben: Die Funktionskarte am Head entspricht `main` in jedem Funktionsstatus. R13RD-02 ist
durch L9–L12 entschieden, „Umsetzung“ folgt L1–L12 und hat Vorrang vor älteren Stellen. R13RD-03 bis R13RD-06 sind wie verlangt
umgesetzt. Der Merge statt Rebase ist sauber: Der Baum ist gleich dem Rebase-Stand, geändert ist nur Doku. Die zwei neuen Befunde
sind P3 und blockieren nicht.

Erwartet offen und bestätigt:
- L7: fehlende Aufnahmen, entscheidet der Nutzer (Aufnahme `runde-13` für Runde 14). „Umsetzung“ führt sie als offen, die
  betroffenen Punkte bleiben `[APPROX]`.
- 0x346D5 (L8): weiter nicht gelesen, zugeordnet an `saison`.
- Der Werkzeugvorschlag `HOK 0x…` für `pruefung-karte` ist nicht umgesetzt (`tools/reverse-engineering/*` gehört
  `pruefung-karte`). Am Head kommt `HOK` in keiner Fundstelle einer Funktion vor, das Präfix braucht also heute keine
  Sonderregel. Es schützt nur, solange die Karte `Datei 0x…` als EXE-Offset liest.

## 2. Befunde

Alle Befunde des vorherigen Reviews sind erledigt:

| ID | Stand am Head |
|---|---|
| R13RD-01 | behoben, Gegenbeispiel unten |
| R13RD-02 | behoben über L9–L12 ([Kommentar](https://github.com/kaaasi/eishockey-manager-redone/pull/567#issuecomment-6633831366)) und Vorrangsatz in [`re-runde-13.md:786`](https://github.com/kaaasi/eishockey-manager-redone/blob/bffbdb9e23cd4d40201c4078fa752bfffc5ae059/docs/analyse/re-runde-13.md#L786) |
| R13RD-03 | behoben (U15-a, R-K5, S-K6-a, S-K6-b in Übersicht und Abschnitt gleich, nach Kopfdefinition) |
| R13RD-04 | behoben (Einschränkung auf 0x18–0x1B, Folgerung für `_germanyRatings`) |
| R13RD-05 | behoben (`MatchEngine.National.cs:58`, Inhalt am Head geprüft) |
| R13RD-06 | behoben (A166 erst mit dem Regressionstest auf `[ORIGINAL]`) |

**R13RDN-01 (P3) – Reste aus der Zeit vor den Entscheidungen.**
- Übersicht [`re-runde-13.md:27`](https://github.com/kaaasi/eishockey-manager-redone/blob/bffbdb9e23cd4d40201c4078fa752bfffc5ae059/docs/analyse/re-runde-13.md#L27): A088 steht noch mit „`saison` (Dateigrenze offen,
  Frage 4)“, entschieden durch L4.
- Eigener Statuspunkt [`arbeitspakete.md:66`](https://github.com/kaaasi/eishockey-manager-redone/blob/bffbdb9e23cd4d40201c4078fa752bfffc5ae059/docs/arbeitspakete.md#L66): noch „8 offene Fragen an die Leitstelle“.
- Die Abschnitte oben nennen weiter `docs/spielberechnung.md` und `re-runde-12.md` für `saison` (Z. 732) bzw. „Frage 2“
  (Z. 778).

Wirkung: keine, denn „Umsetzung“ (Z. 785–787) erklärt die Entscheidungen bei Widerspruch für maßgeblich. Kann beim Merge bleiben
oder von `pruefung-karte` mit `inventur.md` bereinigt werden.

**R13RDN-02 (P3, außerhalb dieses PRs) – L9–L12 stehen noch nicht in `docs/arbeitspakete.md`.**
Laut L12 sind die Dateigrenzen von `pruefung-karte` „um diese drei Dateien erweitert“. Auf `main` `4b8e83d` nennt
`arbeitspakete.md` Z. 136 aber nur `tests/**/Support/`, neue Testdatei, `tools/reverse-engineering/*`, `re-abdeckung.md` und
`inventur.md`. Die „Zuständigkeit für gemeinsame Dateien“ (Z. 44) gibt `GameState.Integration.cs` weiterhin allein an `saison`
(L9 fehlt). `GameSetup.cs` (L11) und `StatisticsStrength.cs` (L10) fehlen ebenfalls. Startet `nationalteam` nach
`arbeitspakete.md`, verletzt die Spur ihre Dateigrenzen, wenn sie die Umsetzung aus `re-runde-13.md` befolgt.

Vorschlag: Die Leitstelle trägt L9–L12 in `arbeitspakete.md` ein, bevor `nationalteam` oder `pruefung-karte` starten.

## 3. Ausgeführte Prüfungen

**Checkout:** Klon (Worktree), `git rev-parse HEAD` = `bffbdb9e23cd4d40201c4078fa752bfffc5ae059`, `git status --porcelain`
leer. Referenz-Repo `e3a591e` (`git ls-remote` erreichbar, ohne `add_repo`), `em.exe` aus `original/gamedata/`.

**Merge statt Rebase:**
- `git log --graph b25d246..bffbdb9`: `f99f3c3` (nur `docs/analyse/re-runde-13.md`, +73/−45), danach der Merge `bffbdb9`
  mit den Eltern `f99f3c3` und `4b8e83d`.
- Die Merge-Basis `f99f3c3`/`4b8e83d` ist `8afd750` (= alter Base). `main` brachte nur `docs/vorlagen/dosbox-aufnahme.md`
  (#575) mit.
- Eigener Rebase von `f99f3c3` auf `4b8e83d` im Scratch: Baum `2214ee1a4697e3a2e413223f2e2a0ba952a18531`, gleich dem Baum von
  `bffbdb9`.
- `git diff --stat 4b8e83d bffbdb9`: nur `docs/analyse/re-runde-13.md` (neu) und `docs/arbeitspakete.md` (eine Zeile,
  eigener Status). Keine Konfliktauflösung, kein Code.

**R13RD-01, Gegenbeispiel erneut:**
Befehl `funktionskarte.py <em.exe> --repo <checkout> --csv <scratch>`, ohne `--write`, je an `main` `4b8e83d`, Head `bffbdb9`
und zum Vergleich am alten Head `b25d246`. Die Arbeitsbäume blieben danach unverändert.

| Stand | Zuordnungen | umgesetzt | genähert | dokumentiert | unbekannt | 0x03AE6 |
|---|---:|---:|---:|---:|---:|---|
| `main` `4b8e83d` | 5097 | 265 | 3 | 154 | 257 | unbekannt |
| Head `bffbdb9` | 5349 | 265 | 3 | 154 | 257 | unbekannt |
| alt `b25d246` | 5355 | 265 | 3 | 155 | 256 | dokumentiert |

- Statusvergleich je Funktion `main` ↔ Head (679 Funktionen): **keine Abweichung**.
- `re-runde-13.md` ist Fundstelle in 69 Funktionen (alt: 72). Weggefallen sind 0x03AE6, 0x05028 und 0x0659C, ohne neue Funktion.
- 0x087F7 behält nur die echte Fundstelle Z. 718 (lrandom-Nutzer). Die falsche Zuordnung aus Z. 49 ist weg. Die Selbstprüfung
  der Datei (5349, 154/257, 69, keine Statusänderung) stimmt.
- Eigene Suche: alle `Datei 0x…` mit Wert < 0x12000 ausgewertet. Treffer: 0x0952C, 0x09718, 0x09672, 0x5C44, 0x7C07, 0x5C50,
  0xBFD5, 0xC037, alle Code in `em.exe`, kein Spielstand-Offset. Für jede Funktion mit der Datei als Fundstelle wurden die Zeilen
  mit „HOK“ oder „Spielstand“ geprüft: Treffer nur Z. 40 (Datei 0x29CFA) und Z. 43 (Datei 0xBFD5), beides echte EXE-Offsets.
- Neuer Kopf (Z. 6–8): „Adressen in `em.exe` stehen als `Datei 0x…`“, Spielstand als `HOK 0x…`. Das Wort „Dateioffsets“ ist
  weg. Die Karte zeigt keine Zuordnung aus dem Kopf.
- Die zwei neu präfixierten Angaben sind echt:
  - Datei 0x7C07 ist `call 0x14b7:0x4d` → 0x426BC (Tageslauf ruft die Auto-Aufstellung).
  - Datei 0x5C50 ist `mov byte [es:bx+0xa],0x0` (nach `or [es:bx+0x9],2` bei 0x5C44 und `+0x17` bei 0x5C4C).

**R13RD-02, Gegenprüfung:**
- „Umsetzung“ (Z. 785–813) gegen L1–L8 ([Kommentar](https://github.com/kaaasi/eishockey-manager-redone/pull/567#issuecomment-6633257714))
  und L9–L12 gelesen. Jede Entscheidung ist richtig wiedergegeben: L1 global, L2 U15-c `[APPROX]`, L3 LeagueSlots je Teil,
  L4 A088 `saison` mit `ResetMatchCounters`/`Player.Factory.cs`, L5 Schema 37/38 und `SaveGame.Migration.Saison.cs`,
  L6 0x3A88A, L7, L8, L9 `GameState.Integration.cs` nur Tageslauf Bundestrainer, L10, L11, L12.
- Gegen `arbeitspakete.md` Runde 13: Übrige Zuordnungen wie im vorherigen Review. Abweichungen sind nur durch L9–L12 gedeckt,
  siehe R13RDN-02.

**`tools/pruefen.sh`** (alle drei Läufe, SDK 10.0.402 aus Release `claude-werkzeuge`, Commit `bffbdb9`, Ergebnis: bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2162 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1818 | 0 | 308 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1819 | 343 | 0 |

Reiner Doku-Delta, deshalb keine Mutationsproben.

**Akzeptierte `[APPROX]`:** keine neuen.

**Annahmen:** keine über das vorherige Review hinaus. Die Spielstände wurden für das Delta nicht erneut entschlüsselt, denn die
Korrektur ändert nur Präfixe und Wörter. Die Zahlen in R13RD-04 (FILE20 `00 06` → FILE21 `33 2E`) stammen aus dem
vorherigen Review.
