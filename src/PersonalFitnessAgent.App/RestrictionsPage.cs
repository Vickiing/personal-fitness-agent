using PersonalFitnessAgent.Core.Domain;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class RestrictionsPage : ContentPage
{
    private readonly Entry _label = Ui.Input("Nome curto da política");
    private readonly Entry _region = Ui.Input("Região corporal, se aplicável");
    private readonly Entry _pattern = Ui.Input("Padrão de movimento, se aplicável");
    private readonly Entry _equipment = Ui.Input("Equipamento, se aplicável");
    private readonly Entry _position = Ui.Input("Posição, se aplicável");
    private readonly Picker _externalLoad = new() { Title = "Carga externa (opcional)" };
    private readonly Picker _action = new() { Title = "Ação" };
    private readonly Picker _source = new() { Title = "Origem" };
    private readonly Picker _status = new() { Title = "Estado" };
    private readonly Picker _existing = new() { Title = "Revisar nova versão de política (opcional)" };
    private readonly Entry _reason = Ui.Input("Motivo informado");
    private readonly VerticalStackLayout _items = new() { Spacing = 8 };

    public RestrictionsPage()
    {
        Title = "Restrições";
        _externalLoad.ItemsSource = new[] { "Não especificado", "Com carga externa", "Sem carga externa" };
        _externalLoad.SelectedIndex = 0;
        _action.ItemsSource = new[] { "Bloquear", "Avisar", "Exigir revisão humana" };
        _action.SelectedIndex = 2;
        _source.ItemsSource = new[] { "Relato do usuário", "Orientação clínica", "Outra origem" };
        _source.SelectedIndex = 0;
        _status.ItemsSource = new[] { "Ativa", "Pendente de revisão", "Aposentada" };
        _status.SelectedIndex = 0;
        _existing.ItemDisplayBinding = new Binding(nameof(RestrictionDto.Label));
        _existing.SelectedIndexChanged += (_, _) => LoadSelectedPolicy();
        RefreshPolicies();
        Content = Ui.Scroll("Políticas de segurança",
            Ui.Body("A origem registra o que foi informado, não verifica a identidade do profissional. Políticas clínicas e preferências ficam separadas; casos desconhecidos exigem revisão."),
            Ui.Section("Adicionar ou revisar versão"), _existing, _label, _region, _pattern, _equipment, _position, _externalLoad,
            _action, _source, _status, _reason,
            Ui.Button("Salvar política", (_, _) => Save()),
            Ui.Section("Políticas registradas"), _items);
    }

    private async void Save()
    {
        try
        {
            bool? external = _externalLoad.SelectedIndex switch { 1 => true, 2 => false, _ => null };
            var policy = App.Data.AddRestriction(_label.Text ?? "", _pattern.Text, _region.Text, _equipment.Text, external,
                _position.Text, (RestrictionAction)_action.SelectedIndex, (RestrictionSource)_source.SelectedIndex, _reason.Text ?? "",
                (_existing.SelectedItem as RestrictionDto)?.Id, (RestrictionStatus)_status.SelectedIndex);
            _label.Text = _region.Text = _pattern.Text = _equipment.Text = _position.Text = _reason.Text = "";
            _existing.SelectedItem = null;
            RefreshPolicies();
            await DisplayAlertAsync("Salvo", $"Política {policy.Label} foi salva localmente como versão {policy.Version}.", "OK");
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private void RefreshPolicies()
    {
        _existing.ItemsSource = App.Data.Restrictions.OrderBy(policy => policy.Label).ToArray();
        _items.Children.Clear();
        foreach (var policy in App.Data.Restrictions.OrderByDescending(item => item.Version))
        {
            var action = (RestrictionAction)policy.Action switch
            {
                RestrictionAction.Block => "Bloqueia",
                RestrictionAction.Warn => "Avisa",
                _ => "Exige revisão"
            };
            var source = (RestrictionSource)policy.Source switch
            {
                RestrictionSource.Clinician => "Origem: clínica",
                RestrictionSource.UserReport => "Origem: usuário",
                _ => "Origem: outra"
            };
            _items.Children.Add(new Border
            {
                Stroke = Color.FromArgb("#DDE3DD"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Padding = 12,
                Content = Ui.Body($"{policy.Label} · v{policy.Version}\n{action} · {source}\n{policy.Reason}\n" +
                    string.Join(" · ", new[] { policy.MovementPattern, policy.Region, policy.Equipment }.Where(x => !string.IsNullOrWhiteSpace(x))))
            });
        }
        if (_items.Children.Count == 0) _items.Children.Add(Ui.Body("Ainda não há políticas registradas."));
    }

    private void LoadSelectedPolicy()
    {
        if (_existing.SelectedItem is not RestrictionDto policy) return;
        _label.Text = policy.Label; _region.Text = policy.Region; _pattern.Text = policy.MovementPattern;
        _equipment.Text = policy.Equipment; _position.Text = policy.Position; _reason.Text = policy.Reason;
        _action.SelectedIndex = policy.Action; _source.SelectedIndex = policy.Source;
        _status.SelectedIndex = policy.Status;
        _externalLoad.SelectedIndex = policy.ExternalLoad switch { true => 1, false => 2, null => 0 };
    }
}
