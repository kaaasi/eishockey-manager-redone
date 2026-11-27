# Nachprüfung: Integration Runde 8 (#250)

- Nachprüfung von `docs/reviews/2026-10-31-integration-runde-8.md` (Branch `review/integration-runde-8`, PR #252).
- PR: #250 · Base `421d96f9c3bee5ce514fc3f4859a7cb272e1d8af` · Head `6ca0621810f910fd7ce12da171b09009fad21977`
- Geprüft: nur Delta `8b47184..6ca0621` (ein Commit, 5 Dateien) und die genannten Befunde.
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf, unbeaufsichtigt), 31.10.2026.
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; `8b47184` ist Vorfahre von `6ca0621`.
  Originaldaten über `tools/claude/fetch-reference.sh original`, SDK 10.0.402 über `tools/claude/setup-workspace.sh`.
- **Annahmen:** „Erwartet offen“ wird gegen die vollständige P3-Liste des Vorreviews geprüft, nicht nur gegen die im
  Auftrag aufgezählten IDs. Prioritäten wie im Vorreview (Wirkung auf Ergebnisse und Geld).

## 1. Merge-Empfehlung

**PR #250: Ja für genau den Head `6ca0621`.**

- R8-D01 ist behoben, mit Disassembly belegt und durch einen Regressionstest abgesichert. Die Gegenprobe macht ihn rot.
- Das Delta enthält nur Fix, Doku, Testumbenennung und Plan.
- Alle drei Läufe von `tools/pruefen.sh` sind am exakten Head grün.
- Die neuen Befunde sind P3. Sie hindern den Merge nicht.
- Issue #253 ist noch offen. Es kann mit dem Merge geschlossen werden.

## 2. Befunde

### R8-D01 (P2, #253) – behoben

[`PlayerDataWindow.cs` Z. 5–37](https://github.com/kaaasi/eishockey-manager-redone/blob/6ca0621810f910fd7ce12da171b09009fad21977/src/EishockeyManager.UI/Screens/Team/PlayerDataWindow.cs#L5-L37)

Disassembly erneut geprüft (`ndisasm -b16 -e 0x41AC -o 0 em.exe`):

```
7D6  mov al,[0xa70e] ; cbw ; imul 0x1af ; mov al,[bx-0x6991]   ; Manager +0x14 = Verein
7E5  les bx,[bp-4] ; mov dl,[es:bx+0xf] ; imul 0x21 ; les bx,[0x8253] ; add bx,ax
7FD  cmp al,[es:bx+0x1d] ; jz 0x820                              ; gleicher Verein → nichts ausblenden
803  cmp byte [bp-9],4 ; jz 0x816                                ; Feld 4 (Gehalt) → immer 0
809  cmp byte [bp-9],1 ; jnz 0x820
80F  cmp byte [0xed8],3 ; jnl 0x820                              ; Feld 1 (Marktwert) → 0, wenn 0xED8 < 3
816  mov word [bp-0xe],0 ; mov word [bp-0x10],0
```

Das passt zum neuen Code (`ReferenceEquals(playerClub, ownClub)`), zu `ShowSalary`/`ShowMarketValue` und zur
berichtigten Doku in `reverse-engineering.md` Z. 442–443.

Gegenbeispiele erneut ausgeführt, am Head grün (`PlayerDataContextTests`, 4 Fälle):

- (a) fremder Verein derselben Liga, Level 3 und 1: `ShowSalary == false`.
- (b) Transfermarkt (`playerClub: null`): `ShowSalary == false`.
- Eigener Verein: `ShowSalary == true`.

Gegenprobe in einem temporären Worktree:

- Mutation zurück auf `playerClub is null || CupClass.Of(playerClub) == CupClass.Of(ownClub)`: **3 von 4 rot**
  (beide `OtherClubSameLeague_HidesSalary`, `MarketPlayer_HidesSalary`).
- Mutation `same = false`: `OwnClub_ShowsSalaryAndMarketValue` rot.

### R8N-01 (P3) – „Transfermarkt-Spieler gehören nie zum Verein des Managers“ ist als `[ORIGINAL]` nicht belegt

[`PlayerDataWindow.cs` Z. 9](https://github.com/kaaasi/eishockey-manager-redone/blob/6ca0621810f910fd7ce12da171b09009fad21977/src/EishockeyManager.UI/Screens/Team/PlayerDataWindow.cs#L9),
[Z. 34](https://github.com/kaaasi/eishockey-manager-redone/blob/6ca0621810f910fd7ce12da171b09009fad21977/src/EishockeyManager.UI/Screens/Team/PlayerDataWindow.cs#L34)

- Das Original vergleicht auch bei Marktspielern nur Pool `+0x1D` mit dem Verein des Managers. Es prüft nicht den
  Besitzer `+0x1A` (4 = Markt).
- Laut `docs/formate/spielstand-original.md` Z. 290 steht `+0x1D` bei einem Marktspieler auf einem echten Vereinsindex.
  Beispiel: DAFFNER, FILE05, `+0x1D` = 3, nach dem Kauf 3 → 2.
- Zeigt `+0x1D` auf den Verein des Managers, etwa bei einem früheren eigenen Spieler, blendet das Original nichts aus. Das
  Remake blendet immer aus.
- Wirkung derzeit keine, denn `Create` hat noch keinen Aufrufer.
- Vorschlag: Den Satz als `[APPROX]` kennzeichnen, bis belegt ist, was `+0x1D` für Marktspieler enthält. Das klären,
  wenn `TransferScreen` auf `Create` umgestellt wird (steht schon im Plan).

### R8N-02 (P3) – Sieben P3-Befunde des Vorreviews fehlen in den Restlücken

[`arbeitspakete.md` Z. 245–251](https://github.com/kaaasi/eishockey-manager-redone/blob/6ca0621810f910fd7ce12da171b09009fad21977/docs/arbeitspakete.md#L245-L251)

Der neue Absatz führt auf: R8-A01, A02, B01–B04, C01, C02, F46-01 und W51-01. Diese P3 des Vorreviews stehen nirgends
in `docs/`:

- R8-A03: Mutationen der Teamwerte fallen nur in Abdrucktests auf; `Math.Clamp` ohne `[APPROX]`.
- R8-D02: Grenzwert 10 der Bundestrainer-Warnung ist ungetestet.
- R8-D03: Index 9/10 der Spielmodus-Optionen in `reverse-engineering.md` Z. 499.
- R8-D04: „Minuten“ statt Spiele bei der Sperre. Das ist ein akzeptiertes `[APPROX]`.
- R8-D05: Dateigrenzen ohne Begründung nach Regel 2.5.
- W51-02 und W51-03 (#251).

Vorschlag: Die fehlenden IDs in den Absatz aufnehmen. Wenigstens A03, D02 und D03 gehören dazu, denn sie betreffen
Code und Doku.

### Erwartet offen (bestätigt)

- Bestätigt offen und als Restlücke eingetragen: R8-A01, R8-A02, R8-C01, R8-C02, R8-B01, R8-B02, R8-B03, R8-B04, F46-01
  und W51-01. Das Delta berührt keinen davon.
- Weiter offen, aber nicht eingetragen: R8-A03, R8-D02, R8-D03, R8-D04, R8-D05, W51-02 und W51-03 (siehe R8N-02).
- Unverändert und schon im Plan: Die Aufrufer nutzen weiter `Show(player, price)` mit `PlayerDataContext.Unknown`
  (`SameClub = true`). Deshalb zeigt das Remake das Gehalt fremder Spieler und von Marktspielern, bis die Aufrufe
  umgestellt sind (Plan, „Für die Leitstelle nach der Integration“).

## 3. Ausgeführte Prüfungen

```
git diff --stat 8b47184 6ca0621          # 5 Dateien: arbeitspakete.md, reverse-engineering.md, PlayerDataWindow.cs,
                                         # PlayerDataContextTests.cs (neu), PlayerInfoWindowTests.cs (nur Umbenennung)
ndisasm -b16 -e 0x41AC -o 0 gamedata/em.exe   # Versatz 0x7C4–0x830
dotnet test tests/EishockeyManager.UI.Tests --filter "PlayerDataContextTests|PlayerInfoWindowTests"   # 57/57 am Head
# Gegenproben im temporären Worktree (siehe R8-D01), danach verworfen
tools/pruefen.sh                         # alle drei Läufe am exakten Head
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1269 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1054 | 0 | 191 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1054 | 215 | 0 |

Commit `6ca0621810f910fd7ce12da171b09009fad21977`, SDK 10.0.402, Ergebnis: bestanden.

Delta-Prüfung:

- Im Delta stehen nur Fix (`PlayerDataWindow.cs`), Doku (`reverse-engineering.md`), der neue Test, die
  Testumbenennung (`HiddenForForeignClass` → `HiddenForForeignClub`, Daten unverändert) und der Plan (`arbeitspakete.md`).
- `SameClass` kommt sonst nirgends vor. Die Umbenennung bricht also keinen Aufrufer.

**Akzeptierte `[APPROX]`:** keine neuen. Das `[ORIGINAL]` für den Vereinsvergleich ist jetzt belegt. Nur der Satz zu den
Marktspielern ist nicht belegt (R8N-01).
