# Review #893 – Spur `finanzuebersicht` (Erstreview)

Geprüft: Head `7e0c7ed95eb0b0f815e631f6563244e2fbbf0748` gegen Base `40f67fcabb0a7b0649f3771d9f00a4ce8d186bea`; Checkout
nachgewiesen (`git rev-parse HEAD` = Head, `git status --porcelain` leer). Reviewer: Claude Opus 5.5, geplanter Lauf, eigene
Sitzung ohne Implementierungskontext (PR aus Sitzung `…01GhACvTy…`, Review aus `…01GhACvW…`).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die Lesart der Original-Übersicht ist an em.exe nachvollzogen und trägt N5 = C: Die rechte
Spalte liest kein Kategoriefeld, sondern nur Konto, Darlehen, Monatszins und Depot; `GameState.Book` ändert außer
`manager.Money` nur das Finanzbuch. Damit erscheinen die sechs Posten in Original und Remake nur im Kontostand. Der Prüftest
trennt die Posten einzeln (Rückschaltprobe je Kategorie, siehe 3.). Keine P0–P2.

## 2. Befunde

**P3-1 (Testlücke, nicht blockierend) – A192/A716 nur über `Book`.**
[`FinanzuebersichtBuchungTests.cs` Z. 71–74, 98–101](https://github.com/kaaasi/eishockey-manager-redone/blob/7e0c7ed95eb0b0f815e631f6563244e2fbbf0748/tests/EishockeyManager.UI.Tests/FinanzuebersichtBuchungTests.cs#L71-L101):
Kategorie und Betrag stehen im Test selbst, die Aussage „Betrag steht in der Kategorie“ ist für diese zwei Fälle tautologisch.
Beleg: Mutation der echten Stellen – `StadiumStatistics.cs` `PenalizeShortHanded` Strafen → Transfers und
`Transfer.SeasonEnd.cs` `Leave` Transfers → Unterhalt – lässt den Test **6/6 grün**. Für die Übersicht selbst ist das ohne
Folgen, da `Book` keine Nebenwirkung hat ([`GameState.Manager.cs` Z. 73–77](https://github.com/kaaasi/eishockey-manager-redone/blob/7e0c7ed95eb0b0f815e631f6563244e2fbbf0748/src/EishockeyManager.Core/GameState.Manager.cs#L73-L77)),
und die echten Pfade sind anderswo abgedeckt: A716-Mutation → Core-Tests **6 rot**
(`TransferVertraegeKomplettTests.SeasonEnd_PlayerLeaves_WithCompensation` ×3, `…SeasonEnd_WithoutScreen_…`,
`TagestaktSommerpauseReviewTests.File44_…`, `KaderStartTeil4Tests.ManagerLoan_ExpiringLoanSlot_…`); A192 prüft
`HeimspielStatistikTests.ShortHanded_AtHome_PaysPenalty_…` (Z. 104, `Book.Current(Strafen)`; nur Codelesen, Mutation nicht gegen
Core gelaufen). Vorschlag (optional): im Test bei A192/A716 auf diese Tests verweisen oder A716 über `CancelRenewal` auslösen.

**P3-2 (bestätigt, erwartet offen) – Zeilen 3/4 und 6/7 der Übersicht.** Nachgelesen (`ndisasm -b16`): Schleife Datei
0x57F49–0x57FBC; Zeilen 3/6 addieren `[bx-0x680a]` (= DGROUP 0x97F6 + 4·i, vorzeichenbehaftet), Zeilen 4/7 rufen 0x57C64(i);
Zeilen ≤ 4 über i ≠ ich und i < `[0x386]`, Zeilen > 4 nur i = 4 (Bank). 0x57C64: Bank 4 % bei Darlehen < 0, sonst 1 % (/100);
Mitmanager 25/1000 = 2,5 %. Remake ([`FinanceScreen.cs` Z. 811–815](https://github.com/kaaasi/eishockey-manager-redone/blob/7e0c7ed95eb0b0f815e631f6563244e2fbbf0748/src/EishockeyManager.UI/Screens/Manager/FinanceScreen.cs#L811-L815)):
Forderungen / −Schulden bzw. Sparbuch / −Kredit statt Saldo / Monatszins. Befund wie im PR beschrieben → Leitstelle, Runde 22.

**Besonders geprüft:**

- **Lesart 0x57ECF / N5 = C – trägt.** Sprungtabelle Datei 0x581A6 (CS-Basis 0x57480, Index = Zeile − 1, Zeile 0 fällt über
  `cmp bx,0xd`/`ja` auf die Überschrift): Zeile 1 → 0x57F2D (`[bx-0x6816]`, 0x1AF·Manager = Konto 0x97EA); 3, 4, 6, 7 → 0x57F49
  (Darlehen/Zins wie oben); 10 → 0x57FC1 (Σ `[bx-0x2dec]` = 0xD214 Kaufpreise); 12 → 0x57FF9 (Σ Stück 0xD218 · Kurs 0xD13A);
  14 → 0x58058 (Depotwert − Σ Kaufpreise); 2, 5, 8, 9, 11, 13 → 0x58099 (nur Text aus 0xDF0). Kein weiterer Speicherzugriff, also
  keine Kategorie und keine Buchungssumme. Remake `DrawSummary` liest ebenfalls kein `Book` (Z. 805–824, Codesuche `Book` in
  `DrawSummary`: kein Treffer).
- **A703 bleibt „Transfers“ – vertretbar.** Das Original kennt keine Kategorie; der Zuschuss ist echtes Geld von außen (kein
  neutraler Umbuchungsfall nach AGENTS.md §4) und hängt am Kauf. Das bestehende `[APPROX]` an `AcceptSponsorSubsidy`
  (`Transfer.ComputerClub.cs` Z. 94) deckt die Wahl ab; `FinanceCategory` hat keine neutrale Kategorie, eine neue wäre ohne Beleg.
- **Dateigrenzen – eingehalten.** Diff: `docs/arbeitspakete.md` (nur eigener Abschnitt), `FinanceScreen.cs` (nur Kommentar),
  neue Testdatei. `Core/FinanceMonthly.cs`, `Core/Finance.cs` und gemeinsame Dateien unberührt.

## 3. Ausgeführte Prüfungen

SDK 10.0.402 (Release `claude-werkzeuge`), Originaldaten aus `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich).
`tools/pruefen.sh` (alle drei Läufe, Erstreview):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3379 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2428 | 0 | 575 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2429 | 950 | 0 |

Ergebnis: bestanden, Commit `7e0c7ed…`.

**Rückschaltproben** (temporärer Worktree, `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests -c Release
--filter FinanzuebersichtBuchungTests`; Mutation in `FinanceScreen.DrawSummary`, Zeile „Depot-Saldo“ `Value(233, value - cost + X)`):

| X | rot | grün |
|---|---|---|
| – (unverändert) | – | 6/6 |
| `finance.Book.CurrentTotal` (Probe des PR) | A192, A207, A511, A513, A703, A716 | – |
| `Book.Current(Strafen)` | A192 | 5 |
| `Book.Current(Versicherung)` | A207 | 5 |
| `Book.Current(Bau)` | A511 | 5 |
| `Book.Current(Talentsuche)` | A513 | 5 |
| `Book.Current(Transfers)` | A703, A716 | 4 |
| echte Stelle A192 Strafen → Transfers | – | 6/6 (P3-1) |
| echte Stelle A716 Transfers → Unterhalt | – | 6/6; Core-Tests 6 rot (P3-1) |

Nach jeder Probe zurückgesetzt (`git checkout -- .`, Worktree sauber).

Disassembly: `ndisasm -b16` der Bereiche Datei 0x57ECF–0x581C2 und 0x57C64–0x57CFB, Sprungtabelle 0x581A6 per Python gelesen.

**Akzeptierte `[APPROX]`:** keine neuen; bestehendes `[APPROX]` A703 (Kategorie Transfers) bestätigt.
**Annahmen:** Lage/Farben der Übersicht nicht nachgemessen (G8, außerhalb des Pakets); kein Videobild mit Darlehen geprüft.
