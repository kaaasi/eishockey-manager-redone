# Nachprüfung PR #177 – Plan Runde 7

Nachprüfung des Reviews [`2026-10-30-sammelreview-runde-7-vorlauf.md`](2026-10-30-sammelreview-runde-7-vorlauf.md)
(Abschnitt #177). Base: `a50a799917e971ac102eecd80c796fd772e6d2ba`
Bisher geprüfter Head: `8546faf355c186168807726cf38ba6442e8d60fa`
Neuer Head: `e15aeacd712c5126e2b9a21f7081434dd0372b8a` (Delta `8546faf..e15aeac`: Commit `1abeebb` „Befunde behoben“ und
Merge mit `main` bis `c3e5fd5`, das #167 und #176 enthält).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head – **nur Codelesen** (reiner Doku-PR, keine Testläufe, kein Testnachweis nach
`zusammenarbeit.md` §6 Nr. 4). Beide Befunde sind behoben, die Merges mit `main` haben keinen Inhalt verloren.
Offen bleiben drei P3-Klarstellungen, die den Merge nicht aufhalten.

## 2. Befunde

### Nachprüfung der früheren Befunde

- **R7V-177-01 (#180) – behoben.** Gegenbeispiel erneut: `CupsNational.GameState.cs:164` (`PlayFriendly`) ruft
  `MatchEngine.Play(RatedNation(…), …)`. [arbeitspakete.md:33–37](https://github.com/kaaasi/eishockey-manager-redone/blob/e15aeacd712c5126e2b9a21f7081434dd0372b8a/docs/arbeitspakete.md#L33-L37)
  weist `Core/CupsNational*.cs` samt `PlayFriendly` allein `weltkugel-dosbox` zu;
  [Zeile 70–73](https://github.com/kaaasi/eishockey-manager-redone/blob/e15aeacd712c5126e2b9a21f7081434dd0372b8a/docs/arbeitspakete.md#L70-L73)
  schließt sie bei `spielberechnung` ausdrücklich aus. Die Signatur `Play(Club, Club, Random)` (`Season.cs:136`) bleibt, die neue
  Berechnung bekommt eine eigene Methode, die Umstellung der Aufrufer in `CupsNational` macht die Leitstelle nach beiden Spuren.
  Kreuzprüfung aller `Dateigrenzen` der sechs Spuren: keine produktive Codedatei steht in zwei Spuren (Details unter P3-1).
  Die Referenz- und Mehrsaisontests sind jetzt namentlich aufgelistet, mit Regel (nur Sollwerte, nur ergebnisverändernde Spuren,
  begründet) und Auflösung über die Integrationsfolge; neue Tests in eigenen Dateien.
- **R7V-177-02 (#181) – behoben.** Alle relativen Links in `docs/archiv/runde-6.md` und `docs/arbeitspakete.md` per Skript
  gegen das Dateisystem am Head aufgelöst: kein toter Link. `(runde-5.md)` und `(./)` sind richtig; der Rest des
  Archivinhalts ist unverändert (Zeilenvergleich gegen den Hauptblock in `c3e5fd5`: fehlend nur die zwei korrigierten Linkzeilen).

### Neue Befunde (alle P3, nicht blockierend)

- **R7N-177-01 · P3 – Aussage zu WM-Spielen passt nicht zur Dateizuordnung.**
  [arbeitspakete.md:36](https://github.com/kaaasi/eishockey-manager-redone/blob/e15aeacd712c5126e2b9a21f7081434dd0372b8a/docs/arbeitspakete.md#L36)
  sagt, die Leitstelle stelle „Freundschafts- und WM-Spiele des Nationalteams“ um. Die WM-Aufrufe stehen aber in `CupsWorld.cs:183`
  und `:203` (`MatchEngine.Play`/`KnockoutMatch.Play` mit `game.RatedNation(…)`), und diese Datei darf `spielberechnung`
  nach Zeile 71–72 selbst umstellen. Nur `PlayFriendly` liegt in `CupsNational`. Abhilfe: im Satz „WM“ streichen oder festlegen,
  dass `spielberechnung` `CupsWorld.cs` umstellt und dabei `RatedNation` (liegt in `CupsNational.GameState.cs:176`, gehört
  `weltkugel-dosbox`) nur aufruft, nicht ändert.
- **R7N-177-02 · P3 – Gemeinsame Testhelfer ohne Besitzer.** Der Plan nennt Referenztests und „neue Tests in eigenen Dateien“,
  aber nicht die von #167 geänderten Hilfsdateien `tests/EishockeyManager.Core.Tests/TestSeason.cs` und
  `tests/EishockeyManager.UI.Tests/KeepSquadHelper.cs` (Diff `8546faf..e15aeac`). Zwei Spuren, die dort etwas ergänzen, kollidieren
  beim Merge. Abhilfe: Regel „Hilfsdateien nur additiv und in der Integration zusammenführen“ oder Besitzer nennen.
- **R7N-177-03 · P3 – `docs/reverse-engineering.md` steht in drei Spuren.** `spielberechnung` (ohne Abschnittsangabe),
  `kader-start` („eigener Abschnitt“), `menue-befunde` („Abschnitt Kopierschutz“). Reine Doku, aber Anhänge an derselben Stelle
  erzeugen Textkonflikte. Abhilfe: bei `spielberechnung` ebenfalls „eigener Abschnitt“.

Hinweis ohne Befund: Zeile 192 (`Optional: Import … wartet auf Original-Spielstände des Nutzers`) ist durch #176 überholt;
sie stand schon auf `main` (`c3e5fd5:docs/arbeitspakete.md:200`), nicht Teil dieses Deltas.

### Besonders geprüft: Merges mit `main` (#167, #176)

- **Kein Inhaltsverlust.** `git diff c3e5fd5 e15aeac --stat` zeigt nur `docs/arbeitspakete.md` und `docs/archiv/runde-6.md`
  als Unterschied zu `main`; alle Code-, Test- und übrigen Doku-Dateien (Schema 9, `OriginalSaveGame.cs`, `HokDump.cs`,
  `spielstand-original.md`, `dosbox-befunde.md` …) sind gleich `main`.
- **#167:** Die einzige Änderung an `arbeitspakete.md` war die Statuszeile der Spur `heimspiel-statistik` Teil 2; sie steht
  wortgleich in `archiv/runde-6.md:65–68`. Die Zeile „#167 … nach Runde 6 mit Option (a)“ unter „Zurückgestellt“ entfiel zu Recht
  (erledigt); der Vorlauf ist als erledigt eingetragen (Zeile 18–19).
- **#176:** Der Abschnitt „Spur `hok-format` (nur Analyse)“ ist in den Abschnitt „Paket `hok-format`“ (Zeile 159–164) überführt;
  die „Neue Anforderung“ steht als Spur `hok-patch` (Ziel, Abnahme, Grenzen), die geänderten Zurückgestellt-Zeilen sind
  übernommen. Verkürzt wurden nur Dateiliste und die Angabe „FILE01–03 … `original/spielstaende/`“; beides steht im Code
  bzw. in `docs/formate/spielstand-original.md`.
- **Integrationsfolge eindeutig:** `kader-start → boerse-start → weltkugel-dosbox → menue-befunde, hok-patch →
  spielberechnung zuletzt`; die Spuren in einer Stufe (`menue-befunde`, `hok-patch`) haben disjunkte Dateien.
  **Referenztests:** Zuständig ist je die Spur, die Ergebnis, Kader, Geld oder Zufall ändert; `spielberechnung` rechnet ihre
  Sollwerte zuletzt auf dem integrierten Stand neu. Die frühere Unstimmigkeit „alle starten nach allen Vorlauf-Merges“ ist
  bereinigt (Zeile 18–19 verweist auf „Start gestaffelt“).

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Nur Codelesen und Skripte am sauberen Checkout (`git rev-parse HEAD` = `e15aeac…`, `git status --porcelain` leer); kein
Build, kein `tools/pruefen.sh`, keine Tests (reiner Doku-PR laut Auftrag).

| Prüfung | Ergebnis |
|---|---|
| `git diff 8546faf..e15aeac --stat`, `git diff c3e5fd5 e15aeac --stat` | Unterschied zu `main` nur die zwei Planungsdateien |
| Linkprüfung (Python `-I`) `arbeitspakete.md`, `archiv/runde-6.md` | 0 tote relative Links |
| Zeilenvergleich Hauptblock `c3e5fd5:arbeitspakete.md` ↔ `archiv/runde-6.md` | nur 2 Linkzeilen abweichend (korrigiert) |
| Aufrufer `MatchEngine.Play` per `grep` | `CupsNational.GameState.cs:164`, `CupsGroup.cs:91`, `CupsWorld.cs:183`, `GameState.cs:104`, `Playoffs.cs:16/:184` |
| Kreuzvergleich der `Dateigrenzen` aller sechs Spuren | keine Codedatei doppelt; Doku-/Testhelfer siehe P3 |

Keine neue [APPROX]-Implementierung in diesem PR; Abnahmekriterien der Spuren unverändert zum früheren Review
(konkrete Messgrößen für „Größenordnungen der Videos“ und Pixelkennzahl bleiben Sache der Implementierungs-PRs).
