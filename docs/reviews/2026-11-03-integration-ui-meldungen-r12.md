# Review: Integration Runde 12 – Spur ui-meldungen (PR #535)

- **PR:** #535 · **Base:** `93ecdf90e9c70bd023bc58904fd69d19106e4517` · **Head:** `3f9355e505a59c4055b1960e1c9b745402137312`
- **Reviewer:** Claude Code (geplanter Lauf, frische Sitzung ohne Implementierungskontext, Claude Opus 5.5)
- **Checkout:** Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; SDK 10.0.402; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`original/gamedata`, Zugriff per `git ls-remote` geprüft). Ausführung am nachgewiesenen
  Checkout, nicht nur Codelesen.

## 1. Merge-Empfehlung

**Ja, für genau `3f9355e`.** Alle drei Läufe von `tools/pruefen.sh` am Head grün und deckungsgleich mit der PR-Tabelle; alle
genannten Gegenbeispiele werden von den neuen Tests erkannt (Gegenproben unten, jede Mutation rot). Schema 34 → 35 ist die
richtige nächste Nummer (`main` = Base, kein anderer offener PR ändert das Schema), ein echter Schema-34-Stand vom Base lädt am
Head. Die Änderungen aus #527 in fremden Dateien sind in #527 je Stelle begründet und auf den Anschluss beschränkt.
Der einzige Widerspruch zur neuen Fassung von `AGENTS.md` §4 (B1: Posteingang setzt beim Öffnen „gelesen“) ist kleiner als der
vorherige Stand (Setzen beim Zeichnen) und kein Merge-Hindernis, braucht aber eine Entscheidung in Runde 13.

## 2. Befunde

**B1 (P2) – Posteingang ändert beim bloßen Öffnen den Spielzustand.**
[`InboxScreen.cs` Z. 35–41](https://github.com/kaaasi/eishockey-manager-redone/blob/3f9355e505a59c4055b1960e1c9b745402137312/src/EishockeyManager.UI/Screens/Manager/InboxScreen.cs#L35-L41):
`Enter()` setzt `Message.Read = true` für die erste Seite. `Message.Read` wird gespeichert (Feld im Spielstand) und steuert,
welche Nachricht die Leiste zeigt (`NewsBar.Shown`). Die präzisierte Regel in `AGENTS.md` §4 sagt „keine weiteren Ausnahmen
für Bildschirme“. Gegenbeispiel: `host.Show(new InboxScreen(…))` ohne Eingabe → `SaveGame.Save` vorher ≠ nachher
(belegt durch den eigenen Test `Inbox_MarksReadOnOpenAndPaging_NotOnDraw`, der genau das zusichert). Vorschlag: „gelesen“
als Ereignis im Spielkern (z. B. `GameState.MarkRead(manager, messages)`, aufgerufen wie `OnMainMenu`) oder ausdrücklich als
Ausnahme in §4 aufnehmen (Entscheidung Nutzer).

**B2 (P3, vorbestehend, nicht aus diesem PR) – weitere Bildschirme setzen beim Öffnen `GameState.CurrentManager`.**
[`SeasonEndSummaryScreen.cs` Z. 101](https://github.com/kaaasi/eishockey-manager-redone/blob/3f9355e505a59c4055b1960e1c9b745402137312/src/EishockeyManager.UI/Screens/SeasonEndSummaryScreen.cs#L101)
(`Enter`) und [`ContractsScreen.cs` Z. 173](https://github.com/kaaasi/eishockey-manager-redone/blob/3f9355e505a59c4055b1960e1c9b745402137312/src/EishockeyManager.UI/Screens/Team/ContractsScreen.cs#L173)
(`Enter` → `NextRenewal`, nur im Saisonendablauf). `CurrentManager` ist gespeicherter Zustand. Fällt unter den Wortlaut
„keine weiteren Ausnahmen“; als Ablaufsteuerung des Saisonendes vertretbar, sollte aber in Runde 13 entweder in den Spielkern
oder als benannte Ausnahme. Übrige Durchsicht (alle `Enter`-Überschreibungen, Konstruktoren, `Draw`-Pfade in
`src/EishockeyManager.UI`): kein `Draw` ändert Spielzustand; `MatchDayScreen.Enter` ruft `Game.Continue()` – Folge von
„und tschüß…“, Zeit über die Zeitleiste, regelkonform; `TransferScreen.Enter` nur lesend; `HighscoreScreen` schreibt die
Bestenlistendatei, nicht den Spielzustand; das Hauptmenü ruft `OnMainMenu` genau einmal je `Enter` bzw. nach Zugabgabe.

**B3 (P3) – Testlücke „Zeichnen ändert nichts“ im Hauptmenü.**
Gegenprobe: `Game.OnMainMenu(Game.ActiveManager, true)` zusätzlich in `MainMenuScreen.Draw` →
`MainMenuEventUiTests` bleibt **grün**. Grund: Der Test nimmt den Abdruck, nachdem das Kennzeichen schon gelöscht und die
Aufstellung schon vollständig ist; ein Ereignis im Zeichnen ändert dann nichts Sichtbares. Ergänzung: nach dem Aufbau eine
Nachricht ablegen bzw. die Aufstellung lückig machen, dann zeichnen und Abdruck/`HasNewMessage`/`Lineup.Count` vergleichen.
[`MainMenuEventUiTests.cs` Z. 52](https://github.com/kaaasi/eishockey-manager-redone/blob/3f9355e505a59c4055b1960e1c9b745402137312/tests/EishockeyManager.UI.Tests/MainMenuEventUiTests.cs#L52).

**B4 (P3) – Erster Zug eines neuen Spiels zieht keinen Gehaltsfaktor.**
`BeginManagerTurn` läuft nur aus `StartTurns` (aufgerufen allein aus `Continue`,
[`SeasonFlow.cs` Z. 137](https://github.com/kaaasi/eishockey-manager-redone/blob/3f9355e505a59c4055b1960e1c9b745402137312/src/EishockeyManager.Core/SeasonFlow.cs#L137))
und `PassTurn`. Nach `NewGame.Create` ist `_contractTurnFactor` = 0, am ersten Tag gilt der alte abgeleitete Wert. Das
Original zieht beim ersten Menüaufbau (0x44E3F). Der `[APPROX]`-Kommentar in `Transfer.Contract.cs` nennt nur „ältere
Spielstände“. Entweder beim Spielstart ziehen oder den Kommentar um „erster Tag eines neuen Spiels“ ergänzen.

**B5 (P3) – Kommentar.** [`MainMenuEventTests.cs` Z. 114](https://github.com/kaaasi/eishockey-manager-redone/blob/3f9355e505a59c4055b1960e1c9b745402137312/tests/EishockeyManager.Core.Tests/MainMenuEventTests.cs#L114):
„Stände vor Schema 33“ muss „vor Schema 35“ (bzw. „Schema 34“) heißen; der Test selbst setzt richtig 34.

**Erwartet offen, bestätigt:** `ArrowPair` hat außer seiner eigenen Datei keinen Aufrufer (Werbung/Stadion nicht
angeschlossen); kein Aufruf des Vereinsfensters aus „Stärken“; `TeamRecords.Enter` nur aus Länderspielen und
`EnterLeagueRecords` (Ligaspiele), keine Pokal-/Play-off-Spiele; `SaveLoadScreen.Wrap` 158 px mit `[APPROX]`.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe) am Head `3f9355e`, SDK 10.0.402, Ergebnis: bestanden.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2147 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1810 | 0 | 301 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1811 | 336 | 0 |

**Gegenbeispiele (rot ohne die Änderung, grün am Head).** Ausgeführt als gezielte Rücknahme der jeweiligen Änderung in einer
temporären Kopie des Heads (`EM_REQUIRE_GAMEDATA=1 dotnet test … --filter …`); die neuen Tests kompilieren am Base bzw. vor
#527 nicht (neue API), daher dieser Weg. Am Head ohne Mutation alle grün.

| ID | Rücknahme / Mutation | rot |
|---|---|---|
| E3 | `MessageBox.Update` schließt wieder auf Rechtsklick/Taste | 5 Fälle `UiMeldungenTeil1Tests.*IgnoresKeysAndRightClick` |
| E4 | `NewsBar.Draw` setzt wieder `Read` | `MainMenuEventUiTests.NewMessage_…_DrawingChangesNothing` |
| E4 | `OnMainMenu` zweimal je `Enter` | dto. (Leiste fehlt) |
| E4 | `OnMainMenu` zusätzlich in `Draw` | **grün** → B3 |
| E4 | `DrawContractTurnFactor` aus `BeginManagerTurn` entfernt | `MainMenuEventTests.ContractTurnFactor_IsDrawnPerManagerTurn…` |
| E4 | `MarkNewMessage` beim Ablegen entfernt | 3 Tests `MainMenuEventTests` |
| E4 | `TakeNewMessage` → nur lesen (nicht löschen) | `MainMenuEventTests.NewMessage_ShowsOnFirstMenuBuild_ThenIsCleared` |
| E4 | Neuaufstellung aus `OnMainMenu` entfernt | `MainMenuEventUiTests.ReturnFromSubScreen_WithAutoLineup…` |
| UM-2 | `EnterLeagueRecords` aus `PlayNextMatchDay` entfernt | `IntegrationR12UiAnschluesseTests.LeagueMatches_EnterClubRecords` |
| UM-2 | Kurve `CurveX` +1 | `VereinsfensterTests.Render_CurveAxesAtTheOriginalPlace` |
| UM-2 | Trefferfläche Vereinsname x 230 → 200 | `VereinsfensterTests.HitAreas_FollowTheOriginal` |
| UM-4 | `FailVersion` setzt `WrongVersion` nicht | 3 Fälle `SaveGameLoad_FlagsWrongVersion`, `Load_WrongVersion_…`, `SaveLoadScreenTests.ForeignFile_…` |
| UM-4 | Ruhestandsprüfung im Transfer abgeschaltet | `Transfer_ClickOnRetiringPlayer_ShowsShortMessageOnly` |
| UM-4 | Kurzmeldung Baufirma entfernt | `StadiumScreenTests.SelectLevelConfirmAndAdjustTicketPrice` |
| UM-4 | Text „ist noch beleidigt...“ geändert | `ShortMessageTextsTests.Message_HasTheOriginalLines…` |
| M-K4 | gedrückter Pfeil x + 3 statt x + 2 | `ArrowPairTests.Pressed_DrawsKb4Source_…` |
| M-K9 | `PageFlip` immer true (auch nach Laden) | `DesktopTransition_PageFlipFollowsStartOrLoad` |
| L8 | Mitte 163 → 160 | `SpielendeOriginalTests.GameEnd_DrawsBlackScreenWithTwoLines` |
| L8 | Bestenliste vor „Das wars...“ entfernt | 2 Tests `SpielendeOriginalTests` |
| #527 | `InboxScreen.Enter` entfernt | `Inbox_MarksReadOnOpenAndPaging_NotOnDraw`, `YouthInboxScreenTests.NewsBar_OpensInbox…` |

Hinweis: `Read` zusätzlich in `InboxScreen.Draw` bleibt grün – ununterscheidbar, weil `Enter` dieselbe Seite schon markiert;
keine Lücke.

**Schema/Migration:** Am Base (`93ecdf9`) einen Zwei-Manager-Stand nach 10 `Continue()` mit offener Nachricht gespeichert
(Schema 34), am Head geladen: Erfolg, `HasNewMessage` aller Manager false, `ContractTurnFactor` im Bereich 85–115 (abgeleiteter
Wert bis zum nächsten Zug), Speichern → Laden → Speichern stabil, 4 weitere `Continue()` mit gezogenem Faktor im Bereich.
Fingerabdruck `4F547EB1845A35D3` und `SchemaVersion` 35 durch `SaveGameTests` abgedeckt; `[34] = MigrateUiMeldungen4`
eingetragen, Doku-Kommentar in `SaveGame.cs` passt.

**Fremde Dateien aus #527:** `GameState.cs` (+1 Zeile), `CupsNational.TeamInfo.cs`, `SaveGame.cs`/`SaveGame.Reader.cs`,
`StadiumScreen.cs`, `NewsBar.cs`, `InboxScreen.cs`, `TransferScreen.cs`, `SaveLoadScreen.cs`, `DesktopGame.cs` – alle in der
PR-Beschreibung von #527 einzeln begründet; der Diff `45e83d4..894c4a8` enthält nur diese Anschlüsse und die angepassten Tests.

**Nicht geprüft (Grenzen):** kein neuer Bildvergleich mit Videobildern für M-K4/M-K9/L8 (Pixelangaben nur über die Tests
und die Fundstellen im Code); Desktop-Ablauf (MonoGame) nicht ausgeführt.

**Akzeptierte `[APPROX]`:** eigene Zufallsquelle `TurnRandom` für den Gehaltsfaktor (hält Abdrücke stabil); Migration ohne
Kennzeichen/Faktor; Pokal-/Play-off-Spiele ohne Vereinsrekorde, keine Bilanzzeichen der Vereine; „gelesen“ ohne Gegenstück im
Original (Ort siehe B1); `PageFlip` nach Laden aus `SaveLoadScreen` erkannt; Wrap 158 px; Spielende-Palette kb\21 und Rückkehr
zum Titel; Seiten zu vier Zeilen für Remake-Meldungen mit mehr als vier Zeilen.
**Annahmen:** Fundstellen der Disassembly (0x44E22/0x44E3F/0x4527C, 0x0C0B8-Aufrufer, 0x4D385) nicht selbst nachdisassembliert.
