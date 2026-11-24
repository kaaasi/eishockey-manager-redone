# Bewertung der Abweichungsliste aus Sicht der Spielmechanik

Stand `ebdc3c6` · [Themen und Vollliste](abweichungen-v1.md) · [Marker und Codebelege](restliste-v1.md)

| Bereich | N | S | O | K | Themen |
|---|---:|---:|---:|---:|---:|
| Bedienung/Pixel | 6 | 13 | 13 | 6 | 38 |
| Finanzen | 0 | 4 | 0 | 5 | 9 |
| Kader/Transfer | 1 | 7 | 0 | 8 | 16 |
| Laden/Speichern | 0 | 3 | 0 | 11 | 14 |
| Live-Spiel | 6 | 1 | 2 | 3 | 12 |
| Tages-/Saisonwechsel | 1 | 3 | 0 | 3 | 7 |
| Werkzeuge | 0 | 0 | 0 | 1 | 1 |
| Wettbewerbe | 2 | 2 | 0 | 8 | 12 |
| **Gesamt** | 16 | 33 | 15 | 45 | 109 |

Gezählt sind 101 Themen und acht D-Themen mit insgesamt 581 IDs. D-Themen zählen zum Bereich der Mehrheit ihrer IDs. Die Belege beziehen sich auf den genannten Stand.

„Liste“ zeigt den bisherigen Vorschlag. **Fett** markiert eine abweichende Empfehlung bei N oder S gegenüber „bleibt“ bzw. K gegenüber „ändern“. O bezeichnet ausschließlich die Art der Abweichung (einschließlich Ton und Animation), unabhängig vom bisherigen Vorschlag. „Ohne“ bedeutet, dass die Liste für das D-Thema keine Entscheidung vorwegnimmt.

## N – Änderung notwendig

| Nr. | Klasse | Thema | Liste | Begründung | Beleg |
|---|:---:|---|---|---|---|
| B12 | N | Hauptmenü: „Tag weiter“ mit rechter Maustaste | ändern | Der Rechtsklick aufs Logo löst im Original den Tageswechsel aus und hebt im Remake zuvor die Auswahl auf. | `MainMenuScreen.cs:27`, `em.exe 0x45483–0x4551E` |
| B15 | N | Gegnerfenster: Gesicht, Tore/Spiel, Schnitt, Bilanzzeichen | ändern | Das Gegnerfenster berechnet Tore je Spiel selbst und kürzt den Schnitt, sodass die angezeigten Gegnerwerte von den Originalfeldern abweichen können. | `TeamInfoWindow.cs:44`, `TeamInfoWindow.cs:197`, `em.exe 0x110:0x5C` |
| B17 | N | Nationalteam: Reihenwerte der Anzeige | ändern | Die Nationalanzeige zeigt den Mittelwert aus Kondition und Technik, obwohl das Original die Reihenstärke mit Funktion 0x977F bestimmt. | `CupsNational.cs:207`, `em.exe 0x977F` |
| B29 | N | Stärken-Bildschirm: Smiley, Managervereine, Startwerte | ändern | Der feste Smiley unterschlägt den Stärketrend und für Managervereine erscheint die aktuelle Aufstellung statt des letzten Tageswerts. | `StrengthScreen.cs:21`, `StatisticsStrength.cs:32` |
| B31 | N | Aufstellung und Taktik verlassen: nur nach Änderung | **bleibt** | Das Original schreibt beim Verlassen auch ohne Eingriff und kann dadurch seit dem letzten Ereignistag gewanderte Werte neu setzen (Antwort 4). | `GameState.DailyRun.cs:263`, `LineupScreen.cs:236`, `GameState.DailyRun.cs:257` |
| B33 | N | Spielerfenster und Spielerinfo | ändern | Für Gegentore beim aktuellen Verein zeigt die Spielerinfo einen Strich und entzieht dem Manager damit eine Originalstatistik. | `PlayerDataWindow.cs:85` |
| K07 | N | Aufstellung: Modell und Automatik | **bleibt** | Ein verletzter Spieler verliert seinen Reihenplatz und spielt dadurch Reihe 0 anstelle seiner bisherigen Reihe ein. | `Squad.SlotValues.cs:58` |
| S01 | N | Live-Spieltag: Paarungen nacheinander statt verschränkt | **bleibt** | Die serielle Berechnung verschiebt spätere Managerpaarungen und verhindert den gleichzeitigen Blick auf ihren Verlauf (Antwort 7). | `MatchEngine.LiveDriver.cs:21`, `MatchEngine.Scoreboard.cs:276` |
| S04 | N | Paarungsseiten im Spiel: Minute, Zwischenstände, Penalty-Anzeige | ändern | Fehlende Spielminute und Zwischenstände entziehen dem Manager beim laufenden Spieltag entscheidungsrelevante Werte. | `PlayoffGamesScreen.cs:116`, `WorldCupKnockoutScreen.cs:37`, `em.exe 0x3183C` |
| S05 | N | Live-Bildschirm: Reporter und Tasten | **bleibt** | Während des Live-Spiels fehlt der Aufruf der Spielmodus-Optionen per Leertaste, während #205 die zusätzlichen Vorspultasten deckt. | `LiveMatchScreen.cs:21`, `LiveMatchScreen.cs:302` |
| S07 | N | Einsätze zählen am Spielende | **bleibt** | Ein im selben Eingriff eingesetzter und wieder entnommener Spieler kann seinen Einsatz verlieren, außerdem fehlt der Zuwachs bis Spielende in der Spalte SP. | `MatchEngine.LiveLineup.cs:20`, `CupsNational.Lineup.InMatch.cs:16` |
| S08 | N | Strafen ohne Spieler und Strafbank-Abfrage | **bleibt** | Eine Strafe ohne wählbaren Spieler erhöht die Strafminuten, fehlt jedoch in der Strafliste und damit in der Auskunft über das laufende Spiel. | `MatchEngine.Scoreboard.cs:62`, `CupsNational.WorldCupLoop.cs:168` |
| T03 | N | Saisonwechsel: Zeitpunkt und Reihenfolge der Würfe | **bleibt** | Der Oberliga-Zug schreibt die Nachrücker bereits am 13. April in den Spielstand, obwohl das Original sie erst am 29. April bestimmt (Antwort 7). | `SeasonFlow.Oberliga.cs:84`, `SeasonFlow.LeagueSlots.cs:20` |
| C07 | N | Pokal- und Play-off-Marken, Klick auf Verein | ändern | Ein Klick auf den Verein verlässt die Pokalansicht und verhindert so das Öffnen des dort verfügbaren Vereinsfensters. | `DebCupGamesScreen.cs:121`, `PlayoffGamesScreen.cs:80`, `em.exe 0x38402` |
| C09 | N | Spielerstatistik-Felder | **bleibt** | Die Kaderreihenfolge kann bei Punktgleichheit den ausgezeichneten Spieler ändern, und das separate Ligatorfeld mit seiner Obergrenze fehlt. | `StatisticsPlayers.cs:41`, `MatchEngine.Live.cs:536` |
| D08 | N | Live-Spiel: Reihenzahl, Pokalweg, Penalty, Verletzungsmerker, Zählfeld | ohne | Bei vierter Reihe und Modus ungleich 1 setzt das Remake einen festen Reihenzähler, den das Original dort ungesetzt lässt, während die konkrete Wechselfolge und die weiteren Live-Fälle eine Aufnahme brauchen. | `MatchEngine.Managed.cs:527`, `MatchEngine.TurnManager.cs:23` |

## S – Änderung sinnvoll

| Nr. | Klasse | Thema | Liste | Begründung | Beleg |
|---|:---:|---|---|---|---|
| B07 | S | Nachrichtenleiste: Umbruch, Hinweis, Zähler | ändern | Der Umbruch schneidet Meldungen ab und zwingt den Manager zum Öffnen des Posteingangs, um den vollständigen Text zu lesen. | `NewsBar.cs:103` |
| B08 | S | Meldungs- und Kurzmeldungsfenster | **bleibt** | Das eigene Hinweisfenster bei Geldmangel und seitenweise Meldungen betreffen den Aufwand zum Lesen und Quittieren. | `TeamUi.cs:233`, `FriendliesScreen.cs:123` |
| B10 | S | Paarungsseiten Pokal/WM und Europapokal-Tabelle merken die letzte Seite nicht | ändern | Das Merken der zuletzt geöffneten Pokal- oder Europapokalseite erspart wiederholtes Blättern. | `CupPairingsScreen.cs:39`, `CupTableMenuEntries.cs:19`, `em.exe 0x45862` |
| B11 | S | Komforttasten Escape, Rechtsklick und „jede Taste schließt“ | **bleibt** | Gezielte Rückwege bleiben nach Antwort 3 erhalten, während „jede Taste schließt“ unbeabsichtigtes Quittieren begünstigt. | `DebCupDrawLive.cs:69`, `WorldCupScreen.cs:32` |
| B13 | S | Nationalaufstellung: Spielerinfo per Rechtsklick fehlt | ändern | Vor dem Ergänzen der Spielerinfo ist der Nationalteam-Aufruf zu prüfen, denn em.exe 0x40872 belegt den Rechtsklick beim Verein. | `NationalLineupScreen.cs:22`, `LineupScreen.cs:146` |
| B16 | S | Vereinsfenster: Bilanz, Ligazeile, Spielplan | **bleibt** | Die widersprüchlichen Befunde zur Pokalbilanz rechtfertigen den Abgleich des Wettbewerbscodes vor einer Korrektur der Anzeige. | `CupsNational.TeamInfo.cs:129`, `ClubInfoWindow.cs:38` |
| B18 | S | Hauptmenü: Kachelmarkierung, Managerkopf, Kurve und Lampe des Bundestrainers | ändern | Das leere Kurvenfeld des Bundestrainers und das Abwählen per Kopfklick rechtfertigen einen Vergleich mit dem Originalmenü. | `MainMenuScreen.cs:326`, `MainMenuScreen.cs:36` |
| B19 | S | Werbung: Absage, Verlängerungsfrage, Blätterstart | **bleibt** | Der ungeklärte Start beim Blättern kann die Navigation verlängern, auch wenn die Textaufteilung nur optisch abweicht. | `AdvertisingScreen.cs:250`, `AdvertisingScreen.cs:292` |
| B21 | S | Jugend-Bildschirm | **bleibt** | Der Rückkehrklick bestimmt neben den reinen Farb- und Abstandsfragen, wie der Manager die Jugendansicht verlässt. | `YouthScreen.cs:15` |
| B22 | S | Stadion: Klickflächen Anzeigetafel und Sitzkomfort | **bleibt** | Ein Abgleich der Sonderflächen klärt, wo der Manager Anzeigetafel und Sitzkomfort tatsächlich auswählen konnte. | `StadiumScreen.cs:120` |
| B30 | S | Statistik-Bildschirme: Bildlauf und Sortierung | **bleibt** | Schrittweite und Listensortierung ändern den Aufwand beim Suchen eines Statistikplatzes. | `EternalTableScreen.cs:23`, `Squad.Strength.cs:26` |
| B35 | S | Transfermarkt: Meldungen | **bleibt** | Ein Vergleich der übrigen Transfermeldungen klärt zusätzliche Bestätigungsklicks und den eigenen Annahmehinweis. | `TransferScreen.cs:91`, `TransferScreen.cs:297` |
| B38 | S | Paarungsseiten nach dem Laden | **bleibt** | Beim Laden im selben Programmlauf erspart das Beibehalten der Paarungsseite erneutes Blättern und benötigt kein gespeichertes Seitenbyte. | `LeaguePairingsPages.cs:23`, `WorldCupPairingsPages.cs:18` |
| F04 | S | Werbebewertung: Platzfelder und Sonderfälle | **bleibt** | Zu prüfen ist, ob der pauschale Ø-Platz 6 im Weltkugelmodus oder das Weiterzählen der Aufstiegsgruppe je ein erreichbares Werbeangebot verändert. | `FinanceSponsorRating.cs:271`, `FinanceSponsorRating.cs:181` |
| F06 | S | Werbeangebot überschreibt Bande | **bleibt** | Die übrigen Bandenfelder bleiben laut Code erhalten, zu prüfen ist der unverändert übernommene Monatsbetrag des Teilvertrags. | `FinanceSponsors.cs:348`, `FinanceSponsors.cs:374` |
| F07 | S | Finanzübersicht: Sparbuch und Kredit getrennt | **bleibt** | Die gegenseitige Verrechnung von Kredit und Sparbuch ließe sich vereinheitlichen, weil die normale Bedienung Doppelbestände bereits verhindert. | `FinanceOverview.cs:18`, `FinanceBank.cs:46`, `FinanceBank.cs:64` |
| F08 | S | Stadion, Zuschauer und Sommerpause: Reihenfolge | **bleibt** | Das ausgewiesene Buchjahr der Sommerbeträge verdient einen Vergleich, weil auch em.exe vor der Schleife die Saison erhöht und die geänderte Reihenfolge allein keinen anderen Betrag belegt. | `GameState.DailyRun.cs:384`, `FinanceStadium.cs:530` |
| K04 | S | Hilfsgrößen ohne Gegenstück | **bleibt** | Das Entfernen des alten Stärkewegs vereinfacht die Berechnung, weil der reguläre Spielpfad ihn umgeht (#936). | `Squad.Strength.cs:106` |
| K06 | S | Nationalaufstellung: Reihenfolge, Ersatz, Klick | **bleibt** | Ein Vergleich der Reihenfolge innerhalb eines Nationalblocks und der Ersatzwahl beim Lesen würde klären, ob je eine andere spielbare Aufstellung entsteht. | `CupsNational.Lineup.cs:327`, `CupsNational.cs:224` |
| K11 | S | Transfer: Abläufe ohne Oberfläche und interne Kennzeichen | **bleibt** | Eine überflüssige zweite Prüfung von Kader und Geld lässt sich vereinfachen, während simulierte Antworten dem Betrieb ohne Oberfläche dienen. | `Transfer.SellerQuestion.cs:20`, `Transfer.Purchase.cs:10` |
| K12 | S | Leihrückkehr bei vollem Kader | **bleibt** | Der Verlust des Vertrags bei voller Rückkehrmannschaft verdient eine Fallprüfung, weil der ungültige Originalplatz keinen verlässlichen Zielkader vorgibt. | `Player.Contract.cs:214` |
| K13 | S | Vertragsdialog: Gehalt 0 | **bleibt** | Das Durchreichen der angezeigten 0 DM würde den Sonderwert von 1 DM im Kern bei gleicher Ablehnung entbehrlich machen. | `ContractDialog.cs:160` |
| L12 | S | Rückfälle für ältere Remake-Spielstände und Aufbauten | **bleibt** | Die Rückfälle für die alte AUTOM.-Anzeige lassen sich gemeinsam pflegen, sofern Anzeige und Ladefähigkeit vor Runde 14 erhalten bleiben (Antwort 8). | `Squad.Tactics.cs:147`, `Squad.Tactics.cs:347` |
| S06 | S | Aufstellung im Spiel: Rechtsklick ohne Reihe | **bleibt** | Ein Vergleich der zulässigen Spieler und Plätze würde klären, ob der Rechtsklick ohne Reihe tatsächlich dieselbe Auswahl wie der Linksklick erlaubt. | `Squad.Lineup.InMatch.cs:29` |
| T04 | S | Saisonwechsel: Pool, Markt, Plätze außerhalb der Ligen | **bleibt** | Für den Saisonübertrag eines Leihspielers ist die Vereinszuordnung nach der Rückkehr zu prüfen, die Computerkader und die Korrektur des Pool-Ziels sind bereits beschlossen (01.11.2026, N2 = B). | `Transfer.SeasonEnd.cs:201`, `Transfer.PoolDistribution.cs:222` |
| C08 | S | Alte Spielwege und toter Code | **bleibt** | Das Entfernen unbenutzter Ergebnis-, Penalty- und Spielplanwege erleichtert die Pflege der tatsächlich verwendeten Regeln (#936). | `Playoffs.cs:11`, `MatchEngine.cs:13` |
| C11 | S | Spielrechnung: Feldspielerzahl zu Beginn | **bleibt** | Eine Prüfung der Leser vor der ersten Neuzählung kann klären, ob sich die unbelegten Startwerte 2 und 3 ganz streichen lassen. | `MatchEngine.Managed.cs:81` |
| D01 | S | Modus „nur Weltkugel“ | ohne | Der Code setzt die 400.000 DM bereits, zu prüfen bleiben der auslösende Vereinsplatz im Weltkugelmodus und die Behandlung seines Kaders. | `SeasonFlow.cs:388`, `CupsNational.WorldCupLoop.cs:233`, `SeasonFlow.cs:431` |
| D02 | S | Saisonwechsel: Absteiger und Rückkehr gelisteter Spieler | ohne | Ein Originalstand mit doppeltem Rang oder 29 belegten Kaderplätzen muss zeigen, wer tatsächlich absteigt und wohin der Rückkehrer gelangt. | `SeasonFlow.Zwangsabstieg.cs:205`, `Transfer.Listed.cs:47` |
| D03 | S | Poolverteilung und Pool-Plätze | ohne | Der Markt liest die Länderwerte bereits über NationalRatings und Gruppe 3 umfasst sieben Vereine, zu prüfen bleiben deren Identität nach Ligawechsel und der Altersersatz verlorener Plätze. | `Transfer.Pool.cs:183`, `Transfer.PoolDistribution.cs:261`, `Transfer.Pool.cs:193`, `CupsNational.GameState.cs:76` |
| D04 | S | Leihe, Bank und Darlehen zwischen Managern | ohne | Ein Spielstand vor und nach dem Leihende sowie ein Stand mit Managerdarlehen können die bisher nur hergeleitete Rückkehr und Kontozuordnung bestätigen. | `Player.Contract.cs:155`, `OriginalImport.Rest.cs:112` |
| D05 | S | Laden: KAPUTT.HOK und Sperren | ohne | Für den Import mit auf null gesetzten Sperren ist zu klären, ob das HOK eine Sperre überträgt und wie das Original nach der Mogelguru-Meldung fortfährt. | `OriginalImport.cs:611`, `OriginalImport.Access.cs:25` |
| D07 | S | Transfer: Meldung „unfair“ und verworfenes Gebot | ohne | Erst ein Grenzgebot und ein nachträglich unausführbares Angebot im Original können eine andere Ablehnungs- oder Verfallsregel belegen. | `Player.Contract.cs:276`, `Transfer.cs:671` |

## O – Reine Darstellung

| Nr. | Klasse | Thema | Liste | Begründung | Beleg |
|---|:---:|---|---|---|---|
| B01 | O | Titelmusik in Endlosschleife | ändern | Die Endlosschleife verändert ausschließlich, wie lange die Titelmusik zu hören ist. | `AudioMixer.cs:39` |
| B04 | O | Optionen: RAM-Zeile, Speicherprüfung, Hintergrund/Scrolling | bleibt | RAM-Zeile, Speicherprüfung und feste Grafikoptionen betreffen auf der Zielplattform die Darstellung. | `GameOptions.cs:30`, `GameOptionsScreen.cs:25` |
| B09 | O | Bildwechsel: Zwischenbild | bleibt | Dauer, Palette und Einrollen ändern nur den sichtbaren Übergang beim Laden. | `ScreenTransition.cs:16`, `ScreenTransition.cs:27` |
| B14 | O | DEB-Pokal-Auslosung live | bleibt | Das Aufdecken ohne rollende Kugel verändert die Animation bei gleicher Ziehung. | `DebCupDrawLive.cs:67` |
| B20 | O | Finanzen-Bildschirm: Balken, Kontostand, Kursgrafik | bleibt | Balkengrenzen, Zentrierung und Linienführung verändern die dargestellten Finanzbeträge nicht. | `FinanceScreen.cs:251`, `FinanceScreen.cs:429` |
| B23 | O | Spieltag-Bildschirm: Tempo und Banner | bleibt | Spieltempo auf dem Bildschirm und Länderbanner beeinflussen die Präsentation desselben Spieltags. | `MatchDayScreen.cs:97`, `MatchDayScreen.cs:34` |
| B24 | O | Tabellenkurve: Lage der Platzzahlen | bleibt | Die Verschiebung der Platzziffern verändert allein deren Lage an der Tabellenkurve. | `RankCurve.cs:16` |
| B25 | O | Saisonende: Meistersymbol und Pokalbild | ändern | Meistersymbol und Pokalbild ändern ausschließlich die Darstellung des bereits feststehenden Siegers. | `SeasonEndSummaryScreen.cs:43`, `SeasonEndTrophyScreen.cs:11` |
| B27 | O | Bestenliste: Farbe, Kennung, Zentrierung | bleibt | Die Sitzungskennung steuert die farbliche Hervorhebung des Eintrags, die Titelgrenze dessen Zentrierung. | `HighscoreScreen.cs:21`, `HighscoreScreen.cs:129` |
| B28 | O | Historie: Balkenhöhe und -farbe | ändern | Balkenhöhe und Farbe zeichnen die gespeicherten Saison- und Pokalergebnisse mit einer angenäherten Skala. | `HistoryScreen.cs:22`, `HistoryScreen.cs:173` |
| B34 | O | Transferdialoge: Lage und Aufbau ohne Videobeleg | bleibt | Die Transferdialoge führen dieselben Verhandlungen mit angenäherten Positionen und Rahmen. | `TransferDialog.cs:7`, `TransferDialog.cs:48` |
| B36 | O | Transfermarkt: Kontoleiste, Trennlinien, Klickflächen | bleibt | Einrückung, Schatten und Trennlinien prägen das Marktbild, für die angenommene Klickfläche ist keine andere Transaktion belegt. | `TransferScreen.cs:181`, `TransferScreen.cs:504` |
| B37 | O | Kleine Darstellungsdetails | bleibt | Ziffernbreite, Schatten und Textkürzungen betreffen die Ausgabe, für die Europapokal-Termine ist eine andere Spielansetzung nicht belegt. | `SeasonEndSummaryScreen.cs:106`, `InfoFileScreen.cs:20` |
| S02 | O | Torszene: Darstellung 2 „Dauer-Eisfeld“ | ändern | Der belegte Darstellungsmodus betrifft die Dauer der Eisfeldansicht, während der genaue Ablauf über den gesamten Spieltag ungeprüft ist. | `GoalSceneCall.cs:32`, `em.exe 0x568A9–0x568B6` |
| S03 | O | Torszene: Wiederholung, Bildtakt, Speicher | bleibt | Der fehlende Rücksprung betrifft die Torszene, und die Wiedergabe derselben Szene ist mit R19-E1 = A ausdrücklich gebilligt. | `MatchEngine.GoalScene.cs:28`, `GoalSceneCall.cs:28` |

## K – Keine Änderung

| Nr. | Klasse | Thema | Liste | Begründung | Beleg |
|---|:---:|---|---|---|---|
| B02 | K | Musik: OPL2-Emulation und Abspielorte | bleibt | Die OPL2-Emulation ersetzt die Soundkarte, und Entscheidung #221 beschränkt die Musik auf den Titel. | `MusicPolicy.cs:19` |
| B03 | K | Geräteeinstellungen, Lautstärke, Zusatzschalter | bleibt | Die zusätzlichen Geräteschalter lassen den ursprünglichen Bedienweg weiterhin zu. | `SettingsScreen.cs:12` |
| B05 | K | Gehaltene Knöpfe und Pfeile: Bildtakt statt Warteschleife | bleibt | Der Bildtakt ersetzt die rechnerabhängige Warteschleife für gehaltene Knöpfe bei unveränderter Blätterfunktion. | `FinanceScreen.cs:398` |
| B06 | K | Posteingang | bleibt | Der zusätzliche Posteingang archiviert Meldungen mit eigener Sortierung und Löschfrist. | `FinanceInbox.cs:169` |
| B26 | K | Spielvorbereitung: Absicherungen | bleibt | Die Startprüfungen schützen zusätzliche oder im Original unerreichbare Eingabekonstellationen. | `GameSetup.cs:230`, `SetupScreen.cs:251` |
| B32 | K | Aufstellung: Remake-Tasten A und C, Pfeilrichtung der Modi | bleibt | Die ausdrücklich akzeptierten Tasten A und C erleichtern vor dem Spiel das Aufstellen und Leeren (Antwort 5). | `LineupScreen.cs:21`, `LineupScreen.cs:159` |
| F01 | K | Ganze DM statt Drittel-DM | bleibt | Ganze DM sind für Gehälter, Prämien und Gebühren durch N1 = B trotz Rundungsdifferenz ausdrücklich gewählt. | `FinanceSponsors.cs:396`, `Finance.cs:97` |
| F02 | K | Buchung statt direkter Gutschrift | bleibt | N5 = C ordnet die Vorgänge dem Finanzbuch zu und erhält dabei den Kontostand. | `StadiumStatistics.cs:184`, `FinanceStadium.cs:505` |
| F03 | K | Finanzbuch und Leihanfrage per Nachricht | bleibt | Finanzbuch und Leihnachricht an einen Mitmanager sind zusätzliche Verwaltungsfunktionen (zusatzfunktionen.md, Teil B). | `FinanceBookScreen.cs:10`, `FinanceScreen.cs:38` |
| F05 | K | Finanzen: erneute Buchung nach Verlassen des Streifens | bleibt | N2 = B deckt das Löschen des Betrags ab, das eine versehentliche zweite Buchung verhindert. | `FinanceScreen.cs:459` |
| F09 | K | Monatliche Grundeinnahme: Sturm-Teamwert | bleibt | Der Ersatzwert für die monatliche Einnahme gilt nur für Remake-Spielstände vor Schema 12. | `FinanceMonthly.cs:54` |
| K01 | K | Computervereine mit eigenem Kader: Aufbau und Werte | bleibt | Die Nutzerentscheidung vom 01.11.2026 verlangt echte Computerkader mit eindeutigen Spielernamen (transfer.md, Teil 5). | `Squad.Setup.cs:37`, `Squad.Setup.cs:272` |
| K02 | K | Kader-KI der Computervereine | bleibt | Vertragsverlängerungen und Zugänge der KI erhalten die ausdrücklich gewünschten Computerkader (01.11.2026, R2-12). | `Transfer.Squads.cs:31`, `Transfer.Squads.cs:366` |
| K03 | K | Spielerpool und Computerkader: Tausch und Namen | bleibt | Der Tausch zwischen Pool und Computerkadern erhält die am 01.11.2026 beschlossene Eindeutigkeit der Namen. | `Transfer.Pool.cs:797`, `Transfer.Pool.cs:543` |
| K05 | K | Heilung von Spielern ohne Managerkader | bleibt | Die Heilung außerhalb des Managerkaders unterstützt das erweiterte Kadermodell, ohne den Heilungsverlauf eines Originalkaders zu ändern. | `GameState.DailyRun.cs:157` |
| K08 | K | Neuer Manager mitten im Spiel | bleibt | Die zufällige Vereinszuteilung erweitert den belegten Wahlweg, und für den Startkader gilt die Entscheidung vom 01.11.2026. | `GameState.AddManager.cs:61`, `GameState.AddManager.Swap.cs:37` |
| K09 | K | Verkauf an Computerverein und Sponsorzuschuss | bleibt | Der Verkauf korrigiert den Bezug auf die falsche Mauszeile gemäß N2 = B, der zusätzliche Zuschussfall betrifft Altstände. | `Transfer.ComputerClub.cs:19`, `Transfer.ComputerClub.cs:76` |
| K10 | K | Unaufgeforderte Angebote | bleibt | N2 = B deckt den korrigierten Stärkebezug ab, und die Unterbringung bei vollem Computerkader folgt dem beschlossenen Kadermodell vom 01.11.2026. | `Transfer.Unsolicited.cs:50`, `Transfer.Unsolicited.cs:167` |
| L01 | K | HOK-Export: Wettbewerbe und Nationalteam | bleibt | Die Vorbelegung unbekannter Wettbewerbsfelder gehört zum ausdrücklich gewählten HOK-Export (E14-1 C, R14-D2 = B). | `OriginalExport.Cups.cs:73`, `OriginalExport.Cups.cs:279` |
| L02 | K | HOK-Export: Liga- und Vereinssätze | bleibt | Die exportierten Liga- und Vereinssätze verwenden für ungedeutete Felder die im zusätzlichen HOK-Export vorgesehenen Ersatzwerte (E14-1 C, R14-D2 = B). | `OriginalExport.League.cs:14`, `OriginalExport.League.cs:72` |
| L03 | K | HOK-Export: Kader und Pool | bleibt | Die abweichende Abbildung von Spieler- und Poolfeldern entsteht beim optionalen Schreiben eines DOS-Spielstands (E14-1 C, R14-D2 = B). | `OriginalExport.Squads.cs:67`, `OriginalExport.Squads.cs:292` |
| L04 | K | HOK-Export: Kopf, Satzfolge und allgemeine Felder | bleibt | Abgeleitete Kennung und Ersatzwerte im Dateikopf gehören zur beschlossenen Exportabbildung (E14-1 C, R14-D2 = B). | `OriginalExport.cs:241`, `OriginalExport.cs:269` |
| L05 | K | HOK-Export: Manager, Rekorde, Werbung | bleibt | Der optionale HOK-Export nähert Manager-, Werbe- und Rekordfelder gemäß E14-1 C und R14-D2 = B an. | `OriginalExport.Rest.cs:152`, `OriginalExport.Managers.cs:127` |
| L06 | K | HOK-Import: Wettbewerbe und Nationalteam | bleibt | Die Rekonstruktion der Wettbewerbshistorie aus unvollständigen HOK-Daten ist durch die Importentscheidung N4 = C gedeckt. | `OriginalImport.Cups.cs:74`, `OriginalImport.National.cs:24` |
| L07 | K | HOK-Import: Markt, Werbung und Spielernamen | bleibt | Die Zuordnung mehrdeutiger Spieler und Marktpositionen erfolgt beim ausdrücklich akzeptierten HOK-Import (N4 = C). | `OriginalImport.Market.cs:60`, `OriginalImport.Market.cs:69` |
| L08 | K | HOK-Import: Nachspielen und allgemeine Felder | bleibt | Der Import ergänzt fehlende Zustände durch Nachspielen und übernimmt gesicherte Schalter gemäß N4 = C. | `OriginalImport.cs:17`, `OriginalImport.Rest.cs:281` |
| L09 | K | HOK-Patcher (Werkzeug) | bleibt | Der größere Wertebereich des HOK-Patchers dient externen Proben und ist über die Spieloberfläche unerreichbar. | `OriginalSaveGamePatcher.cs:28` |
| L10 | K | Laden/Speichern-Dialog und Spielstanddateien | bleibt | Der Sicherungstakt je neuem Zug ist in spielstand.md festgelegt, der Verzicht auf ERROR.HOK durch Nutzerentscheidung R12-D2 = C. | `AutoSave.cs:20`, `SaveLoadScreen.cs:455` |
| L11 | K | Migrationen älterer Remake-Spielstände | bleibt | Die Migrationen erschließen ältere Remake-Dateien und erhalten deren vereinbarte Ladefähigkeit (Antwort 8). | `SaveGame.Migration.Nationalteam.cs:14`, `SaveGame.Migration.HokPlayOffMarken.cs:9` |
| S09 | K | Zusehen und Torhüter draußen | bleibt | Das abgeleitete Reihenfeld nach gezogenem Torhüter liefert beim nächsten Vereinsspiel denselben Wert und dient nur dem Remake-Modell. | `Squad.Tactics.GoaliePulled.cs:12` |
| S10 | K | Bundestrainer: Felder von Satz 5 | bleibt | Das Remake leitet die benötigten Länderwerte beim Spiel aus derselben Aufstellung ab und spart zusätzliche Satzfelder ohne belegte Wirkung. | `MatchEngine.National.cs:70`, `MatchEngine.Preparation.cs:79` |
| S11 | K | Zufall der Darstellung außerhalb des Stroms | bleibt | Der eigene Zufall für reine Darstellung sichert die ausdrückliche Entscheidung N6 = A, dass Zuschauen den Spielverlauf nicht verändert. | `DebCupDrawLive.cs:64`, `Highscore.cs:231` |
| T01 | K | Entlassene Manager (nur ältere Spielstände) | bleibt | Die Sonderbehandlung entlassener Manager dient ausschließlich dem Laden älterer Remake-Stände (Antwort 8). | `SeasonFlow.Oberliga.cs:83` |
| T02 | K | Spielende und Kopierschutz | bleibt | Die Rückkehr zum Titel ersetzt das DOS-Programmende, und die Handbuchfrage entfällt nach Nutzerentscheidung vom 30.10.2026 (portierung.md). | `SeasonEndGameOverScreen.cs:18`, `em.exe 0x4CD46` |
| T05 | K | Saisonende: Kleinigkeiten | bleibt | Die Ersatzregeln für Schalter und Platztausch bewahren in den erreichbaren Saisonendfällen denselben Fortgang. | `GameState.Watch.cs:135`, `NewGame.cs:291` |
| W01 | K | Abgleich- und Exportwerkzeuge (emtool) | bleibt | Die Prüf- und Exportwerkzeuge setzen Toleranzen und Namen außerhalb des laufenden Spiels. | `HokCompareClasses.cs:73`, `SoundExport.cs:9` |
| C01 | K | Zufallsfolge nicht bitgenau | bleibt | Die andere Zufallsfolge ist durch R15-D2 = A und N3 = B bis zum späteren bitgenauen Modus akzeptiert. | `MatchEngine.Original.cs:25`, `GameState.Spielplan.cs:11` |
| C02 | K | Rückfälle für Altstände: Teamwerte und WM-Mischung | bleibt | Das nachträgliche Ziehen fehlender Team- und WM-Werte ermöglicht ältere Stände und Testaufbauten (Antwort 8). | `CupsNational.GameState.cs:95`, `MatchEngine.TeamValues.cs:55` |
| C03 | K | Bildschirm öffnen setzt keine Werte (WM, Freundschaftsspiele) | bleibt | E4 = B verlegt das Setzen von Energie und Länderwerten vom bloßen Öffnen auf das zuständige Spielereignis (AGENTS.md §4). | `CupsNational.GameState.cs:284`, `FriendliesScreen.cs:26` |
| C04 | K | WM-Kader und Länderwerte | bleibt | Die zusätzlichen Berechnungstage liefern beim Abruf dieselben Länderwerte, der breitere Kandidatenkreis ist ein Rückfall für fehlende Pooldaten. | `CupsNational.GameState.cs:87`, `CupsNational.GameState.cs:176` |
| C05 | K | Bestenliste: Kennzahlen und Punkte | bleibt | Die Europapokal-Kennzahl korrigiert gemäß R21-E1 = A und N2 einen Originalfehler, die abweichende Saisonzahl betrifft entlassene Altmanager. | `Highscore.cs:154`, `Highscore.cs:331` |
| C06 | K | Absicherungen statt Absturz oder Endlosschleife | bleibt | Abbruchgrenzen ermöglichen die Fortsetzung in Sonderzuständen, in denen das Original hängen bliebe oder abstürzte (N2 = B). | `FinanceTalent.cs:217`, `FinanceTalent.cs:130` |
| C10 | K | Tabellen: Sortierung und Platzliste | bleibt | E4 = B schützt die nächste Sortierung vor Seiteneffekten durch das bloße Ansehen einer Teiltabelle (AGENTS.md §4). | `Season.cs:201`, `Season.cs:280` |
| C12 | K | Spielvorbereitung: Erweiterungen | bleibt | Die zusätzliche Zufallswahl und die Startoptionen erweitern die Spielvorbereitung, während R21-E1 = A die Prüfung unvollständiger Managereingaben deckt. | `GameSetup.cs:124`, `SetupScreen.cs:218` |
| D06 | K | Talente und Spieler der Computervereine | ohne | Alter, Talent und Werte der zusätzlich erzeugten Computerspieler gehören zum ausdrücklich gewählten Computerkader vom 01.11.2026 (transfer.md, Teil 5). | `Player.Factory.cs:42`, `Player.Factory.cs:110` |

## Fragen und Nutzerentscheidungen

Antwort im Chat zu allen acht Fragen: **„alle Empfehlungen“**. Keine Nutzerentscheidung ist offen. Ausstehende Originalbelege stehen beim jeweiligen Thema.

| Nr. | Frage | Empfehlung und bestätigte Antwort |
|---|---|---|
| 1 | Wie zählen falsche oder fehlende fachliche Anzeigewerte? | A · N, weil auch eine reine Anzeige die Entscheidung des Managers beeinflusst. |
| 2 | Wann verändert ein Bedienunterschied den Ablauf? | B · N für eine fehlende Funktion oder andere Spielaktion, S für Mehrarbeit zum Erreichen derselben Funktion. |
| 3 | Welche zusätzlichen Schließtasten sind gewollt? | B · Gezielte Rückwege wie Escape und Rechtsklick behalten, pauschales Schließen mit jeder Taste angleichen. |
| 4 | Soll unverändertes Verlassen von Aufstellung und Taktik den Original-Schreibvorgang auslösen? | A · Ja, der Ausgang kann inzwischen gewanderte Werte verändern und E4 = B regelt das Öffnen. |
| 5 | Welche zusätzlichen Aufstellungstasten sind vor dem Spiel gewollt? | A · A und C behalten, weil beide vor dem Spiel bewusst ausgelöst werden. |
| 6 | Wie zählt ein Thema mit ungeklärtem Originalverhalten? | A · Stärkste belegte Wirkung werten, sonst S für die gezielte Prüfung. Das im Chat genannte D01-Beispiel ist durch die implementierte Kontosetzung überholt. |
| 7 | Deckt der spätere bitgenaue Zufallsmodus eigenständige Ablaufunterschiede ab? | B · Die beschlossene Zufallsquelle bleibt K, sichtbare Änderungen im Spieltags- oder Saisonablauf zählen als N. |
| 8 | Wie sind Migrationen und Rückfälle für ältere Remake-Stände zu behandeln? | B · Nachweislich doppelte Rückfälle bei gleicher Ladefähigkeit bündeln (S), benötigte Migrationen erhalten (K). |
