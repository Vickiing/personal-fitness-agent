using System.Globalization;
using System.Text;

namespace PersonalFitnessAgent.Core.Domain;

public enum ExerciseMotion
{
    SupportedRow,
    CableRow,
    ChestFly,
    LateralRaise,
    LegExtension,
    TricepsPushdown,
    BodyweightSquat,
    LatPulldown,
    LegCurl,
    ReverseFly,
    BodyweightLunge,
    SeatedCurl
}

public sealed record ExerciseGuide(ExerciseMotion Motion, string ImageDescription, IReadOnlyList<string> Steps);

/// <summary>General movement walkthroughs for the workout viewer; they don't assess an individual's suitability.</summary>
public static class ExerciseGuideCatalog
{
    public static ExerciseGuide For(string exerciseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exerciseName);
        var name = Normalize(exerciseName);
        if (name.Contains("voador inverso") || name.Contains("reverse fly"))
            return Guide(ExerciseMotion.ReverseFly, "Pessoa sentada com o peito apoiado, abrindo os braços para os lados.",
                "Ajuste o apoio para manter o tronco estável.",
                "Comece com os braços à frente e cotovelos levemente flexionados.",
                "Abra os braços até a linha dos ombros, sem embalo.",
                "Retorne devagar à posição inicial.");
        if (name.Contains("puxada") || name.Contains("lat pulldown"))
            return Guide(ExerciseMotion.LatPulldown, "Pessoa sentada puxando a barra à frente do corpo.",
                "Sente-se e apoie as coxas sob o suporte.",
                "Segure a barra acima da cabeça e mantenha o tronco estável.",
                "Puxe a barra à frente em direção à parte alta do peito.",
                "Estenda os braços devagar até a posição inicial.");
        if (name.Contains("flexora") || name.Contains("leg curl"))
            return Guide(ExerciseMotion.LegCurl, "Pessoa na cadeira flexora dobrando os joelhos contra o apoio.",
                "Ajuste o apoio para que fique próximo à parte baixa das pernas.",
                "Mantenha quadril e tronco apoiados.",
                "Dobre os joelhos levando o apoio para trás.",
                "Estenda as pernas com controle, sem soltar o peso.");
        if (name.Contains("extensora") || name.Contains("leg extension"))
            return Guide(ExerciseMotion.LegExtension, "Pessoa na cadeira extensora estendendo os joelhos.",
                "Ajuste o assento para alinhar o eixo da máquina aos joelhos.",
                "Posicione o apoio à frente da parte baixa das pernas.",
                "Estenda os joelhos em movimento controlado.",
                "Retorne devagar sem deixar o peso cair.");
        if (name.Contains("agachamento") || name.Contains("squat"))
            return Guide(ExerciseMotion.BodyweightSquat, "Pessoa fazendo agachamento sem carga externa.",
                "Fique em pé com os pés apoiados e em uma posição confortável.",
                "Leve o quadril para trás enquanto dobra os joelhos.",
                "Desça apenas até a amplitude confortável, mantendo o tronco estável.",
                "Empurre o chão com os pés para voltar a ficar em pé.");
        if (name.Contains("passada") || name.Contains("lunge"))
            return Guide(ExerciseMotion.BodyweightLunge, "Pessoa fazendo passada sem carga externa.",
                "Fique em pé com os pés afastados na largura confortável.",
                "Dê um passo à frente ou para trás e mantenha o tronco estável.",
                "Dobre os joelhos e desça apenas até a amplitude confortável.",
                "Empurre o chão para retornar e repita com a outra perna.");
        if (name.Contains("triceps") || name.Contains("tríceps"))
            return Guide(ExerciseMotion.TricepsPushdown, "Pessoa em pé estendendo os cotovelos na polia.",
                "Fique de frente para a polia e segure o acessório.",
                "Mantenha os cotovelos próximos ao tronco.",
                "Estenda os cotovelos levando as mãos para baixo.",
                "Dobre os cotovelos devagar para retornar.");
        if (name.Contains("lateral"))
            return Guide(ExerciseMotion.LateralRaise, "Pessoa sentada elevando os braços para os lados.",
                "Sente-se com o tronco apoiado e os braços ao lado do corpo.",
                "Mantenha os cotovelos levemente flexionados.",
                "Eleve os braços para os lados até uma altura confortável.",
                "Baixe devagar sem balançar o tronco.");
        if (name.Contains("rosca") || name.Contains("curl"))
            return Guide(ExerciseMotion.SeatedCurl, "Pessoa sentada flexionando os cotovelos com halteres.",
                "Sente-se com as costas apoiadas e os braços ao lado do corpo.",
                "Mantenha os cotovelos próximos ao tronco.",
                "Dobre os cotovelos levando os halteres para cima.",
                "Baixe os halteres devagar até a posição inicial.");
        if (name.Contains("crucifixo") || name.Contains("peck deck") || name.Contains("voador") || name.Contains("fly"))
            return Guide(ExerciseMotion.ChestFly, "Pessoa sentada aproximando os braços à frente na máquina peck deck.",
                "Ajuste o assento para os apoios ficarem na altura do peito.",
                "Mantenha as costas apoiadas e os cotovelos levemente flexionados.",
                "Aproxime os braços à frente sem bater os apoios.",
                "Retorne devagar até a posição inicial.");
        if (name.Contains("remada") || name.Contains("row"))
        {
            var cable = name.Contains("polia") || name.Contains("cable") || name.Contains("baixa");
            return cable
                ? Guide(ExerciseMotion.CableRow, "Pessoa sentada puxando o cabo em direção ao tronco.",
                    "Sente-se de frente para a polia e apoie os pés.",
                    "Comece com os braços estendidos e o tronco estável.",
                    "Puxe o acessório em direção ao tronco, levando os cotovelos para trás.",
                    "Estenda os braços devagar sem balançar o corpo.")
                : Guide(ExerciseMotion.SupportedRow, "Pessoa sentada com o peito apoiado, puxando as alças para trás.",
                    "Ajuste o apoio e encoste o peito nele.",
                    "Segure as alças com os braços estendidos.",
                    "Puxe as alças levando os cotovelos para trás, sem tirar o peito do apoio.",
                    "Estenda os braços devagar até a posição inicial.");
        }
        return Guide(ExerciseMotion.SupportedRow, "Demonstração visual genérica de uma repetição controlada.",
            "Confira o nome e a configuração do exercício na ficha.",
            "Prepare o equipamento e uma posição inicial estável.",
            "Faça a fase principal do movimento de forma controlada.",
            "Retorne devagar à posição inicial.");
    }

    private static ExerciseGuide Guide(ExerciseMotion motion, string imageDescription, params string[] steps) =>
        new(motion, imageDescription, steps);

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
