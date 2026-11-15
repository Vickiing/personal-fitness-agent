using System.Text.Json;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>Loads a private local plan for this development build only; future web sync takes precedence.</summary>
internal static class LocalWorkoutSeed
{
    private const string AssetName = "local-workout-seed.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task LoadIfMissingAsync()
    {
        if (App.Data.ImportedWorkoutSheet is not null) return;
        await Gate.WaitAsync();
        try
        {
            if (App.Data.ImportedWorkoutSheet is not null) return;
            await using var stream = await FileSystem.OpenAppPackageFileAsync(AssetName);
            var sheet = await JsonSerializer.DeserializeAsync<WorkoutSheet>(stream, JsonOptions);
            if (sheet is not null && App.Data.ImportedWorkoutSheet is null)
                App.Data.CacheWorkoutSheet(sheet);
        }
        catch (FileNotFoundException)
        {
            // A clean checkout has no private user seed and remains usable without it.
        }
        catch (DirectoryNotFoundException)
        {
            // The local seed is optional until the web sync adapter is available.
        }
        catch (JsonException)
        {
            // Keep the app usable if the optional local seed is malformed.
        }
        finally
        {
            Gate.Release();
        }
    }
}
