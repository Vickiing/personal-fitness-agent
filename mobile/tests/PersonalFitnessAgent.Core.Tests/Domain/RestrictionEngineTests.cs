using PersonalFitnessAgent.Core.Domain;
using PersonalFitnessAgent.Core.Time;

namespace PersonalFitnessAgent.Core.Tests.Domain;

public class RestrictionEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BlocksMatchingMovementPattern()
    {
        var policy = new RestrictionPolicy(Guid.NewGuid(), "Sem carga axial", RestrictionAction.Block,
            RestrictionSource.UserReport, "User configured movement restriction", movementPattern: "axial-load");
        var context = new ExerciseContext(Guid.NewGuid(), "Squat", new HashSet<string> { "axial-load" },
            new HashSet<string> { "barbell" }, true);

        var result = RestrictionEngine.Evaluate(context, [policy], Now);

        Assert.Equal(RestrictionDecision.Block, result.Decision);
        Assert.Contains(policy.Id, result.PolicyIds);
    }

    [Fact]
    public void RequiresReviewWhenMatchingDataIsUnknown()
    {
        var policy = new RestrictionPolicy(Guid.NewGuid(), "Equipment limit", RestrictionAction.Block,
            RestrictionSource.Clinician, "Configured equipment restriction", equipment: "barbell");
        var context = new ExerciseContext(Guid.NewGuid(), "Unknown movement", null, null, null);

        var result = RestrictionEngine.Evaluate(context, [policy], Now);

        Assert.Equal(RestrictionDecision.RequiresReview, result.Decision);
    }

    [Fact]
    public void PendingPolicyDoesNotAffectExercisesOutsideItsKnownScope()
    {
        var policy = new RestrictionPolicy(Guid.NewGuid(), "Pending squat review", RestrictionAction.Block,
            RestrictionSource.Clinician, "Awaiting review", RestrictionStatus.PendingReview,
            movementPattern: "squat");
        var context = new ExerciseContext(Guid.NewGuid(), "Bench press", new HashSet<string> { "horizontal-press" },
            new HashSet<string> { "barbell" }, true);

        var result = RestrictionEngine.Evaluate(context, [policy], Now);

        Assert.Equal(RestrictionDecision.NoPolicyMatched, result.Decision);
        Assert.Empty(result.PolicyIds);
    }

    [Fact]
    public void PendingPolicyRequiresReviewWhenExerciseCouldMatchItsScope()
    {
        var policy = new RestrictionPolicy(Guid.NewGuid(), "Pending squat review", RestrictionAction.Block,
            RestrictionSource.Clinician, "Awaiting review", RestrictionStatus.PendingReview,
            movementPattern: "squat");
        var context = new ExerciseContext(Guid.NewGuid(), "Unknown exercise", null, null, null);

        var result = RestrictionEngine.Evaluate(context, [policy], Now);

        Assert.Equal(RestrictionDecision.RequiresReview, result.Decision);
        Assert.Contains(policy.Id, result.PolicyIds);
    }

    [Fact]
    public void ReportsUserAndClinicianConflict()
    {
        var policies = new[]
        {
            new RestrictionPolicy(Guid.NewGuid(), "User preference", RestrictionAction.Warn, RestrictionSource.UserReport, "Preference", movementPattern: "press"),
            new RestrictionPolicy(Guid.NewGuid(), "Clinical block", RestrictionAction.Block, RestrictionSource.Clinician, "Clinical instruction", movementPattern: "press")
        };
        var context = new ExerciseContext(Guid.NewGuid(), "Press", new HashSet<string> { "press" }, new HashSet<string>(), false);

        var result = RestrictionEngine.Evaluate(context, policies, Now);

        Assert.Equal(RestrictionDecision.Block, result.Decision);
        Assert.NotEmpty(result.Conflicts);
    }

    [Fact]
    public void BlockedRecommendationCannotBeApprovedNormally()
    {
        var trace = new RestrictionEvaluation(RestrictionDecision.Block, [Guid.NewGuid()], ["Clinical block"], [], Now);
        var recommendation = new Recommendation(Guid.NewGuid(), "progression", 1, [Guid.NewGuid()],
            "Reason", "low", "Increase a load", new RecordedTimestamp(Now, TimeSpan.Zero), trace);

        Assert.Throws<InvalidOperationException>(() => recommendation.Decide(true, "accept", Now));
    }

    [Fact]
    public void DelayedSymptomSuspendsMatchingPendingProposal()
    {
        var pattern = "overhead-press";
        var sessionId = Guid.NewGuid();
        var trace = new RestrictionEvaluation(RestrictionDecision.Warn, [], [], [], Now);
        var recommendation = new Recommendation(Guid.NewGuid(), "progression", 1, [sessionId],
            "Reason", "low", "Maintain load", new RecordedTimestamp(Now, TimeSpan.Zero), trace, [pattern]);
        var symptom = new SymptomEvent(Guid.NewGuid(), new RecordedTimestamp(Now.AddHours(12), TimeSpan.Zero),
            Now, "shoulder", null, "Delayed symptom", sessionId, [pattern]);

        var suspended = Assert.Single(DelayedSymptomReview.SuspendAffected([recommendation], symptom, Now.AddHours(12)));

        Assert.Equal(RecommendationStatus.Suspended, suspended.Status);
    }
}
