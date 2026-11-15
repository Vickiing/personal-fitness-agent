namespace PersonalFitnessAgent.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(TrainingSheetPage), typeof(TrainingSheetPage));
        Routing.RegisterRoute(nameof(CheckInPage), typeof(CheckInPage));
	}
}
