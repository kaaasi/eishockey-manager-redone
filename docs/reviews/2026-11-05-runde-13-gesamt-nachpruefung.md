# Gesamtreview Runde 13 – Nachprüfung (Zwischenstand)

## 1. Merge-Empfehlung

**Nein**, für Head `b634032433e389623d413b14441ceb65fabdb6a5` (Gesamt-Base `8afd750bfc0cc57948041ca8906db6924caa7eb5`; Fix-Base `b0688af89c2cafe701f714259c02f20bf552f442`). Nur partielles Codelesen über den GitHub-Connector; kein nachgewiesener Checkout des Heads und kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4. Die nachfolgend offenen Prüfbereiche verhindern die Freigabe. Der Originaldaten-Nachweis muss für diesen Head im PR bzw. einem Kommentar der Leitstelle stehen; die Angaben in #615 sind nicht unabhängig bestätigt.

## 2. Befunde

Keine neuen reproduzierten Produktbefunde. R13G-01 und #606 sind durch diese Sitzung weder als behoben noch als weiterhin vorhanden bestätigt. Die fehlenden Prüfungen unten sind keine Behauptung eines Produktfehlers.

## 3. Ausgeführte Prüfungen und akzeptierte Annäherungen

- `git cat-file -t b634032433e389623d413b14441ceb65fabdb6a5` scheiterte: Head nicht lokal vorhanden. `git fetch origin` scheiterte mit `Failed to connect to proxy port 8080`. Lokales `origin/main` stand auf `d45ba2387bf76dd8efd27c7908e837a4d9703d49`; laut Connector stand Remote-main auf `b0688af89c2cafe701f714259c02f20bf552f442`. Ein vollständiger Tree wurde nicht rekonstruiert oder verifiziert.
- `PATH=/workspace/.dotnet:$PATH dotnet --version`: `10.0.402`. Lokal wurden weder `em.exe` noch HOK-Spielstände gefunden. Das Referenz-Repository ist über den Connector sichtbar; die benötigten Binärdaten wurden für diese Sitzung nicht lokal bereitgestellt.
- Connector: Gesamt-Compare Base..Head und [#615](https://github.com/kaaasi/eishockey-manager-redone/pull/615) gelesen. Der Fix entfernt im Diff den Aufruf `RenumberPlayers()` aus `ApplyAutoLineup` und trägt eine Migration 38 → 39 ein, die über Vereinslisten Spieler anhand ihrer ID nummeriert. Dies ist eine statische Beobachtung, kein Nachweis der geforderten Gegenbeispiele oder aller Aufrufer.
- Connector: #591, #592, #594, #595, #597, #598, #603, #604, #605, #610, #611 und #601 sind offen. #601 nennt die Auswertung der Aufnahme `runde-13` für Runde 14. Die P3-Zuordnung steht in den Titeln der erstgenannten Issues; die Connector-Antwort lieferte keine Labels. A088 und U15-c sind in dieser Sitzung nicht unabhängig bestätigt.
- Der [vorherige Bericht](2026-11-05-runde-13-gesamt.md) dokumentiert Ausführungen an `b9ce204c9c36b6511280028abe5168983ca6cc61`, darunter die dort beschriebenen Gegenproben und drei Prüfläufe. Seine Stichproben und der ältere Head ersetzen die unten geforderte Vollprüfung nicht.

**Offene Prüfbereiche und nächste Schritte:**

1. Einen vollständigen Checkout des genannten Heads einschließlich Dateimodi und Löschungen über einen funktionierenden Fetch oder durch Rekonstruktion beschaffen, Tree-SHA überprüfen und Originaldaten sowie `original/spielstaende/` lokal verfügbar machen. Anschließend R13G-01 mit `Host.Show(new MainMenuScreen(Host))` und `Draw` an `b0688af` rot und am Head grün ausführen; echten Schema-36-Stand mit freien Nummern bei Manager- und Computerverein laden und alle Kaderplätze prüfen; Migration 36 → 39 mit je vier passiven und aktiven Saisonwechseln ausführen.
2. `tools/pruefen.sh` am sauberen, verifizierten Head mit Originaldaten vollständig ausführen. Alle drei Läufe fehlen; die Tabelle aus #615 ist eine Fremdangabe und wird hier nicht als Ergebnis übernommen.
3. Alle neuen oder geänderten Tests des Gesamtdeltas einzeln inventarisieren und am Base oder mittels gezielter Mutation rot machen; Ergebnis je Test tabellieren. Alle neu eingetragenen oder geänderten Sollwerte, Fingerabdrücke und Saaten einzeln durch Rückmutation der jeweils verursachenden Regel prüfen. Bisher keine vollständige Tabelle; der frühere Bericht nennt nur Stichproben.
4. D1–D3 und L1–L15 jeweils gegen den Code prüfen und einzeln dokumentieren. Die frühere Prüfung war teilweise stichprobenartig; die Entscheidungen wurden hier nicht erneut vollständig geprüft.
5. Jede neue `[ORIGINAL]`-Fundstelle in `em.exe` bzw. Spielstand (`emtool hok dump`) nachlesen; insbesondere die im Fix neu zitierte A091-Stelle. Ohne lokale Originalbinärdaten und ausführbaren Head offen.
6. Jeden Bildschirm und jedes Hauptmenü-Ereignis gegen AGENTS.md §4 prüfen. Die im Vorbericht genannte Freiprüfung hat bekannte Ausnahmen; ihre Stichproben belegen keine Vollständigkeit.
7. A088 und U15-c sowie die akzeptierten `[APPROX]` und die Annahmen am Head eigenständig abgleichen. Die im Vorbericht genannten Annäherungen werden hier nicht neu akzeptiert.

| `tools/pruefen.sh` am angeforderten Head | Ergebnis |
|---|---|
| Mit Originaldaten | offen: kein verifizierter Head und keine lokalen Originaldaten |
| Ohne Originaldaten | offen: kein verifizierter Head |
| Pflichtlauf ohne Daten | offen: kein verifizierter Head |
