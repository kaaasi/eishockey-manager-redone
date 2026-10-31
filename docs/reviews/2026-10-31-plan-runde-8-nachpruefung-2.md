# Zweite Nachprüfung PR #231 – Plan Runde 8

PR #231 (`plan/runde-8`) · Base `d7d8401eb38c4bab9e09f3137d6428f5e0f42a4a` · Head `57338a2be35d52111776a8366a9a36c68464f8eb`
Nachprüfung von `docs/reviews/2026-10-31-plan-runde-8-nachpruefung.md` (PR #237, geprüfter Head `e594686`), nur Delta
`e594686..57338a2` (ein Commit, nur `docs/arbeitspakete.md`, +17/−14) und die Befunde R8P-01, R8N-01 bis R8N-03.
Reviewer: Claude (geplante Sitzung `…bnU`; der Delta-Commit stammt aus Sitzung `…cc`, an #231 nicht beteiligt).

Links: `A` = `https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/docs/arbeitspakete.md`,
`T` = `https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/src/EishockeyManager.Core/Squad.Tactics.cs`.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head – **nur Codelesen** (reiner Doku-PR, kein Build, kein `tools/pruefen.sh`, kein Testnachweis
nach `zusammenarbeit.md` §6 Nr. 4; für einen Plan-PR nicht nötig). Beide offenen P2 (R8P-01 #232, R8N-01 #236) sind
aufgelöst, R8N-02 und R8N-03 umgesetzt. Keine neuen Befunde ab P2; #232 und #236 kann die Leitstelle mit dem Merge schließen.

## 2. Befunde

### R8P-01 · aufgelöst (#232)

[A#L43-L46](https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/docs/arbeitspakete.md#L43-L46),
[A#L175-L176](https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/docs/arbeitspakete.md#L175-L176).
Gegenbeispiel erneut ausgeführt: Das Nationalteam bleibt auf `TacticMode.Auto`, `ShownEffort/ShownAttack` liefern dort
`AutoEffort(0, 0, …)` ([T#L214-L219](https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/src/EishockeyManager.Core/Squad.Tactics.cs#L214-L219)).
Jetzt darf `nationalteam` genau diese Anzeige samt Zustand „Werte des letzten Spiels“ ändern. Der Schreibpunkt liegt in
eigenen Dateien: Länderspiele laufen über `CupsNational.GameState.cs:175` und `CupsWorld.cs:183`; dort kann vor dem Spiel
die Automatik (`Auto*`, statisch, nur gelesen) die Werte ablegen – wie im Original vor jeder Paarung (Datei 0x8365,
`docs/dosbox-befunde.md:96`). Migration dafür: `Core/SaveGame.Migration.<Spur>.cs` (A#L176). Gegenbeispiel greift nicht mehr.

### R8N-01 · aufgelöst (#236)

[A#L43-L46](https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/docs/arbeitspakete.md#L43-L46),
[A#L143-L146](https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/docs/arbeitspakete.md#L143-L146).
Gegenbeispiel erneut ausgeführt: `EffortStrengthBonus` (0x974B, [T#L283-L291](https://github.com/kaaasi/eishockey-manager-redone/blob/57338a2be35d52111776a8366a9a36c68464f8eb/src/EishockeyManager.Core/Squad.Tactics.cs#L283-L291);
Aufrufer `Squad.Strength.cs:81`, `CupsNational.Lineup.cs:198`) und `EffectiveEffort/EffectiveAttack` (0x939A; Aufrufer
`MatchEngine.Live.cs:125-126`, `MatchEngine.Minutes.cs:335-336`) und `Auto*` stehen jetzt in den Dateigrenzen von
`spielstaerke-manager`. Es bleibt eine Rechnung für Verein und Nationalteam (`TacticsBonus` ruft weiter
`EffortStrengthBonus`). Gegenbeispiel greift nicht mehr.

**Überschneidungsfreiheit `Squad.Tactics.cs`** (alle Mitglieder am Head geprüft): `nationalteam` = Startwert (Konstruktor
bzw. eigener Startwert, Vereinsstart unverändert) + `Shown*` + neuer Zustand; `spielstaerke-manager` = `EffortStrengthBonus`,
`Effective*`, `Auto*`. Keine Methode gehört beiden. Einzige Berührung: `Shown*` ruft `Auto*` – durch die Integrationsfolge
`nationalteam` vor `spielstaerke-manager` (A#L46, deckt sich mit A#L32) rebased die spätere Spur auf die frühere. Übrige
Mitglieder (`Effort/SetEffort`, `*Limit`, `Minutes`, `Situation`, `ModeText` …) braucht keine Spur.

### R8N-02 · umgesetzt (P3)

Alle sechs Spuren mit möglichem Zustand (`spielerwert` A#L78, `transfer-original` L94, `tabellen-original` L125,
`spielstaerke-manager` L146, `statistiken` L161, `nationalteam` L176) und die Regel (L47-L48) nennen
`Core/SaveGame.Migration.<Spur>.cs`; `git grep 'Migration\*'` am Head findet keinen alten Platzhalter mehr. Namenskollision
ausgeschlossen.

### R8N-03 · umgesetzt (P3)

`nationalteam`: Modus 1 und 4, Modus 2 als Gegenprobe (A#L173-L174); `spielstaerke-manager`: Modus 1, 2 und 4 (A#L141).
Passt zu `CupsNationalMode.cs:6-15` und #192.

### P3 (nicht blockierend, kein Issue)

- A#L175-L176 (Dateigrenzen `nationalteam`) nennt nur „Startwert und `Shown*`“, nicht den Zustand; der Verweis „siehe
  Zuständigkeiten“ deckt ihn ab. Zeile L176 ist länger als die übrigen (Umbruch). Kosmetik.
- Konstanten und Tabellen hinter den Methoden (`EvenEffort`, `FavouriteAttack`, `ReferenceEffort` …) sind nicht eigens
  genannt; sinngemäß gehören sie zur Methode, die sie nutzt (`Auto*` bzw. `EffortStrengthBonus`).

## 3. Ausgeführte Prüfungen und Annahmen

- Checkout: `git fetch origin pull/231/head`, `git rev-parse` = `57338a2be35d52111776a8366a9a36c68464f8eb`;
  `git diff --stat e594686 57338a2`: nur `docs/arbeitspakete.md` (+17/−14). PR-Stand per REST: offen, Head und Base wie oben.
- **Nur Codelesen:** kein Build, kein `tools/pruefen.sh` (reiner Doku-PR, laut Auftrag nicht nötig). Keine Testtabelle.
- Gelesen: Delta vollständig; `Squad.Tactics.cs` am Head (alle Mitglieder); Aufrufer von `Shown*`, `Effective*`,
  `EffortStrengthBonus`, `new Tactics()` per `git grep` in `src/`; Länderspielpfad (`MatchEngine.PlayWithEvents` in
  `CupsNational.GameState.cs`, `CupsWorld.cs`); `docs/dosbox-befunde.md` (#192); Integrationsfolge A#L32.
- Angenommen (unbeaufsichtigter Lauf): „Startwert“ erlaubt `nationalteam` einen eigenen Startwert für die Nationalteam-Taktik
  (z. B. Fabrik oder Konstruktorparameter), ohne den parameterlosen Vereinskonstruktor zu ändern; die Vereinsanzeige unter
  „AUTOM.“ bleibt bewusst `[APPROX]` (im Original schreibt die Automatik auch Vereinswerte, Datei 0x8365) – Planentscheidung,
  kein Befund.
- Issues: keine neuen. Akzeptierte `[APPROX]`: keine (kein Code).
