# Nachprüfung PR #231 – Plan Runde 8

PR #231 (`plan/runde-8`) · Base `d7d8401eb38c4bab9e09f3137d6428f5e0f42a4a` · Head `e5946861b3ae6786924288ab82d603d7cc34424c`
Nachprüfung von `docs/reviews/2026-10-31-plan-runde-8.md` (PR #235, geprüfter Head `80cda6a`), nur Delta
`80cda6a..e594686` (ein Commit, nur `docs/arbeitspakete.md`) und die Befunde R8P-01 bis R8P-08. Reviewer: Claude
(geplante Sitzung `…qG`; Plan-Commits aus den Sitzungen `…nu` und `…cc`, an #231 nicht beteiligt).

Links: `A` = `https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/docs/arbeitspakete.md`,
`T` = `https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/src/EishockeyManager.Core/Squad.Tactics.cs`.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – **nur Codelesen** (reiner Doku-PR, kein Build, kein `tools/pruefen.sh`, kein
Testnachweis nach `zusammenarbeit.md` §6 Nr. 4). R8P-02, R8P-03 und alle P3 sind aufgelöst. Offen bleiben zwei P2, beide
in `Core/Squad.Tactics.cs` und mit einem Satz in den Zuständigkeiten (A#L43-L44) zu beheben: R8P-01 ist nur teilweise
aufgelöst (die Grenze „nur Startwert“ reicht für #192 nicht), und die neue Alleinzuständigkeit sperrt einen Teil von
`spielstaerke-manager` (R8N-01, #236). Danach genügt eine Nachprüfung dieser Zeilen.

## 2. Befunde

### R8P-01 · P2 – teilweise aufgelöst, bleibt offen (#232)

[A#L43-L44](https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/docs/arbeitspakete.md#L43-L44),
[A#L172-L173](https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/docs/arbeitspakete.md#L172-L173).
Aufgelöst: `nationalteam` hat jetzt `Squad.Tactics.cs` und `SaveGame.Migration*.cs`.
Gegenbeispiel erneut ausgeführt: Der Startwert allein wirkt nicht. Das Nationalteam steht – wie im Original (DGROUP
0xA6FB/0xA707, 0 = „AUTOM.“; `docs/dosbox-befunde.md:46`) – auf `TacticMode.Auto`, und dann zeigt
`ShownEffort/ShownAttack` immer `AutoEffort(0, 0, …)` = 60/0/25/20/30/0 bzw. 0/4/5/6/7/4, unabhängig von den gespeicherten
Werten ([T#L213-L219](https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/src/EishockeyManager.Core/Squad.Tactics.cs#L213-L219)).
Ein Konstruktor mit 0 ändert also weder Anzeige noch Spiel. `docs/dosbox-befunde.md:96` sagt ausdrücklich: „Umsetzung
braucht eine Änderung in `Squad.Tactics.cs` (**Anzeige „AUTOM.“**) und Zustand über den Saisonwechsel“ – im Original zeigt
„AUTOM.“ die zuletzt von der Automatik geschriebenen Werte (0 bis zum ersten Spiel). Die neue Grenze „nur der Startwert …;
Automatik bleibt unverändert“ schließt genau diese Anzeige aus. (Der Wortlaut folgt der Abhilfe des ersten Reviews; die
war zu eng.)
Abhilfe: `nationalteam` in `Squad.Tactics.cs` „Startwert und Anzeige unter ‚AUTOM.‘ (`Shown*`) samt Zustand ‚Werte
des letzten Spiels‘ für das Nationalteam“ erlauben; Vereinsanzeige und Automatik im Spiel unverändert.

### R8N-01 · P2 – `Squad.Tactics.cs` allein bei `nationalteam` sperrt `spielstaerke-manager` (#236, neu durch das Delta)

[A#L43-L44](https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/docs/arbeitspakete.md#L43-L44),
[A#L129-L136](https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/docs/arbeitspakete.md#L129-L136).
Gegenbeispiel: `spielstaerke-manager` soll die offenen Teile aus `docs/spielberechnung.md` §8/§11 umsetzen; dort stehen
(`spielberechnung.md:197-198`) der **Einsatzzuschlag 0x974B** und die **Taktikwerte im Spiel 0x939A** als `[APPROX]` „über
`Tactics.Effective*`“. Beides liegt in `Squad.Tactics.cs`: `EffortStrengthBonus`
([T#L283-L291](https://github.com/kaaasi/eishockey-manager-redone/blob/e5946861b3ae6786924288ab82d603d7cc34424c/src/EishockeyManager.Core/Squad.Tactics.cs#L283-L291),
gerufen aus `Squad.Strength.cs:81` und `CupsNational.Lineup.cs:198`) und `EffectiveEffort/EffectiveAttack`. Vor dem Delta
war die Datei keiner Spur zugeordnet, jetzt „nur `nationalteam`“. `spielstaerke-manager` müsste die Grenze verletzen oder
eine zweite Rechnung in `Squad.Strength.cs` bauen, während das Nationalteam über `TacticsBonus` die alte behält.
Abhilfe: Datei teilen – `nationalteam` Startwert und `Shown*` (R8P-01), `spielstaerke-manager` `EffortStrengthBonus`,
`Effective*`, `Auto*`. Die Integrationsfolge (`nationalteam` vor `spielstaerke-manager`) passt bereits.

### R8P-02 · aufgelöst (#233)

#193 samt `UI/Screens/Statistics/StrengthScreen.cs` gehört allein `spielstaerke-manager` (A#L41-L42, L129–L136, L142);
`nationalteam` nennt ausdrücklich „#193 gehört zu `spielstaerke-manager`“ (L176). `StrengthScreen.cs` steht in keiner
anderen Runde-8-Spur (`statistiken` führt nur die drei neuen Bildschirme; `UI/Screens/Statistics/*` in A#L254 gehört zum
abgeschlossenen Paket `wettbewerbe`). Der Anschluss nach Freundschaftsspielen in `CupsNational*.cs` macht die Leitstelle;
weil `spielstaerke-manager` zuletzt integriert wird, ist die Datei dann frei. Gegenbeispiel greift nicht mehr.

### R8P-03 · aufgelöst (#234)

`optionen-kalender` belegt und dokumentiert nur; Spielstand-Optionen (Spielmodus, WM-Spiele, Länderspiele) zeigt der
Bildschirm lesend an, Umsetzung samt Migration ist Folgepaket `optionen-wirkung` (A#L182-L191, L215-L216). Die
Dateigrenzen bleiben UI-only und brauchen jetzt keine Core-Datei: `GameState.Mode` (`CupsNationalMode.cs:53`) wird nur
gelesen. `optionen-wirkung` ist schlüssig abgegrenzt (Core-Wirkung der 0x2CD1C-Optionen, Abgrenzung zu `nationalteam`
für `CupsNationalMode.cs`, auf den Belegen von `optionen-kalender`, Runde 9). Gegenbeispiel greift nicht mehr.

### P3 (nicht blockierend)

- **R8P-04 bis R8P-08 umgesetzt:** Vorab-Zeile und Aufruferzahl #225 bei `spielerwert` (A#L30, L37–L38); `docs/wettbewerbe.md`
  „eigener Abschnitt“ bei `nationalteam` (L175–L176); Ausnahme `LineupScreen.cs` (L55–L56) und stabiles
  `PlayerInfoPopup.Show(Player, long)` (L106–L107); Migrationsregel mit `SaveGame.cs` bei der Leitstelle (L45–L46) und
  `SaveGame.Migration*.cs` bei `spielerwert`, `transfer-original`, `tabellen-original`; 0x50283 „ob die Liga sie nutzt,
  zuerst klären“ (L117), Highscore nur Anzeige (L73), Lader 0x2EF1B (L133); Lücken-Nummer #194 bei `befunde-p3` (L204).
- **R8N-02 – Namen der Migrationsdateien.** Sechs Spuren legen parallel `SaveGame.Migration*.cs` an; mit der üblichen
  Zählung (`Migration3.cs`, `Migration4.cs`) würden zwei Spuren dieselbe Datei `Migration<n>.cs` anlegen. Vorschlag: Name
  je Spur (`SaveGame.Migration.<Spur>.cs`), die Leitstelle nummeriert in der Integration (wie A#L21 schon sagt).
- **R8N-03 – Modus der Mehrsaisonproben (nicht im Delta).** Modus 1 = nur Weltkugel, 2 = nur Fahne, 4 = beide
  (`CupsNationalMode.cs:6-15`). `nationalteam` prüft „Modus 2 und 4“ (L170), obwohl #192 ausdrücklich Modus 1 und 4 nennt
  und es in Modus 2 kein Nationalteam gibt; `spielstaerke-manager` prüft „Modus 1 und 4“ (L139), obwohl die Managervereine
  in Modus 1 keine Saison spielen (durch #193 ist Modus 1 dort jetzt immerhin sinnvoll). Vorschlag: `nationalteam` 1 und 4
  (2 als Gegenprobe), `spielstaerke-manager` 1, 2 und 4.

## 3. Ausgeführte Prüfungen und Annahmen

- Checkout: `git rev-parse HEAD` = `e5946861b3ae6786924288ab82d603d7cc34424c`, `git status --porcelain` leer; `git diff --stat 80cda6a e5946861b3ae6786924288ab82d603d7cc34424c`: nur
  `docs/arbeitspakete.md` (+45/−22). PR-Stand per REST: offen, Head und Base wie oben.
- **Nur Codelesen:** kein Build, kein `tools/pruefen.sh` (reiner Doku-PR, laut Auftrag nicht nötig). Keine Testtabelle.
- Gelesen: Delta vollständig; Dateigrenzen aller Spuren per `grep` auf `Squad.Tactics`, `StrengthScreen`, `Migration`,
  `Statistics/`; Quelltext `Squad.Tactics.cs` (Konstruktor, `Shown*`, `Effective*`, `EffortStrengthBonus`), Aufrufer per
  `grep` in `src/`; `docs/dosbox-befunde.md` (#192), `docs/spielberechnung.md` §8; Issues #192, #193, #232–#234.
- Angenommen (unbeaufsichtigter Lauf, keine Rückfrage): „Automatik“ in A#L44 meint die ganze AUTOM.-Logik einschließlich
  `Shown*`; liest die Leitstelle es enger, ist R8P-01 durch Klarstellung desselben Satzes erledigt. Aufruferbeziehungen in
  em.exe nicht selbst disassembliert, Angaben aus den Belegdokumenten übernommen.
- Issue neu: #236 (R8N-01, `befund`, `P2`). R8P-01 bleibt unter #232; R8N-02/03 nur Bericht (P3).
- Akzeptierte `[APPROX]`: keine (kein Code).
