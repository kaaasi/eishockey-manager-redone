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

### 5. Export und Import
- **Anforderung:** Vereins- und Spielerdaten exportieren und importieren.

## B. Zusätze

- Geschwindigkeitstasten 1x / 4x / 16x im Live-Spiel; Torszene mit Leertaste oder Klick überspringen.
- Wöchentlicher Autosave (`Autosave.ehm`), abschaltbar.
- Fehlerschutz: interne Fehler werden abgefangen und in `crash.log` protokolliert, statt das Spiel zu beenden.
- Finanzbuch (Klick auf Bank/Konto): Einnahmen und Ausgaben je Kategorie, laufende und vorige Saison.
- Lautstärke für Effekte und Musik getrennt einstellbar, `F8` = Ton aus.