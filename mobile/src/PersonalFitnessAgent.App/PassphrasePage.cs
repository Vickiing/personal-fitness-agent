namespace PersonalFitnessAgent.App;

internal sealed class PassphrasePage : ContentPage
{
    private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Entry _password = new() { IsPassword = true, Placeholder = "Senha do backup", ReturnType = ReturnType.Done };

    private PassphrasePage(string title, string description)
    {
        Title = title;
        Content = Ui.Scroll(title, Ui.Body(description), _password,
            Ui.Button("Continuar", async (_, _) => await Complete(_password.Text)),
            Ui.Button("Cancelar", async (_, _) => await Complete(null), true));
    }

    private async Task Complete(string? value)
    {
        if (value is not null && value.Length < 10)
        {
            await DisplayAlertAsync("Senha curta", "Use pelo menos 10 caracteres.", "OK");
            return;
        }
        _completion.TrySetResult(value);
        await Navigation.PopModalAsync();
    }

    public static async Task<string?> Request(Page owner, string title, string description)
    {
        var page = new PassphrasePage(title, description);
        await owner.Navigation.PushModalAsync(page);
        return await page._completion.Task;
    }
}
