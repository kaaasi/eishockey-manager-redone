# Review der Finanzen-Nachprüfung: Nachprüfung FN-01

## 1. Merge-Empfehlung

**Ja** für Head [`3e514bc8ed610240ec731193982182de5655b9f6`](https://github.com/kaaasi/eishockey-manager-redone/tree/3e514bc8ed610240ec731193982182de5655b9f6), bezogen auf das Delta zum zuvor geprüften Head `8c3c80adb17df95d465af5e16ffb8d13ef15014f` und FN-01. Die Ursachen der fünf Abdrücke sind nun einzeln beschrieben; zwei davon stimmen mit eigenständig wiederholten Vergleichsläufen überein. Die übrigen drei Ursachen wurden nicht unabhängig ausgeführt. Die allgemeine Merge-Prüfung einschließlich aktueller Base und CI bleibt bei der Leitstelle.

## 2. Befunde

Keine neuen Befunde im geprüften Umfang. **FN-01 (#139):** Der P2-Befund aus [Review #138](https://github.com/kaaasi/eishockey-manager-redone/pull/138) ist für diese Nachprüfung ausgeräumt: Die neue [Tabelle](https://github.com/kaaasi/eishockey-manager-redone/blob/3e514bc8ed610240ec731193982182de5655b9f6/docs/finanzen-rekonstruktion.md#L251) benennt für jeden Fall den auslösenden Commit, die erste Zustandsdifferenz und deren Ursache. Die zwei unten nachgestellten Fälle bestätigen die jeweilige Erklärung. #139 ist auf GitHub noch offen und wird laut PR-Beschreibung durch #124 geschlossen.

Erwartet offen: [#132](https://github.com/kaaasi/eishockey-manager-redone/issues/132), [#133](https://github.com/kaaasi/eishockey-manager-redone/issues/133), [#134](https://github.com/kaaasi/eishockey-manager-redone/issues/134), [#135](https://github.com/kaaasi/eishockey-manager-redone/issues/135), [#136](https://github.com/kaaasi/eishockey-manager-redone/issues/136) sind offen und hier nicht erneut als Befunde gezählt.

## 3. Ausgeführte Prüfungen und akzeptierte Näherungen

- Prüfstand: `git rev-parse` und `git diff 8c3c80a 3e514bc`: ausschließlich `docs/finanzen-rekonstruktion.md` ergänzt und XML-Dokumentationskommentar in `RngTwinProbeTests.cs` geändert; kein ausführbarer Code und kein Test-Sollwert im Delta geändert. Base von #124 `27fa1c4da0f1f76603b3c62118804f5e3c663036`; Review-Branch von `main` `b6cee55e1db9477c54ac5ee6637eab213009123a`. SDK `10.0.402`.
- Unabhängige Vergleichsprobe in getrennten temporären Worktrees `af1d24ea9c8dc691e762469db1da1128eb4416bd` und `e3abc900dc6286e07129d1ddbdbc07320643d5fe`: derselbe `RngTwinProbeTests.Run` für vier Saisonwechsel, passiv, Seeds 3 und 11. Nach jedem `Continue` und Saisonwechsel wurden Snapshot des abdruckrelevanten Zustands, die vier RNG-Zustände, Datum und Konto protokolliert; eine zweite Probe verglich den vollständigen vom Abdruck verwendeten Zustandsdump und das Saisonergebnis vor dem ersten Wechsel. Je Stand `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-restore --filter FullyQualifiedName~FinanceIndependentProbe` und anschließend `~FinanceDifferenceProbe`: je zwei Fälle bestanden. Instrumentierung ausschließlich in den temporären Worktrees.
- **Seed 3:** erste Snapshot-Differenz bei Saisonende 1993, Tag 231, Schritt 76: Konto `11.858.903 → 11.758.903 DM`; Verein 2, Bundesliga Platz 4, Europapokalsieger, Prämie `100.000 → 0 DM`. Finaler Abdruck `C81835C179C45656 → 7307C658BC6557A6`.
- **Seed 11:** erste Snapshot-Differenz bei Saisonende 1993, Tag 231, Schritt 77: Konto `11.944.736 → 11.844.736 DM`; Verein 2, Bundesliga Platz 3, Europapokalsieger, Prämie `100.000 → 0 DM`. Finaler Abdruck `8DC2D4C7015C5C1C → 6246CF397993BE92`.
- In beiden Fällen unterscheiden sich im Zustandsdump am ersten Saisonende nur die Kontowerte; die Differenz bleibt bis zum letzten protokollierten Schritt −100.000 DM und alle vier RNG-Zustände bleiben über die Probe gleich. Der Diff in `FinancePremiums.cs` erklärt die entfallene Europapokalprämie. Die aktiven synthetischen Fälle und der aktive Originaldatenfall wurden für diese Nachprüfung nicht einzeln reproduziert; deren Tabellenangaben bleiben eine nicht unabhängig bestätigte Grundlage.
- `PATH=/workspace/.dotnet:$PATH DOTNET_PROCESSOR_COUNT=1 NuGetAudit=false UseSharedCompilation=false tools/pruefen.sh` auf exakt `3e514bc`, mit Originaldaten und Netzwerkberechtigung für VSTest-Sockets: Build erfolgreich, Ergebnis bestanden.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten | 824 | 0 | 0 |
| Ohne Originaldaten | 690 | 0 | 119 |
| Pflichtlauf ohne Daten (erwartet rot) | 690 | 134 | 0 |

**Akzeptierte `[APPROX]`:** Die im Erstreview akzeptierten fehlenden Einzelprämien (#135) bleiben unverändert. **Annahmen:** Die drei nicht nachgestellten Fälle und die nicht von dieser Nachprüfung umfassten Originalbelege werden nicht als unabhängig erneut geprüft ausgegeben.
