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

## Noch offen (`[APPROX]` im Code, weitere Teile)

- **Sperre nach Ablehnung:** abgelehntes Gebot setzt Pool +0x1A |= 0x80 | 1 &lt;&lt; Manager (Datei 0x472F4) – der Manager kann
  für diesen Spieler nicht erneut bieten; Tagesablauf 0x071CC nimmt das Manager-Bit mit random(0, 2) = 0 zurück, wenn nur dieser
  Manager gesperrt ist. Braucht neuen Zustand (Schema).
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
- **Kauf übernimmt Gebot als +0x2C** und ruft die Vertragsverhandlung 0x4A1DB (Datei 0x4781B–0x4786A); Gehalt, Laufzeit und
  Versicherung (0x4A1DB, 0x49A3B, 0x494EF) folgen in Teil 2.
