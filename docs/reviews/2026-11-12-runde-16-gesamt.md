# Gesamtreview Runde 16

## 1. Merge-Empfehlung

**Ja für `d2339895f48b40a10fd65f91d8a4d9c52b0fb996`** (Base `31597e4`): Im risikoorientierten Review kein neuer nachgewiesener Produktfehler. Die unten genannten Prüfgrenzen bleiben; sie begründen für sich keine Ablehnung. Kein PR der Runde wurde hier erneut freigegeben oder gemergt.

## 2. Befunde

Keine neuen nachgewiesenen Produktfehler.

**Testlücke:** Der FILE02→03-Test für #772 prüft bei Computermannschaften die Klassen 0/40/60 und Angriff 0, verlangt aber nicht für jeden Verein den exakten 40/60-Wert des Originals; bei Managergegnern prüft er nur den gültigen Angriffsbereich. Unterschiede in der Stärke können daher unentdeckt bleiben. Siehe [`AbgleichRestPreparationTests.cs`](https://github.com/kaaasi/eishockey-manager-redone/blob/d2339895f48b40a10fd65f91d8a4d9c52b0fb996/tests/EishockeyManager.Core.Tests/AbgleichRestPreparationTests.cs#L28).

**Testlücke:** Die Kontoanzeige #768 ist im Oberflächentest für den ersten Manager mit Prämie geprüft; der Wechsel auf weitere Manager und die Nationaltraineransicht sind darin nicht gesondert gegen ein Originalbild belegt. Siehe [`PruefungKarteRunde16UiTests.cs`](https://github.com/kaaasi/eishockey-manager-redone/blob/d2339895f48b40a10fd65f91d8a4d9c52b0fb996/tests/EishockeyManager.UI.Tests/PruefungKarteRunde16UiTests.cs#L19).

## 3. Ausgeführte Prüfungen und Grenzen

- Sauberer Checkout `d2339895f48b40a10fd65f91d8a4d9c52b0fb996`; .NET 10.0.402, Originaldaten und Originalspielstände vorhanden. Diff und Aufrufer risikoorientiert gelesen: Pool und Schema 47→48→49, Import/Export, WM-Tagesabschluss, Spielvorbereitung und Saisonende-Infoleiste. Keine Vollinventur.
- `dotnet test tests/EishockeyManager.Core.Tests --filter 'FullyQualifiedName~OriginalExportRoundTripTests|FullyQualifiedName~AbgleichRestPreparationTests|FullyQualifiedName~AbgleichRestWorldOnlyTests|FullyQualifiedName~StockPriceZeroTests'`: 78 bestanden, 0 fehlgeschlagen, 0 übersprungen. Umfasst #772 FILE02→03 (Saat 1–3) und #757 Kurs 0.
- #743/R727N2-01: temporärer Archiv-Checkout, `if (!marketTwins.Contains(index))` → `if (true)`; `dotnet test ... --filter 'FullyQualifiedName~MarketTwin_KeepsPoolSlot179AndMarketOwner'`: 4 rot, Besitzer erwartet 4, tatsächlich 0. `dotnet test ... --no-build --filter 'FullyQualifiedName~OriginalStand_ExportMatchesOriginal_ExceptAllowedFields'`: 3 rot (FILE41/43/43A), `far:8253+0x172D` Original 04, Export 00; 49 weitere bestanden.
- #732: `dotnet run --project src/EishockeyManager.Tools -- abgleich <FILE20.HOK> <FILE21.HOK> --seed 1 --alle`: `Managers[0].Money` Original −1.530.327, Remake −1.532.012 DM (zufallsabhängig).
- `tools/pruefen.sh --schnell`: Release-Build mit Warnungen als Fehler bestanden; SDK 10.0.402.

  | Lauf | bestanden | fehlgeschlagen | übersprungen |
  |---|---:|---:|---:|
  | Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2961 | 0 | 0 |
- Erwartet offen bestätigt: #730 zurückgestellt (R16-E2 = C; `[APPROX]` an `OriginalMinutes.ChangeLines`); #773 offen, weitere Klärung benötigt DOSBox. Keine neuen Befunde daraus.
- Akzeptierte `[APPROX]` im geprüften Bereich: Migration/Platzmangel und Startkader des Pools, alte Spielvorbereitungswerte = 0, WM-Kader-Tagesablauf, sowie Kurs 0 ohne Absturz. Dies bestätigt keine vollständige Originaltreue. Annahme: bereitgestellte FILE-Stände entsprechen den dokumentierten Originalständen.
- Nicht selbst nachgestellt: DOSBox-Liveverhalten und eigene Disassemblierung von `em.exe`, vollständiger Bildvergleich, sämtliche Wettbewerbs- und Bedienpfade. Die Regressionen ersetzen diese Untersuchungen nicht.
