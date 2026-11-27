# Gesamtreview Runde 22

## 1. Merge-Empfehlung

**Ja** für genau diesen Head: keine neuen nachgewiesenen Produktfehler; die gezielten Gegenproben und der Gesamtprüflauf sind bestanden. Die genannten Restgrenzen bleiben bestehen. Base `eeb6264f21cbb52b86755919ed3bcb88e8471385`, Head `350f550143b51fc641f01abe9735a45808bb5691`.

## 2. Befunde

Keine neuen nachgewiesenen Produktfehler.

## 3. Ausgeführte Prüfungen und akzeptierte Näherungen

- Checkout vor Prüfung sauber, HEAD = `350f550143b51fc641f01abe9735a45808bb5691`; SDK 10.0.402, Originaldaten und Stände vorhanden.
- `dotnet test tests/EishockeyManager.Core.Tests --filter 'FullyQualifiedName~AbgleichOriginalPaareTests|FullyQualifiedName~ZusehenRestAufstellungTests|FullyQualifiedName~PruefungKarteNationalTests|FullyQualifiedName~NationalTeil1Tests|FullyQualifiedName~HokPlayOffMarkenTests'`: 159 bestanden, 0 Fehler, 0 übersprungen. Enthält FILE02→03/Saat 4711 und FILE38→39/Saat 2: nur die im Test zugelassenen Abweichungen; optionale Muster sind keine Pflichtabweichungen.
- `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-build --filter 'FullyQualifiedName~KaderplatzWerteTeil2Tests|FullyQualifiedName~NationalTeil3Tests|FullyQualifiedName~AbgleichRestWorldOnlyTests|FullyQualifiedName~MigrationEchteStaendeTests'`: 33/33, u. a. A093 FILE71–74, vier Saisonwechsel und echte Schema-51–53-Stände bis Laden, Weiterlauf und erneutem Speichern.
- Weitere Core-Tests mit Filter `RngTwinProbeTests|WettbewerbeRegelnTests|EinzelpunkteTests|NationalTeil2Tests|KaderRegelnTeil1Tests`: 58/58. UI-Filter `PassiveScreenTests|WettbewerbeRegelnUiTests|ZusehenRestAufstellungUiTests|TransferVertraegeKomplettTeil5ScreenTests`: 16/16, jeweils Release, Originaldaten, keine übersprungen. Bildschirmöffnen inklusive Zustands-/Zufallsfingerabdruck; Live-Aufstellung zusätzlich gezielt auf Strafbank geprüft.
- FILE18/19/20/21: Rohwörter Satz 5 und Import je `0,0,0,0,0,0` / `1,0,0,1,3,5` / `1,0,0,90,3,5` / `1,0,0,90,3,5`; Freundschaft FILE18→19 und FILE20→21 zusätzlich durch obige Tests. Für #938 P3-3 kein neuer Beleg.
- FILE22/44: Import-Marken gegen Originalbytes geprüft; Import→Export: Endspiel FILE44 gleich, FILE22 Plätze vertauscht. Reimport beider Exporte scheitert am Tagesindex 217/Zähler 218; frühere Runden werden nicht exportiert. Bereits auf Basis bekannt, siehe [Review Play-off-Marken](../runde-21/2026-11-17-hok-play-off-marken.md); synthetischer Marken-Export/Reimport grün.
- `tools/pruefen.sh --schnell`: Release-Build mit Warnungen als Fehler bestanden; Originaldaten 3787 bestanden, 0 fehlgeschlagen, 0 übersprungen. Erster Versuch scheiterte bei NU1900 (NuGet-Auditquelle nicht erreichbar); Wiederholung gemäß `docs/prozess/umgebung.md` mit `NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1`, Paketcache und freigegebenen lokalen Sockets.
- Erwartete Restpunkte bestätigt: #939 (R22-E2 = B), #948 A873/A913/A951, #925 DEB-Pokal-Rest, #938 P3-1/P3-3, #951 `pruefen.sh`, #954, #930 Rest A917, #936; 21 D für Aufnahme `runde-22`, 24 X-Funktionszeilen. #938 und #930 sind als Issues geschlossen, ihre genannten Teilreste laut Rundenkarte zurückgestellt.
- Akzeptierte `[APPROX]`: Migrationen fehlender Altstandsdaten, A876 (Zähler Satz 5 über Saisonwechsel), A917 (Import übriger Manager), Bildschirm-Verlassen ohne Änderung und bekannte Play-off-Exportgrenzen. Zufallsfolge des Originals bleibt wegen anderer Quelle und Reihenfolge eine Näherung. Annahme: genannte Zurückstellungen gelten weiter.
- Nicht vollständig geprüft: sämtliche Originalfundstellen, alle Eingabepfade und DOSBox-Aufnahme `runde-22`; dies ist ein risikoorientiertes Rundenreview, keine Abschlussprüfung.
