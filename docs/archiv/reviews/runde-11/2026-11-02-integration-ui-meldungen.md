# Review: Integration `ui-meldungen` (PR #466, Stapel #421 → #426 → #430 + Leitstelle-Commits)

Unabhängiges Review (Claude Code, geplanter Lauf, 02.11.2026; nicht an der Umsetzung beteiligt) nach
`docs/vorlagen/review-anfrage.md`. Schwerpunkt: Commit `4e27080` (Aufrufer in fremden Dateien auf 0x4C246/0x0C0B8) gegen
`docs/analyse/re-meldungen.md` §C und `em.exe`. Die Stapel-Inhalte sind in Review #443 geprüft und hier nicht wiederholt.

Ausführung am nachgewiesenen Checkout: Klon, `git rev-parse HEAD` = `f03410f8f06d2d9043788862bb2a2f54c0fff185`,
`git status --porcelain` leer; Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`), SDK 10.0.402.

| PR | Base | Head |
|---|---|---|
| #466 | `08b7b683949b9229d3fad424b48eea6354764bb2` (Branch `integration/runde-11-wettbewerbe-spiel`, #465) | `f03410f8f06d2d9043788862bb2a2f54c0fff185` |

## 1. Merge-Empfehlung

**Nein für diesen Head – wegen einem P2 (MU-01), Fix: eine Zeile plus Test.** Alles andere hält: R11U-02/04/05 behoben, alle
Umstellungen sind je per Mutation rot, die Kurzmeldungen stimmen mit `em.exe` überein, alle drei Läufe grün, Pixelvergleich
der umgestellten DEB-Pokal-Frage gleich wie in #443. Nach Behebung von MU-01 (mit Test) ist ohne weitere Prüfung der übrigen
Teile mergebar; MU-02 und MU-03 sind P3 und kein Hindernis. Gilt als vollständiges Review mit Testnachweis (Ausführung am Checkout).

## 2. Befunde

### MU-01 (P2) – Meldung mit vierter Zeile beschriftet den linken Knopf mit der vierten Textzeile

[`OriginalDialog.LeftText`](https://github.com/kaaasi/eishockey-manager-redone/blob/f03410f8f06d2d9043788862bb2a2f54c0fff185/src/EishockeyManager.UI/OriginalDialog.cs)
liefert „Zeile 4, sonst Zeile 5“ für beide Arten. Das gilt nur für die **Frage**. Disassembly 0x4C246: Der Puffer `[bp-0x32]`
erhält bei 0x4C482 Zeile 4; bei einer Meldung (Modus 0) mit Zeile 4 wird Zeile 4 als Text gezeichnet (0x4C4A0 ff.) und bei
0x4C4E2 der Puffer mit **Zeile 5** überschrieben (`jnz 0x4C508` überspringt die zweite Kopie), bei Meldung ohne Zeile 4 ebenso
(0x4C500). Linker Knopf einer Meldung ist also immer Zeile 5, wie `re-meldungen.md` §C („Meldung – dann beide Knöpfe ‚Z5‘“) und
der Satz im Kommentar der Klasse („Bei einer Meldung ohne vierte Zeile …“) nahelegen; nur die Frage nimmt Zeile 4.

Neu mit `4e27080`: `MessageBox.Dialog` ([TeamUi.cs](https://github.com/kaaasi/eishockey-manager-redone/blob/f03410f8f06d2d9043788862bb2a2f54c0fff185/src/EishockeyManager.UI/Screens/Team/TeamUi.cs))
reicht bei vier Zeilen `Line4 = _lines[3]`, `Line5 = "Na gut."` an. Erreichbar:
[AdvertisingScreen.cs:208](https://github.com/kaaasi/eishockey-manager-redone/blob/f03410f8f06d2d9043788862bb2a2f54c0fff185/src/EishockeyManager.UI/Screens/Manager/AdvertisingScreen.cs#L208)
(„Die Firma / <Name> / wird den Vertrag / nicht verlängern.“) zeigt links einen Knopf „nicht verlängern.“ und rechts „Na gut.“.

Gegenbeispiel (rot am Head, Test in `tests/EishockeyManager.UI.Tests`):

```csharp
var d = new OriginalDialog(OriginalDialogKind.Message, "Hans", "Die Firma", "Acme", "wird den Vertrag", "nicht verlängern.", MessageBox.ButtonText);
Assert.Equal(MessageBox.ButtonText, d.LeftText);   // ist "nicht verlängern."
```

Fix: `LeftText => Kind == OriginalDialogKind.Question && Line4.Length > 0 ? Line4 : Line5;` und die Annotation „links Zeile 4“
in der Klassenbeschreibung auf die Frage einschränken; Test für `OriginalDialog` (Meldung mit Zeile 4) und für die Werbe-Meldung.

### MU-02 (P3) – Testlücke Kurzmeldungen der Aufstellung und Transferliste

Mutation `ShowShort` → `Show` für „noch gesperrt !“ (`LineupScreen.Report`, Suspended) lässt **alle** 487 UI-Tests grün. Ebenso das
Streichen von NotEnoughMoney, TooManyOwnListings, MarketFull und Rejected aus `TransferScreen.ShortTexts`. Rot werden nur
AlreadyRejected (dedizierter Test), Impossible (`WaveBReviewScreenTests`), RosterFull (`TacticsClubScreenTests`), „Der darf da“ und
„Auto-Aufstellung“. Die Texte selbst sind in `em.exe` belegt (Abschnitt 3); es fehlt nur der Test je Eintrag. Ein Theory über
`ShortTexts` würde reichen.

### MU-03 (P3) – Doku/Test-Zuordnung

`arbeitspakete.md` nennt `OriginalDialogCallersTests` als Test je Aufrufer; der Test der Freundschaftsspiele steht in
`NationalScreenTests` (1 Zeile, `Assert.NotNull(screen.Message.Short)`). Er ist rot unter Mutation, die Angabe ist nur ungenau.

## 3. Ausgeführte Prüfungen

### Alle drei Läufe (`tools/pruefen.sh` am Head, Ergebnis: bestanden)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1973 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1662 | 0 | 279 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1663 | 310 | 0 |

### Gegenbeispiele erneut ausgeführt

| ID | Ablauf | Ergebnis |
|---|---|---|
| R11U-02 | `docs/arbeitspakete.md` Abschnitt `marker-werkzeug`: **Status** | „–“ (Z. 189); `ui-meldungen` „Spur fertig – integriert“ im eigenen Abschnitt (Z. 173). Behoben |
| R11U-04 | Mutation: Schluss-`EndPage()` in `InfoFileScreen.Layout` nur bei nichtleerer Seite | rot: `Layout_EndsWithEmptyLastPage…("A#")`, `("#")`, `Layout_FileEndingExactlyAfter27thLine…`. Behoben |
| R11U-05 | Mutation: `InfoFileScreen.Update` ändert `ActiveManager.Money` | rot: `ClickOnHiddenField_OpensInfoFile_PagesThenBack_WithoutChangingGame`. Behoben |
| DebCupDrawLive | Mutation: `OriginalDialogKind.Question` → `Message` | rot: `DebCupDrawQuestion_UsesOriginalQuestionLayout` |
| AdvertisingScreen | dto. in `RenewalDialog` | rot: `AdvertisingRenewalQuestion_UsesOriginalQuestionLayout` |
| TransferDialog | dto. in `AsDialog` | rot: `TransferQuestion_Talent_UsesOriginalQuestionLayout`, `…AsDialog_CarriesTitleLinesAndButtons` |
| TeamUi.MessageBox | Mutation: `Fits => false` (alter Rahmen) | rot: `MessageBox_TwoLines_IsOriginalMessageWindow…` |
| Kurzmeldung Aufstellung | `ShowShort` → `Show` bei „Auto-Aufstellung“ und „Der darf da“ | rot: `Lineup_AutoDisabledMessage_IsShortMessageWith45Ticks`, `Lineup_WrongRole_IsShortMessage…`; „Schon 22 Mann“ rot in `TacticsClubScreenTests.Lineup_RefusesTwentyThirdDressed`; „noch gesperrt !“ **nicht** (MU-02) |
| Kurzmeldung Transferliste | `Say` ohne Kurzmeldung | rot: `TransferScreen_RejectedAndMissingMoney…`; Einzeleinträge siehe MU-02 |
| Kurzmeldung Freundschaftsspiele | `ShowShort` → `Show` | rot: `NationalScreenTests.Friendlies_…_LimitMessage` |

Mutationen in einer Kopie des Checkouts (ohne `.git`), Läufe mit `EM_REQUIRE_GAMEDATA=1`; 12 + 3 rote Tests in den beiden Durchgängen.

### Belegstellen in `em.exe` (Disassembly, DGROUP = Segment 0x14FD, Basis Datei 0x16DD0)

Alle Aufrufe `call 0x38:0x8C8` (= Datei 0x0C0B8, Basis 0x99F) gesucht (19 Stellen) und die Texte aus DGROUP gelesen:

| Aufruf | Zeile 1 / Zeile 2 | Remake |
|---|---|---|
| 0x40AF3, 0x40DF9 | Auto-Aufstellung / ist deaktiviert ! | `LineupScreen`, Kurzmeldung (Modus 45 – siehe unten) |
| 0x40B55 | **Spieler ist** / noch gesperrt ! | Kurzmeldung, Zeile 1 = Spielername (erwartet offen) |
| 0x40EB9 | Der darf da / nicht rein ! – gemeinsamer Aufruf; „Schon 22 Mann / im Team !“ (DGROUP 0x11D5, 0x40E51) springt dorthin | Kurzmeldung |
| 0x40EF7, 0x414C6, 0x3DE9F | Schon 6 Feldspieler / auf dem Eis !; Kein / Platz mehr !; Die Baufirma / ist noch beleidigt... | keine Remake-Entsprechung (erwartet offen) |
| 0x46A4D, 0x47106, 0x472EC, 0x47AAE, 0x47A80, 0x47B11 | genug Geld ! / Schon 3 Spieler auf / Ihr Angebot wurde abgelehnt. (zweimal) / Transfer findet nicht statt / bereits abgelehnt | `TransferScreen.ShortTexts`, Texte stimmen mit `TransferMarket.Message` |
| 0x42CE3 | Schon 11 Mann auf / dem Transfermarkt. | `ShortTexts` (MarketFull) |
| 0x3C2C5 | Nicht mehr als / 8 Freundschaftsspiele | `FriendliesScreen` |
| 0x454EA | Wollen Sie die WM wirklich … | Bundestrainer-Warnung (nicht im Delta geändert) |
| 0x46DEF, 0x5C559, 0x5C59A | Dieser Spieler hört am Saisonende auf; Spielstand-Meldungen | ohne Entsprechung (wie in der PR-Beschreibung) |

„Verletzt“ steht in keinem der sechs Aufrufe der Aufstellung → `[APPROX]` „ist verletzt.“ bleibt Fenster mit Klick: bestätigt.

**Modus „Auto-“ (45 statt 75 Takte):** 0x0C0B8 vergleicht Zeile 1 mit dem Text an DGROUP 0x14F1 (= „Auto-“, 5 Zeichen, `strncmp`) und
berechnet `0x4B − 0x1E · [gleich]` = 75 bzw. 45 (+100, wenn Zeile 2 fehlt; 0x4C246 zieht 100 wieder ab, kein Unterschied). 0x4C246
übergibt `[bp-0xa]` an `0x50:0x19E` (Datei 0x130EE), das durch 3 teilt und auf den BIOS-Zähler (0040:006C, 18,2 Hz) wartet: 25 bzw.
15 Takte. Der Test `Lineup_AutoDisabledMessage…` mit `FramesFor(45)` (49 Bilder) trifft das; `FramesFor(75)` = 82 < 100, daher
verdecken die angepassten `Frames(100)`-Tests nichts.

**Tastatur/Maus in 0x4C246:** Der Rumpf (0x4C246–0x4C6B6) enthält keinen Tastaturaufruf; Eingabe nur über die Ereignisabfrage 0x0C068
(Linksklick, `cmp [0x5A16],1`) und den Knopftest 0x0BEBD. Bestätigt die Aussage in `re-meldungen.md`.

### Pixelvergleich

`emtool screens … debpokal-auslosung` (`01_frage_spielstart`, jetzt über `DebCupDrawLive` → `OriginalDialog`) gegen
`oberflaeche_0346` (Video 3:46), Bereich 75…242 × 107…182, Versatz −2…+2: bester Versatz **0 | 0**, MAD 17,9, 83,8 % der Pixel mit
Abweichung < 40 – gleich wie der Baustein in #443 (17,8 / 83,7 %). Die Umstellung des Aufrufers verändert das Bild nicht. Für
Kurzmeldung und Meldung mit „!“ gibt es weiterhin kein Referenzbild (Hinweis #443 gilt; in `docs/referenz-videos.md` keine Fundstelle).

### Zu den besonders zu prüfenden Punkten

1. **[APPROX] `MessageBox`:** „Na gut.“ für Meldungen mit drei Zeilen ist vertretbar (Original: Text kommt vom Aufrufer, Zeile 5; „Na gut.“ ist bei 0x0C0B8 belegt). **Mit vier Zeilen falsch beschriftet, siehe MU-01.** Alter Rahmen bei > 4 Zeilen: vertretbar, 0x4C246 kennt höchstens vier Textzeilen; betrifft nur Spielstand-/Spielmodus-Meldungen. Schließen per Taste/Rechtsklick: vom Original nicht belegt (0x4C246 wertet keine Tasten aus), aber erhaltenes Verhalten der Meldung vor dem PR und als Komfort gekennzeichnet; vertretbar. Kurzmeldungen nehmen, wie im Original, keine Eingabe an.
2. **Angepasste Tests:** `Click` → `Frames(100)` verdeckt nichts: 100 > 82 Bilder (längste Dauer), und die Mutation „Meldung wieder mit Knopf/Klick“ wird in den dedizierten Tests rot (Tabelle oben). `MainMenuBefundeP3Tests` prüft jetzt ausdrücklich, dass ein Klick nicht schließt.
3. **Zustand durch Öffnen:** `ShortMessage` zählt nur Bilder; die Bildschirme ändern beim Öffnen und beim Ablauf nichts (R11U-05-Mutation und die Prüfsumme `StateFingerprint.Of` im Infofile-Test; Rest der 1973 Tests grün).
4. **Pixelvergleich:** siehe oben.
5. **Drei Läufe:** siehe oben.

### Erwartet offen – bestätigt

Vereinsinfo 0x4D385 (kein Vereinsfenster im Remake), Nationalaufstellung 0x54C9F (behält Meldungsfenster), Aufrufe 0x0C0B8 ohne Remake-Entsprechung (0x3DE9F, 0x40EF7, 0x414C6, 0x46DEF, 0x5C559, 0x5C59A), Zeile 1 „Spieler ist“ bei „noch gesperrt !“ (DGROUP 0x1255), `re-meldungen.md` K1/K3 (Symbollage/Einzug sind umgesetzt, Rest als Komfort/`[APPROX]` benannt).

### Akzeptierte `[APPROX]`

Knopf „Na gut.“ bei drei Zeilen; alter Rahmen bei > 4 Zeilen; Taste/Rechtsklick schließt Meldungsfenster; „ist verletzt.“ als Fenster; nicht über 0x0C0B8 laufende Transfermeldungen als Fenster; Takt 18,2 Hz mit 60 Bildern je Sekunde. **Nicht akzeptiert:** die Beschriftung des linken Knopfs bei vier Zeilen (MU-01).
