# Abweichungsliste vor v1.0 – Entscheidungsvorlage

Runde 24, Spur `abweichungsliste` (R24-D1 = B, R20-D3-Zusatz). Grundlage: `python3 tools/reverse-engineering/markerliste.py --abweichungen` auf `main` `e0b0ea5` (alle Einträge der Restliste außer Klasse X, nach Bereich), unverändert im [Anhang](#anhang-vollliste-generiert). Klassen und Belege: [restliste-v1.md](restliste-v1.md), [restliste-v1-klassen.csv](restliste-v1-klassen.csv).

**So entscheiden:** je Thema „bleibt“ oder „ändern“ (Spalte „Entscheidung“, leer = Vorschlag gilt). „Ändern“ heißt „wie im Original“; die Umsetzung plant die Leitstelle für Runde 25. Klasse **E** folgt einer Nutzerentscheidung, Plattformersatz oder Remake-Erweiterung (Verweis in „Grund“), Vorschlag dort immer „bleibt“; **K** ist ohne Spielwirkung bzw. kosmetisch; **D** wartet auf eine DOSBox-Aufnahme (Abschnitt 4).

**Spalten:** *sichtbar* = der Spieler kann den Unterschied im normalen Spiel sehen oder hören (ja/nein); *Aufwand* für „wie im Original“: **S** bis etwa ein Tag, eine Stelle; **M** eine Funktion, Beleg in em.exe zu lesen oder ein Feld im Spielstand; **L** Umbau von Modell oder Ablauf. Themen bündeln gleiche Funktion bzw. gleichen Bildschirm; Doppelzeilen (z. B. A768/A769, A892/A956) stehen zusammen. Ein Thema steht im Bereich der Mehrzahl seiner IDs, einzelne IDs können aus einem anderen Bereich der Vollliste kommen.

## 0. Entscheidungen des Nutzers (24.11.2026)

Grundlage: Vergleich der Bewertungen von ChatGPT ([PR #992](abweichungen-bewertung-chatgpt.md)) und Gemini
([abweichungen-bewertung-gemini.md](abweichungen-bewertung-gemini.md)) durch die Leitstelle; Klassen je Thema:
[abweichungen-bewertung-claude.md](abweichungen-bewertung-claude.md). Was hier nicht steht, gilt nach Vorschlag.

- **ändern (Runde 25):** die 15 Themen aus Abschnitt 1; zusätzlich **B31** (= A: beim Verlassen wie im Original schreiben, als
  Kern-Ereignis), **B11** (= A: „jede Taste schließt“ wie im Original, Escape und Rechtsklick bleiben), **K07**, **S05**, **S07**
  (niedrige Priorität), **T03** und **S01** (vor v1.0, entgegen „bleibt“).
- **prüfen:** B16 (= A: Wettbewerbscode 0xD0C2 in em.exe lesen, zusammen mit B15; ändern nur bei belegter Abweichung);
  F06, F08 (= A: prüfen, belegte Abweichung ohne Rückfrage wie im Original ändern).
- **aus dem Aufräumkonzept (A4 = A):** Aufräumkonzept K2 (Strafbank beim Bundestrainer), Aufräumkonzept K9 (= Thema B17),
  Aufräumkonzept K11 (Rollenwert +0x18), Aufräumkonzept K13 (Platzwahl 0x29C3B) werden in Runde 25 mit Beleg geklärt
  ([aufraeumen-v1.md](aufraeumen-v1.md) §1.5); Aufräumkonzept K15 bleibt.
- **ins Aufräumen #936:** Themen K04, K11, K13, L11, L12, C02, C08 dieser Liste (L11/C02 nach Aufräumkonzept A1).
- Die Spalte „Entscheidung“ in Abschnitt 1 und 3 bleibt leer; maßgeblich ist dieser Abschnitt (#995).
- **D-Themen:** warten auf DOSBox; D06 bleibt (Nutzerentscheidung 01.11.2026, Computerkader).

**Stand nach Runde 25 (Spur `pruefung-karte`, 24.11.2026; Belege `re-abweichungen.md`, PRs der Spuren gemergt):**

| Thema | Ergebnis | PR |
|---|---|---|
| B01, B07 | umgesetzt (Titelmusik einmal, Trennung an „^“, Hinweis/Zähler wie Original) | #1002 |
| B10 | umgesetzt (gemerkte Seite) | #1003 |
| B11 | umgesetzt (B11 = A: „jede Taste schließt“ nur noch Maus; Escape/Rechtsklick bleiben `[APPROX]`, Nutzer) | #1002 |
| B12 | umgesetzt (Rechtsklick wie Linksklick, kein Abwählen) | #1002 |
| B13 | umgesetzt (Spielerinfo per Rechtsklick in der Nationalaufstellung) | #1004 |
| B15 | Gesicht (A287) und Knopf/Zeile „Historische Ergebnisse“ (A286) umgesetzt; Tore/Spiel, Schnitt (A288, A290) keine Abweichung | #1003 |
| B16 (prüfen) | Wettbewerbscode 0xD0C2 umgesetzt (Knopf, Zeile); Bilanz/Teamsatz (A495, A753) keine Abweichung in der Quelle | #1003 |
| B17 (= Aufräumkonzept K9) | umgesetzt (Reihenwerte über 0x977F) | #1003 |
| B18 | Markierung (A301) und Feld neben dem Kopf (A297) umgesetzt; Kurve (A302) und Lampe (A331) keine Abweichung | #1002 |
| B25, B28 | umgesetzt (Symbol, Pokalbild; Kodierung Platz + 12 · Liga, Pokalbalken) | #1005 |
| B29 | Smiley/Bilanzzeichen (A361) umgesetzt; Managervereine (A699) keine Abweichung; Startwerte (A700) schon erfüllt; kein neues Feld | #1005 |
| B33 | Spielerfenster (A372) umgesetzt; „Gegentore beim Verein“ (A957) keine Abweichung (Original schreibt +0x22 nie) | #1005 |
| B31 | umgesetzt (Verlassen von Aufstellung/Taktik als Kern-Ereignis) | #1004 |
| K07 | umgesetzt (Automatik je Manager, Verletzte, sechster Feldspieler; Schema 64); A165, A807 nur Marker | #1004 |
| S07 | umgesetzt (Einsätze beim Aufstellen im Spiel zählen) | #1004 |
| T03 | umgesetzt (Markt-Verfall je Platz, Oberliga-Zug und Wanderung am 29. April) | #1006 |
| C07 | umgesetzt (Marken, Klick ins Vereinsfenster); A098, A924, A925 nur Marker mit Beleg | #1003 |
| F06 (prüfen) | Abweichung im Geld bestätigt und umgesetzt (Bande je Feld) | #1006 |
| F08 (prüfen) | A509, A512, A526 umgesetzt; A468, A695 keine Abweichung im Ergebnis (nur Marker) | #1006 |
| Aufräumkonzept K2 | umgesetzt (Strafbank beim Bundestrainer) | #1004 |
| Aufräumkonzept K11 | umgesetzt (Rollenwert als Feld, Schema 65) | #1006 |
| Aufräumkonzept K13 | eine gemeinsame Platzwahl; Randfall widerlegt (DGROUP 0x5CF4 nur 0 oder 1), A463 bleibt `[APPROX]` (Erweiterung) | #1006 |
| S01, S02, S04, S05 | Runde 26 (R25-D1 = D) | – |

Belege und Beschlüsse zu den Befunden der Erstreviews: Highscore-Ligabyte (Verdacht #1007) bestätigt und behoben (Beleg
`h1_highscore_liga.py`, PR #1022).

## 1. Themen mit Vorschlag „ändern“ (15 Themen, 44 IDs)

| Nr | Thema | Bereich | IDs | sichtbar | Aufwand | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|
| B01 | Titelmusik in Endlosschleife | Bedienung/Pixel | A002 | ja | S | Original belegt (ein Start, Treiber stoppt am Stückende); Umstellung ist eine Zeile (`loop: false`). |  |
| B07 | Nachrichtenleiste: Umbruch, Hinweis, Zähler | Bedienung/Pixel | A778, A779, A780 | ja | S | Trennung an „^“ ist das belegte Verhalten des Originals; Hinweis „(Klicken)“ und Zähler „n/m“ sind unbelegt. |  |
| B10 | Paarungsseiten Pokal/WM und Europapokal-Tabelle merken die letzte Seite nicht | Bedienung/Pixel | A268, A269 | ja | S | Das Original merkt die Seite (DGROUP 0x5123–0x5127, Datei 0x45862); kleiner Zustand in der Oberfläche, kein Spielzustand. Die Seite nach dem Laden steht getrennt in B38. |  |
| B12 | Hauptmenü: „Tag weiter“ mit rechter Maustaste | Bedienung/Pixel | A296 | ja | M | Das Original nimmt auch die rechte Taste an (Datei 0x45483–0x4551E). Im Remake verbraucht das Hauptmenü den Rechtsklick vorher als „Abwählen“ (MainMenuScreen.cs:179–183, vor dem Logo-Klick :193); die Abfragen müssen umgeordnet werden, vorher in em.exe prüfen, was der Rechtsklick außerhalb des Logos bewirkt. |  |
| B13 | Nationalaufstellung: Spielerinfo per Rechtsklick fehlt | Bedienung/Pixel | A757 | ja | M | Fehlende Funktion, sofern das Original sie auch beim Nationalteam hat: die Fundstelle 0x40872 ist die Rechtsklick-Abfrage der Vereinsaufstellung (LineupScreen.cs:146); eine eigene Stelle für die Nationalaufstellung ist nicht belegt, vorher in em.exe prüfen. Das Infofenster gibt es beim Verein schon. |  |
| B15 | Gegnerfenster: Gesicht, Tore/Spiel, Schnitt, Bilanzzeichen | Bedienung/Pixel | A017, A286, A287, A288, A289, A290 | ja | M | Sichtbar abweichen können Gesicht (A287), Tore/Spiel (A288) und Schnitt (A290); deren Quellen 0x110:0x5C und 0xE0:0x70 sind in em.exe lesbar. A017 ist nur interne Kodierung, A289 Lage nach Bild; bei A286 sind Knopf und Markierung umgesetzt, offen ist nur der Wettbewerbscode 0xD0C2. |  |
| B17 | Nationalteam: Reihenwerte der Anzeige | Bedienung/Pixel | A285, A419, A961 | ja | S | Angezeigte Zahlen weichen ab, keine Spielwirkung. `GameState.NationalPanelLineValue` (0x977F, CupsNational.Strength.cs:56) gibt es schon, nur Tests rufen es; `NationalWidgets.LineValue` nimmt (K+T)/2 – Umleitung an einer Stelle, sofern die Werte von 0x977F belegt sind. |  |
| B18 | Hauptmenü: Kachelmarkierung, Managerkopf, Kurve und Lampe des Bundestrainers | Bedienung/Pixel | A297, A301, A302, A331 | ja | M | Vor allem A302: beim Bundestrainer bleibt das Kurvenfeld leer; aus dem Hauptmenüaufbau in em.exe klärbar. |  |
| B25 | Saisonende: Meistersymbol und Pokalbild | Bedienung/Pixel | A342, A343 | ja | M | Symbol 5 und die Pokalteile aus kb\16 liegen in den Originalgrafiken vor. |  |
| B28 | Historie: Balkenhöhe und -farbe | Bedienung/Pixel | A356, A805, A806 | ja | M | Sichtbare Balken. Die 12er-Kodierung des Abschneidens (A356) und die Formel der Pokalbalken (A806) sind in em.exe nicht gelesen (HistoryScreen.cs:23); „wie im Original“ heißt zuerst lesen, bisher nur Videomesspunkte. |  |
| B29 | Stärken-Bildschirm: Smiley, Managervereine, Startwerte | Bedienung/Pixel | A361, A699, A700 | ja | M | Das Smiley zeigt im Original den Wertverlauf je Verein (StrengthScreen.cs:21); dafür fehlt ein Feld im Spielstand (Migration, wie B33). Wie em.exe den Verlauf aufbaut, ist nicht gelesen (nur A361) – vorher lesen. |  |
| B33 | Spielerfenster und Spielerinfo | Bedienung/Pixel | A372, A957, A958 | ja | M | A957: der Zähler „Gegentore beim Verein“ fehlt (Anzeige „-“); braucht ein Feld im Spielstand. |  |
| S02 | Torszene: Darstellung 2 „Dauer-Eisfeld“ | Live-Spiel | A472 | ja | L | Größte sichtbare Lücke der Torszenen, keine Spielwerte betroffen. Belegt sind nur Moduswechsel und Hallenstufe (Datei 0x568A9–0x568B6); der Spieltagsablauf 0x36B5D/0x3901B ist nicht gelesen (GoalSceneCall.cs:32–37) – Dauermodus über den ganzen Spieltag ist ein Umbau des Ablaufs. |  |
| S04 | Paarungsseiten im Spiel: Minute, Zwischenstände, Penalty-Anzeige | Live-Spiel | A292, A436, F068, F076 | ja | M | Sichtbar fehlende Anzeigen des Originals; Quellen in em.exe bekannt (0x3183C, 0x35CBE). |  |
| C07 | Pokal- und Play-off-Marken, Klick auf Verein | Wettbewerbe | A098, A277, A435, A482, A759, A924, A925 | ja | M | Sichtbar: Punkte am Ergebnis (A277, A435, A482, A759) und fehlender Klick ins Vereinsfenster (A277); A098, A924 und A925 wirken nur im Datenmodell bzw. HOK-Export. Stand vor 0x38402 (DGROUP 0xA0BF) aus em.exe lesbar. |  |

Aufwand der Vorschläge „ändern“: S 4, M 10, L 1.

## 2. Übersicht je Bereich

| Bereich | Themen | davon sichtbar | davon „ändern“ | IDs in Themen | IDs der Vollliste (ohne D) | D-IDs (Abschnitt 4) |
|---|---:|---:|---:|---:|---:|---:|
| Bedienung/Pixel | 38 | 36 | 12 | 137 | 130 | 0 |
| Finanzen | 9 | 6 | 0 | 29 | 36 | 0 |
| Kader/Transfer | 13 | 3 | 0 | 78 | 88 | 11 |
| Laden/Speichern | 12 | 1 | 0 | 160 | 152 | 5 |
| Live-Spiel | 11 | 6 | 2 | 51 | 44 | 4 |
| Tages-/Saisonwechsel | 5 | 2 | 0 | 24 | 29 | 5 |
| Werkzeuge | 1 | 0 | 0 | 7 | 7 | 0 |
| Wettbewerbe | 12 | 4 | 1 | 69 | 69 | 1 |
| **Summe** | **101** | **58** | **15** | **555** | **555** | **26** |

Dazu 8 D-Themen mit 26 IDs (Abschnitt 4).

## 3. Themen je Bereich

### Bedienung/Pixel

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| B01 | Titelmusik in Endlosschleife | A002 | K 1 | Das Remake wiederholt die Titelmusik endlos, das Original spielt sie einmal. | ja | S | **ändern** | Original belegt (ein Start, Treiber stoppt am Stückende); Umstellung ist eine Zeile (`loop: false`). | |
| B02 | Musik: OPL2-Emulation und Abspielorte | A260, A261, A267 | E 3 | Die Musik kommt aus einer nicht bitgenauen OPL2-Emulation und läuft nur im Titelbildschirm. | ja | L | bleibt | Plattformersatz für die Soundkarte; „Musik nur im Titelbildschirm“ ist Nutzerentscheidung #221. | |
| B03 | Geräteeinstellungen, Lautstärke, Zusatzschalter | A001, A469, A481, A516, A763, A802 | E 6 | Geräteeinstellungen, Lautstärkeregler und der Schalter „Ganzer Tag“ sind Zusätze mit eigenem Knopf und Layout. | ja | S | bleibt | Remake-Erweiterung (Lautstärke, Vollbild, Autospeichern, „Ganzer Tag“); Originaldarstellung unberührt. | |
| B04 | Optionen: RAM-Zeile, Speicherprüfung, Hintergrund/Scrolling | A051, A295 | E 1, K 1 | RAM-Zeile und Speicherprüfung fehlen, Hintergrundgrafik und Scrolling sind fest an. | ja | S | bleibt | Plattformersatz: Speicher und Grafikleistung sind kein Engpass mehr. | |
| B05 | Gehaltene Knöpfe und Pfeile: Bildtakt statt Warteschleife | A743, A744, A768, A769, A773 | E 5 | Gehaltene Knöpfe blättern gebremst je Bild statt in der Warteschleife des Originals. | ja | M | bleibt | Plattformersatz: das Original hängt am Rechnertempo bzw. BIOS-Takt; das Remake rechnet in 60-Hz-Bildern. | |
| B06 | Posteingang | A318, A503, A504, A777 | E 2, K 2 | Das Remake hat ein eigenes Posteingangsfenster mit eigener Sortierung und eigenem Aufräumzeitpunkt. | ja | M | bleibt | Remake-Erweiterung ohne Vorbild; die Meldungstexte selbst sind original. | |
| B07 | Nachrichtenleiste: Umbruch, Hinweis, Zähler | A778, A779, A780 | K 3 | Nachrichten werden umbrochen und nach zwei Zeilen abgeschnitten statt an „^“ getrennt; Hinweis und Zähler sind Zusätze. | ja | S | **ändern** | Trennung an „^“ ist das belegte Verhalten des Originals; Hinweis „(Klicken)“ und Zähler „n/m“ sind unbelegt. | |
| B08 | Meldungs- und Kurzmeldungsfenster | A745, A746, A747, A756, A810 | E 1, K 4 | Knöpfe 1 Pixel versetzt, Kurzmeldung mit festem 18,2-Hz-Takt und eigenem Aufbau, lange Meldungen seitenweise; „nicht genug Geld“ bei Freundschaftsspielen als Fenster. | ja | S | bleibt | Lage nach Videobild; Meldungen über vier Zeilen gibt es nur bei Remake-Texten; „nicht genug Geld“ hat im Original keinen eigenen Kurzmeldungsaufruf (FriendliesScreen.cs). | |
| B09 | Bildwechsel: Zwischenbild | A748, A749, A750, A751, F019 | K 5 | Das Zwischenbild steht fest 150 ms mit fester Palette; das zeilenweise Einrollen ist nicht eigens nachgebaut. | ja | S | bleibt | Das Original zeigt das Zwischenbild so lange, wie das Laden dauert – auf heutigen Rechnern kein Gegenstück. | |
| B10 | Paarungsseiten Pokal/WM und Europapokal-Tabelle merken die letzte Seite nicht | A268, A269 | K 2 | Paarungsansicht WM/Pokal (A268) und Europapokal-Tabelle (A269) beginnen beim Öffnen auf der Startseite; Liga- und WM-Paarungen merken die Seite schon je Spiel (LeaguePairingsPages.cs, WorldCupPairingsPages.cs). | ja | S | **ändern** | Das Original merkt die Seite (DGROUP 0x5123–0x5127, Datei 0x45862); kleiner Zustand in der Oberfläche, kein Spielzustand. Die Seite nach dem Laden steht getrennt in B38. |  |
| B11 | Komforttasten Escape, Rechtsklick und „jede Taste schließt“ | A273, A348, A351, A354, A359, A479, A762, A783, A869 | E 1, K 8 | Escape, rechte Maustaste oder jede Taste schließen bzw. verneinen, wo das Original nur die linke Maustaste auswertet. | ja | S | bleibt | Zusätzliche Wege zurück ändern kein Spielergebnis. A273 (Enter/Escape/Rechtsklick im Fragefenster der Auslosung) folgt der Nutzerentscheidung 31.10.2026 (#205, festgehalten in DebCupDrawLive.cs:69); für die übrigen Komforttasten ist die Grundsatzfrage offen (früher A334): wenn „ändern“, alle auf einmal. |  |
| B12 | Hauptmenü: „Tag weiter“ mit rechter Maustaste | A296 | K 1 | „Tag weiter“ reagiert nur auf die linke Maustaste. | ja | M | **ändern** | Das Original nimmt auch die rechte Taste an (Datei 0x45483–0x4551E). Im Remake verbraucht das Hauptmenü den Rechtsklick vorher als „Abwählen“ (MainMenuScreen.cs:179–183, vor dem Logo-Klick :193); die Abfragen müssen umgeordnet werden, vorher in em.exe prüfen, was der Rechtsklick außerhalb des Logos bewirkt. |  |
| B13 | Nationalaufstellung: Spielerinfo per Rechtsklick fehlt | A757 | K 1 | Rechtsklick auf einen Spieler zeigt keine Spielerinfo, Escape führt zurück. | ja | M | **ändern** | Fehlende Funktion, sofern das Original sie auch beim Nationalteam hat: die Fundstelle 0x40872 ist die Rechtsklick-Abfrage der Vereinsaufstellung (LineupScreen.cs:146); eine eigene Stelle für die Nationalaufstellung ist nicht belegt, vorher in em.exe prüfen. Das Infofenster gibt es beim Verein schon. |  |
| B14 | DEB-Pokal-Auslosung live | A270, A272 | K 2 | Das Aufdecken läuft mit festem Tempo ohne rollende Kugel; Fenster und „?“-Felder sind am Video vermessen. | ja | L | bleibt | Ergebnis und Ablauf gleich; die rollende Kugel ist nur aus dem Video nachzubauen. | |
| B15 | Gegnerfenster: Gesicht, Tore/Spiel, Schnitt, Bilanzzeichen | A017, A286, A287, A288, A289, A290 | K 6 | Gesicht immer gelb, Tore/Spiel und Schnitt selbst gerechnet, Zeile „Historische Ergebnisse“ nicht am Wettbewerbscode. | ja | M | **ändern** | Sichtbar abweichen können Gesicht (A287), Tore/Spiel (A288) und Schnitt (A290); deren Quellen 0x110:0x5C und 0xE0:0x70 sind in em.exe lesbar. A017 ist nur interne Kodierung, A289 Lage nach Bild; bei A286 sind Knopf und Markierung umgesetzt, offen ist nur der Wettbewerbscode 0xD0C2. |  |
| B16 | Vereinsfenster: Bilanz, Ligazeile, Spielplan | A433, A495, A753, A754, A755, A867, A868 | K 7 | Bilanz und Tore/Spiel kommen aus den Remake-Ergebnissen, Wettbewerbscode und Ligazeile für Vereine ohne Liga sind genähert. | ja | M | bleibt | Erst klären: laut ClubInfoWindow.cs:38–41 füllt auch das Original den Tabellensatz nur bei Wettbewerbscode 7/8 (0x50283), dann wichen die Bilanzzeichen bei Pokal und Play-off nicht ab; A753 nennt die Gleichheit nur „argumentiert“, A495 widerspricht. A433 (weitere Fensterargumente) und A754 (Glyphe) sind ungeprüft, keine belegte Abweichung. |  |
| B17 | Nationalteam: Reihenwerte der Anzeige | A285, A419, A961 | K 3 | Die Reihenwerte werden als Mittel aus Kondition und Technik statt über 0x977F gezeigt. | ja | S | **ändern** | Angezeigte Zahlen weichen ab, keine Spielwirkung. `GameState.NationalPanelLineValue` (0x977F, CupsNational.Strength.cs:56) gibt es schon, nur Tests rufen es; `NationalWidgets.LineValue` nimmt (K+T)/2 – Umleitung an einer Stelle, sofern die Werte von 0x977F belegt sind. |  |
| B18 | Hauptmenü: Kachelmarkierung, Managerkopf, Kurve und Lampe des Bundestrainers | A297, A301, A302, A331 | K 4 | Gelbe Markierungslinie unbelegt, Klick auf den Kopf wählt ab, Kurvenfeld des Bundestrainers leer, Lampe 0 rot vor der Ernennung. | ja | M | **ändern** | Vor allem A302: beim Bundestrainer bleibt das Kurvenfeld leer; aus dem Hauptmenüaufbau in em.exe klärbar. | |
| B19 | Werbung: Absage, Verlängerungsfrage, Blätterstart | A307, A767, A770, A771 | K 4 | Zeilenaufteilung der Absage und der Verlängerungsfrage sowie der Blätterstart sind angenommen. | ja | S | bleibt | Nur Zeilenaufteilung und Knopftexte; Beleg fehlt (Video), Startindex aus em.exe lesbar. | |
| B20 | Finanzen-Bildschirm: Balken, Kontostand, Kursgrafik | A313, A314, A317, A477, A772, A774 | K 6 | Balkenbegrenzung, Kontostand zentriert, Kurslinie nach Bresenham und Ausschnittstart sind genähert. | ja | S | bleibt | Pixelgenauigkeit am Video abgeglichen; keine Zahl weicht ab. | |
| B21 | Jugend-Bildschirm | A329, A330, A782 | K 3 | Fahnenhintergrund, Abstand der Platzbeschriftung und Rückkehrbedingung sind genähert. | ja | S | bleibt | Nur Farbe, Abstand und Rückkehrklick; am Video abgeglichen. | |
| B22 | Stadion: Klickflächen Anzeigetafel und Sitzkomfort | A781 | K 1 | Die Klickflächen sind an den Tribünen angenommen. | ja | M | bleibt | Erst Sonderflächen 0x3DC59 ff. lesen; ohne Beleg wäre jede Änderung geraten. | |
| B23 | Spieltag-Bildschirm: Tempo und Banner | A458, A478 | K 2 | Eine Spielminute dauert fest 1/3 s; Länder-Freundschaftsspiele zeigen kein Banner. | ja | S | bleibt | Tempo ist Plattformersatz (Rechenzeit); Banner bei Länder-Freundschaftsspielen nur per Video klärbar. | |
| B24 | Tabellenkurve: Lage der Platzzahlen | A784, A785 | K 2 | Lage und Verschiebung der Platzzahlen sind am Bild vermessen. | ja | S | bleibt | Am Bild vermessen; Abweichung höchstens wenige Pixel. | |
| B25 | Saisonende: Meistersymbol und Pokalbild | A342, A343 | K 2 | Für den Meister erscheint der Puck statt Symbol 5; der Pokal wird nicht aus den Einzelteilen zusammengesetzt. | ja | M | **ändern** | Symbol 5 und die Pokalteile aus kb\16 liegen in den Originalgrafiken vor. |  |
| B26 | Spielvorbereitung: Absicherungen | A517, A803 | K 2 | Das Remake fängt zwei im Original nicht erreichbare Startzustände ab. | nein | S | bleibt | Nur im Original unerreichbare Zustände. | |
| B27 | Bestenliste: Farbe, Kennung, Zentrierung | A353, A355, A870 | K 3 | Die Sitzung wird über die Startzeit erkannt; Zeilenfarbe im Menü und Titelgrenze sind genähert. | ja | S | bleibt | Farbe im Menü und Sitzungskennung DGROUP 0x2C4 erst mit DOSBox klären. | |
| B28 | Historie: Balkenhöhe und -farbe | A356, A805, A806 | K 3 | Balkenhöhen je Saison und Pokal sind genähert, die Farbe des Europapokal-Balkens ist ungeprüft. | ja | M | **ändern** | Sichtbare Balken. Die 12er-Kodierung des Abschneidens (A356) und die Formel der Pokalbalken (A806) sind in em.exe nicht gelesen (HistoryScreen.cs:23); „wie im Original“ heißt zuerst lesen, bisher nur Videomesspunkte. |  |
| B29 | Stärken-Bildschirm: Smiley, Managervereine, Startwerte | A361, A699, A700 | K 3 | Das Smiley ist immer gelb; Managervereine zeigen die aktuelle Aufstellung; vor dem ersten Spiel Streuung 0 und Form 50. | ja | M | **ändern** | Das Smiley zeigt im Original den Wertverlauf je Verein (StrengthScreen.cs:21); dafür fehlt ein Feld im Spielstand (Migration, wie B33). Wie em.exe den Verlauf aufbaut, ist nicht gelesen (nur A361) – vorher lesen. |  |
| B30 | Statistik-Bildschirme: Bildlauf und Sortierung | A350, A685 | K 2 | Die Ewige Tabelle läuft je Klick eine Zeile; die Sortierung nach Rolle gilt nur in Listen. | ja | S | bleibt | Nur Schrittweite des Bildlaufs bzw. Sortierung der Oberfläche. | |
| B31 | Aufstellung und Taktik verlassen: nur nach Änderung | A892, A893, A956, A959, A960 | E 3, K 2 | Das Verlassen schreibt Felder nur nach einer Änderung; das Original schreibt sie auch ohne. | nein | M | bleibt | Auslegung: AGENTS.md §4 (Nutzer E4 = B) regelt das bloße Öffnen; das Verlassen ohne Änderung behandelt das Remake gleich (A892, A956). Das Original ruft 0x952C beim Verlassen ohne Bedingung (GameState.DailyRun.cs:257–265); Unterschied nur, wenn die Wanderung 0x9D1B die Felder seit dem letzten Ereignistag bewegt hat. Ausdrückliche Nutzerentscheidung zum Verlassen fehlt – Entscheidung nötig. |  |
| B32 | Aufstellung: Remake-Tasten A und C, Pfeilrichtung der Modi | A365, A373, A953, A955 | E 3, K 1 | Die Aufstellung kennt die Zusatztasten A (automatisch) und C (leeren); die Pfeilrichtung der Taktikmodi ist unbelegt. | ja | S | bleibt (Entscheidung nötig) | Komfort außerhalb des Spiels; im Spiel schon abgeschaltet. Eine schriftliche Nutzerentscheidung zu den Tasten A und C fehlt (A365, inventur.md) – bitte ausdrücklich „bleibt“ oder „ändern“ eintragen. |  |
| B33 | Spielerfenster und Spielerinfo | A372, A957, A958 | K 3 | Gegentore beim Verein fehlen, Sperranzeige ohne Strafbank und Balkenrahmenfarben sind genähert. | ja | M | **ändern** | A957: der Zähler „Gegentore beim Verein“ fehlt (Anzeige „-“); braucht ein Feld im Spielstand. | |
| B34 | Transferdialoge: Lage und Aufbau ohne Videobeleg | A376, A377, A379, A811, A812, A813, A814, A815 | K 8 | Kauf-, Leih-, Verkaufs- und Gebotsdialoge folgen dem Code bzw. der Vertragsverhandlung, nicht einem Videobild. | ja | M | bleibt | Lage aus dem Code; prüfbar erst mit DOSBox-Bildern, dann nur Pixel. | |
| B35 | Transfermarkt: Meldungen | A816, A818 | K 2 | Übrige Transfermeldungen erscheinen als Fenster mit Klick statt als Kurzmeldung (A816); nach Kauf/Leihe erscheint zusätzlich „Ihr Angebot wurde angenommen !“ (A818). | ja | M | bleibt | Erst klären: die Meldung „Dieser Spieler hört am Saisonende auf.“ (0x46DEF) ist umgesetzt (TransferScreen.cs:210, ShortMessageTexts.cs); offen sind nur die Art der übrigen Meldungen (A816) und ob das Original A818 zeigt (Datei 0x47A34) – beides in em.exe nicht gelesen. |  |
| B36 | Transfermarkt: Kontoleiste, Trennlinien, Klickflächen | A724, A817, A819, A820, A821 | K 5 | Einrückung der Beträge, Schattenecken, Trennlinien und die Klickfläche des eigenen Platzes sind genähert. | ja | S | bleibt | Nur Pixel bzw. ein im Original ausgeblendeter Weg. | |
| B37 | Kleine Darstellungsdetails | A278, A437, A447, A764, F033, F166 | K 6 | Ziffernhintergrund, Mindestbreite der Platzzahl am Saisonende, Infodatei-Schatten, Europacup-Termine, Ersatzzeichen und Textkürzung sind genähert. | ja | S | bleibt | Sichtbar gleich bzw. nur Randfälle (Ersatzzeichen, Pfadkürzung im Ordnerdialog); die Breite der Platzzahl (A437) ist an DOSBox-Bildern abgeglichen. |  |
| B38 | Paarungsseiten nach dem Laden | A432, A760 | K 2 | Nach dem Laden beginnen Liga- und WM-Paarungen auf der Startseite statt auf der zuletzt gezeigten Seite. | ja | M | bleibt | Oberflächenzustand bewusst nicht im Spielstand (L11-Vermerk der Klassenliste); das Original behält das globale Byte nur im laufenden Programm (LeaguePairingsPages.cs, WorldCupPairingsPages.cs). |  |

### Finanzen

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| F01 | Ganze DM statt Drittel-DM | A040, A122, A124, A195, A227, A501, A921 | E 5, K 2 | Gehälter, Prämien, Werbeeinnahmen und Gebühren laufen in ganzen DM statt in Drittel-DM. | ja | L | bleibt | Nutzerentscheidung N1 = B; Abweichung unter 1 DM je Betrag. | |
| F02 | Buchung statt direkter Gutschrift | A192, A207, A511, A513, A703, A716 | E 6 | Talentsuche, Bau, Versicherung, Zuschuss, Ablöse und Strafe stehen im Finanzbuch statt ohne Kategorie. | ja | S | bleibt | Nutzerentscheidung N5 = C; Kontostand gleich, Finanzübersicht geprüft (Spur `finanzuebersicht`). | |
| F03 | Finanzbuch und Leihanfrage per Nachricht | A311, A476, A502 | E 3 | Finanzbuch mit Knopf „Buch“ und Leihanfrage an Mitmanager gibt es nur im Remake. | ja | S | bleibt | Remake-Erweiterungen (zusatzfunktionen.md B). | |
| F04 | Werbebewertung: Platzfelder und Sonderfälle | A506, A507, A508, A887 | K 4 | Platzfelder werden später geschrieben, ab 46 nicht geführt; ohne Liga und in der Aufstiegsgruppe zählt das Remake anders. | nein | S | bleibt | Ohne Wirkung im normalen Spiel (Felder ungelesen bzw. nur Modus „nur Weltkugel“). | |
| F05 | Finanzen: erneute Buchung nach Verlassen des Streifens | A775 | E 1 | Das Remake löscht den Betrag beim Verlassen des Streifens; im Original bucht ein späterer Klick ihn erneut. | ja | S | bleibt | Fehler des Originals, bewusst korrigiert (N2 = B). | |
| F06 | Werbeangebot überschreibt Bande | A888 | E 1 | Ein Angebot überschreibt alle Felder eines Bandenvertrags statt nur eines. | ja | M | bleibt | Folge des Datenmodells (eine Bande = ein Vertrag); Änderung bräuchte Felder je Bandenteil. | |
| F07 | Finanzübersicht: Sparbuch und Kredit getrennt | A886 | K 1 | Das Remake führt Sparbuch und Kredit getrennt und zeigt den Saldo. | ja | S | bleibt | Ergebnis gleich, solange nicht beide Konten belegt sind. | |
| F08 | Stadion, Zuschauer und Sommerpause: Reihenfolge | A468, A509, A512, A526, A695 | E 3, K 2 | Baufirmen, Zuschauer-Vorbereitung und Sommerbuchungen laufen in anderer Reihenfolge; unerreichbare Fälle abgefangen. | nein | S | bleibt | Verteilung und Ergebnis gleich; nur Reihenfolge, Buchjahr oder unerreichbare Fälle. | |
| F09 | Monatliche Grundeinnahme: Sturm-Teamwert | A826 | K 1 | Ohne Teamwerte zählen die Abweichungen 0. | nein | S | bleibt | Nur Spielstände vor Schema 12. | |

### Kader/Transfer

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| K01 | Computervereine mit eigenem Kader: Aufbau und Werte | A129, A171, A173, A415, A633, A684, A852, A853, A854, A855, A919, A920, A923, A938, A940, A943, A944 | E 15, K 2 | Computervereine haben echte Spieler mit eigenen Werten; im Original nur Teamwerte und den Spielerpool. | ja | L | bleibt | Nutzerentscheidung 01.11.2026 (jeder Name einmal, Computervereine mit Kadern; transfer.md Teil 5, A395). | |
| K02 | Kader-KI der Computervereine | A233, A234, A235, A236, A237, A240, A252, A430, A431, A862 | E 10 | Verlängerung, Laufzeit, Neuzugänge, Höchstgröße und Pool der Vertragslosen der Computerkader sind Remake-Regeln. | ja | L | bleibt | Remake-Ergänzung R2-12, Folge der Nutzerentscheidung 01.11.2026. | |
| K03 | Spielerpool und Computerkader: Tausch und Namen | A215, A216, A217, A219, A220, A393, A414, A709, A711, A861, A922, A948 | E 12 | Pool und Computerkader tauschen Spieler statt Namen zu überschreiben; Poolplätze werden erst am Saisonwechsel frei. | nein | M | bleibt | Folge der Nutzerentscheidung 01.11.2026 (jeder Name nur einmal im Spiel). | |
| K04 | Hilfsgrößen ohne Gegenstück | A627, A856, A859, A941 | E 3, K 1 | Energie 0–100, Spielwert, Mittel der Teamwerte und der alte Stärkeweg gibt es nur im Remake. | nein | S | bleibt | Interne Größen ohne Spielwirkung; Aufräumen #936 kann tote Wege entfernen. | |
| K05 | Heilung von Spielern ohne Managerkader | A496, A523, A529, A538, A879 | E 2, K 3 | Spieler von Computervereinen und Vertragslose heilen ebenfalls; im Original gibt es sie nicht als verletzte Spieler. | nein | S | bleibt | Ohne Wirkung: im Remake verletzen sich nur Managerspieler. | |
| K06 | Nationalaufstellung: Reihenfolge, Ersatz, Klick | A420, A489, A491 | K 3 | Reihenfolge im Block nach Rückennummer, Ersatz-Automatik beim Lesen, Klick auf verletzt-gesperrten Spieler. | nein | S | bleibt | Regel gleich bzw. Fall tritt im Remake nicht auf. | |
| K07 | Aufstellung: Modell und Automatik | A165, A807, A936, A939, A971, A972 | E 3, K 3 | Automatik je Verein statt je Manager, Verletzte ohne Platz, sechster Feldspieler aus dem Zustand abgeleitet. | nein | M | bleibt | Aufstellungsmodell des Remakes; Wirkung nur in Randfällen (Verletzter spielt Reihe 0 ein). | |
| K08 | Neuer Manager mitten im Spiel | A460, A461, A463, A465, A828 | E 3, K 2 | Ein zufällig zugeteilter Verein wird nicht in die Start-Liga getauscht; Startkader und Platztausch mit Abbruchgrenzen. | ja | M | bleibt | Die Zufallszuteilung ist Remake-Erweiterung: im gelesenen Zweig wählt jeder Manager einen Verein (Datei 0x2964C); der Zweig ohne Wahl (Datei 0x29C3B ff.) ist nicht gelesen (A463). Startkader nach Nutzerentscheidung 01.11.2026; übrige Absicherungen bzw. Altstände. |  |
| K09 | Verkauf an Computerverein und Sponsorzuschuss | A701, A702, A860 | E 3 | Teamwerte des Käufers nach Mittel aus Abwehr und Sturm statt nach der Mauszeile; Zuschuss auch für Vertragslose. | nein | S | bleibt | A701 Fehler des Originals (N2 = B); A702/A860 nur Altstände. | |
| K10 | Unaufgeforderte Angebote | A247, A249, A866 | E 2, K 1 | Bieterstärke nach dem Spieler selbst statt fremdem Pooleintrag; voller Kader gibt den Spieler zu den Vertragslosen. | nein | S | bleibt | A247 Fehler des Originals (N2 = B); A249 Remake-Datenmodell. | |
| K11 | Transfer: Abläufe ohne Oberfläche und interne Kennzeichen | A224, A717, A719, A727, A732, A863, A950, A952 | E 3, K 5 | Ohne Oberfläche antwortet eine Simulation; Kaufzustand nicht gespeichert; Kennzeichen „offene Meldung“ abgeleitet. | nein | S | bleibt | Nur Tests/Werkzeuge ohne Oberfläche oder interne Kennzeichen ohne sichtbare Wirkung. | |
| K12 | Leihrückkehr bei vollem Kader | A116 | K 1 | Hat der Stammverein keine freie Nummer, wird der Spieler vertragslos. | nein | S | bleibt | Original liefert bei vollem Kader einen ungültigen Platz; Fall praktisch unerreichbar. | |
| K13 | Vertragsdialog: Gehalt 0 | A973 | K 1 | Der Kern bekommt 1 DM statt 0 DM; Ergebnis („nicht interessiert“) gleich. | nein | S | bleibt | Der Dialog zeigt schon „0 DM“ (`_salaryZero`, ContractDialog.cs); der Text der Klassenliste („zeigt 1 DM“) ist veraltet. | |

### Laden/Speichern

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| L01 | HOK-Export: Wettbewerbe und Nationalteam | A400, A563, A564, A565, A566, A567, A568, A569, A570, A571, A572, A574, A575, A915 | E 14 | Ungedeutete oder im Remake nicht geführte Pokal-, WM- und Europapokalfelder werden vorbelegt bzw. aus Ständen abgeschrieben. | nein | L | bleibt | Export ist Remake-Erweiterung (E14-1 C, R14-D2 = B); wirkt nur in der Datei für das Original. | |
| L02 | HOK-Export: Liga- und Vereinssätze | A446, A576, A577, A578, A579, A580, A581 | E 7 | Grundwert der Automatik und ungedeutete Vereinsfelder +0x1E–0x25 werden mit Standwerten geschrieben. | nein | M | bleibt | Export ist Remake-Erweiterung (E14-1 C, R14-D2 = B). | |
| L03 | HOK-Export: Kader und Pool | A404, A585, A586, A587, A588, A848, A967 | E 6, K 1 | Rollenwert, Spielerfelder +0x1A/+0x1B, Gegentore und Spieler ohne Poolplatz weichen in der Exportdatei ab. | nein | M | bleibt | Export ist Remake-Erweiterung (E14-1 C, R14-D2 = B). | |
| L04 | HOK-Export: Kopf, Satzfolge und allgemeine Felder | A263, A399, A405, A589, A590, A591, A593, A595, A596, A597, A735, A736 | E 10, K 2 | Spielstand-ID, Füllbytes, Kopf, Monatsbyte, Schalter 0x22A und ungedeutete Blöcke stammen aus Ableitung oder Startwerten. | nein | M | bleibt | Export ist Remake-Erweiterung (E14-1 C, R14-D2 = B); ID und Schlüssel bewusst aus dem Inhalt abgeleitet. | |
| L05 | HOK-Export: Manager, Rekorde, Werbung | A403, A583, A584, A965, A966 | E 3, K 2 | Automatik im Weltmodus, Rekordblock, Bankseite und Vertragsnummern der Werbung sind genähert. | nein | S | bleibt | Export ist Remake-Erweiterung (E14-1 C, R14-D2 = B). | |
| L06 | HOK-Import: Wettbewerbe und Nationalteam | A600, A601, A602, A603, A609, A610, A611, A968 | E 7, K 1 | Frühere Pokalrunden, Auslosungsfolge, WM-K.-o.-Tore und Freundschaftsspielbonus kommen aus dem Nachspielen. | nein | M | bleibt | Ablauf- und Formatgrenzen des Imports (N4 = C); das HOK führt diese Werte nicht. | |
| L07 | HOK-Import: Markt, Werbung und Spielernamen | A604, A605, A606, A849, A969 | E 4, K 1 | Pool-Besitzer, leere Marktplätze, gleichnamige Spieler und Vereine außerhalb der 150 Sätze werden zugeordnet. | nein | S | bleibt | Ablauf- und Formatgrenzen des Imports (N4 = C). | |
| L08 | HOK-Import: Nachspielen und allgemeine Felder | A397, A532, A608, A615, A616, A617, A618, A619, A620, A621, A622, A623, A626, A917, A970 | E 13, K 2 | Was das HOK nicht führt, kommt aus dem Nachspielen nach Remake-Regeln; einzelne Felder werden angenommen. | nein | M | bleibt | Ablauf- und Formatgrenzen des Imports (N4 = C); übernommene Werte sind seit Runde 21 vollständig. | |
| L09 | HOK-Patcher (Werkzeug) | A264, A265 | E 2 | Der Patcher erlaubt weitere Wertebereiche als das Spiel. | nein | S | bleibt | Werkzeug für Proben, nicht Teil des Spiels. | |
| L10 | Laden/Speichern-Dialog und Spielstanddateien | A335, A336, A352, A786, A787, A788, A789, A790, A791, A792, A794, A795, A796, F056 | E 11, K 3 | Ordner statt Laufwerke, *.ems statt *.HOK, Rückfrage beim Überschreiben, eigenes Autospeichern, kein ERROR.HOK. | ja | M | bleibt | Plattformersatz (Ordner statt Laufwerke, eigenes Format *.ems, Rückfragen; docs/formate/spielstand.md, Tabelle „Laden/Speichern“). Autospeichern: das Original schreibt „autosave.hok“ beim 10. Hauptmenü (spielstand.md „Automatisches Speichern“), das Remake `AUTOSAVE.ems` je 10. Zug (AutoSave.cs:20–22). Kein ERROR.HOK nach R12-D2 = C (Nutzer 03.11.2026, docs/archiv/runde-12.md:146). |  |
| L11 | Migrationen älterer Remake-Spielstände | A144, A145, A146, A147, A148, A149, A150, A151, A152, A153, A155, A156, A413, A428, A638, A639, A640, A641, A642, A643, A644, A645, A646, A647, A648, A649, A650, A651, A652, A653, A654, A655, A850, A851, A926, A927, A928, A929, A930, A931, A932, A933, A934 | E 43 | Felder, die ältere Remake-Stände nicht kennen, beginnen mit Startwerten oder werden abgeleitet. | nein | S | bleibt | Betrifft nur alte Remake-Stände; ob sie weiter geladen werden, entscheidet das Aufräumkonzept #936. | |
| L12 | Rückfälle für ältere Remake-Spielstände und Aufbauten | A114, A119, A123, A133, A213, A221, A222, A250, A262, A392, A416, A429, A534, A634, A635, A636, A656, A680, A686, A688, A689, A705, A710, A733, A809, A830, A831, A895 | E 27, K 1 | Fehlt ein gespeicherter Wert, nimmt das Remake einen Ersatz (Mitte der Stufe, 10 % Prämie, Indexfolge …). | nein | S | bleibt | Nur alte Stände, Proben und Aufbauten ohne NewGame.Create; mit #936 bündeln oder entfernen. | |

### Live-Spiel

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| S01 | Live-Spieltag: Paarungen nacheinander statt verschränkt | A442, A450, A454, A541, A542, A547, A554, A555, A766, A909, A954 | E 4, K 7 | Das Remake rechnet die Paarungen eines Spieltags nacheinander; spätere Managerpaarungen erscheinen erst mit ihrem Spiel. | ja | L | bleibt | Umbau des Live-Kerns; sichtbar nur bei mehreren Managerpaarungen, Ergebnisse statistisch gleich. Bitgenau erst mit Zufallsmodus nach v1.0. | |
| S02 | Torszene: Darstellung 2 „Dauer-Eisfeld“ | A472 | K 1 | Bei Darstellung 2 zeigt das Remake nur die einzelne Szene statt den ganzen Spieltag das Eisfeld. | ja | L | **ändern** | Größte sichtbare Lücke der Torszenen, keine Spielwerte betroffen. Belegt sind nur Moduswechsel und Hallenstufe (Datei 0x568A9–0x568B6); der Spieltagsablauf 0x36B5D/0x3901B ist nicht gelesen (GoalSceneCall.cs:32–37) – Dauermodus über den ganzen Spieltag ist ein Umbau des Ablaufs. |  |
| S03 | Torszene: Wiederholung, Bildtakt, Speicher | A439, A470, A471, A473, A474 | E 4, K 1 | Die Wiederholung zeigt dieselbe Szene, Tasten zählen nur während der Szene, 18,2 Bilder/s auf 60 Hz verteilt. | ja | M | bleibt | Neu Würfeln der Wiederholung erst mit bitgenauem Zufallsmodus; Takt und Speichermangel sind Plattformersatz. | |
| S04 | Paarungsseiten im Spiel: Minute, Zwischenstände, Penalty-Anzeige | A292, A436, F068, F076 | K 4 | Während laufender Spiele fehlen „n.Minute“, Zwischenstände und die Farbstreifen des Penaltyschießens. | ja | M | **ändern** | Sichtbar fehlende Anzeigen des Originals; Quellen in em.exe bekannt (0x3183C, 0x35CBE). | |
| S05 | Live-Bildschirm: Reporter und Tasten | A448, A449, A453, A456, A457, A475 | E 2, K 4 | Die Leertaste ist nicht angeschlossen; Reihenfolge der Reportermeldungen in der Minute, Reporterbild und Zuschauerzeile sind genähert. | ja | S | bleibt | Enter/Escape nach Nutzerentscheidung 31.10.2026 (#205), Strahlrücklauf Plattformersatz; A456 nennt den Schützen schon (Code), offen ist nur die Reihenfolge in der Minute; übrige nur Pixel. | |
| S06 | Aufstellung im Spiel: Rechtsklick ohne Reihe | A678 | K 1 | Beim Rechtsklick ohne Reihe folgen Art-Prüfung und Plätze dem Linksklick, ohne eigenen Beleg. | nein | S | bleibt | Remake meldet wie das Original nichts (`ClickPlayerInMatch`); angenommen ist nur, dass Art-Prüfung und Plätze dem Linksklick folgen. | |
| S07 | Einsätze zählen am Spielende | A874, A898 | K 2 | Das Remake zählt Spiele beim Spielende statt beim Aufstellen im Spiel. | ja | S | bleibt | Spalte SP zeigt das Spiel erst nach dem Spiel; Endstand gleich. | |
| S08 | Strafen ohne Spieler und Strafbank-Abfrage | A444, A880, A907, A964 | K 4 | Strafen ohne wählbaren Spieler zählen nur in den Strafminuten; die Nationalaufstellung fragt die Strafbank nicht ab. | nein | S | bleibt | Nur interne Zähler; nach dem Spiel sitzt niemand auf der Strafbank. | |
| S09 | Zusehen und Torhüter draußen | A833, A942 | K 2 | Vereinsfeld 0x16 als IsManaged; Reihe nach „Torhüter draußen“ erst beim nächsten Spiel. | nein | S | bleibt | Gleichwertig für das Spiel; nur der Zeitpunkt des Schreibens weicht ab. | |
| S10 | Bundestrainer: Felder von Satz 5 | A876, A877, A878, A900, A901, A902, A903, A904, A908 | E 3, K 6 | Gehalt, Minutenzähler, eingespielte Reihe und Reihen auf dem Eis des Bundestrainers führt das Remake nicht oder rechnet sie. | nein | M | bleibt | Ohne Leser bzw. ohne Spielwirkung (Datenmodell Satz 5). | |
| S11 | Zufall der Darstellung außerhalb des Stroms | A061, A271, A274, A366, A438, A452 | E 2, K 4 | Zwinkern, Auslosungsanimation, Bestenliste und Preisanzeige ziehen aus eigenen Quellen, damit Zuschauen nichts verändert. | nein | M | bleibt | Nutzerentscheidung N6 = A (Ausnahme bleibt, AGENTS.md §4); mit bitgenauem Zufallsmodus nach v1.0 erneut prüfen. | |

### Tages-/Saisonwechsel

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| T01 | Entlassene Manager (nur ältere Spielstände) | A193, A385, A520, A536, A660, A666, A669, A798, A962 | E 8, K 1 | Entlassene Manager haben keinen Kader, keine Seite und keine Strafe; der Entlassungstext ist unbelegt. | nein | S | bleibt | Das Spiel entlässt nicht mehr; betrifft nur ältere Remake-Stände. | |
| T02 | Spielende und Kopierschutz | A799, A800, F129 | E 3 | Nach dem Spielende kehrt das Remake zum Titel zurück; die Passwortabfrage entfällt. | ja | S | bleibt | Plattformersatz: Rückkehr zum Titel statt DOS, keine Handbuchabfrage. | |
| T03 | Saisonwechsel: Zeitpunkt und Reihenfolge der Würfe | A661, A829, A843, A935 | E 3, K 1 | Wanderung, Ligamischung, Oberliga-Zug und Markt-Verfall laufen in anderer Reihenfolge bzw. zwei Wochen früher. | ja | L | bleibt | Wirkt vor allem auf die Zufallsfolge (A100); sichtbar nur der frühere Oberliga-Zug zwischen 13. und 29. April (A661, SeasonFlow.Oberliga.cs:84–85). Nachziehen mit dem bitgenauen Zufallsmodus nach v1.0. |  |
| T04 | Saisonwechsel: Pool, Markt, Plätze außerhalb der Ligen | A659, A712, A715, A718, A729 | E 4, K 1 | Soll-Spielerzahl ohne Byte-Überlauf, Zielverein nach Rang im Index, Spielzähler auch für Computervereine, neue Listeneinträge, abgeleitete Paare für Plätze 36–41. | nein | S | bleibt | A715 Fehler des Originals (N2 = B); übrige ohne sichtbare Wirkung. | |
| T05 | Saisonende: Kleinigkeiten | A398, A835, A914 | K 3 | Wochentagskorrektur, Sicherung des WM-Schalters und Platztausch „Nur eine Saison“ sind genähert. | nein | S | bleibt | Nicht erreichbar bzw. ohne Wirkung. | |

### Werkzeuge

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| W01 | Abgleich- und Exportwerkzeuge (emtool) | A266, A406, A407, A738, A739, A740, A741 | E 6, K 1 | Toleranzen und Annahmen des HOK-Abgleichs, feste Saat beim Import, Namen der Soundexporte. | nein | S | bleibt | Werkzeuge, nicht Teil des Spiels. | |

### Wettbewerbe

| Nr | Thema | IDs | Klassen | Abweichung | sichtbar | Aufwand | Vorschlag | Grund | Entscheidung |
|---|---|---|---|---|---|---|---|---|---|
| C01 | Zufallsfolge nicht bitgenau | A100, A143, A422, A441, A540, A552, A560, A637, A822, A846, F054 | E 11 | Das Remake zieht aus .NET-Random statt Borland-rand/lrandom; Reihenfolge und Grenzen sind original, die Zahlenfolge nicht. | nein | L | bleibt | R15-D2 = A, N3 = B, Leitstelle (e) Runde 21; bitgenauer Zufallsmodus ist für nach v1.0 vorgemerkt. | |
| C02 | Rückfälle für Altstände: Teamwerte und WM-Mischung | A824, A841, A844, A845, A884, A911 | E 6 | Fehlende Teamwerte bzw. Mischung werden beim ersten Bedarf gezogen. | nein | S | bleibt | Nur Stände vor Schema 12/55 und Aufbauten ohne NewGame.Create; mit #936 bündeln. | |
| C03 | Bildschirm öffnen setzt keine Werte (WM, Freundschaftsspiele) | A279, A486, A488, A883 | E 4 | Energie 70 und Länderwerte werden erst beim ersten Spiel bzw. nach Änderung gesetzt statt beim Öffnen. | nein | S | bleibt | Architekturregel AGENTS.md §4 (Nutzer E4 = B). | |
| C04 | WM-Kader und Länderwerte | A483, A484, A528, A871, A872 | E 2, K 3 | Länderwerte werden an mehr Tagen gerechnet; Grundstärken je Spieler statt je Poolplatz. | nein | S | bleibt | Gelesen werden die Werte nur an WM-Tagen, dort gleich; Kandidatenliste ohne Pool nur Altstände. | |
| C05 | Bestenliste: Kennzahlen und Punkte | A059, A063, A064, A963 | E 2, K 2 | Europapokal-Kennzahl wird geschrieben, Punkte begrenzt, Saisonzahl je Manager, Platzierungsterm 0 im Weltmodus. | ja | S | bleibt | A059 Fehler des Originals (R21-E1 = A, N2); übrige Überlauf bzw. Weltmodus. | |
| C06 | Absicherungen statt Absturz oder Endlosschleife | A069, A211, A417, A423, A515, A550, A693, A776, A889, A890, A947 | E 5, K 6 | Abbruchzähler, Grenzen und Rückgabe 0 statt Endlosschleife oder Division durch null. | nein | S | bleibt | Das Original hinge oder stürzte ab („Divide error“); im normalen Spiel unerreichbar. | |
| C07 | Pokal- und Play-off-Marken, Klick auf Verein | A098, A277, A435, A482, A759, A924, A925 | K 7 | Play-Off-Seiten zeigen keine Marken, Marken nach Verlängerungstor nur nach Video, Klick auf einen Verein führt zurück. | ja | M | **ändern** | Sichtbar: Punkte am Ergebnis (A277, A435, A482, A759) und fehlender Klick ins Vereinsfenster (A277); A098, A924 und A925 wirken nur im Datenmodell bzw. HOK-Export. Stand vor 0x38402 (DGROUP 0xA0BF) aus em.exe lesbar. |  |
| C08 | Alte Spielwege und toter Code | A137, A138, A426, A561, A842 | K 5 | Poisson-Ergebnis, Rückfall-Penaltyschießen und Kreisverfahren bleiben als unbenutzte Wege im Code. | nein | S | bleibt | Ohne Aufrufer im Spielablauf; Entfernen gehört ins Aufräumen #936. | |
| C09 | Spielerstatistik-Felder | A102, A103, A198, A199, A545, A897, A906 | E 2, K 5 | Tore +0x1B, Rollenwert und Spielerreihenfolge folgen dem Remake-Kader statt dem Pool. | ja | M | bleibt | Wirkt nur bei völligem Gleichstand (Topscorer) oder ohne Minuten; Felder des Pools nicht getrennt geführt. | |
| C10 | Tabellen: Sortierung und Platzliste | A159, A161, A164, A657 | E 4 | Die angesehene Teiltabelle beeinflusst die nächste Sortierung nicht; Platzliste aus der Abschlusstabelle. | nein | S | bleibt | Tabellenansicht darf den Spielzustand nicht ändern (AGENTS.md §4); wirkt nur bei völligem Gleichstand. | |
| C11 | Spielrechnung: Feldspielerzahl zu Beginn | A548 | K 1 | Die Feldspielerzahl beginnt bei 2 und 3 statt beim Stand der letzten Neuzählung. | nein | S | bleibt | Leser vor der ersten Neuzählung nicht gefunden. | |
| C12 | Spielvorbereitung: Erweiterungen | A055, A172, A480, A557 | E 4 | Zufallswahl nach Start-Stärke, Schalter DEB-Pokal, Pflicht von Kopf und Verein für neue Manager, Start-Liga je Manager. | ja | S | bleibt | Remake-Erweiterung (Zufallswahl des Vereins, Testschalter); A480 Fehler des Originals (R21-E1 = A). | |

## 4. Klasse D – wartet auf DOSBox

Diese Einträge sind nur mit einer DOSBox-Aufnahme oder einem Video zu belegen (Vormerkung „DOSBox-Aufnahme `runde-22` Sitzungen 2–6 und 7“, [vorlagen/dosbox-aufnahme.md](../vorlagen/dosbox-aufnahme.md)). Kein Vorschlag vor der Aufnahme; danach ordnet die Auswertung sie als „wie im Original“ (kein Eintrag mehr) oder als neues Thema hier ein.

| Nr | Thema | IDs | Offen |
|---|---|---|---|
| D01 | Modus „nur Weltkugel“ | A408, A411, A671 | Kader von Satz 0 ohne Verträge/Training, Poolplätze im Export, Konto 400.000 DM beim Oberliga-Tausch. |
| D02 | Saisonwechsel: Absteiger und Rückkehr gelisteter Spieler | A667, A706 | Absteiger bei doppeltem Platz und Rückkehr bei 29 Spielern sind nicht an einem Stand geprüft. |
| D03 | Poolverteilung und Pool-Plätze | A707, A713, A945, A949 | Gruppen nach Liga statt Indexbereich, Grundstärken von Nationalspielern, Neuanlage bei Alter. |
| D04 | Leihe, Bank und Darlehen zwischen Managern | A402, A613, A629 | Leihende, Sparbuch und Darlehen zwischen Managern nur nach Code bzw. an einem Stand. |
| D05 | Laden: KAPUTT.HOK und Sperren | A599, A625 | Verhalten nach „Der kleine Mogelguru“ unbekannt; Sperren im HOK nicht gefunden. |
| D06 | Talente und Spieler der Computervereine | A128, A131, A132, A427, A630 | Alter, Werte, verstecktes Talent und Namen der Computervereins-Talente ohne belegtes Gegenstück. |
| D07 | Transfer: Meldung „unfair“ und verworfenes Gebot | A117, A259 | Grenze der Meldung nur aus dem Video, Umgang mit nicht ausführbarem Gebot offen. |
| D08 | Live-Spiel: Reihenzahl, Pokalweg, Penalty, Verletzungsmerker, Zählfeld | A095, A896, A910, A912, A918 | Fälle, deren Wirkung nur ein DOSBox-Lauf zeigt (Sitzung 7 in `vorlagen/dosbox-aufnahme.md`). |

## 5. Probe

- Vollliste (Anhang): 581 Tabellenzeilen = 581 eindeutige IDs (K 229, E 326, D 26); davon 573 Markerzeilen `A…` (K 224, E 323, D 26) und 8 Funktionszeilen `F…` der Funktionskarte (K 5, E 3).
- Themen: 101 Themen in Abschnitt 3 und 8 D-Themen in Abschnitt 4 mit zusammen 581 IDs; jede ID der Vollliste steht in genau einem Thema, keine zusätzliche.
- Abgleich mit der Restliste: die `A…`-Zahlen (K 224, E 323, D 26) stimmen mit dem Kopf von `docs/arbeitspakete.md` und `restliste-v1.md` überein. Die 572 (K 223) aus dem Plan-Review #985 kommen von einer Spaltentrennung an jedem „|“: die Zeile A736 enthält im Text „{ \| }“, dadurch fiel ihre ID aus Spalte 4. Die 581 Tabellenzeilen enthalten zusätzlich die 8 `F…`-Zeilen.
- Nachrechnen: `python3 tools/reverse-engineering/markerliste.py --abweichungen 2>/dev/null | grep -oE '\| [AF][0-9]{3} \|$' | sort -u | wc -l` → 581.

## Hinweise zur Klassenliste

Beim Abgleich der Vorschläge mit dem Code fielen veraltete Texte in `restliste-v1-klassen.csv` auf (nicht geändert, Dateigrenze der Spur): **A973** – der Vertragsdialog zeigt schon „0 DM“ (`ContractDialog.cs`, `_salaryZero`); **A456** – vergebene Chancen nennen Schütze und Vorlage (`LiveMatchScreen.cs`), offen ist nur die Reihenfolge in der Minute; **A678** – das Remake meldet beim Rechtsklick ohne Reihe ebenfalls nichts. Die Themen oben folgen dem Code. **A816** – die Meldung 0x46DEF „Dieser Spieler hört am Saisonende auf.“ ist umgesetzt (`TransferScreen.cs:210`, `ShortMessageTexts.cs`), der Beleg „Aufruf 0x46DEF ohne Entsprechung“ ist veraltet. **A495/A753** widersprechen sich (A495: Anzeige kann bei Pokal/Play-off abweichen; A753 und `ClubInfoWindow.cs:38–41`: der Tabellensatz trägt auch im Original nur Code 7/8).

## Anhang: Vollliste (generiert)

Ausgabe von `python3 tools/reverse-engineering/markerliste.py --abweichungen` (stdout) auf `main` `e0b0ea5`, unverändert:

# Abweichungen vom Original (vor v1.0)

## Bedienung/Pixel

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Aufstellung verlassen: Felder nur nach Änderung | Der Bildschirm meldet das Verlassen nur nach einer Änderung (kein Spielzustand durch bloßes Öffnen); ohne Änderung weicht das Original nur ab, wenn die Wanderung 0x9D1B die Felder seit dem letzten Ereignistag bewegt hat. | E | A892 |
| Aufstellung verlassen: nur nach Änderung | Wie A892: Das Verlassen meldet nur nach einer Änderung. | E | A956 |
| Aufstellung: Taste C im Spiel | Die Remake-Taste C gibt es im Spiel nicht (sie würde die Regeln „im Spiel“ umgehen); das Original kennt keine Taste „A“. | E | A955 |
| Finanzen gehaltener Knopf | Die Wartezeiten des gehaltenen Knopfs sind von BIOS-Takten in 60-Hz-Bilder umgerechnet. | E | A773 |
| Fragefenster DEB-Pokal-Auslosung: Tasten | Enter wirkt wie „Aber klar“, Escape und rechte Maustaste wie „Kein Gedanke“. | E | A273 |
| Geräteeinstellungen | Die Geräteeinstellungen (Lautstärke, Vollbild, automatisch speichern) sind eine Remake-Erweiterung mit neuem Layout. | E | A802 |
| Kleiner Knopf | Der schmale Knopf für Remake-Erweiterungen ist aus dem Knopf kb\\2 zusammengesetzt. | E | A481 |
| Lautstärkeregler | Das Remake hat Lautstärkeregler 0–10, die die Amplitude linear skalieren; das Original kennt keine Regler. | E | A001 |
| Meldungsfenster mit mehr als vier Zeilen | Meldungen mit mehr als vier Zeilen gibt es nur im Remake; sie erscheinen seitenweise. | E | A810 |
| Musik nur im Titelbildschirm | Musik läuft nur im Titelbildschirm; ob das Original sie an weiteren Stellen spielt, ist nicht geprüft. | E | A267 |
| Nationalaufstellung: Tasten A und C | Wie beim Verein gibt es die Remake-Tasten A und C im Spiel nicht. | E | A953 |
| OPL2-Hüllkurve | Der Anstieg der Hüllkurve ist exponentiell mit genäherter Schrittweite statt chipgenau berechnet. | E | A261 |
| OPL2-Klangerzeugung | Die Musik wird mit einer nicht bitgenauen OPL2-Emulation erzeugt (Attack genähert, Timer/CSM fehlen). | E | A260 |
| Optionen: RAM-Zeile, Speicherprüfung, Hintergrund/Scrolling | RAM-Zeile und Speicherprüfung entfallen, Hintergrund/Scrolling sind fest an. | E | A295 |
| Optionen: Zusatzknopf „Gerät“ | Der Knopf zu den Geräteeinstellungen ist eine Remake-Erweiterung an frei gewählter Stelle. | E | A469 |
| Optionen: Zusatzschalter „Ganzer Tag“ | Der Zusatzschalter ist eine Remake-Erweiterung, seine Lage ist frei gewählt. | E | A763 |
| Pfeilknöpfe (Halten ≠ 0) | Der Pfeilknopf blockiert beim Halten nicht wie die Warteschleife des Originals, sondern liefert ±1 im Bild, das das Loslassen sieht. | E | A743 |
| Pfeilknöpfe (Loslassen zwischen zwei Bildern) | Ein neuer Klick während gehaltener Richtung wird als Loslassen und neues Drücken gewertet. | E | A744 |
| Posteingang | Der Posteingang ist ein Fenster des Remakes ohne Vorbild, sein Aussehen ist angelehnt. | E | A318 |
| Posteingang | Ein eigenes Posteingangsfenster hat das Original nicht, die Texte gehören zur Nachrichtenleiste. | E | A777 |
| Taktik verlassen: nur nach Änderung | Das Verlassen schreibt Vereinsfeld 0x26 nur nach einer Änderung von Modus, Einsatz oder Angriff; das Original schreibt auch ohne. | E | A959 |
| Werbung Blättern bei gehaltener Taste | Das Remake bremst das Blättern bei gehaltener Taste, das Original blättert so schnell, wie der Rechner läuft. | E | A768 |
| Werbung Blättern bei gehaltener Taste | Wie A768: Bremse bei gehaltener Taste statt ungebremstem Blättern. | E | A769 |
| 0x0AF8D Bildstreifen kopieren/einrollen | Das zeilenweise Einrollen von Bildstreifen ist nicht als eigene Funktion nachgebaut; die Bildschirme zeichnen selbst. | K | F019 |
| 0x0BE71 Zeichen auf darstellbar prüfen (Ersatzliste) | Nicht darstellbare Zeichen werden im Remake ausgelassen statt über die Ersatzliste ersetzt. | K | F033 |
| 0x5E8B0 Text auf Höchstbreite kürzen (Speichern/Laden) | Das Original kürzt den Text hinten bis zur Höchstbreite, das Remake kürzt den Ordnerpfad vorn. | K | F166 |
| Angebotsfenster: Lage | Die Lage des Angebotsfensters stammt nur aus dem Code, ohne Videobeleg. | K | A377 |
| Angebotsfenster: Rahmen | Für das Angebotsfenster gibt es kein DOSBox-Bild; Lage aus dem Code 0x490DE. | K | A812 |
| Anzeigeziffern Stil 0 | Die Ziffer Stil 0 kopiert den Hintergrund aus dem Zielbild statt aus dem Hintergrundpuffer DGROUP 0xC12B; sichtbar gleich. | K | A447 |
| Aufstellung Nationalteam: Rechtsklick | Die Spielerinfo auf Rechtsklick fehlt, Escape führt zurück. | K | A757 |
| Bestenliste am Spielende: Zeilenfarbe | Der Eintrag der Sitzung steht am Spielende im Original in Farbe 10 (130, 211, 227), im Remake bis #882 in Farbe 7; im Menü behält das Remake Farbe 7, weil die Herkunft der Sitzungskennung DGROUP 0x2C4 nicht geprüft ist. | K | A870 |
| Bestenliste: Schließen | Jede Taste und jeder Klick führt zurück. | K | A354 |
| Bestenliste: Sitzungskennung | Statt DGROUP 0x2C4 dient die Startzeit der Sitzung als Kennung für die weiße Hervorhebung. | K | A353 |
| Bestenliste: Titelzentrierung | Die rechte Grenze der Titelzentrierung ist 321 statt 320, um das Video zu treffen. | K | A355 |
| Bildschirm Stärken vor dem ersten Spiel | Vor dem ersten Spiel eines Spielstands zeigt der Bildschirm Streuung 0 und Form 50, weil die Teamwerte noch nicht gezogen sind. | K | A700 |
| Bildschirm Stärken: Managervereine | Die Stärken der Managervereine kommen aus der aktuellen Aufstellung statt dem Stand des letzten Tageslaufs. | K | A699 |
| Bildwechsel: Anzeigedauer | Die Anzeigedauer des Zwischenbilds ist fest 150 ms. | K | A750 |
| Bildwechsel: Dauer des Zwischenbilds | Das Zwischenbild beim Bildschirmwechsel steht eine feste Zeit statt so lange wie der Aufbau im Original. | K | A748 |
| Bildwechsel: Palette des Zwischenbilds | Das Zwischenbild nutzt die Palette von kb\\22 bzw. kb\\21 statt der des zuletzt geladenen Bilds. | K | A749 |
| Bildwechsel: Seitenwechsel nach Laden | Als „nach dem Laden“ gilt im Remake nur ein Spiel aus dem Bildschirm Laden/Sichern, jeder andere Spielwechsel als Spielstart. | K | A751 |
| DEB-Pokal-Auslosung live: Tempo | Das Aufdecken läuft mit festem Tempo (etwa ein Platz je Sekunde) und ohne die rollende Kugel des Originals. | K | A272 |
| Europacup-Gruppenbild: Termine | Termine werden gezeigt, solange in der Gruppe nichts gespielt ist; die genaue Bedingung im Code ist nicht ganz aufgelöst. | K | A278 |
| Ewige Tabelle: Bildlauf | Der Bildlauf geht je Klick eine Zeile, die Schrittweite des Originals ist nicht gelesen. | K | A350 |
| Fair-Play-Tabelle: Schließen | Auch Tasten schließen die Tabelle, das Original wartet nur auf die Maus. | K | A351 |
| Finanzen Betragsbalken | Die Balkenlänge wird für die Zeichnung auf 0–154 begrenzt. | K | A772 |
| Finanzen Klick auf den Balken | Ein Klick in die andere Hälfte zeichnet keinen Balken. | K | A774 |
| Finanzen Kontostand | Der Kontostand steht zentriert wie im Video, das Original füllt die Zahl auf Breite 9 auf. | K | A477 |
| Finanzen Kursgrafik Ausschnitt | Ohne sichtbaren Punkt beginnt der Ausschnitt bei 0, im Original mit einem zufälligen Stapelwert. | K | A314 |
| Finanzen Kurslinie | Die Pixelschritte der Kurslinie folgen Bresenham, die Linienroutine des Originals ist nicht geprüft. | K | A317 |
| Finanzen Regler Streifen und Marke | Streifen und Marke bleiben bis zum Verlassen stehen; ob das Original sie beim Neuzeichnen entfernt, ist nicht geprüft. | K | A313 |
| Frage Sponsorzuschuss: Fenster | Fensteraufbau und Titel der Frage nach dem Sponsorzuschuss sind ohne Videobeleg. | K | A813 |
| Frage an den Verkäufer: Fenster | Das Fragefenster an den verkaufenden Manager ist ohne Videobeleg. | K | A814 |
| Frage „Spiel beenden“ | Escape und rechte Maustaste wirken als Komfort wie „Kein Gedanke“, das Original wertet nur die linke Taste aus. | K | A783 |
| Fragefenster DEB-Pokal-Auslosung | Lage des Fensters und der „?“-Felder am Video vermessen, die „?“-Grafik ist gezeichnet. | K | A270 |
| Freundschaftsspiele: Meldung „nicht genug Geld“ | Die Meldung bleibt ein Meldungsfenster statt einer Kurzmeldung. | K | A756 |
| Gegnerfenster: Gesicht | Das Gesicht neben der Flagge bleibt immer gelb. | K | A287 |
| Gegnerfenster: Lage des Gesichts | Lage des Gesichts am Bild vermessen. | K | A289 |
| Gegnerfenster: Schnitt | Der Schnitt wird mit einer abgeschnittenen Nachkommastelle gezeigt, ohne Beleg der Rundung. | K | A290 |
| Gegnerfenster: Tore/Spiel | „Tore / Spiel“ wird im Remake selbst als Tore·10/Spiele berechnet. | K | A288 |
| Hauptmenü Bundestrainer: Tabellenkurve | Beim Bundestrainer bleibt das Kurvenfeld leer. | K | A302 |
| Hauptmenü: Klick auf Managerkopf | Das Remake wählt beim Klick auf den Managerkopf die Kachel ab. | K | A297 |
| Hauptmenü: Markierung der großen Kachel | Die Markierung der gewählten großen Kachel ist eine gelbe Linie ohne Beleg. | K | A301 |
| Hauptmenü: Tag weiter | Das Remake nimmt nur die linke Maustaste an, das Original auch die rechte. | K | A296 |
| Historie: Balkenfarbe Europapokal | Der Europapokal-Balken hat ungeprüft die Farbe des DEB-Pokals. | K | A805 |
| Historie: Balkenhöhe | Die Balkenhöhe je Saison ist aus dem Abschneiden genähert, die 12er-Kodierung ist nicht gelesen. | K | A356 |
| Historie: Balkenhöhe Pokale | DEB-Pokal 3 Pixel je Runde nach einem Messpunkt, Europapokal geschätzt. | K | A806 |
| Infodatei-Anzeige | Textschatten schwarz 1 Pixel rechts unten statt Schatten 0, Dateigrößeprüfung entfällt. | K | A764 |
| Jugend Fahnen | Die Hintergrundfarbe 4 der Fahnen ist nach dem Video durchsichtig, der Blit-Parameter 0xC0 ist nicht entschlüsselt. | K | A329 |
| Jugend Platz-/Ligabeschriftung | Der Abstand zwischen „n.Platz“ und „m.Liga“ ist gemessen statt über die Feldbreite 2 gesetzt. | K | A330 |
| Jugend Rückkehr | Ein Klick außerhalb der Förderzeilen kehrt zurück; die Abbruchbedingung des Originals ist nicht gelesen. | K | A782 |
| Kaderliste nach Rolle | Die Sortierung nach Rolle und Nr. gilt im Remake nur für Listen der Oberfläche. | K | A685 |
| Kauf/Leihe: Eingabe des Angebots | Das Remake schreibt den Vorgabebetrag vor die Eingabemarke, das Original zeigt nur die Marke. | K | A815 |
| Kurzmeldung: Anzeigedauer | Die Dauer der Kurzmeldung rechnet mit 18,2 Hz BIOS-Takt; ob das Original den Zähler beschleunigt, ist offen. | K | A746 |
| Kurzmeldung: Aufbau | Die Kurzmeldung wird wie ein Meldungsfenster ohne Zeilen 3–5 mit dem Managernamen als Überschrift gezeichnet. | K | A747 |
| Ligapaarungen: Vereinsfenster | Weitere Argumente des Vereinsfensters (Lage y &gt; 140, Wert 1) wertet das Remake nicht aus. | K | A433 |
| Ligapaarungen: gemerkte Seite | Nach dem Laden beginnt die Seite mit dem Startwert statt mit dem zuletzt gezeigten Wert. | K | A432 |
| Länder: Bilanzzeichen | Die Bilanzzeichen der Länder speichert das Remake in eigener Kodierung (1/2/3). | K | A017 |
| Länderspiel-Mannschaftsfeld: Reihenwerte | Die Reihenwerte werden als Mittel aus Kondition und Technik statt aus der Aufstellungsfunktion des Originals gezeigt. | K | A285 |
| Meldungs-/Fragefenster: Knopflage | Die Knöpfe werden 1 Pixel weiter links gezeichnet als im Code des Originals (nach Videobild 3:46). | K | A745 |
| Menülampe 0 beim Bundestrainer | Vor der Ernennung ist Lampe 0 rot wie bei leerem Kader. | K | A331 |
| Nachrichtenleiste Hinweis bei Frage | Der Hinweis „(Klicken)“ bei einer Frage ist nicht belegt. | K | A778 |
| Nachrichtenleiste Hinweis und Zähler | Hinweis bei einer Frage und Zähler „n/m“ bei mehreren Nachrichten sind nicht belegt. | K | A780 |
| Nachrichtenleiste Zeilen | Die Nachrichten werden umbrochen und nach zwei Zeilen abgeschnitten statt an „^“ getrennt. | K | A779 |
| Nationalteam: Reihenwerte in der Anzeige | Die Reihenwerte der Anzeige rechnet das Remake als Mittel aus Kondition und Technik, das Original über 0x977F. | K | A419 |
| Optionen Hintergrundgrafik und Scrolling | Die Optionen Hintergrundgrafik und Scrolling führt das Remake nicht. | K | A051 |
| Paarungsansicht WM/Pokal | Rechtsklick/Esc führen zurück, gedrückte Pfeile werden nicht gezeigt und die zuletzt gewählte Seite wird nicht gemerkt. | K | A268 |
| Pokalbild zum Saisonende | Der Pokal wird nicht aus den Einzelteilen von kb\\16 zusammengesetzt, Lage und Farben der Schrift sind geschätzt. | K | A343 |
| Posteingang: Sortierung | Den Vorrang im Sortierschlüssel setzt das Remake auf 0, die Aufrufer im Original sind nicht ausgewertet. | K | A504 |
| Posteingang: beendete Angebote | Das Remake entfernt Meldungen zu beendeten Angeboten erst im nächsten Tagesabschluss statt sofort. | K | A503 |
| Saisonende: Breite der Platzzahl | Die Mindestbreite 5 der Platzzahl ist an DOSBox-Bildern abgeglichen, nicht im Code gesetzt gefunden. | K | A437 |
| Saisonende: Meistersymbol | Für den Meistertitel zeigt das Remake den Puck statt Symbol 5. | K | A342 |
| Spiele laufen: Tempo | Das Remake zeigt je Spielminute fest 1/3 s statt der Rechenzeit des Originals. | K | A478 |
| Spielerfenster: Gegentore beim Verein | Einen Zähler der Gegentore beim Verein führt das Remake nicht („-“). | K | A957 |
| Spielerhinweis: Sperre ohne Strafbank | Außerhalb der Strafbank zeigt das Remake die verbleibenden Spiele aus SuspendedGames (im Original kommt Status 1 ohne Strafbank nicht vor). | K | A958 |
| Spielerinfo: Rahmenfarben der Balken | Die Rahmenfarben 2/3 und 197 der Balken sind am Videobild geschätzt. | K | A372 |
| Spielerstatistik: Escape | Escape verlässt den Bildschirm wie die rechte Maustaste. | K | A359 |
| Spielplan des Vereins: Esc | Esc schließt wie der Rechtsklick; der Zähler 0x7A9 = 0 (Rückgabe 0 ohne Fenster, Datei 0x5CEC) kommt im Remake nicht vor. | K | A869 |
| Spieltag: Banner bei Länder-Freundschaftsspiel | Länder-Freundschaftsspiele zeigen kein Banner. | K | A458 |
| Spielvorbereitung (Hinzufügen): Escape | Escape kehrt im Hinzufügen-Modus ohne Änderung ins Hauptmenü zurück. | K | A479 |
| Spielvorbereitung: Start nur Weltkugel | Ein Restfall (zweiter, unbenannter Manager vor dem Abschalten der Fahne) wird ohne Meldung abgefangen. | K | A803 |
| Spielvorbereitung: „Los jetzt!“ | Das Remake verweigert den Start in einem im Original nicht erreichbaren Zustand. | K | A517 |
| Stadion Klickflächen Anzeigetafel/Sitzkomfort | Die Klickflächen für Anzeigetafel und Sitzkomfort sind an den Tribünen angenommen und werden nach den Teilen geprüft. | K | A781 |
| Stärkestatistik: Smiley | Das Smiley ist immer gelb, weil das Remake keinen Wertverlauf je Verein führt. | K | A361 |
| Tabelle Europapokal (Kachel) | Das Remake zeigt beim Öffnen immer Gruppe 1 statt der zuletzt gezeigten Seite. | K | A269 |
| Tabellenkurve: Platzzahl | Lage der Platzzahl und Verschiebung bei nahen Kurven am Bild vermessen. | K | A784 |
| Tabellenkurve: Verschiebung der Gegnerzahl | Die Platzzahl des Gegners rückt auf 7 Pixel Abstand, nicht pixelgenau belegt. | K | A785 |
| Taktik verlassen: Bundestrainer und Live-Spiel | Das Verlassen schreibt Vereinsfeld 0x26 nur für den Vereinsbildschirm außerhalb des Spiels; der Bundestrainer hält eigene Werte (DGROUP 0xCAE9), der Eingriff im Live-Spiel führt die Werte im LiveTeam. | K | A893 |
| Taktik verlassen: Reihenfolge der Schreibvorgänge | Die Reihenfolge der beiden Schreibvorgänge nach dem Schalter „automatisch aufstellen“ ist nicht gelesen; sie berühren verschiedene Felder. | K | A960 |
| Taktik: Pfeilrichtung bei den Modi | Welche Pfeilrichtung bei den Modi vor- bzw. zurückschaltet, ist nicht belegt. | K | A373 |
| Talentsuche läuft schon | kein Unterschied im Spiel: Meldung nur für einen Weg, den das Original nicht anbietet (Knopf ausgeblendet). | K | A724 |
| Titelmusik | Das Original spielt die Titelmusik einmal und bleibt dann still; das Remake wiederholt sie in Endlosschleife. | K | A002 |
| Tormeldung: Reihenfolge | Das Remake rechnet die Paarungen nacheinander und zeigt die Meldungen vor dem Halt an der Paarung mit Reihe nach den Meldungen des Reporters in ihrer Minute. | K | A954 |
| Transferdialog: Eingabezeile | Die Lage der Eingabezeile unter „Ihr Angebot:“ ist aus dem Bild geschätzt. | K | A811 |
| Transferdialoge: Lage und Aufbau | Lage und Aufbau der Kauf-, Leih-, Verkaufs- und Gebotsdialoge sind nur für die Talentsuche im Video belegt. | K | A376 |
| Transfermarkt: Art der Meldungen | Die übrigen Transfermeldungen erscheinen als Fenster mit Klick statt als Kurzmeldung; für sie ist die Art im Original nicht gelesen. | K | A816 |
| Transfermarkt: Dialoge | Kauf-, Verkaufs- und Gebotsdialoge sind im Video nicht zu sehen und folgen der Vertragsverhandlung. | K | A379 |
| Transfermarkt: Kontoleiste Beträge | Die Beträge der Kontoleiste rücken fest ein statt nach der Zahlformatierung des Originals. | K | A819 |
| Transfermarkt: Meldung nach Kauf | Das Remake zeigt nach Kauf/Leihe zusätzlich „Ihr Angebot wurde angenommen !“. | K | A818 |
| Transfermarkt: Schattenecken der Kontoleiste | An drei Schattenecken bleibt der Hintergrund stehen statt der wechselnden Farben im Bild. | K | A820 |
| Transfermarkt: Trennlinien | Das Remake vergisst überzeichnete Trennlinien, sobald sich die Liste ändert, statt die Liste neu aufzubauen. | K | A821 |
| Transfermarkt: eigener Marktplatz | Die Klickfläche des eigenen Marktplatzes ist wie Kauf/Leihe der fremden Plätze angenommen. | K | A817 |
| Vereinsfenster: Bilanzzeichen | Die Bilanzzeichen der Vereine führt das Remake nicht, das Vereinsfenster rechnet sie aus den Ergebnissen. | K | A495 |
| Vereinsfenster: Text „keine Informationen“ | Das erste Zeichen der dritten Zeile (Byte 0xE4) wird als „Ä“ gezeigt, die Glyphe ist nicht geprüft. | K | A754 |
| Vereinsstatistik: Schließen | Ein Klick oder eine Taste schließt, statt dass das Original nach dem Zeichnen zurückkehrt. | K | A348 |
| WM-Gruppentabelle: Escape | Escape führt als Komfort des Remakes zurück, das Original prüft keine Tasten. | K | A762 |
| WM-Paarungen: gemerkte Seite | Nach dem Laden beginnt die Seite mit dem Startwert statt mit dem zuletzt gezeigten Wert. | K | A760 |
| Werbung Absage-Meldung | Die vier Texte der Absage-Meldung stehen je in einer Zeile, die Zeilenaufteilung ist nicht belegt. | K | A307 |
| Werbung Blättern aus dem Vertragsmodus | Aus dem Vertragsmodus beginnt das Blättern am jeweiligen Ende der Liste; ob das Original so startet, ist nicht belegt. | K | A770 |
| Werbung Verlängerungsfrage | Zeilenaufteilung und Knopftexte der Verlängerungsfrage sind aus den Texten zusammengesetzt, nicht aus dem Video. | K | A771 |
| Werbung Verlängerungsfrage Knöpfe | Die Knopftexte „Ja“/„Nein“ der Verlängerungsfrage sind angenommen. | K | A767 |

## Finanzen

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Ablöse am Saisonende | Die Ablöse wird als Transfereinnahme gebucht, das Original schreibt sie ohne Buchung aufs Konto. | E | A716 |
| Baufirma wieder versöhnt | Das Remake würfelt nur für beleidigte Baufirmen statt für alle 11 Projekte; die Verteilung bleibt gleich. | E | A512 |
| Börse ohne Spielstart | Ohne den Schritt beim Spielstart (Import eines Originalspielstands, von Hand gebaute Spielstände) entsteht die Börse beim ersten Zugriff aus dem Strom. | E | A831 |
| Börse: Höchstwert „kaufen“ bei Kurs 0 | Bei Kurs 0 liefert das Remake 0 statt wie das Original mit „Divide error“ abzustürzen. | E | A417 |
| Börsengebühr | Das Remake rechnet die Gebühr in ganzen DM und verwirft den Drittel-DM-Rest, das Original zieht ihn mit ab. | E | A501 |
| Finanzbuch | Das Finanzbuch ist eine Erweiterung des Remakes ohne Vorbild im Original. | E | A311 |
| Finanzen Aktienkauf bei Kurs 0 | Bei Kurs 0 liefert das Remake Höchstbetrag 0, das Original würde durch null teilen. | E | A776 |
| Finanzen Buchung nach Verlassen des Streifens | Das Remake löscht den Betrag beim Verlassen des Streifens, im Original bleibt er stehen und ein späterer Klick in die Buchungszeile bucht ihn erneut mit der dann gültigen Auswahl. | E | A775 |
| Finanzen Knopf „Buch“ | Der Knopf „Buch“ zum Finanzbuch ist eine Remake-Erweiterung. | E | A476 |
| Gehalt bei Verhandlung am Saisonende | Gehälter werden in ganzen DM statt Drittel-DM geführt. | E | A227 |
| Gehaltsforderung in der Verhandlung | Das Remake speichert Gehälter in ganzen DM und rundet die Forderung ab, das Original rechnet in Drittel-DM. | E | A124 |
| Leihanfrage an Mitmanager | Die Leihanfrage per Nachricht gibt es im Original nicht. | E | A502 |
| Sommerpause | In der Sommerpause buchen die Bücher in der neuen Saison und Bau, Tagesabschluss und Heilung laufen für alle Manager nacheinander statt verschränkt. | E | A526 |
| Sponsorzuschuss gutschreiben | Der Zuschuss wird als Transfereinnahme gebucht, das Original schreibt ihn ohne Buchung aufs Konto. | E | A703 |
| Stadionausbau: Buchung | Das Remake bucht den Baupreis im Finanzbuch „Bau“, das Original zieht ihn ohne Kategorie ab; Kontostand gleich. | E | A511 |
| Strafe entlassener Manager | Entlassene Manager zahlen keine Strafe für zu wenige Spieler. | E | A193 |
| Strafe „weniger als neun Spieler“ | Die Meldung kommt in den Posteingang statt als Fenster, der Abzug steht im Finanzbuch unter „Strafen“. | E | A192 |
| Talentsuche: Platzwurf ohne freien Platz | Ohne freien Platz zieht das Remake nicht (das Original hinge). | E | A890 |
| Talentsuche: Preis | Das Remake bucht den Preis der Talentsuche als Ausgabe, das Original zieht ihn direkt vom Konto ab; Kontostand gleich. | E | A513 |
| Talentsuche: kein freier Pool-Platz | Hat der Pool keinen freien Platz (Original: Endlosschleife) oder keinen Pool (Stände vor Schema 22), erzeugt das Remake einen neuen Spieler. | E | A889 |
| Versicherungsangebot | Das Remake rechnet die angebotene Prämie in ganzen DM (abgerundet) statt in Drittel-DM wie das Original. | E | A122 |
| Versicherungsleistung | Die Leistung wird als Einnahme der Kategorie Versicherung gebucht, das Original schreibt sie ohne Buchung aufs Konto. | E | A207 |
| Werbeangebot überschreibt kündbaren Vertrag (Bande) | Das Remake führt eine Bande mit mehreren Feldern als einen Vertrag; das Angebot überschreibt dort alle Felder dieses Vertrags. | E | A888 |
| Werbeeinnahmen am Monatsletzten | Das Remake summiert die Werbeeinnahmen in ganzen DM, das Original in Drittel-DM (Rundung bis 1 DM je Vertrag). | E | A040 |
| Abrechnung ohne Vorbereitung | Nicht vorbereitete Spiele prüft die Abrechnung nach dem Spiel statt davor. | K | A695 |
| Anzeige „Benötigter Schnitt“ | Ausgaben und Einnahmen gehen in DM statt in Drittel-DM in die Rechnung ein, der angezeigte Schnitt kann um wenige Zuschauer abweichen. | K | A195 |
| Export Bankseite als Summe | Satz 4 +0x18F schreibt die Summe der Bankeinträge der Manager; belegt ist nur je ein Manager mit Bankeintrag. | K | A965 |
| Export Vertragsnummern Werbung | Nummern 1 … n in der Folge der Verträge, 0x17C = n + 1; die tatsächliche Reihenfolge des Originals ist nicht belegt. | K | A966 |
| Finanzübersicht: Σ Darlehen und Monatszins | Das Remake führt Sparbuch und Kredit getrennt; als Saldo gilt Sparbuch − Kredit. | K | A886 |
| Import Werbeangebot mit Betrag 0 | Ein Angebot mit Betrag 0 gilt als gelöscht; im Original nur die Betragsprüfung am Bildschirm, keine Spielwirkung belegt. | K | A969 |
| Monatliche Grundeinnahme: Sturm-Teamwert | Für den Sturm gilt wie im Lader die Vereinsstärke (Datenbankbyte 0); ohne Teamwerte (nur Spielstände vor Schema 12) zählen die Abweichungen 0. | K | A826 |
| Platzfelder der Liga speichern | Das Remake schreibt die Platzfelder erst im nächsten Tagesabschluss; gelesen werden sie in der laufenden Saison ohnehin aus der Tabelle. | K | A508 |
| Stadionbau ohne Ziel | Restwochen ohne Ziel lassen im Remake die Stufe unverändert; der Fall ist im Original nicht erreichbar. | K | A509 |
| Werbebewertung ohne Liga | Ohne Liga zählt das Remake einen Spieltag (Ø-Platz 6), das Original die Liga des Managers. | K | A507 |
| Werbebewertung: Aufstiegsgruppe | Für die Aufstiegsgruppe zählt das Remake bis zum Saisonwechsel (Wertebereich von DGROUP 0x7B0 nach der letzten Runde nicht gelesen). | K | A887 |
| Werbebewertung: Platzfelder ab 46 | Platzfelder ab 46 gibt es im Remake nicht; sie werden nie gelesen, weil eine Liga höchstens 44 Spieltage hat. | K | A506 |

## Kader/Transfer

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Alter der Talente der Computervereine | Talente, mit denen das Remake Computervereine auffüllt, sind 17–19 Jahre alt; ein Gegenstück im Original ist nicht belegt. | D | A630 |
| Grundstärken neuer Marktspieler | Für Spieler eines Nationalteams gehen die Länderwerte aus der Aufstellung des Bundestrainers nicht in die Grundstärken ein; Vereinsfelder der Vereine sind aus Datenbankstärke genähert. | D | A707 |
| Leihende zwischen Managern | Die Rückkehr eines zwischen zwei Managern geliehenen Spielers ist nur nach dem Code nachgebaut, nicht an einem Spielstand geprüft. | D | A629 |
| Meldung „nicht erlaubt (unfair)“ | Die Grenze für die Meldung „unfair“ (Gehalt unter 1 DM) stammt aus dem Video, die Prüfstelle im Programm ist nicht gelesen. | D | A117 |
| Name der Computervereins-Talente | Der Name eines Talents wird aus Vor- und Nachnamen zweier zufälliger Spieler zusammengesetzt; im Original behält ein Pooleintrag seinen Namen. | D | A132 |
| Nicht mehr ausführbares Gebot | Das Remake verwirft ein nicht mehr ausführbares Gebot; wie das Original damit umgeht, ist offen. | D | A259 |
| Poolgruppen nach Liga | Das Remake führt Ligen über Club.League: Gruppe 0–2 = Vereine mit dieser Liga, Gruppe 3 = sieben Vereine ohne Liga mit dem niedrigsten Index (Reihenfolge der Umordnung unbelegt). | D | A949 |
| Startwerte der Computervereinsspieler | Kondition/Technik ±2 um den Teamwert, Form 40–60 und Energie 50–80 der Computervereinsspieler sind Remake-Werte, weil das Original dort nur Pooleinträge führt. | D | A427 |
| Talentwerte der Computervereine | Talente der Computervereine bekommen Werte 2–8 unter dem Vereinsbasiswert, Form 45–60 und steigende Tendenz – ohne belegtes Gegenstück. | D | A131 |
| Verlorener Pool-Platz: Neuanlage bei Alter | Die Neuanlage bei Alter &gt; random(37, 42) trifft den verlorenen Platz im Remake nicht (unsichtbar, Schleifenkopf nicht gelesen): der Spieler altert weiter. | D | A945 |
| Verstecktes Talent der Computervereins-Talente | Talente der Computervereine bekommen im Remake ein Talent 6–9, im Original ist kein solcher Zugang belegt. | D | A128 |
| Aufstellung: Tasten A und C | Das Remake bietet in der Aufstellung die Tasten A (automatisch aufstellen) und C (leeren), die das Original nicht hat. | E | A365 |
| Automatisch aufstellen je Verein | Das Remake führt die Einstellung je Verein statt je Manager (DGROUP 0x375/0x376). | E | A936 |
| Basis der Computervereine | Die Basis der Spielerwerte der Computervereine kommt aus BaseValue. | E | A173 |
| Belastung: Platzhalter 150 | Platzhalter 150 (frühere Energie 0) für Spieler ohne Kaderplatz, den keine Regel liest. | E | A943 |
| Bieter für unaufgeforderte Angebote | Das Original misst die Stärke eines fremden Pooleintrags (Kaderplatznummer statt Poolnummer), das Remake die Stärke des Spielers selbst. | E | A247 |
| Computerkader im Tageslauf: eigene Zufallsquelle | Die Kader der Computervereine haben im Original kein Gegenstück (dort gibt es nur Teamwerte und den Spielerpool); ihre Würfe zum Saisonwechsel (Laufbahnende, Verlängern, Auffüllen, Vertragslose) bleiben außerhalb des gemeinsamen Stroms (ComputerSquadRandom). | E | A862 |
| Computerkader: eigene Zufallsquelle | Eigene Zufallsquelle der Computerkader (aus der Saat abgeleitet): Im Original hat ein Computerverein keine Spieler, ihre Werte haben kein Gegenstück im gemeinsamen Strom. | E | A853 |
| Eingespielte Reihe eines Verletzten | Ein Verletzter steht im Remake nicht mehr in der Aufstellung (im Original behält er +0x15) und spielt deshalb Reihe 0 ein. | E | A939 |
| Ersatzname POOLSPIELER n | Ohne freien Datenbanknamen heißt ein Platz „POOLSPIELER n“. | E | A215 |
| Erste Aufstellung beim Kaderaufbau | Beim Kaderaufbau stellt das Remake alle Vereine nach Spielwert auf, nicht nach der Regel des Originals. | E | A165 |
| Freundschaftsspiele: Energie 70 | Die Energie der Kaderspieler wird erst beim ersten Freundschaftsspiel statt schon beim Öffnen des Bildschirms auf 70 gesetzt. | E | A279 |
| Gebot: Wurf beim Aufrufer | Aufrufer ohne Anklicken (Tests, Werkzeuge): Der Wurf des Gebots fällt erst hier. | E | A952 |
| Gehalt der Computervereine | Nur Spieler der Computervereine tragen Demand; Computervereine haben im Original keine Kaderplätze und damit kein Gehalt, ihr Gehalt bucht nichts. | E | A919 |
| Gehalt der Computervereine: Rechnung | Ohne Gegenstück im Original (A120): Das Remake braucht einen Wert für den Vertrag (Anzeige, Leihvertrag des Stammvereins); Geld bewegt er nicht. | E | A920 |
| Heilung außerhalb der Managerkader | Spieler von Computervereinen und Vertragslose behalten Verletzungen und heilen am Heiltag. | E | A523 |
| Heilung außerhalb der Managerkader | Spieler ohne Kaderplatz eines Managers heilen weiter am Heiltag. | E | A529 |
| Kader beim Import: eigene Zufallsquelle | Werte und Markt kommen beim Import und in Aufbauten ohne NewGame.Create aus einer eigenen Quelle aus der Saat; ergänzte Plätze ziehen beim Import keine Spieler aus Computervereinen (Platzhalter). | E | A852 |
| Kader der Computervereine | Computervereine haben im Remake Datenbankspieler und Ergänzungen „SPIELER n“. | E | A684 |
| Kader-KI der Computervereine | Verlängerung, Laufzeiten und Nachbesetzung der Computerkader sind eine Remake-Ergänzung. | E | A430 |
| Kader-KI: Altersgrenze Neuzugänge | Vertragslose kommen nur bis 32 Jahre bei Computervereinen unter (Remake-Ergänzung). | E | A235 |
| Kader-KI: Anteil Neuzugänge | 60 % der Neuzugänge kommen aus dem Pool, sonst Talente (Remake-Ergänzung). | E | A236 |
| Kader-KI: Höchstgröße | Computerkader haben höchstens 25 Spieler (Remake-Ergänzung). | E | A237 |
| Kader-KI: Verlängerungschance | Computervereine verlängern mit 75 % Chance (Remake-Ergänzung). | E | A233 |
| Kader-KI: Vertragslaufzeit | Verträge der Computervereine laufen 1–3 Jahre (Remake-Ergänzung). | E | A234 |
| Kauf ohne Oberfläche | Ohne Oberfläche antwortet eine Simulation statt des verkaufenden Managers. | E | A727 |
| Kauf ohne Oberfläche | Ohne Oberfläche ersetzt eine simulierte Antwort die Frage an den verkaufenden Manager. | E | A732 |
| Mindestbestand beim Verkauf | Ein Mindestbestand je Art gilt nur für Computervereine (Remake-Ergänzung). | E | A431 |
| Mittel der vier Teamwerte als eine Zahl | Das Mittel der vier Felder als eine Zahl gibt es im Original nicht; das Remake braucht sie für Spiele gegen Nationalteams ohne Spielvorbereitung 0x8365. | E | A941 |
| Neuer Manager: Teamwerte des Vereins (Altstände) | Ohne Teamwerte (nur Spielstände vor Schema 12) schreibt der Aufruf nichts, um den Zug der Teamwerte nicht vorzuziehen; neue Spiele wie im Original. | E | A828 |
| Platzhalter „SPIELER n“ für ergänzte Kaderplätze | Im Original haben Computervereine keine Spieler; die Namen leerer Datenbankplätze der Managerkader zieht der Poolaufbau (Datei 0x5AAA0). | E | A855 |
| Pool der Vertragslosen | Der Pool der Vertragslosen ist auf 60 begrenzt (Remake-Ergänzung). | E | A252 |
| Pool: Datenbankspieler verlässt den Computerkader | Der gezogene Datenbankspieler verlässt den Kader seines Computervereins (im Original hat der keine Spieler; jeder Name nur einmal im Spiel); ohne freien Namen heißt der Platz „SPIELER n“. | E | A861 |
| Poolplatz nach Verkauf an Computerverein | Der Poolplatz wird erst beim Saisonwechsel statt sofort frei. | E | A414 |
| Poolverteilung: Grenze der Züge | Ohne passenden Verein (synthetische Datenbank) bleibt der Verein; nach 100.000 verworfenen Zügen endet die Schleife (im Original ohne Grenze). | E | A947 |
| Spieler eines Computervereins trägt Poolwerte | Computervereine haben keine Kaderplätze; ihr Spieler trägt die Poolwerte. | E | A923 |
| Spieler verlässt den Managerkader | Der Kaderplatz entfällt, es bleiben die Poolwerte; Computervereine haben keine Kaderplätze. | E | A940 |
| Spielerdatenfenster: Preis | Die Zufallszahl im angezeigten Preis eines Spielers mit Angebot kommt aus festem Startwert statt aus dem Zufall des Spiels. | E | A366 |
| Spielerinfo: Tendenzbalken ohne Wert | Ohne gespeicherten Tendenzwert zeigt der Balken eine von drei Stufen statt der Formel des Originals. | E | A809 |
| Spielerwerte der Computervereine | Spieler der Computervereine bekommen im Remake Grundwerte 20 + Stärke/2. | E | A171 |
| Spielvorbereitung (Hinzufügen): neue Manager | Das Remake verlangt für eine Zeile mit Namen Kopf und Verein; das Original nimmt einen Namen ohne beides an und schreibt dann außerhalb der Vereinstabelle (Fehler des Originals). | E | A480 |
| Spielwert eines Spielers (Auffüllen freier Plätze) | Nur Reihenfolge beim Auffüllen freier Plätze (Lineup.FillGaps); in keinen Teamwert geht der Spielwert ein. | E | A856 |
| Sponsorzuschuss beim Kauf | Vertragslose Spieler lösen den Zuschuss wie Marktspieler aus; Vertragslose auf der Liste gibt es nur in Remake-Altständen. | E | A702 |
| Start-Liga und Start-Stärke | Das Remake führt Start-Liga und Start-Stärke je Manager und lässt fest gewählte Vereine in ihrer Liga. | E | A172 |
| Startkader der Computervereine | Die Werte der Computerkader kommen aus ComputerSquadSource (ohne Gegenstück im Original); Stärke und Liga gelten nach dem Platztausch; der Aufbau verbraucht den gemeinsamen Strom nicht. | E | A854 |
| Startkader der Manager im Pool | Namen, Werte und Alter der Startkader kommen aus dem Kaderaufbau des Remakes; der Alterswurf random(19, 31) des Pools wird nicht verbraucht. | E | A415 |
| Startkader des neuen Managers | Nach 1000 Fehlversuchen nimmt das Remake den ersten freien Poolplatz, der Computerkader verlässt das Spiel und der Zufall kommt aus eigener Quelle. | E | A465 |
| Startkader: Spieler ohne gemerkten Platz | Spieler ohne gemerkten Platz (Aufbauten ohne NewGame.Create, Import) zählen wie bisher ab Anfang der Art. | E | A938 |
| Tausch Pool/Kader | Der in den Computerkader getauschte Spieler bekommt die Energie eines Talents. | E | A711 |
| Teamwerte des Käufers nach Verkauf an Computerverein | Das Original rechnet das Mittel mit einem zufälligen Vereinsteil (Register si = Mauszeile des Transferbildschirms), das Remake mit dem Mittel aus Abwehr und Sturm. | E | A701 |
| Tendenz im Tageslauf: Plätze ohne Steuerbyte | Plätze ohne bekannten Wert oder ohne Steuerbyte (0: ältere Spielstände) behalten ihre Stufe und ziehen nicht; im Original hat jeder Kaderplatz beide Werte (0x4907A). | E | A830 |
| Tendenz: Platzhalter 7 | Platzhalter 7 für Spieler ohne Kaderplatz (Computervereine, freie Poolplätze) und Stände vor Teil 7; kein Leser. | E | A944 |
| Umbenennen der Poolplätze | Pool und Computerkader tauschen Spieler, damit jeder Name nur einmal im Spiel steht. | E | A216 |
| Umbenennen der Poolplätze | Ein Spieler mit Laufbahn wird beim Umbenennen nicht verworfen. | E | A217 |
| Umbenennen der Poolplätze | Statt den Namen zu überschreiben, bekommt der Platz einen neuen Spieler mit den alten Werten. | E | A219 |
| Verkauf an Computerverein: Teamwerte (Altstände) | Ohne Teamwerte (nur Spielstände vor Schema 12) entfällt die Wirkung des Verkaufs auf die Teamwerte des Käufers. | E | A860 |
| Verkauf nach unaufgefordertem Angebot | Ein voller Computerkader oder ein Europapokal-Gegner gibt den Spieler zu den Vertragslosen statt in einen freien Poolplatz. | E | A249 |
| Verkaufter Spieler: Poolplatz bis zum Saisonwechsel | Ein an einen Computerverein verkaufter Spieler hält im Remake seinen Poolplatz bis zum Saisonwechsel (PoolOwner.Taken), hat aber keinen Kaderplatz mehr. | E | A948 |
| Verlängerung eines Ruheständlers: neuer Spieler | Das Remake legt einen neuen Spieler an (neue ID), damit der Ruheständler mit seiner Laufbahn erhalten bleibt; Energie und Tendenz steigend sind ergänzt. | E | A922 |
| Versicherung ohne bekannte Prämie | Wird „versichert“ ohne Betrag gesetzt, nimmt das Remake 10 % des Gehalts als Prämie; das Original kennt nur den Betrag. | E | A114 |
| WM-Kaderplatz: Name, Alter, Talent, Rolle, versteckte Werte | Name, Alter, Talent, Rolle und versteckte Werte führt das Remake am Platz mit; im Original stehen sie im Pool (+0x19 = 0x77). | E | A877 |
| Übrige Managerplätze im Pool | Bei kleinerem Kader werden übrige Platzhalter freie „POOLSPIELER n“. | E | A709 |
| Aufstellung: Meldung „Auto-Aufstellung ist deaktiviert“ | Trifft der Eingriff mit einer anderen Meldung zusammen, gewinnt im Remake diese Meldung; die Reihenfolge im Original ist dafür nicht belegt. | K | A807 |
| Energie (Belastung) | Die Energie 0–100 ist nur eine Umrechnung der originalen Belastung für Anzeigen und ältere Aufrufer und wird nicht gespeichert. | K | A627 |
| Energie neuer Talente | Neue Talente der Computervereine starten im Remake mit Energie 80. | K | A129 |
| Ersatz-Aufstellung Nationalteam | Das Remake wendet die Ersatz-Automatik beim Lesen an, ohne die Aufstellung zu speichern; die Regel selbst ist gleich. | K | A420 |
| Ersatzverein beim Listenwechsel | kein Unterschied im echten Spiel: Absicherung, wenn alle Vereine einer Liga Manager haben. | K | A211 |
| Export Besitzer bei Leihe von Computerverein | Besitzer = Entleiher statt Verleiher (nicht aufgenommen); wirkt nur in der Datei für das Original, Leihende bleibt D. | K | A967 |
| Heilung verletzter Spieler | Das Remake heilt alle Spieler, das Original nur Kader- und Marktplätze der Manager; da sich nur Managerspieler verletzen, ohne Wirkung. | K | A538 |
| Import Nationalaufstellung: dritter Abwehrplatz in Reihe 2–3 | Das Remake hat dort keinen dritten Abwehrplatz; das Original führt ihn unsichtbar in Zelle 5 (nicht beobachtet). | K | A968 |
| Import Vereinsaufstellung: dritter Abwehrplatz in Reihe 2–3 | Das Remake hat dort keinen dritten Abwehrplatz; das Original führt ihn unsichtbar in Zelle 5 (nicht beobachtet). | K | A970 |
| Jugendteams: Zähler über 44 | Zähler über 44 gibt es im Remake nicht; das Original läse dann weitere Stapelbytes. | K | A515 |
| Kaderfolge der Computervereine | Neuzugänge der Computervereine hängt das Remake hinten an; die Folge wirkt nur auf die Anzeige der Nr. und die eigene Aufstellung des Computervereins. | K | A633 |
| Kaufdialog | Der Kaufzustand lebt nur im Dialog und wird nicht gespeichert. | K | A224 |
| Leihgehalt: ganze DM | Ganze DM (Drittel abgerundet, mindestens MinSalary). | K | A921 |
| Leihrückkehr bei vollem Kader | Hat der Stammverein keine freie Nummer mehr, wird der Spieler vertragslos statt zu verschwinden. | K | A116 |
| Liste hinter den Verhandlungen am Saisonende | Nach dem Laden mitten in den Verhandlungen zeigt die Liste das jetzige Gehalt. | K | A717 |
| Nationalaufstellung automatisch: Reihenfolge im Block | Innerhalb eines Blocks sortiert das Remake die Spieler nach Rückennummer wie beim Verein; für das Nationalteam ist das nicht belegt. | K | A491 |
| Nationalaufstellung: Klick auf verletzten und gesperrten Spieler | Bei einem zugleich verletzten und gesperrten Spieler meldet das Remake „verletzt“, das Original lässt den Klick durch; der Fall tritt im Remake nicht auf. | K | A489 |
| Rückfrage an den verkaufenden Manager | kein Unterschied im Spiel: das Remake prüft Eintrag, Kader und Geld erneut, obwohl sich dazwischen nichts ändert. | K | A719 |
| Sechster Feldspieler bei geleertem Sturmplatz | Das Original zeigt Zelle 5 in der Sturmzelle; das Remake lässt den Spieler auf dem sechsten Platz (+0x15 gleich). | K | A972 |
| Sechster Feldspieler: Offen-Kennzeichen | Das Remake leitet das Kennzeichen aus dem Zustand ab (Platz belegt, kein Torhüter) statt es als Bildschirmfeld zu führen. | K | A971 |
| Training nachholen (Tests) | Beim Nachholen über einen Saisonwechsel überspringt das Remake Tage der alten Saison. | K | A693 |
| Unaufgeforderte Angebote: „Offene Meldung“ | „Offene Meldung“ = noch nicht angeklicktes Angebot; Rücktritt = Ankündigung ContractNotice.Retiring. | K | A866 |
| Verletzung außerhalb des Spiels: Text der Art | Der Text der Art in Zeile 2 ist der Name der Art (Ausgabe von 0x05B1A in [bp−0xDA] nicht gelesen); im Modus „nur Weltkugel“ läuft der Tageslauf nicht. | K | A950 |
| Vertragsdialog: Gehalt 0 | Der Kern wertet Gehalt 0 als „nicht eingegeben“; der Dialog gibt deshalb 1 DM weiter und zeigt 1 DM statt 0 DM; Wirkung wie im Original (nicht interessiert). | K | A973 |
| Vertragsende ankündigen: „Offene Meldung“ | „Offene Meldung“ (Pool +0x1E) = schon gemeldet (Player.Notice ≠ None); die Würfe folgen der Reihenfolge von 0x07169 aus dem gemeinsamen Strom. | K | A863 |
| WM-Kaderplatz: Gehalt | Das Gehalt des Satzes-5-Platzes (0x49D36) führt das Remake nicht; der Wurf random(90, 115) wird gezogen und verworfen. | K | A878 |
| WM-Schleife: Heilung | Spieler ohne Kaderplatz eines Managers heilen im Remake ebenfalls; im Original gibt es für sie keine Verletzungen. | K | A496 |

## Laden/Speichern

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| HOK-Export Bank und Managerdarlehen | Die Bankseite als Gegenbuchung ist nur an einem Stand gesehen, Darlehen zwischen Managern an keinem. | D | A402 |
| HOK-Export Modus „nur Weltkugel“ | Im Modus „nur Weltkugel“ vergibt der Export dem Kader von Satz 0 Poolplätze nach Ersatzregel; ob das Original es so führt, ist offen. | D | A411 |
| HOK-Import Sparbuch und Managerdarlehen | Positiver Bankeintrag als Sparbuch und Darlehen zwischen Managern sind an keinem Stand belegt. | D | A613 |
| HOK-Import Sperren | Sperren von Spielern werden beim Import auf 0 gesetzt, weil das Feld im HOK nicht gefunden ist. | D | A625 |
| Laden KAPUTT.HOK | Was das Original nach der Meldung „Der kleine Mogelguru“ tut, ist unbekannt; das Remake lädt nichts und behält den Stand. | D | A599 |
| 0x25B4A Fehlerhalt mit ERROR.HOK | Im Fehlerfall sichert das Remake keinen ERROR.HOK, sondern arbeitet mit Ausnahmen und Autospeichern. | E | F056 |
| Absage „Refused“ bei Verlängerung | Die Remake-Absage „Refused“ gilt wie eine Verhandlungssperre bis zum Saisonwechsel; sie entsteht nur noch in älteren Spielständen. | E | A123 |
| Angezeigter Einsatz AUTOM. alter Stände | Spielstände vor Runde 14 zeigen beim Verein den Einsatz der Automatik für ein ausgeglichenes Spiel. | E | A689 |
| Anzeige AUTOM. alter Stände | Spielstände vor Runde 14 zeigen unter „AUTOM.“ die ausgeglichene Zeile. | E | A688 |
| Automatisches Speichern | Das Remake sichert jeden 10. neuen Zug des ersten nicht entlassenen Managers als AUTOSAVE.ems und nicht am Saison-/Spielende, das Original zählt Hauptmenü-Aufrufe von Manager 0 und schreibt autosave.hok. | E | A335 |
| Bestenliste: Speicherort | Die Bestenliste liegt im Benutzerprofil statt im Ordner high\\ des Spiels. | E | A352 |
| Datenbanknamen alter Spielstände | Namen der Kader gelten in alten Spielständen als vergeben. | E | A222 |
| Datenbankplätze alter Spielstände | Bei alten Spielständen werden Datenbankplätze aus den Kadern rekonstruiert. | E | A213 |
| Dialog Laden/Speichern | Ordner statt Laufwerken (Knöpfe ohne Wirkung), Klick wählt und Enter führt aus, Überschreiben erst nach Rückfrage. | E | A788 |
| Dialog Speichern Original-Spielstände | Beim Sichern sind *.HOK grau, das Remake schreibt im Dialog kein HOK. | E | A792 |
| Eingesetzte in alten Spielständen | Für Stände ohne gespeicherte Eingesetzte gilt im Remake ein Rückfall aus Aufstellung und Ersatzrunde. | E | A680 |
| Export *.HOK: ID, Füllbyte, Schlüssel | Spielstand-ID, Füllbyte und Startschlüssel werden aus dem Inhalt abgeleitet statt gewürfelt. | E | A405 |
| Fehlermeldung Laden Umbruch | Lange Fehlertexte des Remakes werden auf 158 Pixel umbrochen; das Original hat nur feste Zeilen. | E | A795 |
| Gehaltsfaktor alter Spielstände | Ältere Spielstände ohne gezogenen Gehaltsfaktor nutzen einen aus Saison und Tag abgeleiteten Wert. | E | A705 |
| HOK-Export Automatik Bundestrainer | Ohne Bundestrainer wird der aktuelle Automatik-Schalter von Satz 5 fest auf 1 gesetzt. | E | A566 |
| HOK-Export Automatik nur Weltkugel | Im Modus „nur Weltkugel“ wird der Automatik-Schalter der Manager fest auf 1 gesetzt. | E | A403 |
| HOK-Export DEB-Pokal | Eine fertige Pokalrunde vor der nächsten Auslosung wird wie nach dem Endspiel geschrieben (Zähler 2, Siege 0). | E | A569 |
| HOK-Export Europapokal-Füllpaare | Füllpaare (Sätze 61/63) und Platzliste 4/5 hinter den Europapokal-Teilnehmern sind nur von Ständen abgeschrieben. | E | A563 |
| HOK-Export Europapokal-Gruppen | Das dritte Paar je Termin (Füllpaar) wird als 0 geschrieben. | E | A571 |
| HOK-Export Gegentore Torhüter | Gegentore werden ohne Aufteilung nach Wettbewerb komplett der Liga zugeschrieben. | E | A587 |
| HOK-Export Gegner eines Rekords | Gegner eines Rekords ohne Wert wird mit dem Satz des Vereins mit Datenbankindex 0 gefüllt. | E | A584 |
| HOK-Export Grundwert Automatik | Der Grundwert der Automatik +0x2D wird gleich dem Einsatz +0x26 geschrieben. | E | A446 |
| HOK-Export Grundwert Automatik | Das Remake führt den Grundwert 0x2D nicht; beim Gegner einer Managerpaarung weicht der geschriebene Wert vom Original ab. | E | A578 |
| HOK-Export Gruppentabellen | Punkte und Tore werden je Teilnehmer statt je Tabellenplatz geschrieben; welche Zuordnung das Original nutzt, ist offen. | E | A575 |
| HOK-Export Level/Stärke | Das Remake führt Level und Startstärke je Manager, der Export schreibt nur die von Manager 0. | E | A593 |
| HOK-Export Ligafolge | Bei synthetischen Ständen ohne passende Folge gilt die gespeicherte Platzfolge. | E | A591 |
| HOK-Export Managersatz | Die DEB-Pokalrunde im Managersatz +0xE6 ist nur nach Ständen gedeutet, nicht aus em.exe belegt. | E | A400 |
| HOK-Export Monatsbyte 0xA8A0 | DGROUP 0xA8A0 wird als Monat geschrieben, an einem Stand (Ende Januar 8) weicht das Original ab. | E | A595 |
| HOK-Export Nachrichten | Erledigte Nachrichten und Kreditwünsche werden nicht exportiert, nur offene. | E | A568 |
| HOK-Export Nationalkader | Nominierte Spieler von Computervereinen fehlen im exportierten Nationalkader, weil das Format sie nicht einzeln führt. | E | A565 |
| HOK-Export Paarungen Spieltag | Nach dem letzten Spieltag bleiben die Paarungen des letzten Spieltags im Export stehen. | E | A581 |
| HOK-Export Play-off-Ergebnis | Ein Remake-Spiel mit Gleichstand und Penaltysieger bekommt im Export dessen Treffer dazu. | E | A574 |
| HOK-Export Pokalrunde 0x7AD | Ab der zweiten Saison wird die Pokalrunde bis zum ersten Pokaltermin als 0 geschrieben, nur nach Ständen. | E | A570 |
| HOK-Export Pool | Ein Kaderspieler ohne freien Poolplatz entfällt im Export. | E | A585 |
| HOK-Export Pool Altstände | Ein Kaderspieler ohne Poolplatz aus einem alten Remake-Stand entfällt im Export. | E | A586 |
| HOK-Export Pool Nominierte | Beim Nominierten trägt Pool +0x1D das Land statt des Vereins, nur von einem Stand übernommen. | E | A567 |
| HOK-Export Rekordblock | Der übrige Teil des Rekordblocks 0xAB7B (vermutlich Ewige Tabelle) wird als 0 geschrieben. | E | A583 |
| HOK-Export Rollenwert Pool | Das Original führt im Pool +0x18 eine Rollenzahl je Spieler, das Remake schreibt einen festen Wert je Spielerart. | E | A404 |
| HOK-Export Rundenzähler Wettbewerb 3 | DGROUP 0x5CF6 wird fest auf 3 gesetzt statt aus dem Spielverlauf. | E | A564 |
| HOK-Export Satzfolge | Deutsche Vereine ohne Liga stehen in den Sätzen 36–47 nach Index, nur am Startstand belegt. | E | A590 |
| HOK-Export Schalter 0x22A | Schalter DGROUP 0x22A wird fest auf 1 gesetzt, Bedeutung offen. | E | A596 |
| HOK-Export Spielerfelder +0x1A/+0x1B | Spielerfelder +0x1A/+0x1B sind ungedeutet und werden mit Standwerten vorbelegt. | E | A588 |
| HOK-Export Vereinsfeld +0x1E | Vereinsfeld +0x1E ist ungedeutet und wird fest auf 50 gesetzt. | E | A579 |
| HOK-Export Vereinsfelder +0x1E–0x25 | Vereinsfelder +0x1E bis +0x25 sind ungedeutet und werden mit Standwerten vorbelegt. | E | A577 |
| HOK-Export Vereinsfelder +0x1F–0x25 | Vereinsfelder +0x1F–0x25 sind ungedeutet und werden ab dem ersten Tag auf 90 gesetzt. | E | A580 |
| HOK-Export Vereinssätze AUTOM. | Die Zuschläge +0x27–0x2B/+0x35–0x3A der übrigen Vereine werden als 0 geschrieben, weil das Original sie vor jedem Spiel neu rechnet. | E | A576 |
| HOK-Export WM K.-o.-Runde | Tore der K.-o.-Spiele gehen beim Export verloren, weil das HOK sie nicht führt. | E | A572 |
| HOK-Export allgemein | Ungedeutete Felder schreibt der Export als Vorgabe des Spielstarts oder 0. | E | A399 |
| HOK-Export ohne Vereinsdatenbank | Ohne Vereinsdatenbank werden Farbe und Logo als 0 geschrieben. | E | A589 |
| HOK-Export ungedeutete Blöcke | Ungedeutete Blöcke werden mit den Werten des Spielstarts gefüllt. | E | A597 |
| HOK-Export „nur Weltkugel“: Kaderspieler ohne Poolplatz | Die Suche nach einem freien Ligaplatz überspringt Poolplätze auf dem Markt; das Original setzt den Managerkader vor dem Listenwechsel, die FILE01-Plätze 22–31 hängen daher am Zufall (nach dem Lader bei 7 von 16 Saaten getroffen). | E | A848 |
| HOK-Export: Siegbyte 0x14 des fünften Termins | Das Siegbyte 0x14 des fünften Termins schreibt der Export nicht. | E | A915 |
| HOK-Import Automatik Bundestrainer | Der Zug des Bundestrainers gilt nach dem Laden als nicht offen; es zählt der gesicherte Schalter. | E | A611 |
| HOK-Import Automatik aufstellen | Nach dem Laden an einem Tag ohne Ereignis gilt im Remake sofort der gesicherte Automatik-Schalter, im Original bis zum nächsten Ereignistag noch der aktuelle. | E | A397 |
| HOK-Import DEB-Pokal | Frühere Pokalrunden kommen aus dem Nachspielen des Remakes, weil das HOK nur die laufende führt. | E | A600 |
| HOK-Import DEB-Pokal Auslosung | Die Ziehungsfolge vor dem Heimrechttausch 0x347EF fehlt im HOK und wird angenommen. | E | A601 |
| HOK-Import DEB-Pokal-Schalter | Der Remake-Schalter für den DEB-Pokal wird beim Import eingeschaltet, weil das Original keinen hat. | E | A618 |
| HOK-Import Europapokal | Die deutschen Teilnehmer werden aus der Auslosungsfolge 0xA776 abgeleitet, weil die Zahl 0xA775 fehlt. | E | A602 |
| HOK-Import Formtendenz | Tendenzwert 0 lässt die Stufe aus dem Nachspielen stehen. | E | A623 |
| HOK-Import Freundschaftsspiele | Der Wertebonus eines Freundschaftsspiels fehlt im HOK und wird 0. | E | A609 |
| HOK-Import Leihspieler | Der Stammvertrag eines Leihspielers wird aus dem Leih-Kaderplatz übernommen, weil das HOK ihn nicht zeigt. | E | A626 |
| HOK-Import Ligaergebnisse | Ein Spieltag mit 18 Spielen 0:0 gilt als nicht eingetragen. | E | A621 |
| HOK-Import Manager am Zug | Ist der Bundestrainer am Zug, beginnt das Remake nach dem Laden mit Manager 0. | E | A622 |
| HOK-Import Nachrichten | Tag und Saison einer Nachricht werden aus dem Schlüssel abgeleitet, der Vorrang ist ungedeutet. | E | A608 |
| HOK-Import Nachspielen | Pokale, Markt, Training und Finanzen laufen beim Nachspielen nach Remake-Regeln, belegte Felder werden überschrieben. | E | A620 |
| HOK-Import Nationalkader Verein | Pool +0x1D eines Nominierten gilt als Land, der Verein wird anderweitig bestimmt. | E | A610 |
| HOK-Import Pool Besitzer | Im Modus „nur Weltkugel“ wird ein Poolspieler mit Besitzer dem gleichnamigen Spieler des Managervereins zugeordnet. | E | A604 |
| HOK-Import Rekorde | Rekordsätze von Vereinen ohne Remake-Index unter 64 entfallen. | E | A616 |
| HOK-Import Spielernamen | Gleichnamige Spieler behalten Objekt und ID; doppelte Namen bei Computervereinen werden ersetzt. | E | A606 |
| HOK-Import Trainingspreise | Die Preisstufe des Trainings bleibt aus dem Nachspielen (Liga beim Spielstart). | E | A615 |
| HOK-Import Transfermarkt | Marktplatz mit Poolindex 0 gilt als leer, Vertragslose werden wie Poolangebote behandelt. | E | A605 |
| HOK-Import Vereinsstärke | Nach einem Platztausch beim Spielstart übernimmt das Remake die Stärke des Tauschpartners, das HOK zeigt den Tausch nicht. | E | A619 |
| HOK-Import WM K.-o.-Runde | Tore der K.-o.-Spiele fehlen im HOK; der Import setzt Sieger 1:0. | E | A603 |
| HOK-Import allgemein | Was das HOK nicht führt, kommt aus dem Nachspielen des Remakes und gleicht nicht dem Original. | E | A617 |
| HOK-Import: Verein +0x1D außerhalb der 150 Sätze | Verein +0x1D außerhalb der 150 Sätze wird zu Verein 0 (an keinem Stand beobachtet). | E | A849 |
| Import Original-Spielstand Zufall | Nach dem Import startet der Zufall mit neuer Saat. | E | A794 |
| Kaderspieler ohne Poolplatz | Altstände und Managerwechsel zu einem Computerverein setzen Kaderspieler auf den ersten freien Ligaplatz ihrer Art. | E | A416 |
| Kaderspieler ohne Poolplatz | Ohne freien Ligaplatz bleibt der Spieler ohne Platz. | E | A710 |
| Kaufpreis | Nur Managerspieler tragen einen Kaufpreis, ältere Spielstände kennen ihn nicht (0). | E | A634 |
| Listenpreis eigener Spieler | Ohne Kaufpreis (Altstände) wird der Preis aus dem Spielerwert genommen. | E | A733 |
| Migration 47 → 48 | Ältere Stände beginnen mit Einsatz und Angriff des letzten Spiels 0. | E | A413 |
| Migration 49 → 50 Poolform | Ältere Stände kennen die Poolform nicht und nehmen die Form des Kaderplatzes. | E | A428 |
| Migration 52 → 53: Zufallsstrom | Ein älterer Stand setzt seine Folge mit anderen Würfen fort als vor der Migration (der Zustand der entfallenen Quellen wird verworfen); der gespeicherte Faktor des laufenden Zugs bleibt, Objektnummern werden neu vergeben. | E | A850 |
| Migration 53 → 54: Zufallsstrom | Wie 52 → 53: Ein älterer Stand setzt seine Folge mit anderen Würfen fort (Zustand von Pokal- und Finanzquelle wird verworfen). | E | A851 |
| Migration Angebote ohne Spieler | Ältere Angebote kennen ihren Spieler nicht und verfallen wie Meldungen nach drei Tagen. | E | A648 |
| Migration Bundestrainer-Taktik | Nie geänderte alte Taktikwerte des Bundestrainers werden auf den Startwert 0 gesetzt. | E | A145 |
| Migration Börsenkurse | Alte Kurse werden in Basiswert und Tendenz umgerechnet, nur die letzten 36 Tage bleiben. | E | A156 |
| Migration Jugendstärke (Geld 3) | Ältere Stände bekommen als Jugendstärke die Startstärke der Liga. | E | A639 |
| Migration Kaufpreis/Verletzungsart | Ältere Stände bekommen Kaufpreis 0 und Verletzungsart „unbekannt“. | E | A643 |
| Migration Kopf-an-Kopf | Die Ergebnisse der laufenden Saison werden in alten Ständen nicht in die Begegnungstabelle nachgetragen. | E | A147 |
| Migration Nachrichten-Restlaufzeit | Ältere Nachrichten beginnen mit 3 Tagen Restlaufzeit. | E | A647 |
| Migration Nationalkonto | Das Nationalkonto alter Stände beginnt leer. | E | A153 |
| Migration Nationalteam (AUTOM.-Anzeige) | Ältere Stände zeigen unter „AUTOM.“ bis zum nächsten WM-Spiel 0. | E | A144 |
| Migration Platzverlauf (Geld 2) | Ältere Stände kennen den Platzverlauf der Vorsaisons für die Werbebewertung nicht und beginnen mit 0. | E | A638 |
| Migration Pool: Manager des Platzes | Bei mehreren Managern bleibt der Besitzer eines verlorenen Poolplatzes in alten Ständen unbekannt (Export 6). | E | A649 |
| Migration Pool: Managerkader | Alte Stände setzen Kaderspieler eines Managers auf einen freien Ligaplatz, dessen Poolspieler entfällt. | E | A650 |
| Migration Reihen des letzten Spiels | Ältere Stände beginnen mit Reihe 0 wie ein neues Spiel. | E | A651 |
| Migration Rekorde/Kopf-an-Kopf | Ältere Stände beginnen mit leeren Rekorden und Länderbilanzen. | E | A152 |
| Migration Rückennummern | Alte Remake-Stände mit freien Rückennummern werden beim Laden einmalig auf Kaderplatz + 1 umgestellt. | E | A646 |
| Migration Satzfolge der Ligen | Ältere Stände nutzen bis zum nächsten Saisonwechsel die Indexfolge. | E | A653 |
| Migration Satzfolge nächste Saison | Ältere Stände vom Bildschirm „Saisonende“ bilden die Satzfolge erst beim Saisonwechsel. | E | A652 |
| Migration Serien/Über-Unterzahl | Ältere Stände beginnen mit 0 Serien und Über-/Unterzahl. | E | A149 |
| Migration Strafminuten | Ältere Stände beginnen mit 0 Strafminuten der laufenden Saison. | E | A148 |
| Migration Tabellenfeld | Ältere Stände übernehmen das Tabellenfeld aus dem zuletzt gespeicherten Ligaspieltag. | E | A641 |
| Migration Taktikwerte | Wer „SEMI“ oder „MANUELL“ hatte, spielt nach der Migration mit den Startwerten. | E | A655 |
| Migration Talentsuche (Geld 6) | Ältere Stände rechnen die Restzeit der Talentsuche um und setzen den Startpreis 90.000 DM. | E | A640 |
| Migration Tendenzwert | Ältere Stände bekommen Tendenzwert und Steuerbyte 0, es gilt die gespeicherte Stufe. | E | A644 |
| Migration Vereinsspiele/Poolzähler | Ältere Stände kennen Spiele früherer Saisons beim Verein nicht (0), die Poolzähler beginnen bei 0. | E | A645 |
| Migration Verhandlungs- und Marktsperren | Alte Ablehnungen und Marktsperren werden auf 0 gesetzt. | E | A150 |
| Migration Versicherungsprämie | Versicherte Spieler alter Stände bekommen 10 % des Gehalts als Prämie. | E | A151 |
| Migration WM-Platzierungen | Ältere Stände haben keine früheren WM-Platzierungen; die WM-Statistik zählt ab der nächsten WM. | E | A146 |
| Migration Werbebewertung | Ältere Stände kennen die Bewertung beim Abschluss nicht (0). | E | A155 |
| Migration Zähler je Wettbewerb | Ältere Stände zählten alles in der Liga; Pokal- und Europapokalzähler beginnen bei 0. | E | A642 |
| Migration „neue Nachricht“ | Ältere Stände haben das Kennzeichen „neue Nachricht“ nicht gesetzt und den Gehaltsfaktor noch nicht gezogen. | E | A654 |
| Migration: Einzelpunkte (OneSeasonFlag, GoalieStandIn) | Ältere Stände kennen beides nicht: false bzw. niemand. | E | A926 |
| Migration: Kündbar (A582) | Ältere Stände kennen „Kündbar.“ nicht; das Kennzeichen entsteht beim nächsten Aufstieg. | E | A927 |
| Migration: Level und Startstärke | Ältere Stände konnten je Manager verschiedene Werte haben; alle übernehmen die von Manager 0. | E | A930 |
| Migration: Platzverschiebung (A665) | Ältere Stände haben keine Platzverschiebung innerhalb der unteren Liga gespeichert; der Sonderfall bleibt dort wie bisher. | E | A928 |
| Migration: Play-off-Marken | Ein früher in der Verlängerung entschiedenes Spiel bekommt 0, ein importiertes Spiel verliert seine Marken (der Import zog sie bisher ab). | E | A929 |
| Migration: Poolwerte und Reihe | Ältere Stände kennen sie nicht: null bzw. 0; die Drift bisher gesicherter Saisonwechsel fehlt den Spielern mit Kaderplatz. | E | A931 |
| Migration: Torhüter draußen am Spielende | Ältere Stände kennen das Bit nicht: false; der seltene Fall eines Spielendes ohne Torhüter beginnt das nächste Spiel mit der gespeicherten Reihe. | E | A934 |
| Migration: Zähler des Bundestrainers | Ältere Stände kennen die Zähler nicht und beginnen bei 0. | E | A932 |
| Migration: eigener Kaderplatz Satz 5 | Ältere Stände führen keinen eigenen Platz: Beide verweisen bis zum Saisonwechsel auf den Spieler. | E | A933 |
| Patcher: Eintrittspreis | Der Spielstand-Patcher erlaubt Eintrittspreise 1–99 DM, das Spiel selbst nur 10–30 DM. | E | A264 |
| Patcher: Stadionstufen | Der Spielstand-Patcher erlaubt je Stadionstufe 0–9, ohne dass die Obergrenze des Originals belegt ist. | E | A265 |
| Pauschalprämie Versicherung | Ohne bekannten Betrag setzt das Remake die Versicherungsprämie auf 10 % des Gehalts. | E | A119 |
| Pool alter Spielstände | Ohne Pool (Spielstand vor Teil 5) wird er beim Listenwechsel gebaut. | E | A221 |
| Saisonende-Jugend alter Stände | Ein Stand vor Schema 50 vom Bildschirm „Saisonende“ holt Jugendwechsel und Jugend beim Saisonwechsel ohne Ereignissymbol nach. | E | A429 |
| Saisonrekord im Spielstand | Ältere Spielstände (Schema ≤ 17) führen Auf- und Absteiger in Gruppenreihenfolge. | E | A656 |
| Spieler-ID | Das Remake vergibt dauerhafte Spieler-IDs, die das Original nicht kennt. | E | A133 |
| Spielstand-ID beim Export | Das Remake erzeugt die Spielstand-ID im Bereich 0x8000–0xFFFF aus dem Inhalt statt mit der (ungeklärten) Würfelformel des Originals. | E | A263 |
| Spielstand-Info Pfad | Statt des DOS-Pfads steht der (vorn gekürzte) Spielstandordner. | E | A791 |
| Spielstand-Info Remake-Stände | Die Info zu Remake-Spielständen kommt aus dem geladenen Stand statt aus einem Dateikopf. | E | A787 |
| Spielstand-Info per Rechtsklick | Die Info gibt es für Remake- und Original-Spielstände in beiden Modi, bei anderen Dateien bricht der Rechtsklick ab. | E | A790 |
| Spielstand: Managerbyte 0x9744 älterer Stände | Ältere Spielstände kennen das Kennzeichen nicht: false. | E | A895 |
| Spielstanddateien | Spielstände liegen als *.ems in einem Ordner statt *.HOK mit Laufwerk und Verzeichnis. | E | A336 |
| Start-Liga eines Managers | Ohne Eintrag in der Spielvorbereitung (Tests, alte Spielstände) gilt die Liga des Vereins bzw. die Oberliga. | E | A534 |
| Taktik in alten Spielständen | Spielstände vor Runde 14 behalten Startwert und Anzeige der ausgeglichenen Zeile. | E | A686 |
| Tendenzwert ohne gespeicherten Wert | Fehlt der Tendenzwert (alte Spielstände), nimmt das Remake die Mitte der Stufe. | E | A636 |
| Verletzungsart unbekannt | Das Remake kennt eine Verletzungsart „unbekannt“ (−1) für ältere Spielstände und nie verletzte Spieler. | E | A635 |
| Vertragslose auf der Liste | Die Kennung „vertragslos“ eines Listeneintrags gibt es nur noch für Altstände. | E | A250 |
| Vertragslose auf der Liste | Vertragslose auf der Liste bleiben nur in Spielständen vor Runde 15 stehen. | E | A392 |
| Vorbereitete Zuschauer | Die vor dem Spiel gerechneten Zuschauer werden nicht gespeichert und verfallen am nächsten Tagesanfang. | E | A468 |
| Zweiter Tageswechsel nach dem Laden | Neuaufstellung, Teamwerte und Rücksetzungen des zweiten Tageswechsels nach dem Laden werden nicht wiederholt, weil sie im HOK schon enthalten sind. | E | A532 |
| Bestenliste im Weltmodus | Ohne Liga (Weltmodus) bleibt der Platzierungsterm 0; die Punkte betreffen nur die Bestenliste. | K | A963 |
| Bildschirm „Der kleine Mogelguru“ | Lage nur geschätzt, kleine Zeilen weiß statt hellblaugrau, Rückkehr zum Laden-Dialog angenommen. | K | A786 |
| Dialog Marker unter der Maus | Abschrägung und Farbindizes des Markers sind am Bild gemessen, nicht im Code gelesen. | K | A789 |
| Export *.HOK: Kopf hinter der Managerzahl | Im exportierten Kopf stehen hinter den Managern Nullbytes statt zufälligem Speicherinhalt wie im Original. | K | A735 |
| Export *.HOK: kleine Umlaute | Kleine Umlaute werden wie im Remake-Text als { \| } geschrieben, ohne dass das Original dies belegt. | K | A736 |
| HOK-Import: Felder nach Neuaufstellung | Der Import schreibt die Vereinsfelder 0x18–0x1D nur für den Manager am Zug mit 0x375 ≠ 0x376 neu (dessen Neuaufstellung läuft beim Zugbeginn); die Neuaufstellung der übrigen Sätze bildet er nicht nach, und die Wanderung 0x9D1B seit dem letzten Ereignistag bleibt unberücksichtigt. | K | A917 |
| Spielstand-Info Schatten | Der Versatz des Textschattens ist am Bild gemessen. | K | A796 |
| Torhüter-Kennung im Original-Spielstand | Ob ein Kaderspieler Torhüter ist, liest das Remake aus dem Rollenwert statt über die Originalfunktion 0x18:0x0; in allen Ständen gleich. | K | A262 |

## Live-Spiel

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Freundschaftsspiel des Bundestrainers: Zählfeld | Freundschaftsspiele des Bundestrainers zählen im Remake in der Liga; welches Feld das Original dort bucht, ist nicht gelesen. | D | A918 |
| Penaltyschießen: Zweig mit Bit 7 | Für den Zweig mit Bit 7 hat das Remake keine Anzeige (Szene 0x130:0x7F, Meldung 0x130:0x2A je Schuss); ob sie Zufallszahlen ziehen, ist nicht gelesen. Die Penaltytore führt es getrennt, der Spielstand bleibt der Gleichstand. | D | A910 |
| Schneller Pokalweg: Entscheidung je Spieltag | Der schnelle Pokalweg hängt im Remake am Manager (Vereinsfeld 0x16), das Original entscheidet je Spieltag über den Merker 0x36FEA, nicht über die Reihenliste. | D | A896 |
| Verletzung in der Minute: Merker je Tag und Wettbewerb | Das Remake hält den Merker je Spielstand, Tag und Wettbewerb; der Bundestrainer (Satz 5) und DGROUP 0x1DB (zweite Bedingung der Live-Anzeige) fehlen. | D | A912 |
| Bundestrainer: eingespielte Reihe (+0x02) | Der Satz 5 hat ein eigenes +0x02, das das Remake nicht führt: kein Abzug und kein Wurf für den Bundestrainer (Manager 5 in 0x544C0). | E | A900 |
| Live-Spiel Enter/Escape | Enter oder Escape rechnen die restlichen Minuten ohne Anzeige; die Leertaste ist nicht angeschlossen. | E | A449 |
| Minutenzähler: außerhalb des Spielstands | Der Zähler (DGROUP 0x4926–0x4955) gehört im Original nicht zum Spielstand; das Remake hält ihn ebenfalls außerhalb, nach dem Laden 0 (das Original behielte beim Laden im laufenden Programm den alten Wert). | E | A901 |
| Reporter Strahlrücklauf | Ein Strahlrücklauf (70 Hz) zählt als ein Bild bei 60 Bildern je Sekunde. | E | A453 |
| Teamwerte ohne Datenbankbytes | Ohne Datenbankbytes (Rückfall EnsureTeamValues, Aufbauten ohne NewGame.Create) gilt Byte 0 auch für den Sturm. | E | A911 |
| Torszene Bildtakt und Ton | Die 18,2 Bilder je Sekunde werden auf 60 Hz verteilt und „Ton an“ wirkt als Stummschaltung. | E | A474 |
| Torszene Speichermangel | Die Abschaltung der Torszenen bei zu wenig freiem Speicher (mit Meldung) fehlt. | E | A473 |
| Torszene Wiederholung | Die Wiederholung einer Torszene zieht im Remake nicht neu, und die Rückkehr der Szene bei zu wenigen fitten Spielern fehlt. | E | A439 |
| Torszene Wiederholung „r“/„w“ | Die Wiederholung zeigt dieselbe Szene noch einmal, während das Original sie neu auswürfelt. | E | A470 |
| Zufall im Live-Spieltag | Das Remake rechnet die Paarungen eines Spieltags nacheinander statt Minute für Minute verschränkt. | E | A442 |
| Zufall im Minutenschritt | Wie der Zufall auf mehrere Paarungen im Minutenschritt verteilt wird, folgt dem Remake-Spieltag statt 0x37A62. | E | A547 |
| 0x3183C Live-Tore auf den Pokal-/WM-Paarungsseiten | Während laufender Spiele zeigen die Paarungsseiten im Remake keine Zwischenstände. | K | F068 |
| 0x35CBE Hintergrund der Penalty-Anzeige | Das Penaltyschießen läuft im Remake ohne eigene Anzeige, daher fehlen die drei Farbstreifen. | K | F076 |
| Bundestrainer: Managerwert im Spiel | Der gespeicherte Wert DGROUP 0x974B (nur von 0x952C geschrieben) wird im Spiel aus dem Stand gerechnet; zwischen Tageslauf und Spiel ändert sich die Aufstellung nicht. | K | A903 |
| Bundestrainer: SEMI/MANUELL beim Stand 0:0 | Wie bei den Vereinen behält der Bundestrainer mit SEMI/MANUELL die eigenen Werte beim Stand 0:0. | K | A904 |
| Darstellung 2 „Dauer-Eisfeld“ | Bei Darstellung 2 zeigt das Original den ganzen Spieltag das Eisfeld (mit Hallenstufe 2 bei mehreren Managern), das Remake nur die einzelne Szene und danach wieder den Live-Bildschirm. | K | A472 |
| Eingriffsmenü Mausabfrage | Die Maus wird je Bild statt in der Minutenschleife je Managerpaarung abgefragt; die Befehle wirken in beiden Fällen ab der nächsten Rechnung. | K | A454 |
| Eingriffsmenü fertige Paarung | Eine schon fertig gerechnete Paarung nimmt keine Befehle mehr an (Folge des Nacheinander-Rechnens). | K | A766 |
| Gegentore: Torhüter ohne Poolplatz | Torhüter ohne Poolplatz (Datenbankspieler eines Computervereins) folgen in Kaderreihenfolge. | K | A897 |
| Gegnerfenster: Historische Ergebnisse und Einschätzung | Die Zeile „Historische Ergebnisse“ und der dritte Knopf hängen im Original am Wettbewerbscode DGROUP 0xD0C2 = 0 (Hauptmenü); das Remake führt den Code nicht und zeigt beides immer (Knopf und Markierung sind seit zusehen umgesetzt). | K | A286 |
| Höchstzahl Anzeigereihen | Das Remake begrenzt die Anzeige auf vier Reihen, im Original verhindert die Grenze in 0xAE2B mehr Markierungen. | K | A554 |
| Liste der Anzeigereihen | Das Remake beginnt eine neue Reihenliste bei Wechsel von Saison, Tag oder Wettbewerb statt mit einem gemeinsamen Spieltagsablauf. | K | A555 |
| Live-Bildschirm Reporterbild | Die durchsichtige Farbe 0 des Reporterbilds ist aus dem Video abgeleitet, nicht aus dem Code. | K | A457 |
| Live-Bildschirm Zuschauerzeile | Die Mindestbreite der Zuschauerzahl ist mit 1 angenommen statt aus DGROUP 0x374 verfolgt. | K | A475 |
| Live-Spiel Pause bei Geschwindigkeit 62–69 | Geschwindigkeiten 62 bis 69 ergeben keine Pause statt (wie im Original) bis zu einer Stunde Warten; im Original sind diese Werte nicht einstellbar. | K | A448 |
| Live-Spiel Reihen mehrerer Managerpaarungen | Das Remake rechnet die Managerpaarungen nacheinander, spätere erscheinen erst mit ihrem Spiel; im Original laufen alle gleichzeitig. | K | A450 |
| Minutenzähler: Bundestrainer | Der Bundestrainer (Satz 5) beginnt jedes Spiel weiter bei 0. | K | A902 |
| Play-Off-Spiele: Minutenanzeige | Die Anzeige „n.Minute“ während des Spielablaufs fehlt. | K | A436 |
| Rechtsklick ohne Reihe im Spiel | Bei Rechtsklick ohne gewählte Reihe meldet das Original nichts. | K | A678 |
| Reihen auf dem Eis der Länder | Die Reihen 0x2E/0x30 der Länder führt das Remake nicht (Einsatz und Angriff stehen im Satz des Landes). | K | A908 |
| Reporter Zwinkern | Der Zufall für das Zwinkern des Reporters gilt je Spielstand statt für das ganze Programm. | K | A452 |
| Reporter-Meldung vergebene Chance | Bei vergebenen Chancen nennt das Remake keinen Schützen, das Original wählt einen (0x386D1) und meldet ihn im Reporter. | K | A456 |
| Spiele zählen beim Aufstellen im Spiel | Das Remake zählt ein im Spiel neu Aufgestellter (und das Herausnehmen) am Spielende, ein Hinein und Hinaus im selben Eingriff zählt nicht; die Spalte SP zeigt das Spiel erst danach. | K | A898 |
| Spielwertung: Strafenzähler nach dem Spiel | Verweis auf das [APPROX] in MatchEngine.PenaltyBoxQuery.cs: nach einem Spiel sitzt niemand auf der Strafbank, daher kein Zustand im Remake; kein eigener Befund. | K | A964 |
| Strafbank-Abfrage der Aufstellung | Nur Vereine; die Nationalaufstellung fragt nicht ab. Was nach dem Spiel mit +0x0D/Bit 0 eines bei Spielende noch Bestraften geschieht (Abzug 0x5490C, Löschen 0x551D4–0x551E0), bildet das Remake nicht ab. | K | A907 |
| Strafliste der Anzeigereihe | Eine Strafe ohne wählbaren Spieler zählt im Remake nur in den Strafminuten der Anzeige, nicht in der Strafliste. | K | A444 |
| Torhüter draußen am Spielende: Zeitpunkt der Reihe | Das Remake setzt die Reihe beim nächsten Spiel des Vereins (das Original an jedem Spieltag bis dahin, gleichwertig für das Spiel); HOK-Import und -Export führen das Bit nicht. | K | A942 |
| Tormeldung: Paarungen nacheinander gerechnet | Das Remake rechnet die Paarungen nacheinander und legt die Meldung für jede markierte Paarung ohne Reihe in der Reihenliste ab; angezeigt wird sie nur mit Live-Bildschirm. | K | A909 |
| Torszene Taste zur Wiederholung | Das Remake wertet nur eine während Szene oder Pause getippte Taste aus, das Original liest den Tastaturpuffer (auch früher getippte Tasten). | K | A471 |
| WM-Endrunde: Minutenanzeige | Die Anzeige „60.Minute“ während des Spielablaufs fehlt. | K | A292 |
| WM-Spiel: Tore +0x1B und Gegentore | Das Spielerfeld Tore +0x1B führt das Remake nicht getrennt; Gegentore stehen am Spieler (Poolfeld +0x1B des Torhüters nicht gelesen). | K | A906 |
| WM-Tag: Strafe ohne Spieler | Bleibt eine Strafe ohne Spieler aus 0x378C5 (kein Ereignis), schreibt das Original 0xA70E trotzdem; das Remake sieht nur eingetragene Strafen. Die Paarungsfolge des Termins ist die des Remakes. | K | A880 |
| Zusehen: Vereinsfeld 0x16 und Länder | Vereinsfeld 0x16 (Manager + 1) ist im Remake IsManaged; bei Ländern schreibt das Original das Feld auch in der WM-Schleife (Strafen), das bleibt unberücksichtigt; Vereinssätze über die Vereinsliste hinaus (Europapokal-Gegner) zählen nicht. | K | A833 |
| Zwinkern in der Live-Anzeige | Das Zwinkern zieht aus einem eigenen Zufallszustand statt aus dem mit der Spielrechnung geteilten. | K | A438 |

## Tages-/Saisonwechsel

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Modus „nur Weltkugel“: Kader von Satz 0 | Im Modus „nur Weltkugel“ rechnet das Remake für den Kader des Managers keine Verträge, kein Training und keine Aufstellung; belegt nur durch zwei kurze Spielstandpaare. | D | A408 |
| Oberliga-Absteiger bei doppeltem Platz | Mit doppeltem Platz kann ein Zweitligist, der nur den Platz wechselt, als Absteiger gelten; das ist nicht an einem Spielstand geprüft. | D | A667 |
| Oberliga-Tausch im Modus „nur Weltkugel“ | Steht im Modus „nur Weltkugel“ der Managerverein auf den letzten Oberligaplätzen, setzt das Original das Konto von Satz 0 auf 400.000 DM; das Remake lässt es. | D | A671 |
| Rückkehr gelisteter Spieler am Saisonwechsel | Hat der Kader schon 29 Spieler, wird ein von der Liste zurückkehrender Spieler im Remake vertragslos; was das Original tut, ist offen. | D | A706 |
| Verteilung des Pools am Saisonwechsel | Die Gruppen werden über die Ligazugehörigkeit statt über Indexbereiche gebildet; Gruppe 3 umfasst Vereine ohne Liga bis Index 47 statt 36–42. | D | A713 |
| 0x4CD46 Kopierschutz Passwortabfrage | Die Handbuchfrage entfällt im Remake. | E | F129 |
| Fair-Play-Folge ohne Satzfolge | Fehlt die gespeicherte Satzfolge, entscheidet bei gleichen Strafminuten die Indexfolge. | E | A657 |
| Ligamischung am 29. April: lrandom und Wanderung | lrandom ist nicht nachgebaut (Folge nicht bitgenau); die Teamwert-Wanderung (Datei 0x56448), die im Original unmittelbar davor zieht, läuft im Remake erst in StartNextSeason nach der Mischung. | E | A935 |
| Listenwechsel entlassener Manager | Entlassene Manager würfeln den Listenwechsel mit dem nächsten aktiven Zug. | E | A385 |
| Oberliga-Tausch mit entlassenen Managern | Entlassene Manager zählen beim Oberliga-Tausch weiter mit; Entlassene gibt es nur noch in älteren Spielständen. | E | A660 |
| Plätze außerhalb der Ligen | Bei mehreren Managern mit Vereinen ohne Liga leitet das Remake die Paare gewählter/verdrängter Verein ab, statt sie zu speichern. | E | A659 |
| Pool der Vertragslosen am Saisonwechsel | Überzählige Vertragslose (die ältesten, dann die schwächsten) beenden die Laufbahn (Remake-Ergänzung). | E | A240 |
| Saisonabschluss ohne aktive Manager | Sind alle Manager ausgeschieden (nur ältere Spielstände), schließt die Saison noch ab und zeigt das Spielende. | E | A669 |
| Saisonende: Seiten entlassener Manager | Entlassene Manager bekommen am Saisonende keine Seite, das Remake beginnt beim ersten nicht entlassenen. | E | A536 |
| Soll-Spielerzahl je Liga | Bei größeren Kadern wird der Rest R nicht negativ, im Original würde das Byte überlaufen. | E | A712 |
| Spiele beim jetzigen Verein | Der Übertrag der Spiele gilt auch für Computervereine und geliehene Spieler. | E | A718 |
| Spielende: Entlassung | Wortlaut und Gestaltung der Entlassung sind nicht belegt; das Spiel entlässt nicht mehr, nur ältere Spielstände. | E | A798 |
| Spielende: Rückkehr zum Titel | Das Remake kehrt nach dem Spielende zum Titel zurück statt das Programm zu beenden und nutzt die Standardpalette. | E | A799 |
| Spielende: Rückkehr zum Titel | Das Remake zeigt nach dem Spielende den Titel statt das Programm zu beenden. | E | A800 |
| Tageslauf entlassener Manager | Ein entlassener Manager hat im Remake keine Kaderplätze und kein Training mehr. | E | A520 |
| Tageslauf: Verfall, Sperre und Gebotsverfall der Markt-Plätze | Das Original rechnet Verfall, Sperre und Gebotsverfall je Markt-Platz in einer Schleife (Datei 0x0719F–0x0727C, ein weiterer Wurf im Gebotsverfall ab 0x07215); das Remake rechnet erst Verfall und Sperren aller Plätze, dann die Gebote. | E | A829 |
| Tausch Pool/Kader am Saisonwechsel | Beim Tausch werden auch die Werte getauscht, damit die Stärke des Computerkaders bleibt. | E | A393 |
| Top-5-Bit bei Gleichstand | Das Remake nimmt für das Top-5-Bit immer die Abschlusstabelle „Gesamt“; im Original kann die Reihenfolge völlig gleichstehender Vereine vom Wechseln der Tabellenansicht abhängen. | E | A164 |
| Vergebene Poolplätze am Saisonwechsel | Vergebene Plätze, deren Spieler nicht mehr bei einem Manager ist, werden erst am Saisonwechsel neu besetzt. | E | A220 |
| Wanderung der Teamwerte am Saisonwechsel | Das Original wandert am 29. April nach dem Platztausch von Auf- und Absteigern und vor der Mischung der neuen Saison (Datei 0x56448 vor 0x5644E); das Remake wandert in StartNextSeason und geht dort nach der gemischten Folge der neuen Saison; Vereine ohne geführte Folge folgen nach Index. | E | A843 |
| Zielverein eines Poolspielers | Das Remake wählt den Verein nach Rang im Vereinsindex statt nach der umsortierten Tabelle und ersetzt Verein 150 durch 149. | E | A715 |
| Zwangsabstieg nur aktiver Manager | Ausgeschiedene Manager (nur aus älteren Spielständen) lösen keinen Zwangsabstieg aus. | E | A666 |
| Datumstext am Saisonende | Die Wochentagskorrektur hängt am übergebenen Tag statt am Zähler. | K | A398 |
| Markt am Saisonwechsel | Listeneinträge werden neu angelegt, der Eintragstag wird 0. | K | A729 |
| Nur-eine-Saison: Platztausch | Der Platztausch greift im Remake nur, wenn der Verein in der Bundesliga steht. | K | A914 |
| WM-Schalter: Sicherung 0x22C | Das Remake sichert erst bei der Ernennung zum Bundestrainer; ohne Ernennung gibt es in der Schleife kein Menü, in dem der Schalter sich ändern könnte – Sicherung und Rückstellung sind dann wirkungslos. | K | A835 |
| WM-Schleife: Heilung von Spielern ohne Kaderplatz | Spieler ohne Kaderplatz eines Managers heilt das Remake wie die mit Kaderplatz (HealUnmanaged). | K | A879 |
| Zeitpunkt des Oberliga-Zugs | Das Remake zieht die Oberliga-Nachrücker schon am 13. April, das Original am 29. April. | K | A661 |
| Zugfolge bei entlassenem ersten Manager | Beleg nur für zwei Manager ohne Entlassung (FILE76, drei Spieltage Spieler1 → Spieler2); mit entlassenem ersten Manager beginnt das Remake beim ersten aktiven. | K | A962 |

## Werkzeuge

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Abgleich HOK „ohne Eingriff“ | Das Abgleichwerkzeug legt fest, was „ohne Eingriff spielen“ im Remake heißt (je Halt OnMainMenu, Tag weiter, Fragen unbeantwortet). | E | A406 |
| Abgleich: Toleranz Kontostand | Das Abgleichwerkzeug lässt beim Kontostand 50.000 DM bzw. 10 % der Veränderung Abstand zum Original zu. | E | A738 |
| Abgleich: Toleranz Poolmittel | Das Abgleichwerkzeug lässt beim Mittelwert des Spielerpools ±8 Punkte Abstand zum Original zu. | E | A407 |
| Abgleich: Toleranz Teamwerte | Das Abgleichwerkzeug lässt bei Kondition/Technik der Teams ±15 (über einen Saisonwechsel ±35) Abstand zu. | E | A739 |
| Bildschirmfoto-Ablauf Bundestrainer Modus 4 | Der Ablauf ernennt den Manager notfalls ausdrücklich zum Bundestrainer, und zwar nach dem Auswürfeln der Länderwerte. | E | A741 |
| emtool hok import: Zufallssaat | Der Import nutzt ohne --seed die feste Saat 1993 statt einer neuen Saat. | E | A740 |
| emtool export-sound: Effektnamen | Die Dateinamen der exportierten Soundeffekte sind nach Gehör vergeben. | K | A266 |

## Wettbewerbe

| Funktion bzw. Bildschirm | Abweichung | Klasse | ID |
|---|---|---|---|
| Reihenzahl beim Reihenwechsel | Mit vierter Reihe und Modus ≠ 1 ist der Reihenzähler im Original nicht gesetzt; das Remake nimmt 4 bzw. 3. | D | A095 |
| 0x25AF9 Zufallsfolge random(100,300)-mal vorspulen | Das Vorspulen des Zufallsgenerators entfällt, weil das Remake eigene .NET-Zufallsquellen nutzt. | E | F054 |
| Aufstellung des Bundestrainers verlassen | Das Remake rechnet die Länderwerte beim Verlassen nur, wenn die Aufstellung geändert wurde. | E | A486 |
| Aufstiegsrunde: Paarungen | Das Remake zieht weiter den Zufallszug des früheren Kreisverfahrens, obwohl das Original hier nicht würfelt. | E | A637 |
| Highscore: Kennzahlen Play-off und Europapokal | Das Original schreibt die Europapokal-Kennzahl nie (Fehler des Originals); das Remake schreibt 9, 10 bzw. 0x8D. | E | A059 |
| Highscore: Saisonzahl | Die Saisonzahl ist die Länge des Verlaufs dieses Managers statt des globalen Saisonzählers. | E | A064 |
| Highscore: Spielerwerte und Bewertung | Bewertung und Spielerwerte rechnen mit festen Zufallsströmen, damit das Öffnen der Bestenliste nichts verändert. | E | A061 |
| Lage im lrandom-Zyklus | Die Lage im Zufallszyklus beim ersten Zug gilt im Remake als gleichverteilt. | E | A540 |
| Länderspiele: Teamwerte (Altstände) | Nur in Spielständen ohne Teamwerte (vor Schema 12) zieht der Aufruf sie vorher; neue Spiele haben sie ab dem Spielstart wie im Original. | E | A824 |
| Minutenweg je Paarung | Das Remake wählt den Minutenweg je Paarung statt je Spieltag. | E | A541 |
| Neuer Manager ohne Vereinswahl | Ein zufällig zugeteilter Verein wird nicht in die Start-Liga getauscht. | E | A463 |
| Platzliste der Folgesaison | Das Remake setzt die Platzliste aus der Abschlusstabelle zusammen statt eine eigene Liste zu führen. | E | A161 |
| Satzfolge beim Programmstart | Die Ligamischung wird aus dem Spielstart-Seed abgeleitet statt aus dem gemeinsamen Zufall nach srand(time). | E | A560 |
| Schalter DEB-Pokal | Der Schalter zum Abschalten des DEB-Pokals existiert im Original nicht und dient nur Tests. | E | A055 |
| Spielerstatistik und Einzelprämien | Das Remake sortiert alle Spieler der Liga statt der 199 Poolspieler; nur die Reihenfolge bei völligem Gleichstand kann abweichen. | E | A198 |
| Spielplan beim Saisonwechsel | Das Remake zieht weiter Zufallszahlen für eine bedeutungslose Mischung, obwohl das Original hier nicht würfelt. | E | A422 |
| Spielstart: Computerkader und Ligamischung außerhalb des Stroms | Ohne Gegenstück im Original und deshalb außerhalb des Stroms: die Kader der Computervereine (eigene Quelle aus der Saat) und die Ligamischung vor der Spielvorbereitung (aus der Saat abgeleitet, weil die Vereinsauswahl sie vor dem Spielstart zeigt); die Zufallswahl eines Vereins zieht vor dem Pool aus dem Strom. | E | A846 |
| Spielstärke im alten Weg (MatchStrength, ReferenceRating) | Diesen Weg und diese Verschiebung gibt es im Original nicht; die Spielberechnung (PlayWithEvents) benutzt sie nicht. | E | A859 |
| Strafzug im lrandom-Zyklus | Die Lage im Zufallszyklus beim Strafzug gilt als gleichverteilt. | E | A552 |
| Tabellensortierung | Eine angesehene Heim- oder Auswärtstabelle ändert die Ausgangsreihenfolge des nächsten Sortierens nicht. | E | A159 |
| Teamwerte: Rückfall für Altstände | Rückfall für Spielstände ohne Teamwerte (vor Schema 12) und Aufbauten ohne NewGame.Create: zieht die Teamwerte, falls ein Verein noch keine hat, und wendet die Wanderung des Spielstarts an. | E | A844 |
| Teamwerte: Spielstände vor Schema 12 | Ältere Spielstände ohne Teamwerte und Aufbauten ohne NewGame.Create ziehen sie weiterhin beim ersten Bedarf (EnsureTeamValues). | E | A841 |
| Torschützen der Computervereine | Die Spielerreihenfolge kommt aus dem Remake-Kader statt dem Spielerpool, Tore ohne Grenze 255. | E | A103 |
| Ungeseedeter Zufall | Ein ungeseedeter Zufall nimmt einen zufälligen Seed statt des .NET-Standardalgorithmus. | E | A143 |
| WM Vorbereitung: Belastung 70 | Die Belastung der Kaderspieler wird erst beim ersten Freundschaftsspiel statt schon beim Öffnen des Bildschirms auf 70 gesetzt. | E | A488 |
| WM Vorbereitung: Energie 70 vor dem ersten Spiel | Die Energie wird erst beim ersten Freundschaftsspiel statt beim Öffnen des Bildschirms gesetzt. | E | A883 |
| WM-Endrunde im Managertag-Modus | Dass die WM-Endrunde im Managertag-Modus die Minutenschleife erreicht, ist nur aus dem Aufrufmuster geschlossen. | E | A542 |
| WM-Kader: Grundstärken eines Kandidaten | Kondition und Technik führt das Remake nur einmal je Spieler (Kaderplatz = Pool), die Form des Pools eigens (PoolForm). | E | A872 |
| WM-Kader: Kandidatenliste ohne Pool | Ohne Pool (Altstände, Proben) bietet das Remake alle deutschen Spieler nach Kondition + Technik + Form an; mit Pool wie das Original nach Poolplatz. | E | A871 |
| WM: Länderreihenfolge beim Spielstart | Das Original mischt die Länder beim Spielstart in 0x35EDD vor der Spielvorbereitung; das Remake leitet diese eine Mischung aus dem Zustand des Stroms ab, ohne ihn weiterzuschalten. | E | A822 |
| WM: Mischung bei Ständen ohne gespeicherte Reihenfolge | Fehlt die Mischung (Stand vor Schema 55 oder Import nach dem 29. April), mischt das Remake beim Saisonwechsel wie bisher. | E | A884 |
| Wanderung des Spielstarts (Rückfall) | Die Wanderung des Spielstarts (0x43A73, Modus 10) läuft für Altstände erst beim ersten Bedarf. | E | A845 |
| Zufallsfolge der Spielrechnung | Das Remake zieht aus .NET-Random statt aus der lrandom-Folge; nur der 5-Minuten-Zug bildet den Zyklus statistisch nach. | E | A441 |
| Zufallsfunktionen | Das Remake zieht beide Zufallsfunktionen aus einem .NET-Random; Reihenfolge und Grenzen sind original, die Zahlenfolge nicht. | E | A100 |
| Zufallswahl des Vereins | Die Zufallswahl eines Vereins nach Start-Stärke gibt es nur im Remake. | E | A557 |
| Zusatzeinstellung „Ganzer Tag“ | Die Zusatzeinstellung „Ganzer Tag“ ist kein Schalter des Originals; ihr WM-Teil ist nur aus dem Aufrufmuster geschlossen. | E | A516 |
| Alter Spielweg mit Penaltyschießen | Der alte Weg entscheidet Penaltyschießen nach Stärke-Zufall statt nach dem Original. | K | A137 |
| Altes Poisson-Ergebnis | Die alte Poisson-Rechnung hat im Spielablauf keinen Aufrufer mehr. | K | A426 |
| Bundestrainer: Zähler Über-/Unterzahl über den Saisonwechsel | Ein neues Amt beginnt im Remake mit leeren Zählern; im Original überleben die Zähler des Satzes 5 Ernennung und Saisonwechsel. | K | A876 |
| DEB-Pokal-Auslosung live: Aufdeckreihenfolge | Die Aufdeckreihenfolge kommt aus einer eigenen Quelle statt aus dem gemeinsamen Zufallsgenerator, daher verbraucht das Zuschauen keine Zufallszahlen. | K | A271 |
| DEB-Pokal-Auslosung live: Mischung | Die Mischung des Aufdeckens hängt nur von Runde und Ziehung ab und greift nicht in den Pokal-Zufall ein. | K | A274 |
| Ergebnismarke Verlängerung | Das Remake bildet die Ergebnismarken +10/+30 als Kennzeichen „Verlängerung“ ab. | K | A098 |
| Gegentorschnitt ohne Minuten | Ohne Minuten setzt das Remake den Gegentorschnitt auf 0. | K | A199 |
| Highscore: Gesamtpunkte | Die Gesamtpunkte werden auf 1 … 32767 begrenzt statt als 16-Bit-Wert überzulaufen. | K | A063 |
| Kreisverfahren als Rückfall | Für Ligen mit ungleich 12 Vereinen nutzt das Remake ein eigenes Kreisverfahren. | K | A561 |
| Länderspiel: Spiele zählen am Spielende | Das Remake zählt die Länderspiele am Spielende aus der Aufstellung (CreditWorldCupSquad), das Original beim Aufstellen im Spiel. | K | A874 |
| Länderwerte bei leerem WM-Kader | Mit leerem WM-Kader behält Deutschland im Remake seine alten Werte statt 0; im Spiel mit Oberfläche verhindert die Warnung vor dem Weiterschalten diesen Fall wie im Original. | K | A484 |
| Länderwerte im Modus „nur Weltkugel“ | Das Remake rechnet die Werte Deutschlands an jedem Tag statt nur nach dem Menü an WM-Tagen; gelesen werden sie nur an WM-Tagen, wo beide gleich rechnen. | K | A483 |
| Modus „nur Weltkugel“: Länderwerte | Das Remake rechnet Schritt (a) der WM-Schleife auch an Tagen vor dem ersten WM-Tag, an denen das Original keinen Tageslauf hat. | K | A528 |
| Nationalaufstellung: Reihenwerte der Anzeige | Die Reihenwerte der Anzeige bleiben Remake-Werte ohne den sechsten Feldspieler; keine Spielwirkung. | K | A961 |
| Neuzählung Stürmer unter 0 | Fällt die Stürmerzahl rechnerisch unter 0, hält das Remake sie bei 0, das Original rechnet mit einem Byte. | K | A550 |
| Penaltyschießen Plätze ohne Spiel | Plätze mit Ergebnisbyte über 0x63 (Paarung ohne Spiel) führt das Remake nicht, weil es keine solchen Paarungen hat. | K | A423 |
| Platztausch beim Hinzufügen eines Managers | Sind alle Tauschplätze ausgeschlossen, tauscht das Remake nicht, wo das Original endlos weiterzöge. | K | A460 |
| Platztausch: Zeile der Taktik-Automatik | Die zuletzt angezeigte Zeile der Taktik-Automatik wandert beim Platztausch mit, im Original bleibt sie am Platz. | K | A461 |
| Play-Off-Spiele: Marken | Die Punkte nach Penaltyschießen bzw. Verlängerungstor zeigt das Remake bei Play-Offs nicht. | K | A435 |
| Play-off-Marken: Marke des Zusatzwurfs | Die eine Marke des schnellen Wegs (Zusatzwurf der ersten offenen Paarung) bildet das Remake nicht nach; der HOK-Export schreibt 0 statt 1. | K | A925 |
| Play-off-Marken: Tor in der Verlängerung | Zwei Marken wie beim Pokal; 0x38402 setzt +30 auf den Stand aus DGROUP 0xA0BF, dessen Marken nicht verfolgt sind (A482). | K | A924 |
| Pokal: Marken nach Tor in der Verlängerung | Dass nach einem Tor in der Verlängerung zwei Marken am Endstand stehen, ist nur aus dem Video übernommen, nicht im Code verfolgt. | K | A482 |
| Rollenwert Spielerfeld 0x18 | Das Remake nimmt für den Rollenwert die Mitte der Spanne statt des Poolwerts. | K | A102 |
| Rückfall-Penaltyschießen | Ohne Shootout-Ergebnis entscheidet ein Stärke-Zufall statt des Penaltyschießens des Originals. | K | A138 |
| Spielplan des Vereins: „gespielt“ | Gespielt heißt im Remake: Ergebnis des Spieltags vorhanden; das Original vergleicht den Spieltag mit DGROUP 0x7A9 + Liga + b (b = 1 nur aus den Paarungen Seite 2 und der Tabelle, Datei 0x4F0C9–0x4F0DB, 0x5018C); alle Aufrufer des Remakes übergeben b = 0. | K | A868 |
| Startwert Feldspielerzahl | Die Feldspielerzahl je Teil beginnt im Remake bei 2 und 3 statt beim Stand der letzten Neuzählung. | K | A548 |
| Tabelle DEB-Pokal: Marken und Klick | Die zwei Punkte nach Verlängerungstor stützen sich auf ein Videobild, und ein Klick auf einen Verein führt zurück statt das Vereinsfenster zu öffnen. | K | A277 |
| Teamwerte: Speicherung als Abweichung | Kondition und Technik werden als Abweichung vom Datenbankwert gespeichert, die Liga-Grenzen gelten für Datenbankwert + Abweichung; Nationalteams spielen als RatedNation mit eigenen Werten, ihre Form wird nur gezogen und gesetzt. | K | A842 |
| Torzähler Spielerfeld 0x1B | Das Remake führt das Pool-Torfeld (nur Liga) und dessen Grenze 255 nicht getrennt von den Toren je Wettbewerb. | K | A545 |
| Vereinsfenster: Bilanz und Tore/Spiel | Bilanz und Tore je Spiel werden aus den Ligaergebnissen des Remakes statt aus dem Tabellensatz des Originals berechnet. | K | A753 |
| Vereinsfenster: Ligazeile bei Vereinen ohne Liga | Für Vereine ohne Liga zeigt das Remake wie das Original „Oberliga“, der Platz (immer 1.) ist nicht belegt. | K | A755 |
| Vereinsfenster: Wettbewerbscode | Der Wettbewerbscode DGROUP 0xD0C2 wird nicht geführt (die Sprünge Datei 0x4D750 → 0x4D78A hängen an ihm). | K | A867 |
| Vorlagenwahl Torschütze | Das Remake bricht die Vorlagensuche nach 1000 Zügen ab, das Original würde endlos weitersuchen. | K | A069 |
| WM-Endrunde: Marken | Woher der Spielstand vor dem Verlängerungstor kommt, ist nicht verfolgt. | K | A759 |

Nicht aufgeführt: 39 Funktionen ohne Marker mit Grund „nicht nötig“ (keine Spielwirkung).
