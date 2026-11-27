# Review PR #987 – Abweichungsliste v1 (Erstreview)

Geprüft: Base `e0b0ea5`, Head `609ac19` (`git rev-parse HEAD` = Head, `git status --porcelain` leer). Reviewer: Claude Sonnet 5.5, neue Sitzung, nicht am PR beteiligt.
`compare e0b0ea5...609ac19`: genau `docs/analyse/abweichungen-v1.md`, `docs/arbeitspakete.md` – reine Doku, kein Build. Originaldaten (`gamedata/em.exe`) in dieser Sitzung nicht vorhanden: alle `0x…`-Fundstellen nur gegen Repo-Doku und `[ORIGINAL]`-Kommentare geprüft, nicht gegen em.exe.

## 1. Merge-Empfehlung

**Nein für `609ac19`** (Label `review-nein`). Die ID-Zuordnung ist vollständig und korrekt, aber mehrere „ändern“-Vorschläge stützen sich auf falsche oder überzogene Begründungen (B35, B16, B10, S02, B17, C07). Die Liste ist eine Entscheidungsvorlage: Der Nutzer entschiede auf falscher Grundlage. Alles sind Textkorrekturen (P2/P3); danach reicht Delta-Prüfung der Leitstelle (`zusammenarbeit.md` §5 Nr. 4).

## 2. Befunde

Kein P0/P1. Beleg jeweils Befehl im Klon auf dem Head.

**P2-1 · B35: „Meldung 0x46DEF fehlt“ ist veraltet.** Die Meldung ist umgesetzt. A816 in der CSV nennt noch den alten Stand.
- `grep -n "46DEF" src/EishockeyManager.UI/Screens/Team/TransferScreen.cs src/EishockeyManager.UI/ShortMessageTexts.cs` → TransferScreen.cs:210 (`[ORIGINAL] Datei 0x46DEF: Ruhestand angekündigt … nur die Kurzmeldung`), ShortMessageTexts.cs:28 und :59 (`[ORIGINAL]`).
- Übrig bleibt nur A818 („Ihr Angebot wurde angenommen !“, im Original ungeklärt). Vorschlag „ändern“ für B35 also neu begründen (oder „bleibt“/„erst DOSBox“).

**P2-2 · B16: Begründung widerspricht dem Code.** Grund: „Bilanzzeichen weichen bei Pokal- und Play-off-Spielen ab“. ClubInfoWindow.cs:38-41 sagt, der Tabellensatz werde auch im Original nur von Wettbewerbscode 7/8 gefüllt. CSV A753 vermerkt „Gleichheit der Quelle nur argumentiert“. A495 („kann bei Pokal/Play-off abweichen“) und A753 widersprechen sich.
- `grep -n "Tabellensatz\|trägt nur bei" src/EishockeyManager.UI/Screens/Competitions/ClubInfoWindow.cs`; `grep -n "^A753\|^A495" docs/analyse/restliste-v1-klassen.csv`.
- A433 und A754 (Glyph) passen nicht zu „Bilanz/Ligazeile/Spielplan“. „ändern“ nicht belegt → „erst klären“.

**P2-3 · B10: Beschreibung zu weit.** „Paarungs- und Gruppenansichten von Liga, Pokal, WM und Europapokal beginnen immer auf der Startseite“. Liga und WM merken die Seite schon je `GameState`; nur nach dem Laden gilt der Startwert (bewusst, L11). Wirklich fehlend: A268 (CupPairingsScreen) und A269 (Europapokal-Tabelle).
- `grep -n "Nach dem Laden beginnt die Seite" src/EishockeyManager.UI/Screens/Competitions/LeaguePairingsPages.cs` → :23-28 `[ORIGINAL]`/`[APPROX]`-Kommentar; ebenso WorldCupPairingsPages.cs:18.
- A432/A760 (Seite im Spielstand) sind eine andere Entscheidung als A268/A269 → Thema teilen oder Text korrigieren.

**P2-4 · S02: „Ablauf in em.exe belegt“ überzogen.** GoalSceneCall.cs:32-37 und CSV A472 führen den Spieltagsablauf (0x36B5D/0x3901B) als „nicht gelesen“ (Inventur L7). Belegt sind nur Moduswechsel und Hallenstufe (0x568A9–0x568B6). Aufwand M ist für einen Dauermodus über den ganzen Spieltag eher L.
- `grep -n "0x36B5D" src/EishockeyManager.UI/Screens/Live/GoalSceneCall.cs`.

**P2-5 · B17: Aufwand und ID.** `GameState.NationalPanelLineValue` (CupsNational.Strength.cs:56, 0x977F) existiert, wird nur von Tests gerufen; `NationalWidgets.LineValue` (NationalWidgets.cs:69-74) nimmt (K+T)/2. Umleitung wäre eher S als M (sofern Werte belegt). Kommentar CupsNational.cs:208 behauptet fälschlich, die Anzeige nutze es.
- `grep -rn "NationalPanelLineValue" src --include=*.cs` → nur Definition, Tests, Kommentar.
- A961 steht im Code für Forfeit-Zählung (`grep -rn "A961" src --include=*.cs` → MatchEngine.Live.cs:452, MatchGate.cs:110/142/154), in der CSV (Zeile 789) für Reihenwerte → ID im Code doppelt belegt; B17 nennt A961 ohne Hinweis.

**P3 (kurz, je mit Beleg im Klon)**
- **C07:** „sichtbar = ja“ für A098, A924, A925 nicht gedeckt (`grep -E "^A92[45],|^A098," docs/analyse/restliste-v1-klassen.csv` → „nur HOK-Export“, „ohne Spielwirkung“).
- **B28:** Grund „12er-Kodierung in em.exe lesbar“ unbelegt; `grep -n "^A356," docs/analyse/restliste-v1-klassen.csv` → „nicht gelesen“, HistoryScreen.cs:23 ebenso.
- **B29:** Aufwand L gegen M bei B33, obwohl beide „Feld im Spielstand“ (Definition Z. 7: L = Umbau Modell/Ablauf); keine em.exe-Fundstelle für Aufbau des Wertverlaufs (`grep -n "Wertverlauf" docs/analyse/restliste-v1-klassen.csv` → nur A361).
- **B12:** Rechtsklick wird in MainMenuScreen.cs:172-184 vor dem Logo-Klick (:197) als „Abwählen“ verbraucht; Umstellung mehr als eine Zeile.
- **B13:** Fundstelle 0x40872 steht auch für den Vereins-Rechtsklick (`grep -rn "0x40872" src --include=*.cs` → NationalLineupScreen.cs:22, LineupScreen.cs:146); eigene Stelle fürs Nationalteam nicht belegt.
- **B15:** A017 (nur interne Kodierung) und A289 (Messung) tragen „sichtbare Zahlen können abweichen“ nicht; A286 ist laut CSV schon `[ORIGINAL]`.
- **B25:** A437 (Breite der Platzzahl, SeasonEndSummaryScreen.cs:106) gehört nicht zum Thema.
- **B32:** Klasse E mit „bleibt“, aber „schriftliche Nutzerentscheidung fehlt“ (CSV A365) → besser „Entscheidung nötig“ als stiller Vorschlag.
- **B31:** „bleibt“ beruht auf Auslegung von AGENTS.md §4 („bloßes Öffnen“), das Thema betrifft das Verlassen; Original ruft 0x952C beim Verlassen unbedingt (GameState.DailyRun.cs:257-265).
- **K08:** „im Original wählt jeder einen Verein“ als Tatsache; CSV A463: Zweig ohne Wahl „nicht gelesen“.
- **B11:** „offene Grundsatzfrage“, aber A273 trägt Nutzerentscheidung 31.10.2026 (#205), nur im Code (DebCupDrawLive.cs:69).
- **T03:** „sichtbar nein“ zu streng; CSV A661: Zug zeigt sich früher (`grep -n "Remake zieht schon am 13. April" src/EishockeyManager.Core/SeasonFlow.Oberliga.cs` → :84-85).
- **L10:** Verweis `zusatzfunktionen.md` B passt nicht (dort `Autosave.ehm`, wöchentlich; Code `AUTOSAVE.ems` je 10. Zug, AutoSave.cs:20-22); Abweichung trägt `docs/formate/spielstand.md:153`. F056 beruft sich auf „D2 = C (03.11.2026)“ – `grep -rn 'D2 = C' docs | grep -i '03.11'` findet nur die F056-Zeile.

## 3. Ausgeführte Prüfungen

| Prüfung | Befehl / Ergebnis |
|---|---|
| Diff | `compare e0b0ea5...609ac19` → nur `docs/analyse/abweichungen-v1.md`, `docs/arbeitspakete.md`; kein Build, keine Tests nötig |
| ID-Probe (Pflicht) | `python3 tools/reverse-engineering/markerliste.py --abweichungen` auf Base `e0b0ea5` → 581 eindeutige IDs (A 573: K 224, E 323, D 26; F 8: K 5, E 3; gesamt K 229, E 326, D 26). Anhang des PR = generierte Liste (Mengengleichheit). Jede ID genau einmal in genau einem der 100 Themen oder 8 D-Themen (108 Zeilen, 0 Doppelte, 0 fehlende, 0 zusätzliche). **581 = 581 ✔.** Erklärung 572 vs. 573 (Spaltentrennung an „\|“ in A736) plausibel; Stichprobe dort: nicht weiter geprüft |
| Klassen je Thema | je Themenzeile Spalte „Klassen“ gegen Anhang nachgerechnet: 0 Abweichungen |
| Summen | 100 Themen, 56 „sichtbar“, 17 „ändern“ mit 56 IDs, Aufwand S 4 / M 12 / L 1, IDs in Themen 555 + 26 D = 581; Übersichtstabelle stimmt |
| „E ⇒ bleibt“ | Kein Thema mit E-Anteil hat „ändern“; alle 17 „ändern“-Themen sind reine K-Themen |
| Nutzerentscheidungen | N1, N2, N4, N5, R15-E3, R19-E1, E4 = B, #221, „01.11.2026“ in Repo-Doku auffindbar (`restliste-v1.md` Z. 76-88, `docs/archiv/runde-12.md:156`, `docs/transfer.md:186`) |
| Stichprobe | 17 „ändern“-Themen vollständig + 20 weitere (B08, B11, B14, B23, B31, B32, F01, F02, F05, K08, K09, K12, K13, S01, S03, T03, T04, C04, L06, L10) gegen Code/CSV; Befunde oben. Ein im Repo belegter Originalbefund, der bei einem „bleibt“-Thema klar für „ändern“ spräche, wurde nicht gefunden |

**Erwartet offen (bestätigt):** Aufwand S/M/L ist Schätzung (P2-4/P2-5/B29 stellen drei Schätzungen in Frage); veraltete CSV-Texte A973 (`ContractDialog.cs:64,158-163`, bestätigt), A456, A678 sind im PR unter „Hinweise zur Klassenliste“ genannt. **Neu veraltet:** A816 (P2-1) steht dort nicht.

**Akzeptierte `[APPROX]`:** keine neuen (reine Doku). **Annahmen (nicht geprüft):** alle `0x…`-Fundstellen gegen em.exe; Originalverhalten, das nur per Video/DOSBox belegt ist; „sichtbar“ nur gegen Code und CSV beurteilt, nicht im laufenden Spiel.
