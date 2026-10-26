namespace EishockeyManager.Core;

/// <summary>Schulden und Entlassung eines Managers (Saisonablauf).</summary>
public sealed partial class Manager
{
    /// <summary>Grenze für die Entlassung: unter −1,5 Mio. DM.</summary>
    public const long DebtLimit = -1_500_000;

    /// <summary>Acht Wochen am Stück unter der Grenze führen zur Entlassung.</summary>
    public const int DebtDaysUntilDismissal = 8 * 7;

    /// <summary>Tage in Folge unter <see cref="DebtLimit"/>.</summary>
    public int DebtDays { get; set; }

    /// <summary>Entlassen – spielt nicht mehr mit.</summary>
    public bool Dismissed { get; set; }
}
