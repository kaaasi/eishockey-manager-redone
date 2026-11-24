# Bewertung der Abweichungsliste (Claude, Leitstelle Runde 24)

Stand `ebdc3c6`. Grundlage: [abweichungen-v1.md](abweichungen-v1.md), Vergleich mit [ChatGPT](abweichungen-bewertung-chatgpt.md) (PR #992) und [Gemini](abweichungen-bewertung-gemini.md); Entscheidungen des Nutzers vom 24.11.2026 eingearbeitet (B11 = A, B31 = A, S01 vor v1.0).

**Klassen:** N – Spielmechanik, Werte oder Abläufe weichen ab; S – Änderung sinnvoll (Bedienung, Code-Struktur, Prüfung bei Geld); O – nur Anzeige/Ton, Änderung empfohlen; K – keine Änderung; D – wartet auf DOSBox. Reine Pixelabweichungen ohne Nutzen einer Änderung stehen unter K (Nutzer 24.11.2026).

**Zählung:** N 6, S 15, O 10, K 71, D 7 (zusammen 109).

| Nr | Thema | Claude | ChatGPT | Gemini | Begründung |
|---|---|:---:|:---:|:---:|---|
| B01 | Titelmusik in Endlosschleife | **O** | O | O | Ton: Titelmusik einmal statt Schleife; keine Spielwirkung. |
| B07 | Nachrichtenleiste: Umbruch, Hinweis, Zähler | **S** | S | O | Abgeschnittene Meldungen zwingen in den Posteingang; Trennung an „^“ belegt. |
| B10 | Paarungsseiten Pokal/WM und Europapokal-Tabelle merken die letzte Seite nicht | **S** | S | S | Bedienung: gemerkte Seite erspart Blättern; kein Spielzustand. |
| B12 | Hauptmenü: „Tag weiter“ mit rechter Maustaste | **S** | N | S | Bedienung: zweiter Weg zum Tageswechsel; vorher em.exe außerhalb des Logos lesen. |
| B13 | Nationalaufstellung: Spielerinfo per Rechtsklick fehlt | **S** | S | S | Fehlende Bedienfunktion, sofern em.exe sie fürs Nationalteam belegt. |
| B15 | Gegnerfenster: Gesicht, Tore/Spiel, Schnitt, Bilanzzeichen | **O** | N | O | Anzeigewerte; keine Spielregel betroffen. |
| B17 | Nationalteam: Reihenwerte der Anzeige | **O** | N | O | Anzeigewerte; Funktion 0x977F schon vorhanden. |
| B18 | Hauptmenü: Kachelmarkierung, Managerkopf, Kurve und Lampe des Bundestrainers | **O** | S | O | Hauptmenügrafik; Kurvenfeld des Bundestrainers. |
| B25 | Saisonende: Meistersymbol und Pokalbild | **O** | O | O | Grafik am Saisonende; Teile liegen vor. |
| B28 | Historie: Balkenhöhe und -farbe | **O** | O | O | Balken; vorher em.exe lesen. |
| B29 | Stärken-Bildschirm: Smiley, Managervereine, Startwerte | **O** | N | O | Anzeige; braucht Spielstandfeld. |
| B33 | Spielerfenster und Spielerinfo | **O** | N | O | Anzeige; braucht Spielstandfeld. |
| S02 | Torszene: Darstellung 2 „Dauer-Eisfeld“ | **O** | O | O | Darstellung; Ablauf vorher in em.exe lesen. |
| S04 | Paarungsseiten im Spiel: Minute, Zwischenstände, Penalty-Anzeige | **O** | N | O | Fehlende Anzeigen; Quellen bekannt. |
| C07 | Pokal- und Play-off-Marken, Klick auf Verein | **S** | N | S | Bedienung (Klick ins Vereinsfenster) und Marken. |
| B02 | Musik: OPL2-Emulation und Abspielorte | **K** | K | K | Plattformersatz; Musik nur im Titel nach #221. |
| B03 | Geräteeinstellungen, Lautstärke, Zusatzschalter | **K** | K | K | Remake-Erweiterung. |
| B04 | Optionen: RAM-Zeile, Speicherprüfung, Hintergrund/Scrolling | **K** | O | K | Plattformersatz. |
| B05 | Gehaltene Knöpfe und Pfeile: Bildtakt statt Warteschleife | **K** | K | K | Plattformersatz (Bildtakt statt Rechnertempo). |
| B06 | Posteingang | **K** | K | K | Remake-Erweiterung, Texte original. |
| B08 | Meldungs- und Kurzmeldungsfenster | **K** | S | K | Pixellage bzw. nur Remake-Texte. |
| B09 | Bildwechsel: Zwischenbild | **K** | O | K | Ladezeit hat kein Gegenstück. |
| B11 | Komforttasten Escape, Rechtsklick und „jede Taste schließt“ | **S** | S | K | „Jede Taste schließt“ angleichen, Escape/Rechtsklick behalten (Nutzer 24.11.2026, B11 = A). |
| B14 | DEB-Pokal-Auslosung live | **K** | O | K | Ergebnis gleich; nur Animation. |
| B16 | Vereinsfenster: Bilanz, Ligazeile, Spielplan | **K** | S | K | Abweichung nicht belegt (A495/A753 widersprechen sich) – Frage 1. |
| B19 | Werbung: Absage, Verlängerungsfrage, Blätterstart | **K** | S | K | Zeilenaufteilung ohne Beleg. |
| B20 | Finanzen-Bildschirm: Balken, Kontostand, Kursgrafik | **K** | O | K | Pixel; keine Zahl weicht ab. |
| B21 | Jugend-Bildschirm | **K** | S | K | Pixel und Rückkehrklick, am Video abgeglichen. |
| B22 | Stadion: Klickflächen Anzeigetafel und Sitzkomfort | **K** | S | K | Ohne Beleg wäre jede Änderung geraten. |
| B23 | Spieltag-Bildschirm: Tempo und Banner | **K** | O | K | Tempo Plattformersatz; Banner unbelegt. |
| B24 | Tabellenkurve: Lage der Platzzahlen | **K** | O | K | Wenige Pixel. |
| B26 | Spielvorbereitung: Absicherungen | **K** | K | K | Unerreichbare Zustände. |
| B27 | Bestenliste: Farbe, Kennung, Zentrierung | **K** | O | K | Pixel/Farbe, Kennung erst mit DOSBox. |
| B30 | Statistik-Bildschirme: Bildlauf und Sortierung | **K** | S | K | Nur Oberfläche. |
| B31 | Aufstellung und Taktik verlassen: nur nach Änderung | **N** | N | K | Original schreibt beim Verlassen auch ohne Änderung; Kern-Ereignis beim Verlassen (Nutzer 24.11.2026, B31 = A). |
| B32 | Aufstellung: Remake-Tasten A und C, Pfeilrichtung der Modi | **K** | K | K | Komfort vor dem Spiel; im Spiel abgeschaltet. |
| B34 | Transferdialoge: Lage und Aufbau ohne Videobeleg | **K** | O | K | Lage aus dem Code; nur Pixel. |
| B35 | Transfermarkt: Meldungen | **K** | S | K | 0x46DEF umgesetzt; Rest unbelegt. |
| B36 | Transfermarkt: Kontoleiste, Trennlinien, Klickflächen | **K** | O | K | Pixel. |
| B37 | Kleine Darstellungsdetails | **K** | O | K | Pixel/Randfälle. |
| B38 | Paarungsseiten nach dem Laden | **K** | S | K | Oberflächenzustand nicht im Spielstand (wie Original nach Neustart). |
| F01 | Ganze DM statt Drittel-DM | **K** | K | K | N1 = B. |
| F02 | Buchung statt direkter Gutschrift | **K** | K | K | N5 = C. |
| F03 | Finanzbuch und Leihanfrage per Nachricht | **K** | K | K | Remake-Erweiterung. |
| F04 | Werbebewertung: Platzfelder und Sonderfälle | **K** | S | K | Ohne Wirkung im normalen Spiel. |
| F05 | Finanzen: erneute Buchung nach Verlassen des Streifens | **K** | K | K | Fehler des Originals (N2 = B). |
| F06 | Werbeangebot überschreibt Bande | **S** | S | K | Prüfen: übernommener Monatsbetrag des Teilvertrags (Geld) – Frage 2. |
| F07 | Finanzübersicht: Sparbuch und Kredit getrennt | **K** | S | K | Saldo gleich. |
| F08 | Stadion, Zuschauer und Sommerpause: Reihenfolge | **S** | S | K | Prüfen: Buchjahr der Sommerbeträge (Geld) – Frage 2. |
| F09 | Monatliche Grundeinnahme: Sturm-Teamwert | **K** | K | K | Nur Altstände. |
| K01 | Computervereine mit eigenem Kader: Aufbau und Werte | **K** | K | K | Nutzerentscheidung 01.11.2026. |
| K02 | Kader-KI der Computervereine | **K** | K | K | Folge der Nutzerentscheidung 01.11.2026. |
| K03 | Spielerpool und Computerkader: Tausch und Namen | **K** | K | K | Folge der Nutzerentscheidung 01.11.2026. |
| K04 | Hilfsgrößen ohne Gegenstück | **S** | S | S | Toter Stärkeweg und Hilfsgrößen entfernen (#936). |
| K05 | Heilung von Spielern ohne Managerkader | **K** | K | K | Ohne Wirkung. |
| K06 | Nationalaufstellung: Reihenfolge, Ersatz, Klick | **K** | S | K | Regel gleich. |
| K07 | Aufstellung: Modell und Automatik | **N** | N | K | Verletzter spielt Reihe 0 ein; wirkt auf Werte im Spiel. |
| K08 | Neuer Manager mitten im Spiel | **K** | K | K | Remake-Erweiterung bzw. Nutzerentscheidung. |
| K09 | Verkauf an Computerverein und Sponsorzuschuss | **K** | K | K | Fehler des Originals (N2 = B) bzw. Altstände. |
| K10 | Unaufgeforderte Angebote | **K** | K | K | Fehler des Originals (N2 = B). |
| K11 | Transfer: Abläufe ohne Oberfläche und interne Kennzeichen | **S** | S | K | Doppelte Prüfung vereinfachen (#936). |
| K12 | Leihrückkehr bei vollem Kader | **K** | S | K | Original liefert ungültigen Platz; unerreichbar. |
| K13 | Vertragsdialog: Gehalt 0 | **S** | S | K | Sonderwert 1 DM entfernen (#936). |
| L01 | HOK-Export: Wettbewerbe und Nationalteam | **K** | K | K | Export ist Remake-Erweiterung. |
| L02 | HOK-Export: Liga- und Vereinssätze | **K** | K | K | Export ist Remake-Erweiterung. |
| L03 | HOK-Export: Kader und Pool | **K** | K | K | Export ist Remake-Erweiterung. |
| L04 | HOK-Export: Kopf, Satzfolge und allgemeine Felder | **K** | K | K | Export ist Remake-Erweiterung. |
| L05 | HOK-Export: Manager, Rekorde, Werbung | **K** | K | K | Export ist Remake-Erweiterung. |
| L06 | HOK-Import: Wettbewerbe und Nationalteam | **K** | K | K | N4 = C. |
| L07 | HOK-Import: Markt, Werbung und Spielernamen | **K** | K | K | N4 = C. |
| L08 | HOK-Import: Nachspielen und allgemeine Felder | **K** | K | K | N4 = C. |
| L09 | HOK-Patcher (Werkzeug) | **K** | K | K | Werkzeug. |
| L10 | Laden/Speichern-Dialog und Spielstanddateien | **K** | K | K | Plattformersatz, R12-D2 = C. |
| L11 | Migrationen älterer Remake-Spielstände | **S** | K | S | Nach Aufräumkonzept A1 (#936). |
| L12 | Rückfälle für ältere Remake-Spielstände und Aufbauten | **S** | S | S | Rückfälle bündeln oder entfernen (#936). |
| S01 | Live-Spieltag: Paarungen nacheinander statt verschränkt | **N** | N | N | Paarungen verschränkt wie im Original (Nutzer 24.11.2026: vor v1.0). |
| S03 | Torszene: Wiederholung, Bildtakt, Speicher | **K** | O | K | Plattformersatz; Wiederholung mit Zufallsmodus nach v1.0. |
| S05 | Live-Bildschirm: Reporter und Tasten | **N** | N | K | Fehlende Funktion: Spielmodus-Optionen per Leertaste. |
| S06 | Aufstellung im Spiel: Rechtsklick ohne Reihe | **K** | S | K | Meldet wie Original nichts. |
| S07 | Einsätze zählen am Spielende | **N** | N | K | Einsatz geht bei Ein- und Auswechseln im selben Eingriff verloren (Statistikwert); niedrige Priorität. |
| S08 | Strafen ohne Spieler und Strafbank-Abfrage | **K** | N | K | Strafminuten stimmen; nur Liste unvollständig. |
| S09 | Zusehen und Torhüter draußen | **K** | K | K | Gleichwertig. |
| S10 | Bundestrainer: Felder von Satz 5 | **K** | K | K | Ohne Spielwirkung. |
| S11 | Zufall der Darstellung außerhalb des Stroms | **K** | K | K | N6 = A. |
| T01 | Entlassene Manager (nur ältere Spielstände) | **K** | K | K | Nur Altstände. |
| T02 | Spielende und Kopierschutz | **K** | K | K | Plattformersatz. |
| T03 | Saisonwechsel: Zeitpunkt und Reihenfolge der Würfe | **N** | N | N | Oberliga-Zug am 29. statt 13. April. |
| T04 | Saisonwechsel: Pool, Markt, Plätze außerhalb der Ligen | **K** | S | K | Fehler des Originals bzw. ohne Wirkung. |
| T05 | Saisonende: Kleinigkeiten | **K** | K | K | Unerreichbar bzw. ohne Wirkung. |
| W01 | Abgleich- und Exportwerkzeuge (emtool) | **K** | K | K | Werkzeuge. |
| C01 | Zufallsfolge nicht bitgenau | **K** | K | K | N3 = B; Zufallsmodus nach v1.0. |
| C02 | Rückfälle für Altstände: Teamwerte und WM-Mischung | **S** | K | S | Nach Aufräumkonzept A1 (#936). |
| C03 | Bildschirm öffnen setzt keine Werte (WM, Freundschaftsspiele) | **K** | K | K | AGENTS.md §4. |
| C04 | WM-Kader und Länderwerte | **K** | K | K | An WM-Tagen gleich. |
| C05 | Bestenliste: Kennzahlen und Punkte | **K** | K | K | Fehler des Originals bzw. Überlauf. |
| C06 | Absicherungen statt Absturz oder Endlosschleife | **K** | K | K | Absicherung statt Absturz. |
| C08 | Alte Spielwege und toter Code | **S** | S | S | Toten Code entfernen (#936). |
| C09 | Spielerstatistik-Felder | **K** | N | K | Nur bei völligem Gleichstand. |
| C10 | Tabellen: Sortierung und Platzliste | **K** | K | K | AGENTS.md §4. |
| C11 | Spielrechnung: Feldspielerzahl zu Beginn | **K** | S | K | Kein Leser vor der ersten Neuzählung. |
| C12 | Spielvorbereitung: Erweiterungen | **K** | K | K | Remake-Erweiterung bzw. N2. |
| D01 | Modus „nur Weltkugel“ | **D** | S | N | Wartet auf DOSBox. |
| D02 | Saisonwechsel: Absteiger und Rückkehr gelisteter Spieler | **D** | S | N | Wartet auf DOSBox. |
| D03 | Poolverteilung und Pool-Plätze | **D** | S | N | Wartet auf DOSBox. |
| D04 | Leihe, Bank und Darlehen zwischen Managern | **D** | S | N | Wartet auf DOSBox. |
| D05 | Laden: KAPUTT.HOK und Sperren | **D** | S | S | Wartet auf DOSBox. |
| D06 | Talente und Spieler der Computervereine | **K** | K | N | Folge der Nutzerentscheidung 01.11.2026 (Computerkader). |
| D07 | Transfer: Meldung „unfair“ und verworfenes Gebot | **D** | S | N | Wartet auf DOSBox. |
| D08 | Live-Spiel: Reihenzahl, Pokalweg, Penalty, Verletzungsmerker, Zählfeld | **D** | N | N | Wartet auf DOSBox. |

## Entscheidungen zu den Fragen (Nutzer 24.11.2026)

1. **B16** = A: 0xD0C2 in em.exe lesen (mit B15), ändern nur bei belegter Abweichung.
2. **F06/F08** = A: prüfen, belegte Abweichung ohne Rückfrage wie im Original ändern.
