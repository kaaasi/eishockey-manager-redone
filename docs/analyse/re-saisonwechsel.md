# Analyse `re-saisonwechsel` – Manager ohne Liga (#166)

Runde 10 (nur Doku), Spur `re-saisonwechsel`. Disassembly `ndisasm -b 16` von `em.exe` (Overlay 0x130 ab Datei 0x51E90),
Adressen als Dateioffset von em.exe, DGROUP-Variablen ausdrücklich als „DGROUP“. Grundlage und Vorarbeit:
`reverse-engineering.md` (Spur `wettbewerbe-komplett`, Teil 1 und Teil 4), `saison.md` (Statusbyte),
`finanzen-rekonstruktion.md` (Oberliga-Austausch).

## Ergebnis in Kürze

**Ein Manager ohne Liga kommt im Original nicht vor.** Scheidet ein Managerverein als Letzter oder Vorletzter aus der
Oberliga aus, tauscht der Saisonwechsel ihn zweimal mit dem nachrückenden Verein (Datei 0x562F2 und Datei 0x5636D) –
der zweite Tausch hebt den ersten auf. Der Verein bleibt in der Oberliga, der gezogene Verein ohne Liga bleibt draußen.
Stattdessen bekommt der Manager **beide Pfeile** (Statusbit 0x02 Abstieg und 0x01 Aufstieg) und sein **Konto wird auf
400.000 DM gesetzt**. Teil 4 in `reverse-engineering.md` („der Manager bleibt bei seinem Verein, der jetzt einen Index 36–41
ohne Liga hat“) hat den zweiten Tausch übersehen und ist damit **widerlegt**; die Fragen „Saison ohne Liga“ (Spieltage,
Hauptmenü, Tabellen) entfallen. Das Remake (`SeasonFlow.FinishClubSeason`) nimmt Managervereine ebenfalls aus, weicht
aber in drei Punkten ab (nächstschlechterer Verein steigt ab, kein Konto, keine Ereignisse); dazu kommt der kleinere
Nachrückerkreis 36–41 statt 36–47.

Das Ergebnis stammt nur aus dem Code; ein DOSBox-Beleg fehlt noch (Anleitung unten).

## Zuordnung der Adressen der Spur

| Adresse | Stelle | Ergebnis | Abschnitt |
|---|---|---|---|
| Datei 0x54C9F | Tagesfunktion (Ov 0x130, Stub-Eintrag 0x98, Aufrufer Datei 0x437D5 über `0x130:0x98`) | Saisonwechsel am 29. April zugeordnet: Reihenfolge, Daten, Regeln; übrige Tagespfade nur so weit, wie der Saisonwechsel sie braucht (sie sind anderswo belegt) | 1–4 |
| Datei 0x562C6 | in Datei 0x54C9F, Ausnahme Gruppe 2 (Oberliga) | Konto 400.000 DM und erster Tausch; mit dem zweiten Tausch bleibt der Managerverein in der Oberliga | 3 |

Weitere gelesene Stellen (nicht Adressen der Spur, nur Beleg): Vereinstausch Datei 0x36169, Liga eines Vereins Datei
0x34FD5, Ewige Tabelle Datei 0x5667A, Platzverlauf Datei 0x43BED, DEB-Pokal-Auslosung Datei 0x341AB.

## 1. Ablauf des Saisonwechsels in Datei 0x54C9F

Einstieg: Nach den WM-Tagen (Kalendermarke 0x10) springt die Tagesschleife bei der nächsten Marke ≠ 0x10, ≠ 0 (29. April,
Marke 255) nach Datei 0x55743 (Bedingung Datei 0x55724–0x55740). Danach in dieser Reihenfolge:

1. Datei 0x55743–0x5575D: Deutschland verliert den Bundestrainer (Vereinsfeld +0x16 = 0), Sicherung DGROUP 0x22C zurück.
2. Datei 0x5576B: je Manager `0xC8:0x20(1)` (Ranglisten; Ergebniswort je Manager im Rahmen, später an Datei 0x26F20
   übergeben); Datei 0x55793–0x55800: WM-Kader des Bundestrainers aufgelöst, die Spieler bekommen ihren Verein zurück
   (Spielerfeld +0x1D).
3. Datei 0x55861–0x55DC1, je Manager: Ewige Bilanz aus den Tabellenfeldern des Vereins, Eintrag „Früher“ (Platz über
   Datei 0x43BED, Play-Off-Ergebnis, Meister), Titelbits 0x08/0x10/0x20 (Datei 0x55C09, 0x55D12, 0x55DAD; `saison.md`).
4. Datei 0x55DD1–0x55EF8: Tabellenfelder aller 64 Vereine auf 0 (`reverse-engineering.md`, Teil 1).
5. **Zwangsabstieg** Datei 0x55F17–0x560DB (Abschnitt 2).
6. Datei 0x560EB–0x5612E: Auf-/Absteigerlisten (je Liga zwei Paare) auf 100 = leer.
7. **Austausch** Datei 0x56137–0x563A4, drei Tauschgruppen (Abschnitt 3).
8. Datei 0x563AF: Ewige Tabelle Datei 0x5667A (Abschnitt 4, Nebenbefund).
9. Datei 0x563B2–0x56418: Liga-Stufe je Manager neu (Abschnitt 4).
10. Datei 0x5641A–0x56444: letzte Saison → Highscore `0x140:0x7A(0)`, Funktion 51F3Dh(0) („Carpe diem !“, Spur `re-livespiel`), Programmende über
    Stub C0h:61h mit 63h (Spur `re-meldungen`; Suffix-Schreibweise, damit die Karte fremde Spuradressen nicht zählt).
11. Datei 0x56448: Wanderung der Teamwerte (Modus 10), danach `0xF0:0x43`, `0xE8:0x8E`, Datei 0x5645D Saisonende-Fenster
    `0xD0:0x20` = Datei 0x26F20 (Prämien, Ereignisse aus dem Statusbyte), `0x140:0x48(0)`/`(1)`, Datei 0x56476 Saisonzähler.

Aufgerufene im Saisonwechsel: Datei 0x36169 (`0xF0:0x4D`), `0x38:0x847` (random(a, b) mit beiden Grenzen), Datei 0x5667A,
Datei 0x09D1B (`0x28:0x1C5B`), Datei 0x26F20, Highscore `0x140:0x7A`.

## 2. Zwangsabstieg wegen Schulden (Datei 0x55F17–0x560DB, Statusbit 0x04)

**Daten:** Konto DGROUP 0x97EA + 0x1AF · Manager (32 Bit, Drittel-DM), Liga-Stufe DGROUP 0x9746 + 0x1AF · Manager,
Statusbyte DGROUP 0x974E + 0x1AF · Manager, Zähler je Liga im Rahmen [bp−0x18 + Liga].

**Regel** je Manager m (Schleife über DGROUP 0xA70E bis Managerzahl):

```
if konto(m) < -2_000_000 Drittel            // Vergleich Datei 0x55F22–0x55F34 (Hochwort, dann Tiefwort; Grenze FFE1 7B80 hex)
   and (status(m) & 0x28) == 0              // weder Deutscher Meister noch Europacupsieger
   and zaehler[liga(m)] < 2 and liga(m) < 2 // höchstens zwei je Liga, nur Bundesliga und 2. Liga
then
   status(m) |= 0x04                        // Datei 0x55F87
   Verein in die Gruppentabelle der Aufstiegsrunde auf Platz 3 bzw. 4 schieben (Datei 0x55F93–0x560C5)
   zaehler[liga(m)]++
```

−2.000.000 Drittel = **−666.666,67 DM**; die Grenze ist echt kleiner (gleich zählt nicht). Damit ist der Eintrag „Konto
≤ −2.000.000 Drittel“ in `reverse-engineering.md`, Teil 4, um eins zu streng. Das Einschieben verdrängt die Plätze dahinter
nach oben; der Verein wird im Austausch (Abschnitt 3) als Absteiger gefunden. Symbol im Fenster „Ereignisse“: Bit 2 →
Symbol 0 (Tabelle DGROUP 0x4B2E, `saison.md`).

**Remake:** nicht vorhanden (`SeasonFlow` kennt nur die Entlassung nach 8 Wochen unter −1,5 Mio. DM). Gilt nicht für die
Oberliga; einen Abstieg aus der Oberliga erzwingt es nicht.

## 3. Austausch der drei Tauschgruppen (Datei 0x56137–0x563A4)

**Daten:** Platzliste DGROUP 0x5CCB (Liga · 12 + Platz → Vereinsindex), Gruppentabellen der Aufstiegsrunden DGROUP 0xCF20
(0x24 je Gruppe) mit Platzfolge DGROUP 0xCD94 (6 je Gruppe), Ligagrenzen DGROUP 0x7C1/0x7C2 (12, 24), Ligagrößen DGROUP
0x7B1–0x7B3 (je 12), Zahl der Tausche je Gruppe DGROUP 0x7B9 + Gruppe (2), Vereinssätze far DGROUP 0x9657 (0x3D Byte,
Feld +0x16 = Manager + 1), Statusbyte über DGROUP 0x959F + 0x1AF · Feld (= 0x974E + 0x1AF · Manager), Konto über DGROUP
0x963B + 0x1AF · Feld (= 0x97EA + 0x1AF · Manager), Auf-/Absteigerlisten DGROUP 0xD0E7 (ein) und 0xD0ED (aus), je 2 · Gruppe + k.

**Gruppen 0 und 1** (Datei 0x56157–0x56235, belegt in Teil 1): je Paar der nächste Verein der unteren Liga unter den Plätzen
1–2 der Gruppentabelle (ein) und der nächste Verein der oberen Liga unter den Plätzen 3–4 (aus); fehlt einer, entfällt das Paar.

**Gruppe 2** (Datei 0x56238–0x56284): aus = Platzliste DGROUP 0x5CCB + 24 + 11 − k (k = 0 Letzter, k = 1 Vorletzter der
Oberliga), ein = random(36, 41) (`0x38:0x847(a, a + 5)`, a = DGROUP 0x7C2 + 0x7B3); neu gewürfelt, solange der Zug dem
vorigen gleicht (Datei 0x56266–0x56282, Vorwert [bp−0x1D] je Gruppe auf 0).

**Verarbeitung je Paar** (Datei 0x56287–0x56372), alle Gruppen:

```
m = verein[aus].manager                       // Feld +0x16, 0 = kein Manager
if m != 0 and (status(m) & 0x04) == 0:
    status(m) |= 0x02                         // Datei 0x562C1, Pfeil runter
    if gruppe >= 2:                           // Datei 0x562C6
        konto(m) = 1_200_000 Drittel          // Datei 0x562D8/0x562DE: 0x00124F80 = 400.000 DM, gesetzt, nicht gebucht
        tausche(aus, ein, 1)                  // Datei 0x562F2, erster Tausch
m = verein[ein].manager                       // Datei 0x562FA: nach dem ersten Tausch ist das der Managerverein
if m != 0 and (status(m) & 0x04) == 0:
    status(m) |= 0x01                         // Datei 0x56334, Pfeil hoch
liste_aus[gruppe][k] = aus; liste_ein[gruppe][k] = ein   // Datei 0x56348, 0x5635B
tausche(aus, ein, 1)                          // Datei 0x5636D, zweiter (regulärer) Tausch
```

`tausche` = Datei 0x36169 (`0xF0:0x4D`, Schalter 1): vertauscht Vereinsnummer der Spieler (+0x1D, Plätze 1–199), Verein der
Manager (DGROUP 0x966F + 0x1AF · Manager), Titelträger DGROUP 0x387, 0xD0E6, 0x388 und die ganzen Sätze (Verein 0x3D Byte,
Tabelle 0x50 Byte, Serien und Kopf-an-Kopf). Alle Schritte sind reine Vertauschungen (Datei 0x363F9–0x3672C: Puffer
anlegen, drei Kopien, freigeben); zweimal angewandt ergibt sich der Ausgangszustand.

**Folgerung für einen Managerverein unter den letzten zwei der Oberliga:**

- Er **bleibt in der Oberliga** auf seinem Index (24 + 11 − k); der gezogene Verein bleibt auf seinem Index 36–41 ohne Liga.
  Es wechselt in diesem Paar niemand.
- Bit 0x02 und Bit 0x01 werden gesetzt: das Fenster „Ereignisse“ (Datei 0x26F20, nach dem Austausch) zeigt **Pfeil runter und
  Pfeil hoch**.
- **Konto = 400.000 DM**, unabhängig vom Stand vorher (Schulden fallen weg, Guthaben über 400.000 DM gehen verloren). Keine
  Buchung im Finanzbuch.
- Die Auf-/Absteigerlisten enthalten das Paar trotzdem (aus = Index des Managervereins, ein = Index 36–41). Wie das Fenster
  „Saisonende“ das zeigt, ist offen (DOSBox).
- Das zweite Paar läuft normal: Es verlässt höchstens ein Verein die Oberliga. Sind beide Letzten Managervereine, keiner.
- Mit gesetztem Bit 0x04 (Zwangsabstieg) wirkt nichts davon; der kann in der Oberliga aber nicht entstehen (Abschnitt 2).

**Warum es nie einen Manager ohne Liga gibt:** Ein Managerverein verlässt eine Liga nur über die Paare. Gruppen 0/1 tauschen
zwischen zwei Ligen, Gruppe 2 hebt den Tausch für Managervereine auf. Beim Spielstart kommt der Managerverein in die
Start-Liga (Tausch mit Schalter 0x80 samt Feld +0x16, Teil 1). Die Liga-Stufe (Datei 0x563C0–0x56409: (Verein ≥ 12) +
(Verein ≥ 24)) und Datei 0x34FD5 (Liga eines Vereins: 1, 2, 4; Vereine 24–51 alle 4) würden einen Verein 36–41 wie die
Oberliga behandeln, ebenso die DEB-Pokal-Auslosung (Datei 0x341AB setzt die Vereine aller Manager ohne Ligaprüfung); dieser
Fall ist aber nicht erreichbar. Der Zweig Datei 0x562FA ff. („nachrückender Verein gehört einem Manager“) wirkt nur in der
Ausnahme oben.

**Abgleich Remake** (`SeasonFlow.FinishClubSeason`, `[APPROX]` „Vereine von Managern ausgenommen“, Datei
`src/EishockeyManager.Core/SeasonFlow.cs`):

| Punkt | Original | Remake |
|---|---|---|
| Managerverein unter den letzten zwei der Oberliga | bleibt, Platz wird nicht nachbesetzt | bleibt, der nächstschlechtere Nicht-Managerverein steigt dafür ab (`Where(…).Take(2)`) |
| Konto | auf 400.000 DM gesetzt (29. April) | unverändert |
| Ereignisse | Pfeil runter und Pfeil hoch | keine |
| Nachrücker | random(36, 41): sechs Plätze 36–41, Folgezug ≠ Vorzug | zufällig aus allen Vereinen 0–47 ohne Liga (bis zu zwölf) |
| Zeitpunkt | 29. April (Datei 0x54C9F, Saisonwechsel) | Auswahl schon am 13. April (`FinishClubSeason`), Wirkung beim Saisonwechsel |
| Zwangsabstieg | Bit 0x04, Bundesliga/2. Liga, Konto < −666.666,67 DM | fehlt |

Die Plätze 36–41 sind Plätze, keine festen Datenbankvereine: Der Absteiger übernimmt den Platz des Nachrückers. Datenbankvereine
42–47 kommen im Original nie in die Oberliga, es sei denn, der Spielstart-Tausch (Teil 1) hat einen Ligaverein auf ihren Platz
gelegt – dann bleibt dieser Verein für immer draußen (zu prüfen am Spielstart-Tausch, nicht Teil dieser Spur).

## 4. Nebenbefunde im Saisonwechsel

- **Ewige Tabelle nach dem Tausch** (Datei 0x563AF ruft Datei 0x5667A erst nach dem Austausch): Die Ligapunkte (40 bzw. 20)
  hängen am **neuen** Vereinsindex, der Platz (Platzfeld des Tabellensatzes, Schreiber Datei 0x51115, mit dem Satz getauscht)
  am Verein. Ein Aufsteiger in die
  Bundesliga bekäme damit 40 + 12 − Platz statt 20 + 12 − Platz + 3, ein Absteiger umgekehrt; ein Oberliga-Absteiger (jetzt 36–41)
  0, ein Nachrücker 12 − (alter Platz) und +3, wenn das alte Platzfeld 0 ist. Das Remake (`StatisticsHistory.EternalPoints`) rechnet
  mit der Liga der gespielten Saison. Nicht am Video belegt (die Belege in `wettbewerbe.md` betreffen Vereine ohne Wechsel).
- **Letzte Saison** (Datei 0x5641A): Das Programm endet nach dem Austausch und dem Highscore, **vor** dem Saisonende-Fenster
  Datei 0x26F20 (Datei 0x5645D) – in der letzten Saison erscheinen Prämien und Ereignisse nicht. Remake: zeigt sie (zu prüfen
  am Video; Folge mit Spielende).

## Beleg im Original (DOSBox) – Anleitung

Keine Vorlage in `original/spielstaende/` hat einen Manager in der Oberliga (FILE01–11: Bundesliga bzw. „nur Weltkugel“);
`emtool hok patch` kann die Liga nicht ändern (Feld `liga` abgewiesen). Deshalb zwei Schritte.

**Schritt 1 (Nutzer, einmalig):** In DOSBox ein neues Spiel: ein Manager, Spielmodus Fahne + Weltkugel, Level 3, **Dauer ≥ 2**
(sonst endet das Spiel am 29. April vor dem Fenster, Abschnitt 4), **Start-Liga Oberliga**, Start-Stärke Abstiegskampf; sofort
sichern als `FILE12.HOK` und mit Bild des Hauptmenüs (Verein, Konto) ins Referenz-Repo `original/spielstaende/`.

**Schritt 2 (Leitstelle):** Sprung vor den 42. Spieltag (Kalender em.exe ab Datei 0x175F9: 42. Spieltag = Tag 143 = 31.01.1994,
Tageszähler 144):

```
emtool hok patch FILE12.HOK FILE13.HOK datum=31.01.1994 zaehler=144 spieltag=42
```

`datum`, `zaehler`, `spieltag` ändern nur diese Zähler: Die Tabelle hat danach 0 Spiele, DEB-Pokal-Termine davor sind übersprungen
(Pokalzustand ggf. ungewöhnlich, für diesen Beleg ohne Bedeutung). Gegenprobe ohne Sprung: Schritt 1 ganz durchspielen.

**Schritt 3 (Nutzer, DOSBox, Bilder ins Referenz-Repo, Namen `file13_…`):**

1. FILE13 laden. Hauptmenü fotografieren (31.01.1994, 42. Spieltag, Konto).
2. Damit der eigene Verein sicher Letzter wird: in der Aufstellung so viele Spieler herausnehmen, dass **weniger als neun**
   eingesetzt sind (Meldung „Weniger als neun Spieler!“, Spiel 0:2, −200.000 DM). Für die Spieltage 42–44 so lassen.
3. Nach dem 44. Spieltag: Tabelle Oberliga fotografieren (eigener Verein auf 11 oder 12). Sonst FILE13 neu laden und wiederholen.
4. „Tag weiter“ bis zum 29. April. Unterwegs ggf. die Aufstiegsrunde (Paarungen, Seite Aufstieg) fotografieren.
5. Am 29. April alle Fenster fotografieren, besonders **Saisonende** (Vereine je Liga, rot/grün) und **Ereignisse** (erwartet:
   Pfeil runter **und** Pfeil hoch).
6. Erstes Hauptmenü der neuen Saison (10. September): **Konto** (erwartet 400.000 DM zuzüglich etwaiger Prämie des Platzes),
   Infoleiste (Gegner, „1.Spieltag“), Tabelle Oberliga (erwartet: eigener Verein weiter dabei, nur ein neuer Verein), Statistik
   „Früher“. Spielstand sichern als `FILE14.HOK`.

Gegenbefund, der die Analyse widerlegen würde: eigener Verein fehlt in der Oberliga, zwei neue Vereine, oder Konto unverändert.

**Beleg Zwangsabstieg** (Abschnitt 2, mit vorhandener Vorlage, optional):

```
emtool hok patch FILE01.HOK FILE15.HOK konto=-1000000 datum=31.01.1994 zaehler=144 spieltag=42
```

Kölner EC (Bundesliga) bis zum 29. April spielen (die Entlassung droht erst nach 8 Wochen unter −1,5 Mio. DM; Konto im Blick
behalten), Ereignisse und Saisonende fotografieren. Erwartet: Symbol 0 in „Ereignisse“, Köln in der neuen Saison in der 2. Liga,
aus der Aufstiegsrunde steigt dafür ein Bundesligist weniger ab. Nicht bei Platz 1 (Meister) oder Europacupsieg.

## Kandidaten Runde 11

| Nr. | Prio | Ziel | Dateien | Testidee |
|---|---|---|---|---|
| K1 | P1 | Oberliga-Austausch wie im Original: die letzten zwei verlassen die Liga; ein Managerverein darunter bleibt, sein Platz wird nicht nachbesetzt (nicht der nächstschlechtere), Konto des Managers auf 400.000 DM (neutral gesetzt, nicht über `Book`; Finanzbuch-Eintrag mit Nutzer klären), Ereignisse Abstieg und Aufstieg | `Core/SeasonFlow.cs`, `Core/SeasonRecord.cs` (ggf. neues Feld), `GameState.Integration.cs` nur falls der Kontowert am 29. April angeschlossen werden muss | Synthetische Saison mit Managerverein auf Platz 12 der Oberliga: danach weiter Oberliga, genau ein Absteiger/Nachrücker, Konto 400.000 DM, Events beide; Gegenprobe Platz 11 und zwei Manager auf 11/12 (kein Wechsel); Mehrsaisonprobe 4 Saisons passiv/aktiv |
| K2 | P2 | Nachrücker aus den Plätzen 36–41 (sechs), zweiter Zug ≠ erster; Absteiger übernehmen den Platz des Nachrückers | `Core/SeasonFlow.cs`, ggf. `Core/NewGame.cs` (Startbesetzung der Plätze) | Über 20 Saisons nie ein Datenbankverein 42–47 in der Oberliga; Absteiger der Vorsaison wieder ziehbar |
| K3 | P2 | Zwangsabstieg wegen Schulden (Bit 0x04): Konto < −666.666,67 DM am 29. April, Bundesliga/2. Liga, nicht Meister/Europacupsieger, höchstens zwei je Liga, Einschub auf Platz 3/4 der Aufstiegsrunde, Symbol 0 | `Core/SeasonFlow.cs`, `Core/SeasonRecord.cs`, `SeasonEvent` (Schema!) | Manager mit −700.000 DM in der Bundesliga steigt ab, mit −600.000 DM nicht; Meister mit Schulden nicht; Mehrsaisonprobe |
| K4 | P3 | Ewige Tabelle mit dem Index nach dem Tausch prüfen (erst DOSBox-Beleg: Ewige Tabelle nach einem Aufstieg) | `Core/StatisticsHistory.cs` | Aufsteiger in die Bundesliga: Punkte der Saison nach Beleg |
| K5 | P3 | Letzte Saison ohne Saisonende-Fenster (nach Video prüfen) | `UI/Screens/SeasonEndSummaryScreen.cs`, Ablauf Spielende | Dauer 1: nach dem 29. April direkt Highscore/Ende |

Für alle Kandidaten gilt: K1 erst nach dem DOSBox-Beleg auf `[ORIGINAL]` stellen; bis dahin `[APPROX]` mit Verweis auf diese Datei.
