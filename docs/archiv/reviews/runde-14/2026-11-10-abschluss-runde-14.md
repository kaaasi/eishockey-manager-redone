# Review PR #717: Abschluss Runde 14 (Doku-Prüfung)

Geprüft: Base `31fa32a81206a8d54fbe8196b62cc931f3a5e7e4` → Head `6c9b446efe834b55b65e788b8bfa8949370c2956`
(Tree `ed1156e`). Reviewer: Claude Code, geplanter Lauf, eigene Sitzung ohne Mitarbeit an #717. Reine Doku-Prüfung, kein Build.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Der ausgelagerte Abschnitt ist bis auf die zwei angepassten Links wortgleich, nichts Nötiges geht
verloren, alle relativen Links lösen auf, „Ergebnis“ und „Abschluss“ stimmen mit den PRs und Reviews überein – bis auf eine falsche
Anfangszahl beim Schema (R717-01, P3). Die P3-Punkte können beim Plan der Runde 15 mit erledigt werden.

## 2. Befunde

**R717-01 (P3) – Schemaangabe falsch.** [`docs/archiv/runde-14.md` Z. 9](https://github.com/kaaasi/eishockey-manager-redone/blob/6c9b446efe834b55b65e788b8bfa8949370c2956/docs/archiv/runde-14.md#L9)
nennt „Spielstand-Schema 41 → 47“; die Runde begann bei 39 (Z. 37 derselben Datei: „Stand Schema 39“).
Beleg: `git show 401efa7:src/EishockeyManager.Core/SaveGame.cs | grep 'SchemaVersion ='` → `39` (Planstand der Runde);
am Merge von #652 (erste Integration, `saison`) → `40`, nach #661 → `41`, an `31fa32a` → `47`. Richtig: „39 → 47“.

**R717-02 (P3) – Übergangsliste nicht nachgeführt.** In [`docs/arbeitspakete.md` Z. 45](https://github.com/kaaasi/eishockey-manager-redone/blob/6c9b446efe834b55b65e788b8bfa8949370c2956/docs/arbeitspakete.md#L45)
fehlen bei „Offene Issues aus Runde 14“ #712 und #716 (Z. 14 nennt sie, also kein Verlust). Unter „Zurückgestellt“ sind
Z. 23 („#349: in Runde 14 …“; #349 ist geschlossen) und Z. 25 („Import/Export ist in Runde 14“; Export ist Runde 15, R14-D2 = B)
überholt. Beleg: `gh api repos/kaaasi/eishockey-manager-redone/issues/349 --jq .state` → `closed`. Der Wortlaut ist
gegenüber der Base unverändert übernommen; das ist zulässig, die Zeilen sollten aber beim Plan der Runde 15 bereinigt werden.

**R717-03 (P3) – Kopfsatz des Archivs unvollständig.** [`runde-14.md` Z. 3](https://github.com/kaaasi/eishockey-manager-redone/blob/6c9b446efe834b55b65e788b8bfa8949370c2956/docs/archiv/runde-14.md#L3)
sagt „Inhalt unverändert bis auf Überschrift, ‚Ergebnis‘ und ‚Abschluss‘“. Tatsächlich fehlen im Archiv die Unterabschnitte
„Zurückgestellt“ und „Vormerkungen Runde 15“ (Base Z. 223–254, 32 Zeilen), die bewusst in `arbeitspakete.md` bleiben, und
zwei Links sind angepasst. Beleg: Diff unten (Z. 212–243 nur in der Base). Vorschlag: „… und ohne ‚Zurückgestellt‘/‚Vormerkungen‘
(bleiben in `arbeitspakete.md`)“.

Erwartet offen, bestätigt (alle `open`): #620, #698, #699, #703, #712, #716.

## 3. Ausgeführte Prüfungen

Checkout: `git fetch origin pull/717/head`, `git checkout 6c9b446`; `git rev-parse HEAD` = Head, `git status --porcelain` leer;
Merge-Basis mit `origin/main` = Base (`31fa32a`, zugleich aktueller `main`). Diff `--stat`: nur `docs/arbeitspakete.md`
(−211/+3) und neu `docs/archiv/runde-14.md` (+229).

**(1) Unverändert ausgelagert:** Base-Abschnitt `arbeitspakete.md` Z. 12–254 gegen Archiv ab Z. 19 (`diff`):
einzige Abweichungen Z. 2–3 (Linkziele `archiv/runde-13.md` → `runde-13.md`, `archiv/` → `./`, Linktext gleich, wie in
`runde-13.md`) und die nicht archivierten Unterabschnitte (R717-03). Rest von `arbeitspakete.md` (Z. 1–10 und ab „Paket
`hok-format`“) gegen die Base: `diff` leer. „Zurückgestellt“ und „Vormerkungen Runde 15“ im Head wortgleich zur Base.

**(2) Ergebnis/Abschluss gegen GitHub** (`gh api …/issues/<nr>`): #652, #661, #666, #669, #670 sind die Integrations-PRs
`saison`, `nationalteam`, `ui-rest`, `kader-start`, `hok-import`; #672 `nationalteam` Teil 4; #701 Integration `pruefung-karte`;
#683 Play-off-Halbfinale; #688 Tagestakt im Hauptweg – alle gemergt. `re-dosbox` lief über #628/#642 (im Archivtext Z. 77–78
belegt; die Klammer in Z. 8 nennt nur Integrations-PRs, kein Fehler). Gesamtreview #704–#707, #710, #715 gemergt;
`2026-11-09-runde-14-gesamt-teil6.md` Empfehlung **Ja**, Zuschnitt Entscheidung C 10.11.2026, keine neuen Produktfehler, nur P3/Testlücken
(→ #716 offen). Prozess: #684/#686 (sparsame Kette und Modellwahl, PR-Text Punkt 2), #708 (Beleg, Verkettung, Längen, Leitstelle
je Runde, `review-ja`), #713 (risikoorientiert, `review-ja`) gemergt. Schema: siehe R717-01.

**(3) Links:** alle relativen Ziele in `runde-14.md` (`runde-13.md`, `./`) und `arbeitspakete.md` (`../AGENTS.md`, `archiv/`,
`archiv/runde-14.md`, `archiv/runde-3.md`, `prozess/zusammenarbeit.md`, `wettbewerbe.md`) per Skript auf Existenz geprüft: alle
vorhanden. Pfade in Backticks (`reviews/…`) sind keine Links. Zeilenanker `arbeitspakete.md#L…` anderer Dateien zeigen nur auf
feste SHAs, brechen also nicht.

Nicht geprüft: Build/Tests (nur Doku, laut Auftrag entfallen). Keine `[APPROX]` betroffen.
