using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

public enum ExerciseVideoPlayback
{
    Html5,
    YouTubeEmbed
}

public sealed record ExerciseVideoAsset(
    ExerciseMotion Motion,
    string Title,
    string VideoUrl,
    string? VideoMimeType,
    string? PosterUrl,
    ExerciseVideoPlayback Playback,
    string Creator,
    string SourceName,
    string SourcePageUrl,
    string LicenseName,
    string LicenseUrl);

/// <summary>Video sources and license metadata for every movement supported by the workout guide.</summary>
public static class ExerciseVideoCatalog
{
    private const string PexelsLicenseUrl = "https://www.pexels.com/license/";
    private const string PublicDomainLicenseUrl = "https://creativecommons.org/publicdomain/mark/1.0/";
    private const string YouTubeTermsUrl = "https://www.youtube.com/t/terms";

    private static readonly Dictionary<ExerciseMotion, ExerciseVideoAsset> Clips = new()
    {
        [ExerciseMotion.SupportedRow] = YouTube(
            ExerciseMotion.SupportedRow,
            "Remada com apoio no banco",
            "0-DXJiceG-0",
            "OPEX Fitness",
            "https://www.youtube.com/watch?v=0-DXJiceG-0"),
        [ExerciseMotion.CableRow] = Pexels(
            ExerciseMotion.CableRow,
            "Remada sentada na polia",
            "https://videos.pexels.com/video-files/4367635/4367635-hd_1920_1080_30fps.mp4",
            "https://images.pexels.com/videos/4367635/barbell-dumbbells-exercise-bike-fitness-equipment-4367635.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "Pavel Danilyuk",
            "https://www.pexels.com/video/a-man-doing-a-seated-cable-row-4367635/"),
        [ExerciseMotion.ChestFly] = Pexels(
            ExerciseMotion.ChestFly,
            "Voador na máquina",
            "https://videos.pexels.com/video-files/4921645/4921645-hd_1066_1920_25fps.mp4",
            "https://images.pexels.com/videos/4921645/20-25----4921645.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "Antoni Shkraba",
            "https://www.pexels.com/video/a-man-working-out-in-the-gym-4921645/"),
        [ExerciseMotion.LateralRaise] = Pexels(
            ExerciseMotion.LateralRaise,
            "Elevação lateral com halteres",
            "https://videos.pexels.com/video-files/5319088/5319088-uhd_2160_3840_25fps.mp4",
            "https://images.pexels.com/videos/5319088/pexels-photo-5319088.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "Tima Miroshnichenko",
            "https://www.pexels.com/video/a-man-working-out-using-dumbbell-5319088/"),
        [ExerciseMotion.LegExtension] = Cdc(
            ExerciseMotion.LegExtension,
            "Cadeira extensora",
            "https://www.cdc.gov/physicalactivity/videos/Leg_extension_Ipod-Lg.mp4",
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/1/13/Muscle_Strengthening_at_the_Gym_-_Leg_Extension.webm/250px--Muscle_Strengthening_at_the_Gym_-_Leg_Extension.webm.jpg",
            "https://commons.wikimedia.org/wiki/File:Muscle_Strengthening_at_the_Gym_-_Leg_Extension.webm"),
        [ExerciseMotion.TricepsPushdown] = Cdc(
            ExerciseMotion.TricepsPushdown,
            "Extensão de tríceps na polia",
            "https://www.cdc.gov/physicalactivity/videos/Arm_Extension_Ipod-Lg.mp4",
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/8/85/Muscle_Strengthening_at_the_Gym_-_Arm_Extension.webm/250px--Muscle_Strengthening_at_the_Gym_-_Arm_Extension.webm.jpg",
            "https://commons.wikimedia.org/wiki/File:Muscle_Strengthening_at_the_Gym_-_Arm_Extension.webm"),
        [ExerciseMotion.BodyweightSquat] = Pexels(
            ExerciseMotion.BodyweightSquat,
            "Agachamento livre",
            "https://videos.pexels.com/video-files/6525519/6525519-hd_1080_1920_30fps.mp4",
            "https://images.pexels.com/videos/6525519/exercises-girl-sport-sporty-6525519.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "Polina Tankilevitch",
            "https://www.pexels.com/video/young-woman-exercising-6525519/"),
        [ExerciseMotion.LatPulldown] = Pexels(
            ExerciseMotion.LatPulldown,
            "Puxada na frente",
            "https://videos.pexels.com/video-files/36871349/15620300_1080_1920_50fps.mp4",
            "https://images.pexels.com/videos/36871349/pexels-photo-36871349.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "Anil Sharma",
            "https://www.pexels.com/video/young-woman-exercising-on-lat-pulldown-machine-36871349/"),
        [ExerciseMotion.LegCurl] = Cdc(
            ExerciseMotion.LegCurl,
            "Cadeira flexora",
            "https://www.cdc.gov/physicalactivity/videos/Leg_Curl_Ipod-Lg.mp4",
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/1/14/Muscle_Strengthening_at_the_Gym_-_Leg_Curl.webm/250px--Muscle_Strengthening_at_the_Gym_-_Leg_Curl.webm.jpg",
            "https://commons.wikimedia.org/wiki/File:Muscle_Strengthening_at_the_Gym_-_Leg_Curl.webm"),
        [ExerciseMotion.ReverseFly] = YouTube(
            ExerciseMotion.ReverseFly,
            "Voador inverso na máquina",
            "vJYkqD7a0gM",
            "Gerardi Performance",
            "https://www.youtube.com/watch?v=vJYkqD7a0gM"),
        [ExerciseMotion.BodyweightLunge] = new ExerciseVideoAsset(
            ExerciseMotion.BodyweightLunge,
            "Passada à frente",
            "https://upload.wikimedia.org/wikipedia/commons/0/02/Forward_lunge_training.webm",
            "video/webm",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/0/02/Forward_lunge_training.webm/500px--Forward_lunge_training.webm.jpg",
            ExerciseVideoPlayback.Html5,
            "Skhulile Mthiyane",
            "Wikimedia Commons",
            "https://commons.wikimedia.org/wiki/File:Forward_lunge_training.webm",
            "CC BY-SA 4.0",
            "https://creativecommons.org/licenses/by-sa/4.0/"),
        [ExerciseMotion.SeatedCurl] = Pexels(
            ExerciseMotion.SeatedCurl,
            "Rosca sentada com halteres",
            "https://videos.pexels.com/video-files/4921648/4921648-hd_1066_1920_25fps.mp4",
            "https://images.pexels.com/videos/4921648/20-25----4921648.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "Antoni Shkraba",
            "https://www.pexels.com/video/a-man-working-out-in-the-gym-4921648/")
    };

    public static IReadOnlyCollection<ExerciseVideoAsset> All => Clips.Values;

    public static ExerciseVideoAsset? For(ExerciseMotion motion) =>
        Clips.TryGetValue(motion, out var clip) ? clip : null;

    private static ExerciseVideoAsset Pexels(
        ExerciseMotion motion,
        string title,
        string videoUrl,
        string posterUrl,
        string creator,
        string sourcePageUrl) =>
        new(
            motion,
            title,
            videoUrl,
            "video/mp4",
            posterUrl,
            ExerciseVideoPlayback.Html5,
            creator,
            "Pexels",
            sourcePageUrl,
            "Licença Pexels",
            PexelsLicenseUrl);

    private static ExerciseVideoAsset Cdc(
        ExerciseMotion motion,
        string title,
        string videoUrl,
        string posterUrl,
        string sourcePageUrl) =>
        new(
            motion,
            title,
            videoUrl,
            "video/mp4",
            posterUrl,
            ExerciseVideoPlayback.Html5,
            "CDC",
            "Wikimedia Commons",
            sourcePageUrl,
            "Domínio público",
            PublicDomainLicenseUrl);

    private static ExerciseVideoAsset YouTube(
        ExerciseMotion motion,
        string title,
        string videoId,
        string creator,
        string sourcePageUrl) =>
        new(
            motion,
            title,
            $"https://www.youtube-nocookie.com/embed/{videoId}?rel=0&playsinline=1",
            null,
            $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg",
            ExerciseVideoPlayback.YouTubeEmbed,
            creator,
            "YouTube",
            sourcePageUrl,
            "Player incorporado do YouTube",
            YouTubeTermsUrl);
}
