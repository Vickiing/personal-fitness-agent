using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

public enum ExerciseVideoPlayback
{
    Html5
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
    private static readonly Dictionary<ExerciseMotion, ExerciseVideoAsset> Clips = new()
    {
        [ExerciseMotion.SupportedRow] = Pexels(
            ExerciseMotion.SupportedRow,
            "Remada sentada na polia",
            "https://videos.pexels.com/video-files/35585654/15079810_1080_1920_30fps.mp4",
            "https://images.pexels.com/videos/35585654/pexels-photo-35585654.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "khezez | خزاز",
            "https://www.pexels.com/video/man-exercising-on-cable-machine-in-gym-35585654/"),
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
        [ExerciseMotion.ReverseFly] = new ExerciseVideoAsset(
            ExerciseMotion.ReverseFly,
            "Voador inverso na máquina",
            "https://upload.wikimedia.org/wikipedia/commons/transcoded/a/ab/%EB%88%84%EA%B5%AC%EB%82%98_%EA%B0%80%EC%8A%B4%EC%9A%B4%EB%8F%99%EC%9D%84_%ED%95%B4%EC%95%BC_%ED%95%9C%EB%8B%A4%21_%ED%97%AC%EC%8A%A4%EC%9E%A5_%EA%B8%B0%EA%B5%AC_%ED%8E%99_%EB%8D%B1_%ED%94%8C%EB%9D%BC%EC%9D%B4_%26_%EB%A6%AC%EC%96%B4_%EB%8D%B8%ED%86%A0%EC%9D%B4%EB%93%9C_How_to_do_Pec_dec_fly_and_rear_deltoid.webm/%EB%88%84%EA%B5%AC%EB%82%98_%EA%B0%80%EC%8A%B4%EC%9A%B4%EB%8F%99%EC%9D%84_%ED%95%B4%EC%95%BC_%ED%95%9C%EB%8B%A4%21_%ED%97%AC%EC%8A%A4%EC%9E%A5_%EA%B8%B0%EA%B5%AC_%ED%8E%99_%EB%8D%B1_%ED%94%8C%EB%9D%BC%EC%9D%B4_%26_%EB%A6%AC%EC%96%B4_%EB%8D%B8%ED%86%A0%EC%9D%B4%EB%93%9C_How_to_do_Pec_dec_fly_and_rear_deltoid.webm.360p.mpeg4.mov",
            "video/mp4",
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/a/ab/%EB%88%84%EA%B5%AC%EB%82%98_%EA%B0%80%EC%8A%B4%EC%9A%B4%EB%8F%99%EC%9D%84_%ED%95%B4%EC%95%BC_%ED%95%9C%EB%8B%A4%21_%ED%97%AC%EC%8A%A4%EC%9E%A5_%EA%B8%B0%EA%B5%AC_%ED%8E%99_%EB%8D%B1_%ED%94%8C%EB%9D%BC%EC%9D%B4_%26_%EB%A6%AC%EC%96%B4_%EB%8D%B8%ED%86%A0%EC%9D%B4%EB%93%9C_How_to_do_Pec_dec_fly_and_rear_deltoid.webm/960px--%EB%88%84%EA%B5%AC%EB%82%98_%EA%B0%80%EC%8A%B4%EC%9A%B4%EB%8F%99%EC%9D%84_%ED%95%B4%EC%95%BC_%ED%95%9C%EB%8B%A4%21_%ED%97%AC%EC%8A%A4%EC%9E%A5_%EA%B8%B0%EA%B5%AC_%ED%8E%99_%EB%8D%B1_%ED%94%8C%EB%9D%BC%EC%9D%B4_%26_%EB%A6%AC%EC%96%B4_%EB%8D%B8%ED%86%A0%EC%9D%B4%EB%93%9C_How_to_do_Pec_dec_fly_and_rear_deltoid.webm.jpg",
            ExerciseVideoPlayback.Html5,
            "심으뜸의 마이너스 라이프",
            "Wikimedia Commons",
            "https://commons.wikimedia.org/wiki/File:%EB%88%84%EA%B5%AC%EB%82%98_%EA%B0%80%EC%8A%B4%EC%9A%B4%EB%8F%99%EC%9D%84_%ED%95%B4%EC%95%BC_%ED%95%9C%EB%8B%A4!_%ED%97%AC%EC%8A%A4%EC%9E%A5_%EA%B8%B0%EA%B5%AC_%ED%8E%99_%EB%8D%B1_%ED%94%8C%EB%9D%BC%EC%9D%B4_%26_%EB%A6%AC%EC%96%B4_%EB%8D%B8%ED%86%A0%EC%9D%B4%EB%93%9C_How_to_do_Pec_dec_fly_and_rear_deltoid.webm",
            "CC BY 3.0",
            "https://creativecommons.org/licenses/by/3.0/"),
        [ExerciseMotion.BodyweightLunge] = Pexels(
            ExerciseMotion.BodyweightLunge,
            "Passada à frente",
            "https://videos.pexels.com/video-files/4488004/4488004-hd_1080_1920_25fps.mp4",
            "https://images.pexels.com/videos/4488004/crossfit-fit-fitness-girl-4488004.jpeg?auto=compress&dpr=1&h=750&w=1260",
            "olia danilevich",
            "https://www.pexels.com/video/woman-doing-leg-lunges-4488004/"),
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


}
