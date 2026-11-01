# Nachprüfung: PR #320 – Integration Runde 9 (finanzen-rest)

- PR: #320 · Base `3bf25094a274ec160d241a228071f3558c0dd562` · Head `215151e55998ebac1633b8565bbf3e4a095c792b`
- Nachprüfung von `docs/reviews/2026-11-01-integration-runde-9-finanzen-rest.md` (geprüfter Head `5729e69`); geprüft nur
  das Delta `5729e69..215151e` (1 Commit, 3 Dateien, +14/−12) und Befund F1 (#326).
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 01.11.2026
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer. Gebaut und getestet (nicht nur Codelesen).

## 1. Merge-Empfehlung

**Ja für Head `215151e`.** F1 ist behoben: Der Rechtsklick auf einen Spielstand lässt Namensfeld und Auswahl unverändert
und bleibt im Dialog; das fehlende Infofeld ist jetzt `[APPROX]` (#326) statt `[ORIGINAL]`, Code-Kommentar und
`docs/finanzen-rekonstruktion.md` sind berichtigt. Alle drei Läufe grün, CI (`build`) auf genau diesem Head grün.
Offen bleiben nur F2 (P3, #327) und ein veralteter Testkommentar (R9N-01, P3) – keine Merge-Bedingung.

## 2. Befunde

### F1 (P2, #326) – behoben

[`SaveLoadScreen.cs` Z. 213–231](https://github.com/kaaasi/eishockey-manager-redone/blob/215151e55998ebac1633b8565bbf3e4a095c792b/src/EishockeyManager.UI/Screens/SaveLoad/SaveLoadScreen.cs#L213-L231):
`TryRightClickEntry` meldet nur noch „Eintrag getroffen“, schreibt `_name`/`_selected` nicht. Gegen ndisasm nachgeprüft
(Datei 0x5E5ED–0x5E6A0): Taste 3 baut Verzeichnis + `"\"` (DGROUP 0x564D) + Eintrag in `[bp-0x7c]`, ruft 0x5B661
(`call` bei 0x5E656, Ziel 0x5E65F − 0x2FFE), setzt `[0x5A16] = 0` und springt zurück in die Schleife; `[bp+6]` wird nicht
beschrieben.

Gegenbeispiel erneut ausgeführt (temporärer Test, nicht committet): Spiel sichern als `RECHTS`, weiterspielen, Laden
öffnen (Feld `""`), Rechtsklick auf `RECHTS` → Feld `""`, Dialog offen; Enter → Dialog bleibt offen, Feld `""`,
Spielstand byte-gleich zum Stand vor dem Öffnen (nichts geladen). Vorher (Head `5729e69`) lud Enter `RECHTS`.

### R9N-01 (P3, neu) – Veralteter Kommentar über dem umgedrehten Test

[`SaveLoadScreenTests.cs` Z. 145](https://github.com/kaaasi/eishockey-manager-redone/blob/215151e55998ebac1633b8565bbf3e4a095c792b/tests/EishockeyManager.UI.Tests/SaveLoadScreenTests.cs#L145)
sagt noch „er übernimmt den Namen“; der Test darunter prüft das Gegenteil (der Kommentar im Testkörper ist richtig).
Behebung: eine Zeile, bei Gelegenheit. Keine Merge-Bedingung.

### Erwartet offen – bestätigt

- **F2 (P3, #327):** `GameState.Manager.cs` Z. 56 unverändert (`YouthTeam.StartTeams(Clubs[…].League)`, `[APPROX]`).
- **Infofeld 0x5B661 des Rechtsklicks:** nicht nachgebaut, im Code (Klassenkommentar und `TryRightClickEntry`) und in
  `docs/finanzen-rekonstruktion.md` als `[APPROX]` mit #326 gekennzeichnet.

## 3. Ausgeführte Prüfungen

```
git rev-parse HEAD                 # 215151e55998ebac1633b8565bbf3e4a095c792b, status leer
git diff --stat 5729e69..215151e   # 3 Dateien
tools/claude/fetch-reference.sh original && tools/claude/setup-workspace.sh ~/referenz/original/gamedata   # dotnet 10.0.402
tools/pruefen.sh                   # alle drei Läufe
ndisasm -b16 -e 0x5E5ED gamedata/em.exe
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1450 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1204 | 0 | 222 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1204 | 246 | 0 |

Commit 215151e55998ebac1633b8565bbf3e4a095c792b, SDK 10.0.402, Ergebnis: bestanden. CI-Check-Runs des Heads: `build` success.

**Gegenproben** (Mutation in `SaveLoadScreen.cs`, gezielter Lauf, danach zurückgesetzt, Baum sauber):

| Mutation | Test | Ergebnis |
|---|---|---|
| Rechtsklick übernimmt wieder Namen und Auswahl (Stand `5729e69`) | `RightClickOnEntry_KeepsNameAndDoesNotLeaveOrLoad`, Gegenbeispiel | rot (2) |
| Rechtsklick auf Eintrag bricht ab | dieselben | rot (2) |

**Akzeptierte `[APPROX]`:** Infofeld 0x5B661 fehlt (#326); übrige wie im Review zu `5729e69`.
**Annahmen:** Deutung von 0x5B661 als Spielstand-Info aus dem Vorreview übernommen; für F1 genügt, dass das Namensfeld
im Rechtsklick-Zweig unberührt bleibt (selbst nachgeprüft).
