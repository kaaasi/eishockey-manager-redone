namespace EishockeyManager.Core;

/// <summary>Ergebnis des Auftrags an den „Spion“ (beide Einstiege: Jugendteams und Knopf „Talent“ im Transfer).</summary>
public enum TalentSearchStart
{
    /// <summary>Bezahlt, der „Spion“ ist unterwegs; das Ergebnis kommt nach einigen Wochen als Nachricht.</summary>
    Started,

    /// <summary>Es läuft schon eine Suche.</summary>
    AlreadyRunning,

    /// <summary>„Sie haben nicht genug Geld !“</summary>
    NotEnoughMoney,

    /// <summary>Kader voll.</summary>
    SquadFull,
}

/// <summary>
/// Die eine Talentsuche des Spiels: „Möchten Sie, daß Ihr 'Spion' für Sie auf Talentsuche geht?“ mit „Preis:“
/// und „Rabatt:“ (Texte aus em.exe). Erreichbar über das Fenster „Jugendteams“ und den Knopf „Talent“ im Transfer;
/// beide rufen <see cref="GameState.StartTalentSearch"/>. Ablauf in <c>FinanceYouth.cs</c>, Spieler aus der
/// <see cref="PlayerFactory"/>.
/// </summary>
/// <remarks>
/// Bis Runde 2 gab es zwei Suchen mit abweichenden Regeln (Transfer: 90.000 DM, sofortiges Ergebnis, 60 %,
/// Spieler aus einem Reserveverein, Grenze 22; Jugend: 30.000 DM mit Rabatt, 4 Wochen, 40–85 %, neuer Spieler,
/// Grenze 30). Vereinheitlicht wie unten; die mit [OFFEN] markierten Punkte sind Produktentscheidungen,
/// die das Original nicht belegt.
/// </remarks>
public static class TalentScout
{
    /// <summary>
    /// Grundpreis vor Rabatt. [ORIGINAL] Video („LetsShow“, ca. 11:15, Knopf „Talent“): „Preis: 90.000 DM“.
    /// [OFFEN] Ob dort bereits ein Rabatt abgezogen war, ist nicht zu sehen.
    /// </summary>
    public const long BasePrice = 90_000;

    /// <summary>
    /// „Rabatt:“ in Prozent: je 1.000 DM Jugendförderung im Monat 2 %, höchstens 50 %. [APPROX]
    /// </summary>
    public const int DiscountPerFundingStep = 2, MaxDiscount = 50;

    /// <summary>Dauer der Suche in Wochen. [OFFEN] Nicht belegt; früher Transfer sofort, Jugend 4 Wochen.</summary>
    public const int Weeks = 4;

    /// <summary>
    /// Höchstzahl Spieler im Kader. [OFFEN] „Schon 22 Mann im Team !“ (em.exe) gilt für Transfers; Manager-Kader
    /// der Level 0/1 haben laut Originaltabelle aber schon 25 Spieler, daher hier die großzügige Grenze der Jugend.
    /// </summary>
    public const int MaxSquad = 30;

    /// <summary>
    /// Erfolgschance: 40 %, mit der besten Jugendmannschaft bis zu 85 % (1. Platz 1. Liga). [APPROX]
    /// [OFFEN] früher Transfer pauschal 60 %.
    /// </summary>
    public static double SuccessChance(ManagerFinance finance)
    {
        var best = finance.Youth.Min(t => t.Position);
        return 0.4 + 0.45 * (1 - best / (double)(YouthTeam.Leagues * YouthTeam.TeamsPerLeague - 1));
    }

    /// <summary>Meldung zum Start (für beide Bildschirme).</summary>
    public static string[] Message(TalentSearchStart result) => result switch
    {
        TalentSearchStart.Started => ["Ihr 'Spion' geht", "auf Talentsuche."], // [APPROX] Text nicht in em.exe gefunden
        TalentSearchStart.AlreadyRunning => ["Ihr 'Spion' ist schon", "auf Talentsuche."], // [APPROX]
        TalentSearchStart.NotEnoughMoney => TransferMarket.Message(TransferResult.NotEnoughMoney),
        _ => ["Ihr Kader ist", "schon komplett."], // [APPROX]
    };

    /// <summary>Knopf „Talent“ im Transfer: dieselbe Suche wie der „Spion“ im Fenster „Jugendteams“.</summary>
    public static TalentSearchStart Search(GameState game, Manager manager) => game.StartTalentSearch(manager);
}
