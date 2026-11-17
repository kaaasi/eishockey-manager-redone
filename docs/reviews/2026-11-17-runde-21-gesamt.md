# Gesamtreview Runde 21

## 1. Merge-Empfehlung

**Ja für `e5cf5e32f09302a66125ef851a1e793c21c6cfc2`**, vorbehaltlich der unten genannten offenen Punkte. In den risikoorientierten Gegenproben kein neuer nachgewiesener Produktfehler; die bekannten Abweichungen bleiben. Base: `1941f6a0c0a4535037bf3e1c6cd536c42bf0407b`. Die Empfehlung ist keine Aussage über vollständige Originaltreue.

## 2. Befunde

Keine neuen belegten Produktfehler. **Testlücke:** Die Migrationstests konstruieren ältere Remake-Stände aus einem aktuellen Stand; sie belegen nicht das Laden tatsächlich mit alten Versionen geschriebener Dateien. Die Kette und die Objektumbenummerierung werden damit dennoch ausgeführt. Keine vollständige Prüfung aller historischen Schemata oder aller Testwertänderungen.

**Erwartet offen:** GitHub #896, #897, #899, #908 und #912 sind offen. Die FILE71/72-Importprobe bestätigt die in #908 beschriebene Abweichung; FILE72→73 ergibt in der hier ausgeführten Probe für München 68/69/69/69/55/48 statt des im Review #906 dokumentierten Originalstands 77/70/75/69/60/48 (Kondition A/S, Technik A/S, Form A/S). FILE72/73-Automatiktests allein erkennen den Aufstellungstausch nicht. #897 umfasst Finanzübersicht 3/4 und 6/7, Pokal-Gegentore, `ChangeList` Gehalt 0, Verletzung 0x07462, Poolplatz 179 und `SlotSeed`; keine erneute Vollprüfung dieser Punkte.

**Akzeptierte `[APPROX]`:** `ComputerSquadRandom` und `StartOrder` außerhalb des gemeinsamen Stroms; A100 nicht bitgenau; alte Speicherstände setzen nach Entfernen eigener Quellen mit verändertem Zufallsverlauf fort. Weitere bereits markierte Näherungen bleiben bestehen. Annahme: Die nachgebildeten Schemata repräsentieren die relevanten Felder und Referenzen alter Stände; echte Dateien dieser Schemata wurden nicht geprüft.

## 3. Ausgeführte Prüfungen

- Exakter Head nach Fetch ausgecheckt, Arbeitsbaum vor den Prüfungen sauber; .NET 10.0.402, Originaldaten und FILE-Stände lokal verfügbar.
- `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter 'FullyQualifiedName~StartLaderTests|FullyQualifiedName~TeamwertAufstellungTests|FullyQualifiedName~ZufallsstromSpielstartTests|FullyQualifiedName~ZufallsstromTeil4Tests|FullyQualifiedName~SaveGameTests|FullyQualifiedName~ZufallsstromMehrsaisonTests'`: **91 bestanden, 0 fehlgeschlagen, 0 übersprungen**. Enthält vier Saisonwechsel mit aktivem und passivem Manager sowie regelmäßigem Speichern/Laden.
- Weitere gezielte Core-Tests (`ZusehenWatch`, `ZusehenWmSchalter`, `ZufallsstromTeil1` und die genannten Gegenbeispiele): **71/71**; `HokImportWerte`, `ZufallsstromTeil2/3`: **115/115**. Synthetisch nachgebaute Schemata 51, 52 und 53 durchlaufen Migrationen bis 54.
- UI-Tests `FinanzuebersichtBuchung`, `ZusehenKnopf`, `ZusehenSpielplan`: **20/20**. N5=C und HOK-Import im Diff und anhand dieser Tests geprüft; keine neue Pixel- oder Disassembly-Vollprüfung.
- A080: `TeamwertAufstellungTests` einschließlich FILE72→73; zusätzlich temporäre Importprobe FILE71/72 und Fortsetzung FILE72→73 mit `HokCompare.Import/PlayTo` und `ClubFieldValues`. FILE71 und FILE72 nach Aktualisierung jeweils 68/70/69/69/55/48; FILE73 wie oben. Temporärer Test entfernt.
- R21-D2=A: angepasste fremde Tests und Begründungen stichprobenartig im Diff geprüft; keine Einzelrückführung aller Saaten und Sollwerte. Kein neuer Sollwert aus einem Originalstand festgestellt.
- Nicht umfassend geprüft: alle Wettbewerbs- und Bedienungswege, Bildschirmfotos, em.exe und reale Remake-Altstände. Original-HOK-Stände wurden in den Gegenproben verwendet.
- `PATH=/workspace/.dotnet:$PATH NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh --schnell`: Build bestanden; SDK 10.0.402. Der erste Versuch ohne Offline-Einstellung scheiterte an NU1900 (NuGet-Vulnerability-Dienst nicht erreichbar); Audit wurde nur für den Wiederholungslauf abgeschaltet.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3583 | 0 | 0 |
