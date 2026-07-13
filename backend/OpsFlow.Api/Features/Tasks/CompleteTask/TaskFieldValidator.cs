using FluentValidation.Results;
using OpsFlow.Domain.Entities;
using System.Text.Json;
using StoreSettingsEntity = OpsFlow.Domain.Entities.StoreSettings;

namespace OpsFlow.Api.Features.Tasks.CompleteTask;

internal sealed record TaskFieldValidationResult(List<ValidationFailure> Errors, List<CorrectiveActionDto> CorrectiveActions);

/// <summary>
/// Validates submitted checklist field values against their template specs (required fields, checklist
/// sub-items, numeric range, boolean corrective triggers) and, for Safe-category items, checks till
/// amounts against the store's base float for variance.
/// </summary>
internal static class TaskFieldValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Validates a standalone task's submission against a single template's fields (mode (a)), with no
    /// ChecklistTemplateItem wrapper. A null template is a notes-only task, which has no fields to
    /// validate. Reuses the checklist path by wrapping the template in a transient item.
    /// </summary>
    public static TaskFieldValidationResult ValidateAdHoc(
        TaskTemplate? template,
        List<FieldSubmission> fieldValues,
        StoreSettingsEntity? storeSettings)
    {
        if (template is null)
            return new TaskFieldValidationResult([], []);

        var item = new ChecklistTemplateItem { TemplateId = template.Id, Template = template };
        return Validate([item], fieldValues, storeSettings);
    }

    public static TaskFieldValidationResult Validate(
        IEnumerable<ChecklistTemplateItem> checklistItems,
        List<FieldSubmission> fieldValues,
        StoreSettingsEntity? storeSettings)
    {
        var items = checklistItems.ToList();
        var fieldErrors = new List<ValidationFailure>();
        var corrective = new List<CorrectiveActionDto>();

        foreach (var item in items)
        {
            var specs = JsonSerializer.Deserialize<List<TemplateFieldSpec>>(item.Template!.FieldsJson, JsonOptions) ?? [];

            foreach (var spec in specs)
            {
                var submitted = fieldValues.FirstOrDefault(f => f.TemplateId == item.TemplateId && f.FieldId == spec.Id);

                if (spec.Required && (submitted == null || string.IsNullOrWhiteSpace(submitted.Value)))
                {
                    if (spec.Type == "Checklist" && spec.SubItems?.Count > 0)
                    {
                        var checkedIds = submitted?.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet()
                            ?? [];
                        var missing = spec.SubItems.Where(s => s.Required && !checkedIds.Contains(s.Id));
                        foreach (var m in missing)
                            fieldErrors.Add(new ValidationFailure($"FieldValues[{spec.Id}]", $"Required checklist item '{m.Label}' in '{spec.Label}' must be checked."));
                    }
                    else
                    {
                        fieldErrors.Add(new ValidationFailure($"FieldValues[{spec.Id}]", $"Field '{spec.Label}' is required."));
                    }
                    continue;
                }

                if (submitted == null) continue;

                if (spec.Type == "Numeric" && double.TryParse(submitted.Value, out var numVal))
                {
                    var outOfRange = (spec.RangeMin.HasValue && numVal < spec.RangeMin.Value)
                        || (spec.RangeMax.HasValue && numVal > spec.RangeMax.Value);
                    if (outOfRange && !string.IsNullOrEmpty(spec.CorrectiveActionText))
                        corrective.Add(new CorrectiveActionDto(spec.Label, spec.CorrectiveActionText));
                }

                if (spec.Type == "Boolean"
                    && submitted.Value.Equals("false", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(spec.CorrectiveActionText))
                {
                    corrective.Add(new CorrectiveActionDto(spec.Label, spec.CorrectiveActionText));
                }
            }
        }

        ValidateSafeTillVariance(items, fieldValues, storeSettings, fieldErrors, corrective);

        return new TaskFieldValidationResult(fieldErrors, corrective);
    }

    private static void ValidateSafeTillVariance(
        List<ChecklistTemplateItem> items,
        List<FieldSubmission> fieldValues,
        StoreSettingsEntity? storeSettings,
        List<ValidationFailure> fieldErrors,
        List<CorrectiveActionDto> corrective)
    {
        var safeItems = items.Where(i => i.Template?.Category == "Safe").ToList();
        if (safeItems.Count == 0 || storeSettings == null) return;

        bool tillVarianceDetected = false;
        foreach (var safeItem in safeItems)
        {
            var safeSpecs = JsonSerializer.Deserialize<List<TemplateFieldSpec>>(safeItem.Template!.FieldsJson, JsonOptions) ?? [];
            foreach (var spec in safeSpecs.Where(s => s.Type == "Numeric"))
            {
                decimal? baseAmount = spec.Id switch
                {
                    "till_a" => storeSettings.TillABase,
                    "till_b" => storeSettings.TillBBase,
                    _ => null
                };
                if (baseAmount == null) continue;

                var sub = fieldValues.FirstOrDefault(f => f.TemplateId == safeItem.TemplateId && f.FieldId == spec.Id);
                if (sub == null || !double.TryParse(sub.Value, out var subVal)) continue;

                if (Math.Abs(subVal - (double)baseAmount.Value) > 0.01)
                {
                    tillVarianceDetected = true;
                    if (!corrective.Any(c => c.FieldLabel == spec.Label))
                        corrective.Add(new CorrectiveActionDto(spec.Label, spec.CorrectiveActionText ?? "Variance detected"));
                }
            }

            if (tillVarianceDetected)
            {
                var noteKey = fieldValues.FirstOrDefault(f => f.TemplateId == safeItem.TemplateId && f.FieldId == "variance_note");
                var initialsKey = fieldValues.FirstOrDefault(f => f.TemplateId == safeItem.TemplateId && f.FieldId == "manager_initials");
                if (string.IsNullOrWhiteSpace(noteKey?.Value))
                    fieldErrors.Add(new ValidationFailure("FieldValues[variance_note]", "Variance Reason is required when till amounts deviate from the base."));
                if (string.IsNullOrWhiteSpace(initialsKey?.Value))
                    fieldErrors.Add(new ValidationFailure("FieldValues[manager_initials]", "Manager Initials are required when till amounts deviate from the base."));
            }
        }
    }

    internal sealed class TemplateFieldSpec
    {
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public string Label { get; init; } = "";
        public bool Required { get; init; }
        public double? RangeMin { get; init; }
        public double? RangeMax { get; init; }
        public string? CorrectiveActionText { get; init; }
        public List<SubItemSpec>? SubItems { get; init; }
    }

    internal sealed class SubItemSpec
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public bool Required { get; init; }
    }
}
