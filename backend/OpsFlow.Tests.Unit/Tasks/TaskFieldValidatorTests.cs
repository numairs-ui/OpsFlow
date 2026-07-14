using FluentAssertions;
using OpsFlow.Api.Features.Tasks.CompleteTask;
using OpsFlow.Domain.Entities;
using System.Text.Json;
using Xunit;

namespace OpsFlow.Tests.Unit.Tasks;

/// <summary>
/// TaskFieldValidator is pure — it takes checklist items, submitted values, and store settings, and
/// returns errors/corrective actions with no DB access, so it is tested directly.
/// </summary>
public sealed class TaskFieldValidatorTests
{
    private static ChecklistTemplateItem Item(Guid templateId, string fieldsJson, string category = "General") =>
        new()
        {
            TemplateId = templateId,
            Template = new TaskTemplate
            {
                Id = templateId,
                TenantId = "t1",
                Name = "Template",
                Category = category,
                Scope = "Store",
                FieldsJson = fieldsJson,
                CreatedByUserId = "system",
            },
        };

    private static string FieldsJson(object spec) => JsonSerializer.Serialize(new[] { spec });

    private static TaskTemplate Template(Guid id, string fieldsJson, string category = "General") =>
        new()
        {
            Id = id,
            TenantId = "t1",
            Name = "Template",
            Category = category,
            Scope = "Store",
            FieldsJson = fieldsJson,
            CreatedByUserId = "system",
        };

    // ── ValidateAdHoc (standalone tasks, A1) ─────────────────────────────────────

    [Fact]
    public void ValidateAdHoc_null_template_is_notes_only_no_errors()
    {
        var result = TaskFieldValidator.ValidateAdHoc(null, [], null);

        result.Errors.Should().BeEmpty();
        result.CorrectiveActions.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAdHoc_required_field_missing_produces_error()
    {
        var templateId = Guid.NewGuid();
        var template = Template(templateId, FieldsJson(new { Id = "f1", Type = "Text", Label = "Name", Required = true }));

        var result = TaskFieldValidator.ValidateAdHoc(template, [], null);

        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Name"));
    }

    [Fact]
    public void ValidateAdHoc_valid_submission_passes()
    {
        var templateId = Guid.NewGuid();
        var template = Template(templateId, FieldsJson(new { Id = "f1", Type = "Text", Label = "Name", Required = true }));
        var submission = new FieldSubmission(templateId, "f1", "Done");

        var result = TaskFieldValidator.ValidateAdHoc(template, [submission], null);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Required_field_missing_produces_error()
    {
        var templateId = Guid.NewGuid();
        var items = new[] { Item(templateId, FieldsJson(new { Id = "f1", Type = "Text", Label = "Name", Required = true })) };

        var result = TaskFieldValidator.Validate(items, [], null);

        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Name"));
        result.CorrectiveActions.Should().BeEmpty();
    }

    [Fact]
    public void Required_checklist_subitem_unchecked_produces_error()
    {
        var templateId = Guid.NewGuid();
        var spec = new
        {
            Id = "checklist1",
            Type = "Checklist",
            Label = "Prep Steps",
            Required = true,
            SubItems = new[] { new { Id = "sub1", Label = "Wash hands", Required = true } },
        };
        var items = new[] { Item(templateId, FieldsJson(spec)) };
        var submission = new FieldSubmission(templateId, "checklist1", "");

        var result = TaskFieldValidator.Validate(items, [submission], null);

        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Wash hands"));
    }

    [Fact]
    public void Numeric_out_of_range_triggers_corrective_action()
    {
        var templateId = Guid.NewGuid();
        var spec = new { Id = "temp", Type = "Numeric", Label = "Fridge Temp", Required = false, RangeMin = 0.0, RangeMax = 40.0, CorrectiveActionText = "Call maintenance" };
        var items = new[] { Item(templateId, FieldsJson(spec)) };
        var submission = new FieldSubmission(templateId, "temp", "55");

        var result = TaskFieldValidator.Validate(items, [submission], null);

        result.Errors.Should().BeEmpty();
        result.CorrectiveActions.Should().ContainSingle(c => c.FieldLabel == "Fridge Temp" && c.Text == "Call maintenance");
    }

    [Fact]
    public void Boolean_false_triggers_corrective_action()
    {
        var templateId = Guid.NewGuid();
        var spec = new { Id = "clean", Type = "Boolean", Label = "Floor Clean", Required = false, CorrectiveActionText = "Re-mop" };
        var items = new[] { Item(templateId, FieldsJson(spec)) };
        var submission = new FieldSubmission(templateId, "clean", "false");

        var result = TaskFieldValidator.Validate(items, [submission], null);

        result.CorrectiveActions.Should().ContainSingle(c => c.Text == "Re-mop");
    }

    [Fact]
    public void Till_variance_within_settings_requires_note_and_initials()
    {
        var templateId = Guid.NewGuid();
        var spec = new { Id = "till_a", Type = "Numeric", Label = "Till A", Required = false };
        var items = new[] { Item(templateId, FieldsJson(spec), category: "Safe") };
        var settings = new StoreSettings { StoreId = Guid.NewGuid(), TenantId = "t1", TillABase = 200m };
        var submission = new FieldSubmission(templateId, "till_a", "150");

        var result = TaskFieldValidator.Validate(items, [submission], settings);

        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Variance Reason"));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Manager Initials"));
        result.CorrectiveActions.Should().ContainSingle(c => c.FieldLabel == "Till A");
    }

    [Fact]
    public void Till_variance_with_note_and_initials_supplied_passes()
    {
        var templateId = Guid.NewGuid();
        var spec = new { Id = "till_a", Type = "Numeric", Label = "Till A", Required = false };
        var items = new[] { Item(templateId, FieldsJson(spec), category: "Safe") };
        var settings = new StoreSettings { StoreId = Guid.NewGuid(), TenantId = "t1", TillABase = 200m };
        var submissions = new List<FieldSubmission>
        {
            new(templateId, "till_a", "150"),
            new(templateId, "variance_note", "Register short at open"),
            new(templateId, "manager_initials", "AB"),
        };

        var result = TaskFieldValidator.Validate(items, submissions, settings);

        result.Errors.Should().BeEmpty();
        result.CorrectiveActions.Should().ContainSingle();
    }

    [Fact]
    public void Till_within_tolerance_produces_no_corrective_action()
    {
        var templateId = Guid.NewGuid();
        var spec = new { Id = "till_a", Type = "Numeric", Label = "Till A", Required = false };
        var items = new[] { Item(templateId, FieldsJson(spec), category: "Safe") };
        var settings = new StoreSettings { StoreId = Guid.NewGuid(), TenantId = "t1", TillABase = 200m };
        var submission = new FieldSubmission(templateId, "till_a", "200.00");

        var result = TaskFieldValidator.Validate(items, [submission], settings);

        result.Errors.Should().BeEmpty();
        result.CorrectiveActions.Should().BeEmpty();
    }
}
