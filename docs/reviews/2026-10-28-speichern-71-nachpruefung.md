# Nachprüfung Speichern #71

## 1. Merge-Empfehlung

**Ja für `27c4af1d138689b8dc14cea1b4e1f64a299a39c2` gegen Base `8aae8aa29fdb2b13a0b001b4070049e7b6d2f77c`**, soweit die hier beauftragte Nachprüfung reicht: S71-01 und S71-03 sind reproduzierbar behoben; der Rebase hat die bisherigen Format-Patches unverändert übernommen. S71-02 (#80) bleibt gemäß Nutzerentscheidung zurückgestellt. Nicht Gegenstand dieser Freigabe sind der weiterhin separate Dialogteil und neue Änderungen nach diesem Head. Merge ausschließlich durch Leitstelle/Nutzer nach deren Merge-Prüfung.

Nachprüfung des [Erstreviews](2026-10-28-speichern-68-70-71.md), bisheriger Head `d4d04866ebfbe1c91a212ef09a320ef5db9b21ed`. Reviewer: ChatGPT, keine Beteiligung an der Implementierung oder den Fixes. Umfang: Rebase-Abgleich und Fix-Commits `d6a1f45`, `27c4af1`; kein erneutes Gesamtreview der unveränderten Teile.

## 2. Befunde

**Keine neuen P0–P3-Befunde im geprüften Delta.**

- **S71-01 / #79 (zuvor P2): behoben.** Das unveränderte Gegenbeispiel `Clubs.items[0] = null` wird mit „darf nicht leer sein“ abgelehnt; keine Ausnahme, kein geladener Teilzustand. [Reader:184](https://github.com/kaaasi/eishockey-manager-redone/blob/27c4af1d138689b8dc14cea1b4e1f64a299a39c2/src/EishockeyManager.Core/SaveGame.Reader.cs#L184) verwendet die Nullable-Metadaten des Feldes und reicht sie an Sammlungselemente bzw. Wörterbuchwerte weiter. `Player?[,]`-Elemente und nullable Zufallsquellen bleiben zulässig. Eine gezielt deaktivierte Prüfung macht beide neuen Null-Tests wieder rot, einschließlich der früheren NullReferenceException; es wurde kein pauschales NRE-Catch als Ersatz eingeführt.
- **S71-03 / #81 (zuvor P3): behoben für den verlangten Feldnachweis.** `if (name == "Money") continue;` im Writer macht jetzt **beide** Vollständigkeitstests rot. [CoverageProbe:280](https://github.com/kaaasi/eishockey-manager-redone/blob/27c4af1d138689b8dc14cea1b4e1f64a299a39c2/src/EishockeyManager.Core/SaveGame.cs#L280) benutzt die tatsächlichen Writer-/Reader-Feldroutinen für alle erreichbaren Zustandstypen, auch unbelegte. Bei der absichtlich leeren Probe ist nur die Nullbarkeitsprüfung abgeschaltet; die produktive Load-Strecke behält sie. Grenze: Standardwerte belegen die Behandlung der Feldnamen, nicht die Korrektheit sämtlicher möglicher Werte; dafür bleiben Roundtrip-/Zwillingsproben notwendig.
- **S71-02 / #80 (P3): weiterhin offen und vom Nutzer zurückgestellt.** Sammlungsspeicherung/-rekonstruktion wurde nicht geändert. Issue #80 ist offen. Keine erneute Bewertung oder Freigabe des Remove/Add-Risikos.

Rebase: `git range-diff 0745fa6..d4d0486 8aae8aa..27c4af1` liefert `3ecb884 = d82dc4d` (leerer Startcommit), `d4d0486 = 7dde3bd` (Format-Patch); zusätzlich ausschließlich `d6a1f45` und `27c4af1`. Deren Delta betrifft nur `SaveGame.Reader.cs`, `SaveGame.Writer.cs`, `SaveGame.cs`, `SaveGameTests.cs` und `docs/formate/spielstand.md`. Damit kein verstecktes Rebase-Delta im Formatteil festgestellt. Der unveränderte Schemafingerprint ist Teil der bestandenen Tests; neue Schemaversion/Migration ist für diese Fixes nicht eingeführt.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Exakten Head als detached HEAD ausgecheckt und mit `git rev-parse HEAD` geprüft. .NET SDK `10.0.402`; `DOTNET_ROOT=/workspace/.dotnet`, PATH entsprechend, `DOTNET_PROCESSOR_COUNT=1`. Quellcode des geprüften Checkouts blieb unverändert. Mutationen ausschließlich in `/tmp/eishockey-review-71-followup` aus `git archive 27c4af1`, anschließend zurückgenommen.

Zuerst die beauftragten Gegenbeispiele:

```bash
dotnet test tests/EishockeyManager.Core.Tests -c Release -m:1 -p:UseSharedCompilation=false \
  --filter 'FullyQualifiedName~NullClub_IsRejectedWithMessage|FullyQualifiedName~NullAnywhere_NeverThrows|FullyQualifiedName~CoverageProbe_WriterAndReaderCoverEveryFieldOfEveryType|FullyQualifiedName~SchemaCoverage_EveryStateFieldIsSaved'
```

Ergebnis: **4 bestanden, 0 fehlgeschlagen, 0 übersprungen**.

| Gegenprobe in temporärer Kopie | Ergebnis |
|---|---|
| Writer lässt `Money` aus; Filter auf beide Vollständigkeitstests | Beide rot: fehlendes `Manager.Money`; 2 Fehler, 0 bestanden |
| Produktive Nullbarkeitsprüfung deaktiviert; Filter auf beide neuen Null-Tests | Beide rot: NullReferenceException im Vereins-/Spielerzugriff; 2 Fehler, 0 bestanden |
| Unveränderter Fix: drei verzögerte RNG-Quellen auf `null`, zusätzlich alle Aufstellungsplätze leeren; Datei sichern/laden/sichern | 1 unabhängige Positivprobe bestanden; RNG-Abbild und Datei bytegleich; nullable `Player?[,]`-Elemente akzeptiert |

Zur Positivprobe: Spieler bleiben im Kader; nur die Aufstellung wird über `Lineup.Remove(player)` geleert. Deshalb handelt es sich um einen zulässigen Zustand und nicht um die Akzeptanz einer beschädigten Datei.

Originaldaten: lesender Sparse-Abruf ausschließlich von `original/` aus `kaaasi/eishockey-manager-referenz`; `sha256sum -c --quiet SHA256SUMS` erfolgreich. Daten ausschließlich außerhalb des Code-Repositories, über ignoriertes `gamedata` verlinkt; keine Originaldatei oder Abbildung in diesem PR.

Vollständiger Pflichtlauf auf dem exakten Implementierungs-Head:

```bash
DOTNET_PROCESSOR_COUNT=1 bash tools/pruefen.sh
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 580 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 496 | 0 | 69 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 496 | 84 | 0 |

Release-Build mit Warnungen als Fehler bestanden; Skript-Exit 0. Die 84 Fehler des Pflichtlaufs wurden anhand aktueller TRX-Dateien als ausschließlich fehlende Originaldaten ausgewertet. Kein Absturz oder sachfremder Testfehler als erwartetes Scheitern gewertet.

Der volle Lauf enthält insbesondere die gültigen täglichen Roundtrips über zwei Saisons und die vier Saisonwechsel der passiven/aktiven Zwillingsspiele, synthetisch und mit Originaldaten. Zusammen mit der zusätzlichen Null-Positivprobe wurde keine Fehlablehnung gültiger Spielstände beobachtet; dies ist keine erschöpfende Prüfung aller möglichen Zustandskombinationen. Nullable-Metadaten mit `Unknown` werden von der neuen Regel nicht pauschal als Pflichtverweise behandelt.

CI auf exakt `27c4af1`: Check `build` erfolgreich, `publish` übersprungen (lesend geprüft). Kein Merge ausgeführt.

**Akzeptierte [APPROX]:** keine neuen im Delta; die im Erstreview akzeptierten Annäherungen bleiben unberührt. **Grenzen/Annahmen:** Null-Stichprobe umfasst 150 gezogene Positionen mit anschließendem `Distinct`, keine vollständige Fuzzing-Prüfung. Der Ladedialog und dessen Zustandsreinheit sind weiter separate Folgearbeit; die Nachprüfung ändert daran nichts.
