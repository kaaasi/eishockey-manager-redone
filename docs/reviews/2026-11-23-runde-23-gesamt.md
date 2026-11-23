# Gesamtreview Runde 23

Unabhängiges Review durch Codex, ohne Implementierungsbeteiligung. Base `70a09fc`, geprüfter Head
`4ae1735718278b3aa19d79aed738a77bf0cb6075` (sauberer Checkout); Umfang R23-D4 = B: #962 v-rest und #963 feldspieler-sechs.

## 1. Merge-Empfehlung

**Ja für Head `4ae1735`**, bezogen auf den beauftragten Umfang: keine neuen nachgewiesenen Produktfehler;
beide Gegenbeispiele bestanden, die relevanten Übergänge wurden risikoorientiert geprüft. Das ist keine Freigabe
anderer Spuren des Runden-Diffs oder eine vollständige Originaltreueprüfung. Die unten genannte Testlücke bleibt.

## 2. Befunde

Keine neuen nachgewiesenen Produktfehler P0–P3.

**Testlücke:** Die Spielwertung zählt Spiele für Liga und DEB-Pokal in
[`SpielwertungVorbereitungTests.cs:104`](https://github.com/kaaasi/eishockey-manager-redone/blob/4ae1735718278b3aa19d79aed738a77bf0cb6075/tests/EishockeyManager.Core.Tests/SpielwertungVorbereitungTests.cs#L104);
ein eigener Fall für das Europapokalfeld fehlt. Die gemeinsame Wettbewerbszuordnung ist kein Nachweis für diesen Ablauf.
Kein nachgewiesener Produktfehler; bereits in der Nachprüfung zu #962 vermerkt.

## 3. Ausgeführte Prüfungen

- `git rev-parse HEAD`, `git status --porcelain`: angeforderter Head, sauber vor der Prüfung; SDK 10.0.402.
- `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests -c Release -m:1 -p:UseSharedCompilation=false --filter
  'FullyQualifiedName~PurchaseFromManager_IgnoresSellerSlotPenalties|FullyQualifiedName~OriginalExportImport_National_KeepsSixthSkater'`:
  **3 bestanden, 0 fehlgeschlagen, 0 übersprungen**. VR-01 und R963-01 (Abwehr und Sturm) erneut ausgeführt.
- `PATH=/workspace/.dotnet:$PATH NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh --schnell`:

  | Lauf | bestanden | fehlgeschlagen | übersprungen |
  |---|---:|---:|---:|
  | Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3850 | 0 | 0 |

  Build mit Warnungen als Fehler und Prüfung bestanden. NuGet Audit war für diesen Lauf ausgeschaltet.
  Der erste Teststart in der Sandbox war wegen gesperrter lokaler Sockets abgebrochen; die obigen Läufe erfolgten mit Freigabe.
- Gezieltes Codelesen: neuer Kaderplatz und Verhandlung, Spielwertung einschließlich Zuschauer/Strafe und Zufallsreihenfolge,
  verletzte WM-Kandidaten, sechster Platz und Live-Eingriff, Schema 62→63, HOK-Import Verein und Satz 5 und zugehörige Tests.
  Die passive Mehrsaisonprobe prüft jetzt auch den Grund für das Schließen des sechsten Platzes (R963-02).
- Original `em.exe`: `dd if=gamedata/em.exe of=/tmp/r23.bin bs=1 skip=$((0x413c9)) count=$((0xeb))` und
  `objdump -D -b binary -m i8086 -M intel --adjust-vma=0x413c9 /tmp/r23.bin`: 0x413D2–0x413E5 prüft
  Platz- und Torhüterkennzeichen; 0x413E8–0x4140C übergibt Zelle 5 an das Aufstellen.
- A876 und A917 bleiben wie in `docs/analyse/re-reste-23.md` §3–4 als Klasse K bestätigt; keine neuen Befunde daraus.
- Akzeptierte `[APPROX]` aus #963 bestätigt: abgeleitetes `SixthSkaterOpen`, geleerte Sturmzelle Reihe 1,
  dritter Abwehrspieler beim Import Reihen 2–3, `NationalLineup.LineValue` ohne sechsten Feldspieler.
- Annahmen/Grenzen: Originaltreue der genannten Regeln nicht erneut vollständig disassembliert; keine eigenen DOSBox- oder
  Bildvergleiche. Andere Spuren und übrige Tages-, Finanz- und Saisonpfade außerhalb des beauftragten Deltas nicht gesondert geprüft.
