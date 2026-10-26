namespace EishockeyManager.Core;

/// <summary>Art einer Nachricht im Posteingang („Neuer“).</summary>
public enum MessageKind
{
    /// <summary>Reine Meldung.</summary>
    Info,

    /// <summary>„Möchten Sie den Vertrag mit … verlängern ?“ – Ja/Nein.</summary>
    SponsorRenewal,

    /// <summary>Kreditangebot eines anderen Managers – Annehmen/Ablehnen, verfällt nach einer Woche.</summary>
    LoanRequest,
}

/// <summary>Nachricht an einen Manager. Texte nach em.exe, je Zeile höchstens eine Zeile der Nachrichtenleiste.</summary>
public sealed class Message
{
    public required MessageKind Kind { get; init; }
    public required int Day { get; init; }
    /// <summary>Saison der Erstellung; zusammen mit Day ein Zeitpunkt auf der fortlaufenden Zeitleiste.</summary>
    public int Season { get; internal set; }
    public required string[] Lines { get; init; }
    public bool Read { get; set; }

    /// <summary>Erledigt (beantwortet oder verfallen); erledigte Fragen bleiben als Meldung stehen.</summary>
    public bool Done { get; set; }

    /// <summary>Verlängerung: der abgelaufene Vertrag.</summary>
    public SponsorContract? Contract { get; init; }

    /// <summary>Kreditwunsch: wer leihen möchte und wie viel.</summary>
    public int Borrower { get; init; }
    public long Amount { get; init; }

    public bool IsQuestion => Kind != MessageKind.Info && !Done;
}

public sealed partial class ManagerFinance
{
    /// <summary>Posteingang, neueste Nachricht zuletzt.</summary>
    public List<Message> Inbox { get; } = [];
}

public sealed partial class GameState
{
    /// <summary>Tage, nach denen eine unbeantwortete Kreditanfrage verfällt.</summary>
    public const int LoanRequestDays = 7;

    public IReadOnlyList<Message> Inbox(Manager manager) => Finance(manager).Inbox;

    public Message Notify(Manager manager, params string[] lines) => Post(manager, new Message { Kind = MessageKind.Info, Day = Day, Lines = lines });

    private Message Post(Manager manager, Message message)
    {
        message.Season = Season;
        Finance(manager).Inbox.Add(message);
        return message;
    }

    /// <summary>
    /// „Geld von … leihen (2.5%)“: der andere Manager bekommt eine Anfrage in den Posteingang
    /// (laut Anleitung beim nächsten Hauptmenü, verfällt nach einer Woche).
    /// </summary>
    public Message? RequestLoan(int borrower, int lender, long amount)
    {
        if (amount <= 0 || borrower == lender)
            return null;
        // [APPROX] Wortlaut der Anfrage nicht im Video; aus den Texten „Geld an … verleihen (2.5%).“ gebildet.
        return Post(Managers[lender], new Message
        {
            Kind = MessageKind.LoanRequest,
            Day = Day,
            Lines = [$"{Managers[borrower].Name} möchte sich {FormatMoney(amount)} leihen.", "Geld verleihen (2.5%) ?"],
            Borrower = borrower,
            Amount = amount,
        });
    }

    /// <summary>Beantwortet eine Frage im Posteingang. Liefert false, wenn es nichts (mehr) zu beantworten gibt.</summary>
    public bool Answer(Manager manager, Message message, bool yes)
    {
        if (!Managers.Contains(manager) || !Finance(manager).Inbox.Contains(message) || !message.IsQuestion)
            return false;
        if (MessageExpired(message, Math.Max(Day, FinanceDay)))
        {
            message.Done = true;
            return false;
        }
        message.Done = true;
        message.Read = true;
        if (!yes)
            return true;
        switch (message.Kind)
        {
            case MessageKind.SponsorRenewal when message.Contract is { } old:
                var ads = Finance(manager).Sponsoring;
                var free = old.Kind != AdSlotKind.Bande || old.Boards.All(b => ads.ContractAt(new AdSlot(AdSlotKind.Bande, b)) is null);
                if (free && !ads.Contracts.Any(c => c.Kind == old.Kind && old.Kind != AdSlotKind.Bande))
                    ads.Contracts.Add(new SponsorContract
                    {
                        Kind = old.Kind, Sponsor = old.Sponsor, Boards = old.Boards,
                        MonthlyAmount = old.MonthlyAmount, MonthsLeft = 12, // [APPROX] Laufzeit der Verlängerung
                    });
                break;
            case MessageKind.LoanRequest:
                LendBetweenManagers(IndexOf(manager), message.Borrower, message.Amount);
                break;
        }
        return true;
    }

    /// <summary>Täglich: unbeantwortete Kreditanfragen verfallen nach einer Woche.</summary>
    private void ExpireMessages()
    {
        foreach (var manager in Managers)
            foreach (var m in Finance(manager).Inbox)
                if (!m.Done && MessageExpired(m, FinanceDay))
                    m.Done = true;
    }

    private bool MessageExpired(Message message, int day) => message.Kind == MessageKind.LoanRequest
        && (long)(Season - message.Season) * SeasonCalendar.Length + day - message.Day >= LoanRequestDays;

    /// <summary>Abgelaufener Werbevertrag: Frage nach Verlängerung oder Absage der Firma (Texte aus em.exe).</summary>
    private void ContractExpired(Manager manager, SponsorContract contract)
    {
        var name = contract.Partner.Name;
        // [APPROX] Wie oft eine Firma verlängern will, ist unbekannt.
        if (FinanceRandom.NextDouble() < 0.6)
            Post(manager, new Message
            {
                Kind = MessageKind.SponsorRenewal,
                Day = Day,
                Lines = ["Ein Werbepartner mit Verlängerungsoption", "bittet Sie an den Verhandlungstisch.",
                         $"Möchten Sie den Vertrag mit {name} verlängern ?"],
                Contract = contract,
            });
        else
            Notify(manager, $"Die Firma {name}", "wird den Vertrag nicht verlängern.");
    }

    private void ConstructionCompleted(Manager manager, StadiumConstruction construction) =>
        Notify(manager, $"Der Stadion-Ausbau ({construction.Label})", "ist abgeschlossen.");
}
