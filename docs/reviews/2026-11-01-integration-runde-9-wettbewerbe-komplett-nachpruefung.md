# Nachprüfung: PR #322 – Integration Runde 9, wettbewerbe-komplett

- PR: #322 · Base `215151e55998ebac1633b8565bbf3e4a095c792b` (gestapelt auf #320) · Head `6504333bd2185bbe1369936197d845ca27cc42e5`
- Nachprüfung von `docs/reviews/2026-11-01-integration-runde-9-wettbewerbe-komplett.md` (geprüfter Head `3f4fcea`); geprüft
  nur das Delta `3f4fcea..6504333` ohne den Merge von #320 (`983d969`), also `983d969..6504333` (1 Commit, 5 Dateien),
  und die Befunde R9W-D01 (#328), R9W-D03 (#330).
- Merge `983d969` (Eltern `3f4fcea`, `215151e`): `git diff 3f4fcea 983d969` ist identisch mit `git diff 5729e69 215151e`
  – keine Konfliktauflösung, nur der Stand von #320 (siehe eigene Nachprüfung).
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 01.11.2026
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer. Gebaut und getestet (nicht nur Codelesen).

## 1. Merge-Empfehlung

**Ja für Head `6504333`** (nach bzw. zusammen mit #320). R9W-D01 ist behoben und gegen em.exe nachgeprüft, R9W-D03 ist
behoben, die geänderte Mehrsaisonprobe ist mit dem Original vereinbar. Alle drei Läufe grün, CI (`build`) auf genau diesem
Head grün. Offen bleibt nur R9W-D02 (P3, #329) – keine Merge-Bedingung.

## 2. Befunde

### R9W-D01 (P2, #328) – behoben

[`SeasonFlow.cs` Z. 393](https://github.com/kaaasi/eishockey-manager-redone/blob/6504333bd2185bbe1369936197d845ca27cc42e5/src/EishockeyManager.Core/SeasonFlow.cs#L393)
ruft nach `Season++` [`ClearNationalForm`](https://github.com/kaaasi/eishockey-manager-redone/blob/6504333bd2185bbe1369936197d845ca27cc42e5/src/EishockeyManager.Core/CupsNational.TeamInfo.cs#L190-L195)
(nur `_nationalForm`). Kommentar jetzt `[ORIGINAL]` mit Fundstelle, `docs/reverse-engineering.md` Z. 781 berichtigt.
`Season++` gibt es nur dort, andere Saisonwechselwege fehlen nicht.

- em.exe nachgeprüft: Datei 0x55EBA–0x55EE0 `push ds; push 0x5503; push ds; push 0x825E + 0x50·[bp−1] + 9·[bp−0xA];
  call 0:0x1B0B`, Schleife `[bp−0xA]` < 2; DGROUP 0x5503 (Datei 0x1C2D3) = `"--------\0"`. Die Schleife beginnt bei
  0x55E39 (`[bp−1] = 0`, `[bp−0xA] = 0`, Tabellenfelder 0x825A …).
- Gegenbeispiel erneut ausgeführt (temporärer Test): `GameModeTests.Create(7, true, true, 2, 30)`, bis Saisonende
  (`SeasonOver`, 1993) → `NationalForm(56)` = `0,0,0,2,3,3,1,1`; `StartNextSeason()` → `0,0,0,0,0,0,0,0`, alle 12 Länder
  leer, auch nach Sichern/Laden. Alle acht Rekordarten und Kopf-an-Kopf (Platz 0) aller Länder unverändert.

### R9W-D03 (P3, #330) – behoben

[`SaveGameTests.cs` Z. 36](https://github.com/kaaasi/eishockey-manager-redone/blob/6504333bd2185bbe1369936197d845ca27cc42e5/tests/EishockeyManager.Core.Tests/SaveGameTests.cs#L36):
„Runde 9: 18 …“ statt „Vorläufig 18“; Fingerabdruck unverändert.

### Besonders geprüft: `MultiSeason_NationalData_GrowsPerWorldCup` – „fünf Zeichen je Saison“

[`GegnerFensterTests.cs` Z. 107–142](https://github.com/kaaasi/eishockey-manager-redone/blob/6504333bd2185bbe1369936197d845ca27cc42e5/tests/EishockeyManager.Core.Tests/GegnerFensterTests.cs#L107-L142):
Die Prüfung steht am Saisonende (nach der WM, vor `StartNextSeason`). Erwartung `globe ? 5 : 0` statt
`min(8, 5 · WM-Anzahl)` ist mit dem Original vereinbar:

- Geleert wird zwischen zwei WMs (0x55EBA, s. o.).
- Geschrieben wird die Bilanz nur bei WM-Gruppenspielen: In 0x50283 springt `cmp [0xD0C2],7 / cmp [0xD0C2],8` sonst
  direkt ans Funktionsende (Datei 0x5030D–0x5031B → 0x50B41); alle vier Aufrufe von 0x4F4F4 (0x5079A, 0x5092D, 0x50B21,
  0x50B3B) liegen dahinter. Endrunde (Code 10) und Freundschaftsspiele (Code 0xD) schreiben also keine Bilanz – wie
  `EnterNationalResult` (Rückkehr bei `kind != WorldCupGroup`).
- Zwei Gruppen zu sechs Ländern → fünf Gruppenspiele je Land und WM.
- Die Probe wird rot, wenn der Aufruf `ClearNationalForm()` fehlt (4 der 5 Fälle; Modus 2 ohne WM bleibt naturgemäß grün).

**Annahme:** dass 0x55E39–0x55EF8 zwischen dem Ende der WM und dem ersten Spiel der Folgesaison läuft (Aufrufer nicht
selbst verfolgt), stützt sich auf das Vorreview und das Videobild `folge06_02238` (2. Saison, 1. Spieltag, Bilanz
`--------` + ein Zeichen).

### Erwartet offen – bestätigt

- **R9W-D02 (P3, #329):** `Season.cs`/Migration im Delta unverändert; `StartOrder` gilt weiter rückwirkend für geladene
  Stände von Schema ≤ 17 mitten in Saison ≥ 2.

## 3. Ausgeführte Prüfungen

```
git rev-parse HEAD                      # 6504333bd2185bbe1369936197d845ca27cc42e5, status leer
git merge-base --is-ancestor 215151e 6504333
diff <(git diff 5729e69 215151e) <(git diff 3f4fcea 983d969)   # leer
git diff 983d969..6504333               # 5 Dateien
tools/pruefen.sh                        # alle drei Läufe
ndisasm -b16 -e 0x55E39 / 0x55EBA / -o 0x50283 -e 0x50283 gamedata/em.exe
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1478 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1228 | 0 | 225 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1228 | 250 | 0 |

Commit 6504333bd2185bbe1369936197d845ca27cc42e5, SDK 10.0.402, Ergebnis: bestanden. CI-Check-Runs des Heads: `build` success.

**Gegenproben** (Mutation in temporärer Kopie, Lauf `GegnerFensterTests` + Gegenbeispiel, danach zurückgesetzt):

| Mutation | Ergebnis |
|---|---|
| Aufruf `ClearNationalForm()` in `StartNextSeason` entfernt | rot: `SeasonChange_Clears…`, 4 × `MultiSeason_…`, Gegenbeispiel |
| `ClearNationalForm` leert auch Kopf-an-Kopf | rot: `SeasonChange_Clears…`, Gegenbeispiel |
| `ClearNationalForm` leert auch die Rekorde | rot: `SeasonChange_Clears…`, Gegenbeispiel |

**Akzeptierte `[APPROX]`:** unverändert wie im Review zu `3f4fcea` (Bilanz nur der Länder, Vereinsspiele nicht
eingetragen, Kopf-an-Kopf nur für Deutschland, Migration 17 → 18 leer, #166).
**Annahmen:** Reihenfolge Saisonwechsel/WM (s. o.); im Übrigen wie im Vorreview.
