# Bewertung der Abweichungen (`abweichungen-bewertung-gemini.md`)

## Übersicht nach Klasse und Bereich

| Bereich | N | S | O | K | Gesamt |
|---|---:|---:|---:|---:|---:|
| Bedienung/Pixel | 0 | 3 | 9 | 26 | 38 |
| Finanzen | 0 | 0 | 0 | 9 | 9 |
| Kader/Transfer | 3 | 1 | 0 | 12 | 16 |
| Laden/Speichern | 1 | 3 | 0 | 10 | 14 |
| Live-Spiel | 2 | 0 | 2 | 8 | 12 |
| Tages-/Saisonwechsel | 3 | 0 | 0 | 4 | 7 |
| Werkzeuge | 0 | 0 | 0 | 1 | 1 |
| Wettbewerbe | 0 | 3 | 0 | 9 | 12 |
| **Gesamt** | **9** | **10** | **11** | **79** | **109** |

## Klasse N – Änderung notwendig (9 Themen)

| Nr | Klasse | Begründung | Beleg | Abweichung von Liste |
|---|---|---|---|---|
| S01 | N | Sequenzielle statt verschränkte Live-Paarungen verändern bei mehreren Managern Ablauf und Eingriffszeitpunkte im Spieltag. | `abweichungen-v1.md` S01 (A442, A450, A547, A766, A954) | Liste: „bleibt“ (wegen `C01` verschoben); mechanisch abweichender Mehrspieler-Ablauf. |
| T03 | N | Der vorgezogene Oberliga-Zug (13. statt 29. April) und die Teamwert-Wanderung nach Ligamischung verändern Termine und Wertefolge. | `abweichungen-v1.md` T03 (`SeasonFlow.Oberliga.cs:84–85`, A661, A843, A935) | Liste: „bleibt“ (wegen `C01` verschoben); verändert Spieltermin und Saisonwechsel-Ablauf. |
| D01 | N | Im Modus „nur Weltkugel“ weichen Vertrags-/Trainingslauf sowie die 400.000-DM-Kontosetzung beim Oberliga-Tausch potenziell ab. | `abweichungen-v1.md` D01 (A408, A411, A671) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Spielwerte betroffen sind. |
| D02 | N | Absteigerregelung bei doppeltem Platz und Rückkehr gelisteter Spieler bei 29 Kaderplätzen verändern potenziell Liga- und Kaderzusammensetzung. | `abweichungen-v1.md` D02 (A667, A706) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Liga- und Kaderregeln betroffen sind. |
| D03 | N | Poolgruppenbildung, Nationalspieler-Grundstärken und Alters-Neuanlage beeinflussen potenziell Spielerwerte und Marktverteilung. | `abweichungen-v1.md` D03 (A707, A713, A945, A949) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Spielerwerte betroffen sind. |
| D04 | N | Leihende zwischen Managern sowie Sparbuch- und Darlehensführung betreffen potenziell Kader- und Kontostände beim Laden/Speichern. | `abweichungen-v1.md` D04 (A402, A613, A629) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Finanzen und Kader betroffen sind. |
| D06 | N | Alter, Startwerte, verstecktes Talent und Namen nachrückender KI-Spieler beeinflussen potenziell die Kaderstärke der Computervereine. | `abweichungen-v1.md` D06 (A128, A131, A132, A427, A630) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Gegnerstärken betroffen sind. |
| D07 | N | Die Gehaltsgrenze für „unfair“ und der Umgang mit nicht mehr ausführbaren Geboten verändern potenziell das Transferverhalten. | `abweichungen-v1.md` D07 (A117, A259) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Transferregeln betroffen sind. |
| D08 | N | Reihenzähler, schneller Pokalweg, Penalty-Bit-7 und Verletzungsmerker beeinflussen potenziell Spielablauf und -statistik. | `abweichungen-v1.md` D08 (A095, A896, A910, A912, A918) | Liste: „offen (D)“; vor v1.0 per DOSBox klären, da Live-Spielregeln betroffen sind. |

## Klasse S – Änderung sinnvoll (10 Themen)

| Nr | Klasse | Begründung | Beleg | Abweichung von Liste |
|---|---|---|---|---|
| B10 | S | Das Merken der letzten Seite in Pokal-, WM- und Europapokal-Ansichten stellt die gewohnte Navigation ohne Spielzustandsänderung her. | `abweichungen-v1.md` B10 (`LeaguePairingsPages.cs`, A268, A269) | Entspricht Vorschlag „ändern“. |
| B12 | S | Rechtsklick auf „Tag weiter“ im Hauptmenü stellt die ursprüngliche Bedienlogik durch Umordnen der Mausabfragen wieder her. | `abweichungen-v1.md` B12 (`MainMenuScreen.cs:179–193`, A296) | Entspricht Vorschlag „ändern“. |
| B13 | S | Rechtsklick auf Spieler in der Nationalaufstellung ergänzt das aus der Vereinsaufstellung bekannte Infofenster konsistent. | `abweichungen-v1.md` B13 (`LineupScreen.cs:146`, A757) | Entspricht Vorschlag „ändern“. |
| K04 | S | Das Entfernen ungenutzter Hilfsgrößen und alter Stärkewege bereinigt den Code von Sonderfällen ohne Spielwirkung. | `abweichungen-v1.md` K04 (`#936`, A627, A856, A859, A941) | Liste: „bleibt“; als Code-Bereinigung (`#936`) sinnvoll. |
| L11 | S | Das Abschalten veralteter Schema-Migrationen im Rahmen von `#936` baut komplexe Migrations-Sonderfälle im Lader ab. | `abweichungen-v1.md` L11 (`#936`, A144–A934) | Liste: „bleibt“; als Code-Bereinigung (`#936`) sinnvoll. |
| L12 | S | Das Entfernen von Rückfallwerten für ältere Remake-Spielstände vereinfacht den Lade- und Initialisierungscode nachhaltig. | `abweichungen-v1.md` L12 (`#936`, A114–A895) | Liste: „bleibt“; als Code-Bereinigung (`#936`) sinnvoll. |
| D05 | S | Die Klärung des Originalverhaltens nach „Der kleine Mogelguru“ (`KAPUTT.HOK`) und der HOK-Sperren sichert den Fehlerpfad sauber ab. | `abweichungen-v1.md` D05 (A599, A625) | Liste: „offen (D)“; als Fehlerfall-Absicherung sinnvoll. |
| C02 | S | Das Bündeln und Entfernen von Altstand-Rückfällen für Teamwerte und WM-Mischung reduziert Sonderlogik im Wettbewerbskern. | `abweichungen-v1.md` C02 (`#936`, A824, A841, A844, A845, A884, A911) | Liste: „bleibt“; als Code-Bereinigung (`#936`) sinnvoll. |
| C07 | S | Der Klick auf einen Verein in Pokal-/Play-off-Ansichten öffnet das Vereinsfenster und ergänzt die sichtbaren Verlängerungsmarken. | `abweichungen-v1.md` C07 (A098, A277, A435, A482, A759, A924, A925) | Entspricht Vorschlag „ändern“. |
| C08 | S | Das Entfernen unbenutzter Altwege (Poisson-Ergebnis, Rückfall-Penaltyschießen, Kreisverfahren) verbessert die Codestruktur. | `abweichungen-v1.md` C08 (`#936`, A137, A138, A426, A561, A842) | Liste: „bleibt“; als Code-Bereinigung (`#936`) sinnvoll. |

## Klasse O – Änderung rein optisch (11 Themen)

| Nr | Klasse | Begründung | Beleg | Abweichung von Liste |
|---|---|---|---|---|
| B01 | O | Das einmalige Abspielen der Titelmusik statt Endlosschleife betrifft ausschließlich die Audio-Wiedergabe ohne Spielwirkung. | `abweichungen-v1.md` B01 (`loop: false`, A002) | Entspricht Vorschlag „ändern“. |
| B07 | O | Zeilentrennung an „^“ sowie Verzicht auf „(Klicken)“ und „n/m“ korrigieren rein die Darstellung der Nachrichtenleiste. | `abweichungen-v1.md` B07 (A778, A779, A780) | Entspricht Vorschlag „ändern“. |
| B15 | O | Gesichtsfarbe, Tore/Spiel und Schnitt im Gegnerfenster korrigieren ausschließlich die Informationsanzeige ohne Spielwirkung. | `abweichungen-v1.md` B15 (A017, A286–A290) | Entspricht Vorschlag „ändern“. |
| B17 | O | Die Umleitung der Nationalteam-Reihenwerte auf `GameState.NationalPanelLineValue` korrigiert nur die angezeigten Zahlen. | `abweichungen-v1.md` B17 (`CupsNational.Strength.cs:56`, A285, A419, A961) | Entspricht Vorschlag „ändern“. |
| B18 | O | Kachelmarkierung, Managerkopf, Kurvenfeld und Lampe 0 des Bundestrainers betreffen rein die Grafik des Hauptmenüs. | `abweichungen-v1.md` B18 (A297, A301, A302, A331) | Entspricht Vorschlag „ändern“. |
| B25 | O | Meistersymbol (Symbol 5 statt Puck) und zusammengesetztes Pokalbild aus `kb\16` sind reine Grafikkorrekturen am Saisonende. | `abweichungen-v1.md` B25 (A342, A343) | Entspricht Vorschlag „ändern“. |
| B28 | O | Balkenhöhe und -farbe in der Historie betreffen ausschließlich die visuelle Auswertung ohne Rückwirkung auf den Spielstand. | `abweichungen-v1.md` B28 (`HistoryScreen.cs:23`, A356, A805, A806) | Entspricht Vorschlag „ändern“. |
| B29 | O | Verlaufsgesteuerte Smiley-Farbe und Startwerte auf dem Stärken-Bildschirm schließen eine reine Anzeigelücke per Spielstandfeld. | `abweichungen-v1.md` B29 (`StrengthScreen.cs:21`, A361, A699, A700) | Entspricht Vorschlag „ändern“. |
| B33 | O | Der Zähler „Gegentore beim Verein“ und die Balkenrahmen im Spielerfenster ergänzen rein darstellende Statistikwerte. | `abweichungen-v1.md` B33 (A372, A957, A958) | Entspricht Vorschlag „ändern“. |
| S02 | O | Darstellung 2 „Dauer-Eisfeld“ über den gesamten Spieltag verändert nur die visuelle Präsentation ohne Einfluss auf Spielwerte. | `abweichungen-v1.md` S02 (`GoalSceneCall.cs:32–37`, A472) | Entspricht Vorschlag „ändern“. |
| S04 | O | Minutenanzeige, Live-Zwischenstände und Farbstreifen beim Penaltyschießen ergänzen rein optische Spieltagsinformationen. | `abweichungen-v1.md` S04 (A292, A436, F068, F076) | Entspricht Vorschlag „ändern“. |

## Klasse K – Keine Änderung (79 Themen)

| Nr | Klasse | Begründung | Beleg | Abweichung von Liste |
|---|---|---|---|---|
| B02 | K | OPL2-Emulation ist Plattformersatz; Musik nur im Titelbildschirm folgt Nutzerentscheidung `#221`. | `abweichungen-v1.md` B02 (`#221`, A260, A261, A267) | Keine (Liste: „bleibt“). |
| B03 | K | Geräteeinstellungen, Lautstärkeregler und „Ganzer Tag“ sind Remake-Erweiterungen ohne Einfluss auf Originalabläufe. | `abweichungen-v1.md` B03 (A001, A469, A481, A516, A763, A802) | Keine (Liste: „bleibt“). |
| B04 | K | Wegfall der RAM-Prüfung und feste Hintergrundgrafik sind Plattformersatz ohne Spielwirkung. | `abweichungen-v1.md` B04 (A051, A295) | Keine (Liste: „bleibt“). |
| B05 | K | Bildtakt-Steuerung (60 Hz) statt BIOS-Warteschleife ist notwendiger Plattformersatz. | `abweichungen-v1.md` B05 (A743, A744, A768, A769, A773) | Keine (Liste: „bleibt“). |
| B06 | K | Das Posteingangsfenster ist eine Remake-Erweiterung auf Basis unveränderter Originaltexte. | `abweichungen-v1.md` B06 (A318, A503, A504, A777) | Keine (Liste: „bleibt“). |
| B08 | K | Pixelgenaue Fensterlage und mehrseitige Remake-Meldungen folgen Videobildern ohne Spielwirkung. | `abweichungen-v1.md` B08 (`FriendliesScreen.cs`, A745–A747, A756, A810) | Keine (Liste: „bleibt“). |
| B09 | K | Feste 150-ms-Anzeige des Zwischenbilds ersetzt die Ladezeit-Abhängigkeit des Originals auf moderner Hardware. | `abweichungen-v1.md` B09 (A748–A751, F019) | Keine (Liste: „bleibt“). |
| B11 | K | Komforttasten (Escape, Rechtsklick, beliebige Taste) schließen an Entscheidung `#205` an und ändern keine Spielwerte. | `abweichungen-v1.md` B11 (`#205`, `DebCupDrawLive.cs:69`, A273–A869) | Keine (Liste: „bleibt“). |
| B14 | K | Auslosungsergebnis und -ablauf sind identisch; die rollende Kugel ist nur eine unbelegte Animationsnuance. | `abweichungen-v1.md` B14 (A270, A272) | Keine (Liste: „bleibt“). |
| B16 | K | Keine belegte Abweichung im Vereinsfenster, da der Tabellensatz auch im Original nur Code 7/8 führt. | `abweichungen-v1.md` B16 (`ClubInfoWindow.cs:38–41`, A433–A868) | Keine (Liste: „bleibt“). |
| B19 | K | Zeilenumbrüche und Knopftexte in der Werbung betreffen nur Layout ohne Videobeleg. | `abweichungen-v1.md` B19 (A307, A767, A770, A771) | Keine (Liste: „bleibt“). |
| B20 | K | Pixelmaße auf dem Finanzen-Bildschirm sind am Video abgeglichen; keine Finanzzahl weicht ab. | `abweichungen-v1.md` B20 (A313, A314, A317, A477, A772, A774) | Keine (Liste: „bleibt“). |
| B21 | K | Farben, Abstände und Rückkehrklick im Jugend-Bildschirm sind am Video abgeglichen ohne Spielwirkung. | `abweichungen-v1.md` B21 (A329, A330, A782) | Keine (Liste: „bleibt“). |
| B22 | K | Klickflächen im Stadion sind an den Tribünen abgeleitet; ohne Beleg aus `0x3DC59` keine Änderung nötig. | `abweichungen-v1.md` B22 (A781) | Keine (Liste: „bleibt“). |
| B23 | K | Festes Minutentempo (1/3 s) ist Plattformersatz; fehlendes Banner bei Länder-Freundschaftsspielen ist unbelegt. | `abweichungen-v1.md` B23 (A458, A478) | Keine (Liste: „bleibt“). |
| B24 | K | Lage und Versatz der Platzzahlen in der Tabellenkurve sind am Bild vermessen ohne Spielwirkung. | `abweichungen-v1.md` B24 (A784, A785) | Keine (Liste: „bleibt“). |
| B26 | K | Die Absicherungen greifen ausschließlich in Zuständen, die im Original gar nicht erreichbar sind. | `abweichungen-v1.md` B26 (A517, A803) | Keine (Liste: „bleibt“). |
| B27 | K | Sitzungserkennung und Titelzentrierung in der Bestenliste haben keine Wirkung auf Spielablauf oder Punkte. | `abweichungen-v1.md` B27 (A353, A355, A870) | Keine (Liste: „bleibt“). |
| B30 | K | Bildlauf-Schrittweite und Listensortierung nach Rolle betreffen nur die Oberflächendarstellung. | `abweichungen-v1.md` B30 (A350, A685) | Keine (Liste: „bleibt“). |
| B31 | K | Schreiben nur nach Änderung beim Verlassen von Aufstellung/Taktik setzt Architekturregel `AGENTS.md §4` (E4 = B) fort. | `abweichungen-v1.md` B31 (`AGENTS.md §4`, A892, A893, A956, A959, A960) | Keine (Liste: „bleibt“). |
| B32 | K | Zusatztasten `A`/`C` in der Aufstellung sind im Live-Spiel deaktiviert und dienen als Bedienkomfort ohne Wertänderung. | `abweichungen-v1.md` B32 (A365, A373, A953, A955) | Keine (Liste: „bleibt“). |
| B34 | K | Dialogpositionen im Transferbereich folgen dem Code ohne Videobeleg und beeinflussen keine Transaktionen. | `abweichungen-v1.md` B34 (A376, A377, A379, A811–A815) | Keine (Liste: „bleibt“). |
| B35 | K | Transfermeldungen sind funktional abgedeckt; die Darstellung einzelner Meldungsfenster ändert keine Transferwerte. | `abweichungen-v1.md` B35 (`TransferScreen.cs:210`, A816, A818) | Keine (Liste: „bleibt“). |
| B36 | K | Pixeldetails bei Kontoleiste, Schattenecken und Trennlinien im Transfermarkt haben keine Spielwirkung. | `abweichungen-v1.md` B36 (A724, A817, A819–A821) | Keine (Liste: „bleibt“). |
| B37 | K | Kleine Darstellungsdetails (Ersatzzeichen, Pfadkürzung, Platzzahlbreite) sind am Bild abgeglichen oder betreffen Randfälle. | `abweichungen-v1.md` B37 (A278, A437, A447, A764, F033, F166) | Keine (Liste: „bleibt“). |
| B38 | K | Dass Paarungsseiten nach dem Laden auf Seite 1 starten, entspricht dem Verzicht auf UI-Zustände im Spielstand (L11). | `abweichungen-v1.md` B38 (`LeaguePairingsPages.cs`, A432, A760) | Keine (Liste: „bleibt“). |
| F01 | K | Führung aller Beträge in ganzen DM statt Drittel-DM folgt Nutzerentscheidung N1 = B. | `abweichungen-v1.md` F01 (N1 = B, A040–A921) | Keine (Liste: „bleibt“). |
| F02 | K | Kategorisierte Verbuchung im Finanzbuch bei identischem Kontostand folgt Nutzerentscheidung N5 = C. | `abweichungen-v1.md` F02 (N5 = C, A192–A716) | Keine (Liste: „bleibt“). |
| F03 | K | Finanzbuch („Buch“) und Leihanfrage per Nachricht sind bewusste Remake-Erweiterungen (`zusatzfunktionen.md` B). | `abweichungen-v1.md` F03 (A311, A476, A502) | Keine (Liste: „bleibt“). |
| F04 | K | Platzfelder und Sonderfälle der Werbebewertung bleiben im normalen Spiel ohne Wirkung. | `abweichungen-v1.md` F04 (A506–A508, A887) | Keine (Liste: „bleibt“). |
| F05 | K | Löschen des Betrags beim Verlassen des Buchungsstreifens behebt einen Originalfehler gemäß Entscheidung N2 = B. | `abweichungen-v1.md` F05 (N2 = B, A775) | Keine (Liste: „bleibt“). |
| F06 | K | Überschreiben des gesamten Bandenvertrags ist konsistente Folge des Remake-Datenmodells (eine Bande = ein Vertrag). | `abweichungen-v1.md` F06 (A888) | Keine (Liste: „bleibt“). |
| F07 | K | Getrennte Führung von Sparbuch und Kredit bei identischem Saldo verändert kein Spielergebnis. | `abweichungen-v1.md` F07 (A886) | Keine (Liste: „bleibt“). |
| F08 | K | Geänderte Ausführungsreihenfolge bei Stadionbau und Sommerpause liefert identische Verteilungen und Kontostände. | `abweichungen-v1.md` F08 (A468, A509, A512, A526, A695) | Keine (Liste: „bleibt“). |
| F09 | K | Abweichung 0 ohne Teamwerte greift nur bei veralteten Spielständen vor Schema 12. | `abweichungen-v1.md` F09 (A826) | Keine (Liste: „bleibt“). |
| K01 | K | Echte Spielerkader für Computervereine setzen die Nutzerentscheidung vom 01.11.2026 (`transfer.md` Teil 5) um. | `abweichungen-v1.md` K01 (A129–A944) | Keine (Liste: „bleibt“). |
| K02 | K | Die Kader-KI der Computervereine ist eine Remake-Ergänzung (R2-12) als Folge der Entscheidung vom 01.11.2026. | `abweichungen-v1.md` K02 (R2-12, A233–A862) | Keine (Liste: „bleibt“). |
| K03 | K | Der Spielertausch zwischen Pool und Computerkadern wahrt die Eindeutigkeit aller Namen gemäß Entscheidung vom 01.11.2026. | `abweichungen-v1.md` K03 (A215–A948) | Keine (Liste: „bleibt“). |
| K05 | K | Die Heilung verletzter Nicht-Managerspieler hat keine Spielwirkung, da sich nur Managerspieler verletzen. | `abweichungen-v1.md` K05 (A496, A523, A529, A538, A879) | Keine (Liste: „bleibt“). |
| K06 | K | Sortierung nach Rückennummer und Ersatz-Automatik in der Nationalaufstellung sind regelgleich zum Original. | `abweichungen-v1.md` K06 (A420, A489, A491) | Keine (Liste: „bleibt“). |
| K07 | K | Das Remake-Aufstellungsmodell weicht nur in unkritischen Randfällen (Reihe 0 bei Verletzten) ab. | `abweichungen-v1.md` K07 (A165, A807, A936, A939, A971, A972) | Keine (Liste: „bleibt“). |
| K08 | K | Zufallszuteilung neuer Manager mitten im Spiel und Startkader folgen der Remake-Erweiterung vom 01.11.2026. | `abweichungen-v1.md` K08 (A460, A461, A463, A465, A828) | Keine (Liste: „bleibt“). |
| K09 | K | Berechnung der Käufer-Teamwerte aus Abwehr und Sturm behebt den Mauszeilen-Fehler des Originals (N2 = B). | `abweichungen-v1.md` K09 (N2 = B, A701, A702, A860) | Keine (Liste: „bleibt“). |
| K10 | K | Bewertung unaufgeforderter Angebote anhand des Spielers selbst behebt einen Indexfehler des Originals (N2 = B). | `abweichungen-v1.md` K10 (N2 = B, A247, A249, A866) | Keine (Liste: „bleibt“). |
| K11 | K | Simulierte Transferantworten ohne Oberfläche und abgeleitete Status-Flags betreffen nur Tests bzw. interne Zustände. | `abweichungen-v1.md` K11 (A224–A952) | Keine (Liste: „bleibt“). |
| K12 | K | Vertragslosigkeit bei vollem Stammverein-Kader nach Leihende verhindert einen ungültigen Kaderplatz des Originals. | `abweichungen-v1.md` K12 (A116) | Keine (Liste: „bleibt“). |
| K13 | K | Übergabe von 1 DM bei Eingabe 0 DM führt zum identischen Verhandlungsergebnis („nicht interessiert“). | `abweichungen-v1.md` K13 (`ContractDialog.cs`, A973) | Keine (Liste: „bleibt“). |
| L01 | K | Vorbelegung von Wettbewerbs- und Nationalteamfeldern im HOK-Export ist Teil der Remake-Erweiterung (E14-1 C, R14-D2 = B). | `abweichungen-v1.md` L01 (A400–A915) | Keine (Liste: „bleibt“). |
| L02 | K | Exportwerte für Liga- und Vereinssätze wirken ausschließlich in exportierten HOK-Dateien (E14-1 C, R14-D2 = B). | `abweichungen-v1.md` L02 (A446, A576–A581) | Keine (Liste: „bleibt“). |
| L03 | K | Exportanpassungen für Kader und Pool betreffen ausschließlich die HOK-Exportdatei (E14-1 C, R14-D2 = B). | `abweichungen-v1.md` L03 (A404, A585–A588, A848, A967) | Keine (Liste: „bleibt“). |
| L04 | K | Deterministische Spielstand-ID und Kopfbytes im HOK-Export sind bewusste Erweiterungsregeln (E14-1 C, R14-D2 = B). | `abweichungen-v1.md` L04 (A263–A736) | Keine (Liste: „bleibt“). |
| L05 | K | Näherungen für Manager, Rekorde und Werbung im HOK-Export betreffen nur die Exportdatei (E14-1 C, R14-D2 = B). | `abweichungen-v1.md` L05 (A403, A583, A584, A965, A966) | Keine (Liste: „bleibt“). |
| L06 | K | Rekonstruktion fehlender Wettbewerbsfelder beim HOK-Import ergibt sich aus den Formatgrenzen gemäß N4 = C. | `abweichungen-v1.md` L06 (N4 = C, A600–A603, A609–A611, A968) | Keine (Liste: „bleibt“). |
| L07 | K | Zuordnung von Poolbesitzern und Spielernamen beim HOK-Import folgt den Import-Regeln gemäß N4 = C. | `abweichungen-v1.md` L07 (N4 = C, A604–A606, A849, A969) | Keine (Liste: „bleibt“). |
| L08 | K | Nachspielen fehlender HOK-Daten nach Remake-Regeln beim Import entspricht Entscheidung N4 = C. | `abweichungen-v1.md` L08 (N4 = C, A397–A970) | Keine (Liste: „bleibt“). |
| L09 | K | Erweiterte Wertebereiche im HOK-Patcher betreffen nur ein externes Testwerkzeug, nicht das Spiel. | `abweichungen-v1.md` L09 (A264, A265) | Keine (Liste: „bleibt“). |
| L10 | K | Ordnerdialog, `*.ems`-Format, `AUTOSAVE.ems` und Verzicht auf `ERROR.HOK` folgen Plattformersatz und R12-D2 = C. | `abweichungen-v1.md` L10 (`AutoSave.cs:20–22`, R12-D2 = C, A335–F056) | Keine (Liste: „bleibt“). |
| S03 | K | Fester 60-Hz-Bildtakt und fehlende DOS-Speicherprüfung sind Plattformersatz; Szenen-Wiederholung wartet auf Zufallsmodus. | `abweichungen-v1.md` S03 (A439, A470, A471, A473, A474) | Keine (Liste: „bleibt“). |
| S05 | K | Enter/Escape im Live-Spiel folgen Entscheidung `#205`, der 60-Hz-Takt ist Plattformersatz und Reporterdetails sind rein optisch. | `abweichungen-v1.md` S05 (`#205`, A448–A475) | Keine (Liste: „bleibt“). |
| S06 | K | Rechtsklick ohne gewählte Reihe im Spiel meldet wie im Original nichts und ändert keinen Zustand. | `abweichungen-v1.md` S06 (`ClickPlayerInMatch`, A678) | Keine (Liste: „bleibt“). |
| S07 | K | Einsatzzählung am Spielende statt beim Einwechseln liefert denselben Endstand und wirkt nur auf die Anzeige während des Spiels. | `abweichungen-v1.md` S07 (A874, A898) | Keine (Liste: „bleibt“). |
| S08 | K | Nicht geführte Strafbank-Zähler nach Spielende haben keine Wirkung, da nach dem Spiel kein Spieler auf der Strafbank sitzt. | `abweichungen-v1.md` S08 (A444, A880, A907, A964) | Keine (Liste: „bleibt“). |
| S09 | K | Zeitpunkt des Zurücksetzens nach „Torhüter draußen“ ist für das nachfolgende Spiel funktional gleichwertig. | `abweichungen-v1.md` S09 (A833, A942) | Keine (Liste: „bleibt“). |
| S10 | K | Verzicht auf separate Satz-5-Zähler beim Bundestrainer folgt dem Remake-Datenmodell ohne spielentscheidende Wirkung. | `abweichungen-v1.md` S10 (A876–A878, A900–A904, A908) | Keine (Liste: „bleibt“). |
| S11 | K | Eigene Zufallsquellen für Darstellungsdetails (Zwinkern, Auslosung) setzen Nutzerentscheidung N6 = A (`AGENTS.md §4`) um. | `abweichungen-v1.md` S11 (`AGENTS.md §4`, N6 = A, A061–A452) | Keine (Liste: „bleibt“). |
| T01 | K | Sonderregeln für entlassene Manager betreffen nur ältere Spielstände, da das Remake nicht mehr entlässt. | `abweichungen-v1.md` T01 (A193–A962) | Keine (Liste: „bleibt“). |
| T02 | K | Rückkehr zum Titelbildschirm nach Spielende und Wegfall der Kopierschutz-Abfrage sind Plattformersatz. | `abweichungen-v1.md` T02 (A799, A800, F129) | Keine (Liste: „bleibt“). |
| T04 | K | Vermeidung des Byte-Überlaufs bei der Soll-Spielerzahl behebt einen Originalfehler (N2 = B); übrige Punkte sind wirkungslos. | `abweichungen-v1.md` T04 (N2 = B, A659, A712, A715, A718, A729) | Keine (Liste: „bleibt“). |
| T05 | K | Wochentagskorrektur und Sicherung des WM-Schalters am Saisonende sind im Spielverlauf wirkungslos bzw. unerreichbar. | `abweichungen-v1.md` T05 (A398, A835, A914) | Keine (Liste: „bleibt“). |
| W01 | K | Toleranzen und Exportnamen in `emtool` betreffen ausschließlich externe Abgleich- und Hilfswerkzeuge. | `abweichungen-v1.md` W01 (A266, A406, A407, A738–A741) | Keine (Liste: „bleibt“). |
| C01 | K | Nutzung von `.NET-Random` statt `Borland-rand/lrandom` folgt R15-D2 = A und N3 = B; bitgenauer Modus ist für nach v1.0 geplant. | `abweichungen-v1.md` C01 (R15-D2 = A, N3 = B, A100–F054) | Keine (Liste: „bleibt“). |
| C03 | K | Setzen von Energie 70 und Länderwerten erst beim Spiel/Ändern statt beim Öffnen folgt Architekturregel `AGENTS.md §4` (E4 = B). | `abweichungen-v1.md` C03 (`AGENTS.md §4`, A279, A486, A488, A883) | Keine (Liste: „bleibt“). |
| C04 | K | Länderwerte sind an WM-Tagen identisch zum Original; die poollose Kandidatenliste greift nur bei Altständen. | `abweichungen-v1.md` C04 (A483, A484, A528, A871, A872) | Keine (Liste: „bleibt“). |
| C05 | K | Schreiben der Europapokal-Kennzahl in der Bestenliste behebt einen Originalfehler (R21-E1 = A, N2); Punkte sind überlaufgeschützt. | `abweichungen-v1.md` C05 (R21-E1 = A, A059, A063, A064, A963) | Keine (Liste: „bleibt“). |
| C06 | K | Abbruchzähler und Divisionsschutz verhindern Endlosschleifen und „Divide error“-Abstürze des Originals. | `abweichungen-v1.md` C06 (A069–A947) | Keine (Liste: „bleibt“). |
| C09 | K | Sortierung der Spielerstatistik nach Remake-Kader statt Pool folgt dem Kadermodell und wirkt nur bei völligem Gleichstand. | `abweichungen-v1.md` C09 (A102, A103, A198, A199, A545, A897, A906) | Keine (Liste: „bleibt“). |
| C10 | K | Unabhängigkeit der Tabellensortierung von zuvor betrachteten Teiltabellen setzt `AGENTS.md §4` um. | `abweichungen-v1.md` C10 (`AGENTS.md §4`, A159, A161, A164, A657) | Keine (Liste: „bleibt“). |
| C11 | K | Startwert 2 und 3 der Feldspielerzahl ist wirkungslos, da vor der ersten Neuzählung kein Leser existiert. | `abweichungen-v1.md` C11 (A548) | Keine (Liste: „bleibt“). |
| C12 | K | Zufallswahl des Vereins und DEB-Pokal-Schalter sind Remake-Erweiterungen; Manager-Pflichtfelder beheben Fehler R21-E1 = A. | `abweichungen-v1.md` C12 (R21-E1 = A, A055, A172, A480, A557) | Keine (Liste: „bleibt“). |

## Klärungsfragen und Entscheidungen

| Nr | Thema / Problem | Gewählte Option | Entscheidung & Auswirkung |
|---|---|---|---|
| 1 | Einordnung der 8 untersuchungsoffenen D-Themen (`D01`–`D08`) vor Vorliegen einer DOSBox-Aufnahme | **Option D** (Empfehlung) | `D01`–`D04` und `D06`–`D08` als **N** (potenzielle Spielmechanik-/Werteabweichungen vor v1.0 klären); `D05` (`KAPUTT.HOK`) als **S**. |
| 2 | Fehlende Nutzerentscheidung zu Aufstellung und Taktik verlassen (`B31`) | **Option A** (Empfehlung) | `B31` als **K** (Beibehaltung als konsistente Erweiterung von `AGENTS.md §4` / E4 = B: Öffnen/Schließen ändert keinen Spielzustand). |
| 3 | Fehlende Nutzerentscheidung zu Zusatztasten `A`/`C` (`B32`) und Komfort-Shortcuts (`B11`) | **Option A** (Empfehlung) | `B32` und `B11` als **K** (Beibehaltung aller Komforttasten außerhalb laufender Spiele analog zu Entscheidung `#205`). |
| 4 | Abgrenzung zwischen rein optisch (**O**) und sinnvoll (**S**) bei den 15 „ändern“-Vorschlägen | **Option A** (Empfehlung) | Reine Darstellungs-/Audiokorrekturen (`B01`, `B07`, `B15`, `B17`, `B18`, `B25`, `B28`, `B29`, `B33`, `S02`, `S04`) → **O**; Bedien-/Navigationslogik (`B10`, `B12`, `B13`, `C07`) → **S**. |
| 5 | Themen mit Spielstand-Migration für rein optische Anzeigen (`B29` Smiley-Verlauf, `B33` Gegentore) | **Option A** (Empfehlung) | `B29` und `B33` als **O** umsetzen, um die sichtbaren Anzeigelücken trotz nötiger Spielstand-Erweiterung zu schließen. |
| 6 | Bis nach v1.0 verschobene Ablauf-Abweichungen (`T03` Saisonwechsel, `S01` Live-Spieltag) | **Option A** (Empfehlung) | `T03` und `S01` nach rein spielmechanischer Sicht als **N** eingestuft, da Termine und Mehrspieler-Ablauf vom Original abweichen. |
| 7 | Grenzfall-Abweichungen aus Remake-Datenmodellen (`C09` Gleichstand, `S10` Satz 5, `F06` Bande) | **Option A** (Empfehlung) | `C09`, `S10` und `F06` als **K** belassen, da sie direkte Folgen entschiedener Datenmodell-Vereinfachungen des Remakes sind. |
| 8 | Umgang mit Altstand-Migrationen und totem Code (`L11`, `L12`, `C02`, `C08`, `K04` / `#936`) | **Option A** (Empfehlung) | `L11`, `L12`, `C02`, `C08` und `K04` als **S** eingestuft, da deren Bereinigung (`#936`) Sonderfälle im Code abbaut. |