# Transfermarkt nach em.exe (Spur `transfer-original`)

Overlay 0x118 (Datei 0x46000–0x4ADE1). Beträge im Original in **Drittel-DM** (Eingabe · 3 über DGROUP 0x79D/0x7A1,
Datei 0x4727B; Anzeige / 3 über 0x99F:0x2D8). Der Transfermarkt ist Kaderplatz-Satz 4 (DGROUP 0x7623 = 0x5DC3 + 4 · 0x618);
eigene Spieler auf dem Markt stehen dort mit Besitzer = Manager (Pool +0x1A). Umsetzung: `Core/Transfer.Original.cs`
(`TransferRules`), angeschlossen in `Core/Transfer.cs`; Tests `TransferOriginalTests`.

## Belegt und umgesetzt (Teil 1)

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Listenpreis „(K)“ | Start 0x29EB4, Anzeige 0x4628B | +0x2C = Spielerwert 0x118:0x70 Art 0; Anzeige +0x2C (ohne Bit 31) / 3 DM |
| Leihgebühr „(L)“ | 0x462EB | (+0x2C / 3) / 3 DM, also Kaufpreis in DM / 3 |
| Annahme eines Gebots (Computermarkt) | 0x4991D–0x49A3A, gerufen 0x472C8 | P = +0x2C (bei Leihe / 3); Gebot &lt; P · random(75, 85) / 100 → abgelehnt; &gt; P · random(110, 130) / 100 → angenommen; sonst angenommen, wenn random(70, 120) &lt; Gebot · 100 / P |
| Reihenfolge | 0x472C8 vor 0x474F0 | Annahme vor der Geldprüfung |
| Gebot an anderen Manager | 0x47546–0x475DB | Wert frisch (bei Leihe / 3); unter 60 % oder über 140 % „Dieser Betrag ist nicht erlaubt (unfair) !“; sonst Rückfrage an den Verkäufer „Nehmen Sie das Angebot (… DM) für … an ?“ (DGROUP 0xD98) |
| Angebot für eigenen Spieler auf dem Markt (Tageswurf) | 0x072DD–0x073C2 | w = Wert / 10000; random(0, 180) &lt; w und random(0, 2) ≠ 0 und keine offene Meldung (Pool +0x1E) → +0x09 Bit 0x80, Verein in +0x16 (0x238:0x1838) |
| Höhe dieses Angebots | 0x47347–0x473DF | V = Wert (mit Angebot: · random(95, 100) / 100 bzw. · 1,6); V + random(V/20, V/10) − V/15; · random(85, 115) / 30000 · 300 |
| random(a, b) | 0xC037 (0x99F:0x847) | 16-Bit-Grenzen, bei b &lt; a getauscht, rand() mod (hi + 1 − lo) + lo |
| Schalter DGROUP 0x4860 | 0x6FD7, 0x72FD, 0x79FE | nur gelesen, nie beschrieben, in em.exe 0 → Zweige „sofort Angebot“ entfallen |

Gegenprobe: alle 20 Markt-Plätze der Original-Spielstände FILE03/05/07 (Preis +0x2C und Gehalt +0x28 = Spielerwert) und
die sechs Preise der Transferliste im Video `oberflaeche_1102` (exakt mit einer Tendenz im Startbereich 35–65).

## Belegt und umgesetzt (Teil 2: Vertragsverhandlung, Sperren)

Umsetzung: `Core/Transfer.Contract.cs` (`ContractRules`), `Core/Player.Contract.cs` (`Contracts.Negotiate`, `Availability`,
`DemandAfterYears`), Gebotssperre in `Core/Transfer.cs`; Ansicht `UI/Screens/Team/ContractsScreen.cs`; Tests
`TransferOriginalVertragTests`, `TransferOriginalVertragScreenTests`.

Stubeinträge Ov 0x118 (Stub-Segment 0x14C7, Datei 0x16A70): 0x2A → 0x465F1 Transferliste, 0x43 → 0x48E28, 0x4D → 0x4907A,
**0x5C → 0x494EF** (Wechsel der Liste), 0x70 → 0x49D36 Spielerwert, **0x75 → 0x4A1DB** Vertragsverhandlung.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Aufrufer der Verhandlung | 0x4786A (Kauf), 0x27D91 (Saisonende 0x26F20), 0x40208 (Bildschirm „Verträge“ 0x3FFE0) | `f(Kaderplatz far*, y)`; Ergebnis 1 = Vertrag steht |
| Prüfung beim Anklicken von Laufzeit/Gehalt | 0x4A692–0x4A770 | +0x0C ≠ 0 „… ist leider von &lt;Verein&gt; nur ausgeliehen.“; +0x18 Bit 0x80 „… wird sich mit Ablauf des Vertrages zur Ruhe setzen.“; +0x18 ≠ 0 und Tageszähler 0x3B7 &lt; 233 „… ist im Moment nicht verhandlungsbereit.“ |
| Angebot des Spielers | 0x4A3AF, 0x4AD7D | +0x18 &gt; 99: beim Öffnen +0x0B = +0x18 − 100, +0x18 = 0; die Meldung (Pool +0x1E) wird beim Schließen gelöscht |
| Forderung nach Eingabe der Laufzeit | 0x4A81B–0x4A9F2 | S = Wert(Gehalt) · [0xE1A4] / 100; Rest = 100 − 100 · nächster Spieltag / 44; U = (Jahre − 1) · 100 + Rest; S = S · ((U / 6 + 122) · 8 / 10 + 4) / 100; nach dem Saisonende · (152 − Alter) / 100 (Alter aus Kaderplatz 3, DI = 3 nach der Zeichenschleife); mindestens bisheriges Gehalt; im Feld nur bei 0xED8 ≠ 0 (Level &lt; 5) |
| Faktor [0xE1A4] | 0x44E3F | random(85, 115) zu Beginn jedes Managerzugs |
| „Mach' ich!“ | 0x4AB66–0x4AD7A | &gt; 4 Jahre „SOOOO lange verpflichtet sich kein Eishockey-Spieler !“; Forderung im Feld (Merker, nach Laufzeit ohne spätere Gehaltseingabe) → angenommen; keine Eingabe bzw. gleiche Laufzeit und mindestens gleiches Gehalt → angenommen; kürzer „So dumm ist … leider nicht...“; sonst 0x49A3B, abgelehnt „… ist nicht an Ihrem Angebot interessiert.“ |
| Annahme | 0x49A3B–0x49D35 | gleiche Laufzeit: mehr Gehalt ja, weniger nein; kürzer: &gt; 120 % ja, ≤ 105 % nein; D = Wert(Gehalt) · random(85, 115) / 100 · random(W − 6, W + 4) / 100 mit W = (U / 6 + 110) · 8 / 10, nach dem Saisonende · random(132 − Alter, 152 − Alter) / 100; D ≥ 100.000 nein; sonst ja, wenn Gehalt &gt; D |
| Sperre nach gescheiterter Verhandlung | 0x4AD3E, Tagesablauf 0x76E5–0x76F6 | +0x18 = random(10, 18), täglich − 1 (Abbruch sperrt nicht) |
| Angebot des Spielers verfällt | Tagesablauf 0x767A–0x76E3 | +0x18 &gt; 99: random(0, 5) = 0 und Meldung offen → +0x18 = random(9, 17), Meldung gelöscht |
| Versicherung | 0x4AA74–0x4AAC3, 0x4AB73 | Umschalten nur ohne +0x2C Bit 31 und ohne Verletzung (+0x09 &amp; 3 = 2); Prämie +0x30 = Wert(Gehalt) / 2; bei Abschluss im Dialog +0x2C Bit 31 (danach nicht mehr kündbar) |
| Saisonende | 0x27D05–0x27DF3 | +0x0B = 0: +0x18 = 101, +0x0B = 1, Gehalt = Wert(Gehalt) · (147 − Alter) / 100, dann Verhandlung; scheitert sie: „… kehrt Ihrem Verein den Rücken...“ |
| Gebotssperre | 0x472F4, 0x47140, 0x47B01, Tagesablauf 0x071CC | abgelehntes Gebot vom Computermarkt: **Kaderplatz** +0x1A \|= 0x80 \| 1 &lt;&lt; Manager (nicht Pool +0x1A, das ist der Besitzer); Anklicken mit beiden Bits „Angebot wurde bereits abgelehnt.“; täglich (nur im Zug von Manager 0) hebt random(0, 2) = 0 die Sperre auf, wenn allein Manager 0 gesperrt ist |
| Reihenfolge Gebot an einen Manager | 0x474F0 vor 0x47546 | erst Geld, dann Unfair-Grenze (R8-C02) |
| Wechsel der Liste | 0x494EF (Stub 0x5C) | gerufen beim Saisonstart (0x43A9F) und täglich mit random(0, Managerzahl + 2) = 0 (0x54E71): Markt-Plätze leeren, aus dem Pool (Besitzer 6) Spieler 1–179 bzw. mit random(0, 5) = 0 180–199 ziehen, Form random(45, 55), Preis/Gehalt = Wert, +0x19 = random(3, 12) · 16 + random(3, 12) |

Abbildung im Remake: Tageszähler &gt; 232 = Phase `SeasonOver`; [0xE1A4] je Saison und Tag aus eigenem Startwert
(`GameState.ContractTurnFactor`, `[APPROX]`); Zufall der Verhandlung aus `ContractRandom`; Kaderplatz 3 = vierter Spieler
in `Club.Players` (`[APPROX]`).

## Noch offen (`[APPROX]` im Code, weitere Teile)

- **Unaufgeforderte Angebote** für Spieler im eigenen Kader (Kaderplatz +0x09 Bit 0x40, Tagesablauf 0x079DE–0x07A8F: w wie oben,
  random(0, 200) &lt; w und random(0, 450) = 0; Höhe 0x46E0E: Wert · random(87, 125) bzw. random(112, 155) / 30000 · 300) –
  im Remake gibt es Angebote nur für Spieler auf dem Markt.
- **Antwort des verkaufenden Managers** (Hot-Seat-Rückfrage 0x47608–0x47743) fehlt; das Remake nimmt ab dem geforderten
  Betrag an, darunter bis 85 % mit sinkender Wahrscheinlichkeit.
- **Bietender Verein** 0x238:0x1838 und Wechsel der Liste je Spieltag (Abgänge, Zugänge, Pflege 0x7C12) nicht belegt.
- **Zeitpunkt der Züge:** das Original würfelt die Angebotshöhe beim Anklicken, das Remake beim Angebot.
- **Listenpreis eigener Spieler:** das Original kopiert den Kaderplatz (0x48:0x2BD4) samt altem +0x2C (Start- bzw. Kaufpreis);
  das Remake rechnet den Wert beim Anbieten frisch.
- **Spieler, der am Saisonende aufhört** (Kaderplatz +0x18 Bit 0x80, Meldung DGROUP 0x12BD): bekommt im Original keine
  Angebote und kann nicht gekauft werden – nicht abgebildet.
- **Kauf übernimmt Gebot als +0x2C** und ruft die Vertragsverhandlung 0x4A1DB (Datei 0x4781B–0x4786A); scheitert sie, findet
  der Transfer nicht statt (0x47874 → 0x47A3E). Im Remake kommt der Spieler noch mit Startlaufzeit und Gehalt aus
  `Contracts.Demand` (`[APPROX]`).
- **Verhandlung am Saisonende** (0x27D05–0x27DF3) für ausgelaufene Verträge fehlt; das Remake beendet sie ohne Rückfrage.
- **Start- und Wechselgehalt** = Wert(Gehalt) (0x29EE4, 0x497A1) noch nicht umgestellt; das Remake nutzt die Forderung aus
  Stärke und Talent. Die Verhandlung rechnet schon mit dem Wert, bisheriges Gehalt bleibt Untergrenze.
- **Versicherungsprämie** = Wert(Gehalt) / 2 beim Abschluss (+0x30) und Sperre des Kündigens (+0x2C Bit 31) – das Remake
  rechnet 10 % des Gehalts; Abrechnung 0x07089 offen.
- **Angebot des Spielers verfällt** (random(0, 5), dann Sperre random(9, 17)) und Angebot der Ankündigung „von 1 auf n Jahre“
  stammen im Remake aus der Spur „kader“ (`[APPROX]`, R2-11); umgesetzt ist nur, dass „Mach' ich!“ ohne Eingabe das Angebot
  annimmt.
- **Wechsel der Liste 0x494EF** (Pool von 200 Spielern) – das Remake zieht aus den vollen Kadern der Computervereine.
- **Pixelvergleich** Transferliste und Verhandlungsfenster mit Kennzahl.
