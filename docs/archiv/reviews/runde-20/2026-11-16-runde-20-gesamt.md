# Gesamtreview Runde 20

## Merge-Empfehlung

**Ja für `a509f5c7e051cff4fc5ee89b1c7f59cb689b3769`** (Delta ab `e5262e8`): In der risikoorientierten Stichprobe kein neuer nachgewiesener Fehler. Die offenen Punkte und Grenzen unten bleiben bestehen; dies ist keine Abschlussprüfung vor v1.0.

## Befunde

Keine neuen belegten P0–P3-Produktfehler oder neuen Testlücken. Erwartet offen bestätigt: [#882](https://github.com/kaaasi/eishockey-manager-redone/issues/882) (P3 inkl. Export/Import-Testlücke), N6 (Darstellungszufall), A093 (D, Aufnahme nötig). Bekannte `[APPROX]` einschließlich Bit 3 bei #872 und Migration alter Stände auf 0/0 sind akzeptierte, bereits dokumentierte Abweichungen, keine neuen Befunde.

## Ausgeführte Prüfungen

- Sauberer Checkout `a509f5c`; .NET SDK 10.0.402; `git diff --check e5262e8 a509f5c` ohne Ausgabe.
- #872: `File61[2][:3] = 000`, `File61[3][:3] = 222`; gezielt `dotnet test tests/EishockeyManager.Core.Tests --no-build --filter 'FullyQualifiedName~ReiheBeiSpielbeginnTests|FullyQualifiedName~File61_LinesWithoutClick'` mit Originaldaten und aktuellen Originalständen: 7 bestanden, 0 fehlgeschlagen. Enthält Startreihen aus FILE50/61/63, Migration 50→51, Speichern/Laden und vier Saisonwechsel. `objdump -D -b binary -m i8086` auf `em.exe` ab Datei 0x84EB: 0x853F setzt +0x2F (indexiert auch +0x31) auf 0, 0x855D prüft Bit 3; Ausnahme siehe #882. Import, Export und Aufrufer gelesen; Export-Rundlauf bleibt dort offen.
- `python3 tools/reverse-engineering/markerliste.py` zweimal: jeweils Exit 0, `cmp` gleich; 216 Dateien, 618 Markerzeilen, wie `grep -rc '\[APPROX\]' src/ | awk -F: '$2>0{n++;s+=$2}END{print n,s}'`. `--abweichungen`: z. B. F019/F033 erscheinen, F020/F021/F023/F027 ohne Spielabweichung nicht.
- Einordnung stichprobenartig anhand CSV, Code und Begründungen: V A020/A054/A612/A703, D A002/A057/A093, E A040/A609/A701, K A017/A195/A271, X A006/A018/A021; Funktionen F019–F021/F023/F027–F029/F033/F035/F036. N1=B, N2=B, N3=B, N4=C, N5=C in den zugehörigen Beispielen nachgezogen; N6 bleibt offen. Keine Vollinventur und keine vollständige Rückprüfung aller Originalfundstellen.
- Originalstände aus separatem Referenz-Checkout `36363bb`, `EM_SAVEGAMES` darauf gesetzt; `em.exe` der vorhandenen Daten und des Referenz-Checkouts SHA-256-gleich. Bildschirmfotos nicht erneut pixelweise verglichen; Finanz-, Wettbewerbs-, Bedienungs- und Kaderpfade nur risikoorientiert anhand Delta, Stichproben und Tests geprüft.
- `NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh --schnell` (mit `EM_SAVEGAMES` wie oben): Release-Build mit Warnungen als Fehler bestanden; Audit für Offline-Lauf aus. Der erste unveränderte Aufruf war durch NU1900 (NuGet-Audit nicht erreichbar) beim Restore blockiert, kein Produkttest.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3373 | 0 | 0 |
