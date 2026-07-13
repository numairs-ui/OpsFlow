using FluentAssertions;
using OpsFlow.Domain.Checklists;
using OpsFlow.Domain.Entities;
using Xunit;

namespace OpsFlow.Tests.Unit.Tasks;

/// <summary>
/// ChecklistScoring is pure — it takes checklist items + submitted scores and returns a composite
/// percent and the failed items, with no DB access, so it is tested directly.
/// </summary>
public sealed class ChecklistScoringTests
{
    private static ChecklistTemplateItem Item(
        Guid templateId, string? scoringType, decimal weight = 1.0m,
        int? failThreshold = null, string? correctiveText = null) =>
        new()
        {
            TemplateId = templateId,
            ScoringType = scoringType,
            Weight = weight,
            FailScoreThreshold = failThreshold,
            FailCorrectiveActionText = correctiveText,
        };

    [Fact]
    public void Composite_of_no_scored_items_is_null()
    {
        var items = new[] { Item(Guid.NewGuid(), null) };
        ChecklistScoring.ComputeCompositeScore(items, []).Should().BeNull();
    }

    [Fact]
    public void PassFail_pass_is_100_fail_is_0()
    {
        var pass = Guid.NewGuid();
        var fail = Guid.NewGuid();
        var items = new[] { Item(pass, "PassFail"), Item(fail, "PassFail") };
        var scores = new[] { new ItemScore(pass, 1), new ItemScore(fail, 0) };

        ChecklistScoring.ComputeCompositeScore(items, scores).Should().Be(50.0m);
    }

    [Fact]
    public void Scale1To5_maps_to_percentage()
    {
        var id = Guid.NewGuid();
        var items = new[] { Item(id, "Scale1To5") };
        // 4/5 = 80%.
        ChecklistScoring.ComputeCompositeScore(items, [new ItemScore(id, 4)]).Should().Be(80.0m);
    }

    [Fact]
    public void Weight_biases_the_composite()
    {
        var heavy = Guid.NewGuid();
        var light = Guid.NewGuid();
        var items = new[] { Item(heavy, "PassFail", weight: 3m), Item(light, "PassFail", weight: 1m) };
        // heavy=pass(100)*3 + light=fail(0)*1 = 300 / 4 = 75.
        var scores = new[] { new ItemScore(heavy, 1), new ItemScore(light, 0) };

        ChecklistScoring.ComputeCompositeScore(items, scores).Should().Be(75.0m);
    }

    [Fact]
    public void Unscored_submitted_items_do_not_affect_composite()
    {
        var scored = Guid.NewGuid();
        var unscored = Guid.NewGuid();
        var items = new[] { Item(scored, "PassFail"), Item(unscored, null) };
        var scores = new[] { new ItemScore(scored, 1), new ItemScore(unscored, 0) };

        ChecklistScoring.ComputeCompositeScore(items, scores).Should().Be(100.0m);
    }

    [Fact]
    public void Failures_only_returned_for_failed_items_with_corrective_text()
    {
        var failWithText = Guid.NewGuid();
        var failNoText = Guid.NewGuid();
        var pass = Guid.NewGuid();
        var items = new[]
        {
            Item(failWithText, "PassFail", correctiveText: "Re-clean"),
            Item(failNoText, "PassFail"),
            Item(pass, "PassFail", correctiveText: "Never fires"),
        };
        var scores = new[]
        {
            new ItemScore(failWithText, 0),
            new ItemScore(failNoText, 0),
            new ItemScore(pass, 1),
        };

        var failures = ChecklistScoring.DetermineFailures(items, scores);

        failures.Should().ContainSingle();
        failures[0].TemplateId.Should().Be(failWithText);
        failures[0].CorrectiveActionText.Should().Be("Re-clean");
    }

    [Fact]
    public void Scale1To5_fails_at_or_below_threshold()
    {
        var id = Guid.NewGuid();
        var items = new[] { Item(id, "Scale1To5", failThreshold: 2, correctiveText: "Fix it") };

        ChecklistScoring.DetermineFailures(items, [new ItemScore(id, 2)]).Should().ContainSingle();
        ChecklistScoring.DetermineFailures(items, [new ItemScore(id, 3)]).Should().BeEmpty();
    }
}
