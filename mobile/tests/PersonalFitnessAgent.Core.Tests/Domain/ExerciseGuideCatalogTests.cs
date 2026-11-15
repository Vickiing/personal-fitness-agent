using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.Core.Tests.Domain;

public sealed class ExerciseGuideCatalogTests
{
    [Theory]
    [InlineData("supported chest row", ExerciseMotion.SupportedRow)]
    [InlineData("cable low row", ExerciseMotion.CableRow)]
    [InlineData("machine fly", ExerciseMotion.ChestFly)]
    [InlineData("seated lateral raise", ExerciseMotion.LateralRaise)]
    [InlineData("leg extension", ExerciseMotion.LegExtension)]
    [InlineData("cable triceps pushdown", ExerciseMotion.TricepsPushdown)]
    [InlineData("bodyweight squat", ExerciseMotion.BodyweightSquat)]
    [InlineData("lat pulldown", ExerciseMotion.LatPulldown)]
    [InlineData("leg curl", ExerciseMotion.LegCurl)]
    [InlineData("reverse fly", ExerciseMotion.ReverseFly)]
    [InlineData("bodyweight lunge", ExerciseMotion.BodyweightLunge)]
    [InlineData("seated dumbbell curl", ExerciseMotion.SeatedCurl)]
    public void RecognizedExerciseHasAnIllustrationAndNumberedSteps(string name, ExerciseMotion expectedMotion)
    {
        var guide = ExerciseGuideCatalog.For(name);

        Assert.Equal(expectedMotion, guide.Motion);
        Assert.False(string.IsNullOrWhiteSpace(guide.ImageDescription));
        Assert.Equal(4, guide.Steps.Count);
        Assert.All(guide.Steps, step => Assert.False(string.IsNullOrWhiteSpace(step)));
    }

    [Fact]
    public void MatchesPortugueseNamesWithoutDependingOnAccents()
    {
        var guide = ExerciseGuideCatalog.For("Tríceps na polia");

        Assert.Equal(ExerciseMotion.TricepsPushdown, guide.Motion);
    }
}
