namespace PersonalFitnessAgent.App;

public partial class App : Application
{
    public static FitnessDataService Data { get; set; } = null!;

    public App() => InitializeComponent();

    protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());
}
