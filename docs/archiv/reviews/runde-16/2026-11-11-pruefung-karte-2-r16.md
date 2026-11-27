# Review PR #781 – Spur pruefung-karte Teil 2 (Runde 16)

Reviewer: Claude Opus 5.5 (geplanter Lauf, eigene Sitzung ohne Implementierungskontext) · Base `8715f8e` · Head `1ead299`
(`git rev-parse HEAD` = `1ead2990ae99a87a0994b57d45dd7c9cfa1e1bc7`, `git status --porcelain` leer) · SDK 10.0.402 · Originaldaten
aus `kaaasi/eishockey-manager-referenz` (`ls-remote` erreichbar).

## 1. Merge-Empfehlung

**Ja** für Head `1ead299`. Alle drei Läufe grün, alle Gegenbeispiele und Rückmutationen bestätigt, Inventur für Runde 16
vollständig (17 neue/umgeschriebene `[APPROX]`-Zeilen in A408–A417). Zwei P3 nur in der Doku, nicht mergehindernd.

## 2. Befunde

**R781-P3-1 – Funktionskarte am Head nicht reproduzierbar (nur Zählzeile).** [`docs/re-abdeckung.md:152`](https://github.com/kaaasi/eishockey-manager-redone/blob/1ead2990ae99a87a0994b57d45dd7c9cfa1e1bc7/docs/re-abdeckung.md#L152)
nennt 6824 Zuordnungen, das Skript liefert am Head 6826. Probe in sauberem Worktree am Head:
`funktionskarte.py em.exe --csv f.csv` → „6826 Zuordnungen“; `--write` → Diff genau eine Zeile (6824 → 6826), kein Statuswechsel.
Ursache: `docs/analyse/abgleich-original.md` wurde nach dem Erzeugen noch ergänzt (mit dessen Stand `0803e91` → 6824; neu u. a.
0x26F20, 0x54CEE, 0x5569A, 0x56512). Die Aussage „zweiter `--write` ohne Diff“ gilt also nicht für den Head; ebenso
„6598 → 6824“ in [`inventur.md:310`](https://github.com/kaaasi/eishockey-manager-redone/blob/1ead2990ae99a87a0994b57d45dd7c9cfa1e1bc7/docs/analyse/inventur.md#L310). Vorschlag: `--write` erneut, Zahl nachziehen.

**R781-P3-2 – A404 Textfehler.** [`inventur.md:605`](https://github.com/kaaasi/eishockey-manager-redone/blob/1ead2990ae99a87a0994b57d45dd7c9cfa1e1bc7/docs/analyse/inventur.md#L605):
„**Stand Runde 16 (PR #765; Spur `pool`, #743):, Teil 3):** …“ – doppelte, unvollständige Klammer (Beleg: Zeile lesen). Inhalt
stimmt mit dem Code (Ersatzregel entfallen, Rest A411/A416).

Keine P0–P2.

## 3. Ausgeführte Prüfungen

**Gegenbeispiele (Mutation jeweils in temporärem Worktree am Head):**

| ID | Probe | Ergebnis |
|---|---|---|
| #780 | `OriginalExport.Squads.cs:123` `if (missing.Count > 0)` → `if (false)` | `WorldOnly_NewGame_Export_SquadOfRecord0OnFile01Slots` rot (`Collections differ`); Head grün |
| #768 | `SeasonEndSummaryScreen.cs` auf Base zurück | `SeasonEndEvents_InfoBar_ShowsMoneyBeforePremium` rot (`NotEqual … are equal`) |
| #768 | Rückmutation: `Money - result.Premium` → `Money` | rot; Head grün |
| #778 | `Player.Contract.cs:197` `ContractYears == 0` → `<= 1` (zwei Jahre Laufzeit gehen) | `Schema22Fixture_SquadLoss_OnlyExpiringContracts` rot („Saisonwechsel 1: SPIELER 41 ging …“) |
| #776 | `Player.Contract.cs` Rückkehrer: `if (ContractYears > 0) continue;` → `if (true) continue;` | `LoanBetweenManagers_LastLoanYear_ReturnsToOriginWithOpenRenewal` rot (`PendingRenewals`) |

- **Inventur-Gegenprobe:** `grep -rc '\[APPROX\]' src/ | awk …` → `195 573` (wie PR). Inhaltlicher Abgleich `31597e4..HEAD`
  (Markerzeilen ohne Zeilennummer): 17 neu/umgeschrieben, 10 entfallen. Zuordnung: WorldCupLoop 116/182 → A408; Managed 504 →
  A409; Preparation 21/24 → A410; Export.Squads 129 → A411, 102 → A416; Migration.AbgleichRest 8 → A413; Migration.Pool 9/12 →
  A416; Transfer.Pool 17 → A414, 351/425 → A415, 426/484 → A416; FinanceStocks 183 + FinanceScreen 523 → A417. Vollständig;
  Stand und Thema je Zeile am Code nachgelesen (u. a. A414 0x46FCA, A417 Divide error 0x01F74, A408 Heilung aller Spieler).
- **Punkt 2 (§5 Nr. 7):** #778 und #776 als „kein Fehler“ durch Wächtertests mit je einer roten Rückmutation belegt (Tabelle).
  #776 prüft die Rückkehr mit Leih-Laufzeit 0, offene Verhandlung beim Stammverein und Poolplatz „vergeben“.
- **Punkt 3 (`MainMenuScreen.cs`):** Diff +7/−1: Eigenschaft `InfoMoney` (Z. 123) und Fallback in `DrawInfoBar` (Z. 349).
  Nur `SeasonEndSummaryScreen.Draw` setzt sie, `finally` setzt zurück; kein Spielzustand geändert (`Draw` ruft weder
  `OnMainMenu` noch `Continue`, Managerwechsel bleibt an der Eingabe Z. 82). Minimal und im PR begründet.
- **Erwartet offen, bestätigt:** #773 offen (Issue offen, `abgleich-original.md` „offen“, DOSBox-Stand nach Spieltag 1 nötig);
  #730 offen, zurückgestellt R16-E2 = C (A409, `[APPROX]` `MatchEngine.Managed.cs:504`). Der PR schließt beide nicht.
- `tools/pruefen.sh` (alle drei Läufe):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2961 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2174 | 0 | 502 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2175 | 786 | 0 |

Commit `1ead2990…`, SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:** keine neuen im Code dieses PR; A408–A417 als Einträge der Runde 16 korrekt eingeordnet.
**Annahmen (nicht geprüft):** #768 gegen das Videobild `file46_2` nur über den Kommentar (Test vergleicht mit Hauptmenü „Konto − Prämie“).
