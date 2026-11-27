# Review PR #767 – Spur pruefung-karte Teil 1 (Runde 16: Kurs 0, FILE13→14, R701-06)

Geprüft: Head `18e08242f5df3a6fb39a5b89c6cabac0444e1628` (`git rev-parse` am PR-Head), Base `d6fa442c6552213e9e810c9505117eb6ab457680`
(= Merge-Basis mit `main`), Arbeitsbaum sauber. Reviewer: Claude Code (Opus 5.5), geplanter Lauf, eigene Sitzung ohne
Implementierungskontext. Mutationen nur in einem temporären Worktree, danach verworfen.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün; die drei Gegenbeispiele (#757, #734, R701-06) sind
nachgestellt und am Head behoben bzw. erklärt, jede der vier neuen bzw. verschärften Prüfungen wird bei Rückmutation rot. Die
em.exe-Fundstellen zu #757 und R701-06 stimmen (Abschnitt 3). Die Eingriffsliste FILE13→14 trifft genau die genannten 49 Zeilen
(Konto, Automatik, sieben Teamwerte, 40 verdeckte Werte KREFELD), die Belastung bleibt verglichen. Der Beleg in #703 trägt das
Schließen. Einziger Befund P3 (Doc-Kommentar verrutscht) blockiert nicht.

## 2. Befunde

**R767-01 (P3) – Doc-Kommentar von `BuyStock` hängt jetzt an `MaxStockPurchase`.**
[FinanceStocks.cs:181–199](https://github.com/kaaasi/eishockey-manager-redone/blob/18e08242f5df3a6fb39a5b89c6cabac0444e1628/src/EishockeyManager.Core/FinanceStocks.cs#L181-L199):
Die neue Methode ist zwischen den bestehenden `<summary>` von `BuyStock` (Zeile 181–186, „Kauft Aktien … Liefert die Zahl der
gekauften Stücke“) und `BuyStock` eingefügt. `MaxStockPurchase` trägt damit zwei `<summary>`-Blöcke (der erste beschreibt den
Kauf), `BuyStock` (Zeile 199) keinen mehr. Beleg: `sed -n 181,199p src/EishockeyManager.Core/FinanceStocks.cs`. Der Build
warnt nicht (keine Doc-Prüfung). Vorschlag: den alten Block hinter `MaxStockPurchase` vor `BuyStock` stellen.

**Hinweise (kein Befund):**

- FILE13→14: Mit `TeamValues.*` fällt auch `DayForm` KREFELD unter den Eingriff (vorher Teil des Pflichtmusters `*Form`). Das ist
  folgerichtig (gleiche Ursache), verdeckt aber einen weiteren Zufallswert des Managervereins; in `abgleich-original.md` ist die
  Verdeckung einer Finanzmutation in diesem Paar bereits genannt. Das Konto bleibt über `AbgleichFixMoneyTests.File13_MoneyDeviation_
  ComesFromShortHandedLineup` (grün im Lauf mit Daten) inhaltlich geprüft.
- #703 R701-05: Das Review #702 hielt die Reflexion für zulässig, schlug aber auf Dauer `internal`-Testhaken vor
  (`_nationalTurn` per `SetValue` umgeht `BeginNationalCoachTurn`). „Verworfen“ im Issue lässt diesen Nachteil fallen; für das
  Schließen ausreichend, ggf. in einer späteren Runde als Pflege aufnehmen.
- R701-06 „jede Saat“: nur mit der Importvorgabe des Tests geprüft (Assertion `Equal` am Head grün); andere Saaten nicht
  nachgerechnet (Annahme).

**Erwartet offen, bestätigt:** #768 (offen, P3 Konto in der Infoleiste „Saisonende – Ereignisse“), #730 (offen; FILE13→14 am Head
noch 5 Abweichungen: Belastung EAKIN, J.MEYER, KUHNEKATH, THOMSON und Pool-Mittel).

## 3. Ausgeführte Prüfungen

SDK 10.0.402 (`tools/claude/setup-workspace.sh`), Originaldaten aus `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich).

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2925 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2153 | 0 | 494 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2154 | 771 | 0 |

`tools/pruefen.sh` (ohne Option), Commit `18e0824`, Ergebnis: bestanden.

**Gegenbeispiele erneut ausgeführt**

- **#757:** Base-Fassung von `BuyStock`-Zeile und `FinanceScreen.MaxLeft` im Worktree → `StockPriceZeroTests.BuyStock_AtPriceZero_…`
  und `FinanceStockPriceZeroScreenTests.BuySide_AtPriceZero_…` rot mit `System.DivideByZeroException`; am Head grün.
- **#734:** `emtool abgleich FILE13.HOK FILE14.HOK --gamedata gamedata` (Saat 1993): mit Base-`HokCompareClasses.cs`
  **10 Abweichungen** (Konto 183.787/354.934, `_autoLineup`, AttackForm, DefenseForm, DefenseTechnique, 4× Belastung, Pool-Mittel),
  Eingriff 0; am Head **5** (nur Belastung/Pool, #730), Eingriff 49. Mit `--alle` gruppiert: die 49 Eingriffszeilen sind genau
  `Managers[0].Money`, `Clubs[6 KREFELDER EV]._autoLineup`, sieben `TeamValues.*`, je 20 `Players[*].HiddenSkill1/2`; keine davon
  mit Markierung ABWEICHUNG außerhalb dieser Felder, kein anderer Verein, keine `.Strain`-Zeile. Saaten 2/3/77/4711 ebenfalls je
  49 Eingriffe (17/4/15/3 Abweichungen, alle aus den optionalen Mustern; `Pair_OtherSeed_SameExpectedList` grün).
- **R701-06:** Grundeinnahmen +15.000 DM (`GameState.Manager.cs:227`, `MonthlyBaseIncome(manager) + 15_000`) →
  `File44_SeasonEndScreen_…` **rot** (Expected 17523876, Actual 17538876); mit der Base-Fassung des Tests (Spanne `InRange`) **grün**.

**Rückmutation je neuem Test**

| Test | Mutation | Ergebnis |
|---|---|---|
| `StockPriceZeroTests.MaxStockPurchase_…` | Wächter `price == 0 ? 0 :` entfernt | rot (mit `BuyStock_…`) |
| `StockPriceZeroTests.BuyStock_AtPriceZero_…` | Base-Zeile `Math.Max(0, Money) / stock.Price` | rot (DivideByZero) |
| `StockPriceZeroTests.SellStock_AtPriceZero_…` | `if (stock.Price == 0) return 0;` in `SellStock` | rot |
| `FinanceStockPriceZeroScreenTests.BuySide_…` | Base-`MaxLeft` | rot (DivideByZero) |
| `AbgleichFile13InterventionTests` | Eintrag `FILE13→FILE14` aus `Interventions` entfernt | rot (Expected Intervention, Actual Random) |
| `TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_…` | Grundeinnahmen +15.000 DM | rot (s. o.) |

**Fundstellen em.exe** (`ndisasm -b16 -e <off> -o <off> gamedata/em.exe`, Kopf 0x1E00)

- 0x596F2–0x59700: Konto `[bx−0x6814]:[bx−0x6816]` ≤ 0 → 0x59737; 0x59702–0x5972E: Kurs `[bx−0x2EC6]` per `cwd` vorzeichen-
  erweitert, Konto gepusht, `call 0x0:0x47C` (= Datei 0x0227C, LDIV). 0x02296 ff.: Divisor-High 0 und Dividend-High 0 bzw.
  Divisor-Low 0 → 0x0231A `div bx` mit bx = 0 → INT 0. INT-0-Vektor bei Datei 0x01FB2 (`mov ax,0x2500`, dx = 0x174 → 0x01F74);
  0x01F74: cx = 0x0E, dx = 0x2F → 0x02080 (`int 21h` AH 40h auf stderr), 0x02088 `mov ax,3` → Programmende. DGROUP 0x14FD:
  Datei 0x16DD0 + 0x2F = „Divide error\r\n“ (14 Byte). Bestätigt; ebenso der Hinweis auf `cwd` bei Datei 0x59713.
- 0x26F20 ff.: Datei 0x2700A `call 0xc8:0x25` (Overlay 0xC8 = F057 0x26D9A „Ereignisse“ mit Infoleiste, `inventur.md`) **vor**
  den Kontobuchungen `add/adc [bx−0x6816]/[bx−0x6814]` bei 0x270F1, 0x2711E, 0x271BF, 0x27205, 0x27252 (Einzelprämie 0x493E0
  Drittel = 100.000 DM). Reihenfolge Infoleiste → Prämie bestätigt.

**#703 R701-01…05:** #753 gemergt (`9815ef9`), Review `docs/reviews/2026-11-11-pruefung-karte-1.md` mit roten Gegenproben zu
R701-01/02/03 (M07) und akzeptiertem `[APPROX]` R701-04 (`Season.cs:108`); Tests `TransferList_HeaderLine_…`
(`PruefungKarteRunde14UiTests.cs`), `CoachWarning_Boundary` (`WettbewerbeNationalteamTests.cs`), `File44_WorldCupLoop_…`
(`LoadQuirkTests.cs`) am Head vorhanden und grün; R701-05 laut `2026-11-09-integration-pruefung-karte-r14.md` „zulässig“.

**Weitere Suche:** keine weitere Division durch einen Aktienkurs in `src/` (`grep -rnE "/ *…\.Price\b|/ *price\b"`: nur
Spielerpreise in `Transfer.Original.cs`, `StatisticsClub.cs`); `BuyStock` nur aus `FinanceScreen.cs:582`.

**Akzeptierte `[APPROX]`:** Kurs 0 → Höchstwert 0 statt Programmabbruch (`FinanceStocks.cs` `MaxStockPurchase`); der Absturz
des Originals ist kein nachbildbares Verhalten.
**Annahmen (nicht geprüft):** Probe des Nutzers vom 11.11.2026 (`file13_r15_*`, `file12_r15_*`, „Eingesetzt: 20“) nicht selbst
angesehen; Saatunabhängigkeit des April-Abschlusses (s. o.).
