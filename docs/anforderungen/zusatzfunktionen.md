# Zusatzfunktionen zum Originalspiel

Hier stehen alle Funktionen, die über das DOS-Original „Eishockey Manager“ (Software 2000, 1993) hinausgehen.
Das Originalspiel selbst wird „Hybrid“ portiert: Regeln und Formeln möglichst wie im Original; wo sie noch nicht
entschlüsselt sind, ist das im Code mit [APPROX] markiert.

## A. Von dir angeforderte Zusatzfunktionen

### 1. Moderne Grafik
- **Anforderung:** zusätzlich zur Originaldarstellung eine moderne, hochauflösende Darstellung.
- **Entscheidung vom 19.10.2026:** kommt erst, wenn das Originalspiel komplett portiert ist. Bis dahin gibt es nur
  die Originaldarstellung (320×240, Originalfonts, pixelgenau skaliert).
- **Offen für später:** Gestaltung des modernen Looks (Schrift, Auflösung, eigene Grafiken), Umschaltung im Spiel.

### 2. Weitere Sprachen
- **Anforderung:** das Spiel in anderen Sprachen als Deutsch spielbar.
- **Einschränkung:** Die Originalfonts kennen nur ASCII und deutsche Umlaute. Andere Zeichen werden ersetzt
  (z. B. é → e). Sprachen mit anderen Schriftzeichen brauchen später eigene Fonts.

### 3. Cheatmenü
- **Anforderung:** Cheatmenü zusätzlich zum Original.
- („Mogelguru“, nur wenn beim Spielstart erlaubt): Geld, Schulden tilgen, Stadion maximal, Spielerwerte
  (Kondition, Technik, Form, Talent, Alter) und Namen ändern, Vereinsname, Heilen, Spieler kostenlos verpflichten.
- **Im Netzwerk:** Der Server entscheidet, ob Cheats erlaubt sind; alle Mitspieler werden über Cheats informiert.

### 4. Netzwerkmodus mit Dedicated Server
- **Anforderung:** Mehrspieler über das Netzwerk, auch mit eigenständigem Server.
- **Leihen bei einem Mitmanager** (Entscheidung Nutzer 01.11.2026, E2 in `docs/analyse/re-geld.md`): Im Netzwerkspiel
  (alle spielen den Spieltag gleichzeitig, er endet erst, wenn alle fertig sind) als Anfrage mit Zustimmung des Verleihers;
  im lokalen Spiel wie im Original sofort.

### 5. Export und Import
- **Anforderung:** Vereins- und Spielerdaten exportieren und importieren.

### 6. Unabhängigkeit von den Originaldateien zur Laufzeit
- **Anforderung:** das Remake läuft ohne Kopie der DOS-Version. Heute liest es Grafiken, Paletten, Fonts, Datenbank,
  Halle, Figuren, Torszenen, Musik und Klänge zur Laufzeit aus `gamedata/`.
- **Entscheidung vom 31.10.2026 (Nutzer):** Arbeit daran beginnt ab v1.0, nach Erreichen der Parität. Bis dahin
  bleiben die Originaldateien Voraussetzung für Laufzeit und Tests.
- **Rahmen:** Ersatzinhalte müssen eigene, rechtlich freie Werke sein (keine Kopien oder Ableitungen der
  Originalgrafiken, -texte oder -musik). Die Originaldarstellung bleibt wählbar, wenn `gamedata/` vorhanden ist.
  `em.exe`, Original-Spielstände und Referenzbilder bleiben Beleg- und Testgrundlage. Berührt sich mit Nr. 1 (moderne
  Grafik).

## B. Zusätze

- Geschwindigkeitstasten 1x / 4x / 16x im Live-Spiel; Torszene mit Leertaste oder Klick überspringen.
- Wöchentlicher Autosave (`Autosave.ehm`), abschaltbar.
- Fehlerschutz: interne Fehler werden abgefangen und in `crash.log` protokolliert, statt das Spiel zu beenden.
- Finanzbuch (Klick auf Bank/Konto): Einnahmen und Ausgaben je Kategorie, laufende und vorige Saison.
- Lautstärke der Effekte einstellbar, `F8` = Ton aus. Musik nur im Titelbildschirm (#221), ohne Einstellung.

## C. Ideen (ab v1.0)

Vom Nutzer vorgemerkt, Umsetzung erst nach Erreichen der Parität (v1.0); als abschaltbare Zusatzanzeige, die
Originaldarstellung bleibt unverändert.

- **Verjüngung des Spielerpools (01.11.2026):** Im Original altern die 200 Poolspieler jede Saison um ein Jahr (em.exe 0x274A7)
  und bekommen beim Saisonwechsel nur neue Namen (0x5ACA0); ein Laufbahnende gibt es nicht, die Transferliste wird über die
  Jahre älter. Das Remake bleibt bis v1.0 originaltreu ([docs/transfer.md](../transfer.md), Teil 5). Erweiterung: Ein Platz, der
  beim Saisonwechsel einen neuen Namen bekommt, erhält auch neue Startwerte nach 0x5AF50 (Alter 18–33, Grundwert 30–89,
  Talent 0–9) – ein neuer Name steht dann für einen neuen Spieler. Abschaltbar; Ort im Code: `TransferMarket.StartPoolSeason`
  (`Core/Transfer.Pool.cs`).
- **Annahmewahrscheinlichkeit beim Spielerkauf (31.10.2026):** Gibt man im Kauf- oder Leihdialog der Transferliste ein
  Gebot ein, zeigt das Spiel die Wahrscheinlichkeit, dass es angenommen wird. Grundlage ist die Annahmeregel des
  Originals (em.exe 0x4991D, `TransferRules.Accepts`, [docs/transfer.md](../transfer.md)): Für Spieler vom
  **Computermarkt** hängt sie nur von Gebot und Listenpreis ab (bei Leihe vom Listenpreis / 3) und lässt sich aus den drei
  Zufallszügen (75–85 %, 110–130 %, 70–120) exakt ausrechnen, ohne Zufall zu verbrauchen – die Anzeige verändert damit keinen
  Spielzustand. Einschränkungen (W51-02): Vertragslose Spieler werden ohne diese Regel angenommen (Anzeige 100 %); nach einer
  Ablehnung ist der Spieler für den Manager gesperrt (`TransferListing.RejectedBy`, „schon abgelehnt“), die Anzeige gilt also
  nur für einen Versuch; Gebote an andere Manager folgen einer eigenen Regel (Grenze „unfair“ 60–140 % des frisch berechneten
  Werts 0x47546, danach Antwort des Verkäufers). Offen: Darstellung (Prozentzahl oder Ampel) und ob die Anzeige im
  Netzwerkmodus erlaubt ist.
- **Spieltag mit Managerverein ganz in der Minutenrechnung (01.11.2026, #349):** Im Original entscheidet ein Merker je
  Spieltag (em.exe Datei 0x36FEA): Spielt an einem Spieltag von Pokal, Play-offs oder Aufstiegsrunde ein Managerverein,
  laufen alle Paarungen des Spieltags durch die Minutenschleife, sonst je nach Anzeigeschalter des Wettbewerbs
  (0x3702E–0x37085). Das Remake entscheidet je Paarung und bleibt dabei (Entscheidung Nutzer). Nach v1.0 als Einstellung
  „wie im Original“ anbieten; sie ändert Ergebnisse und Zufallsverbrauch der Spiele ohne Manager.
