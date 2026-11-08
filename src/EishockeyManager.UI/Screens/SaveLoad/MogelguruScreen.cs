namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>Stub (Teil 3, roter Test).</summary>
public sealed class MogelguruScreen(ScreenHost host, Screen back) : Screen(host)
{
    public Screen Back { get; } = back;
    public override void Update(InputState input) { }
    public override void Draw(Surface s) { }
}
