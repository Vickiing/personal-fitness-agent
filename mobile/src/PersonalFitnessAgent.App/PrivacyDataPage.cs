using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Infrastructure.Sqlite;

namespace PersonalFitnessAgent.App;

public sealed class PrivacyDataPage : ContentPage
{
    public PrivacyDataPage()
    {
        Title = "Privacidade e dados";
        var content = Ui.Form("Seus dados neste aparelho",
            Ui.Body("Os registros são mantidos localmente. Os backups podem conter informações pessoais de saúde; escolha um destino privado e guarde a senha do backup criptografado."),
            Ui.Section("Exportar"),
            Ui.Button("Exportar backup JSON", async (_, _) => await ExportBackup()),
            Ui.Button("Exportar backup criptografado", async (_, _) => await ExportEncryptedBackup(), true),
            Ui.Button("Exportar tabelas CSV", async (_, _) => await ExportCsv(), true),
            Ui.Section("Restaurar"),
            Ui.Body("A importação mostra uma prévia e substitui os dados locais somente após sua confirmação."),
            Ui.Button("Importar backup com prévia", async (_, _) => await ImportBackup(), true),
            Ui.Section("Apagar dados"),
            Ui.Body("Esta ação remove registros, restrições, auditoria e fila de sincronização deste aparelho."),
            Ui.Button("Apagar todos os registros deste aparelho", async (_, _) => await DeleteAll(), true));
        Content = new ScrollView { Content = content, BackgroundColor = Color.FromArgb("#0B0F14") };
    }

    private async Task ExportBackup()
    {
        try
        {
            var confirm = await DisplayAlertAsync("Dados sensíveis", "O backup JSON inclui seus registros e histórico de auditoria. Salve ou compartilhe apenas em um destino privado.", "Continuar", "Cancelar");
            if (!confirm) return;
            var path = Path.Combine(FileSystem.CacheDirectory, $"fitness-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(path, App.Data.ExportJson());
            await Share.Default.RequestAsync(new ShareFileRequest("Backup Pulso", new ShareFile(path)));
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task ExportCsv()
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"fitness-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            await File.WriteAllTextAsync(path, App.Data.ExportCsv(), new System.Text.UTF8Encoding(true));
            await Share.Default.RequestAsync(new ShareFileRequest("Exportação Pulso", new ShareFile(path)));
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task ExportEncryptedBackup()
    {
        try
        {
            var first = await PassphrasePage.Request(this, "Criptografar backup", "Crie uma senha que você conseguirá guardar em local seguro.");
            if (first is null) return;
            var second = await PassphrasePage.Request(this, "Confirmar senha", "Digite a mesma senha novamente.");
            if (second is null) return;
            if (!StringComparer.Ordinal.Equals(first, second))
            {
                await DisplayAlertAsync("Senhas diferentes", "A senha de confirmação não corresponde.", "OK");
                return;
            }
            var path = Path.Combine(FileSystem.CacheDirectory, $"fitness-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pfa.json");
            await File.WriteAllTextAsync(path, App.Data.ExportEncrypted(first));
            await Share.Default.RequestAsync(new ShareFileRequest("Backup criptografado Pulso", new ShareFile(path)));
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task ImportBackup()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selecione um backup JSON" });
            if (file is null) return;
            var json = await File.ReadAllTextAsync(file.FullPath);
            if (BackupEncryption.IsEncrypted(json))
            {
                var passphrase = await PassphrasePage.Request(this, "Descriptografar backup", "Digite a senha usada na exportação.");
                if (passphrase is null) return;
                json = BackupEncryption.Decrypt(json, passphrase);
            }
            var preview = App.Data.PreviewImport(json);
            if (!preview.IsValid)
            {
                await DisplayAlertAsync("Backup inválido", preview.Error ?? "O arquivo não pode ser restaurado.", "OK");
                return;
            }
            var approved = await DisplayAlertAsync("Prévia do backup",
                $"Versão {preview.FormatVersion}: {preview.Records} registros, {preview.AuditEvents} eventos de auditoria e {preview.OutboxEvents} eventos pendentes.\n\nA restauração substitui os dados locais atuais. Continuar?",
                "Restaurar", "Cancelar");
            if (!approved) return;
            App.Data.Restore(json);
            await DisplayAlertAsync("Restaurado", "Os registros e os IDs de origem foram restaurados.", "OK");
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task DeleteAll()
    {
        var approved = await DisplayAlertAsync("Apagar dados locais", "Isso remove registros, restrições, auditoria e fila de sincronização deste aparelho. Esta ação não pode ser desfeita sem um backup.", "Apagar tudo", "Cancelar");
        if (!approved) return;
        try
        {
            App.Data.DeleteAllData();
            await DisplayAlertAsync("Dados apagados", "Os registros locais foram removidos deste aparelho.", "OK");
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }
}
