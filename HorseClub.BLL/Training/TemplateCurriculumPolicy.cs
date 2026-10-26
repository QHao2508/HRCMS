using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Training;

public static class TemplateCurriculumPolicy
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public static List<TrainingType> RequiredTypes(TrainingTemplate template) => string.IsNullOrWhiteSpace(template.RequiredTrainingTypesJson) ? [] : JsonSerializer.Deserialize<List<TrainingType>>(template.RequiredTrainingTypesJson, Json) ?? [];
    public static TemplateCurriculum? Read(TrainingTemplate template) => string.IsNullOrWhiteSpace(template.CurriculumJson) ? null : JsonSerializer.Deserialize<TemplateCurriculum>(template.CurriculumJson, Json);
    public static bool Matches(TrainingTemplate template, IEnumerable<TrainerExpertise> skills)
        => RequiredTypes(template).All(type => skills.Any(skill => skill.TrainingType == type && skill.Level >= template.MinimumExpertiseLevel));
    public static void Validate(TemplateRequest request)
    {
        if (request.Curriculum is null && request.RequiredTrainingTypes is null) return; // Preserve legacy requests.
        var types = request.RequiredTrainingTypes;
        var curriculum = request.Curriculum;
        bool Text(string? value, int max = 1000) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
        Ensure.That(types is { Count: > 0 } && types.Count <= Enum.GetValues<TrainingType>().Length && types.All(Enum.IsDefined)
            && types.Distinct().Count() == types.Count && request.MinimumExpertiseLevel is >= 1 and <= 3
            && curriculum?.PlanOptions is { Count: > 0 and <= 20 } && curriculum.SessionOptions is { Count: > 0 and <= 30 }, Messages.Text(MessageKey.InvalidTemplateCurriculum));
        Ensure.That(curriculum!.PlanOptions.All(p => p is not null && Text(p.Key, 100) && Text(p.Label, 200) && Text(p.Goal) && Text(p.Phase, 100))
            && curriculum.PlanOptions.Select(p => p.Key).Distinct().Count() == curriculum.PlanOptions.Count
            && curriculum.SessionOptions.All(s => s is not null && Text(s.Key, 100) && Text(s.Label, 200) && Text(s.Target) && Text(s.Surface, 200)
                && types!.Contains(s.TrainingType) && Enum.IsDefined(s.Intensity) && s.DistanceMetres is > 0 and <= 100000 && s.DurationMinutes is >= 1 and <= 480)
            && curriculum.SessionOptions.Select(s => s.Key).Distinct().Count() == curriculum.SessionOptions.Count, Messages.Text(MessageKey.InvalidTemplateCurriculum));
        Ensure.That(JsonSerializer.Serialize(curriculum, Json).Length <= 16000, Messages.Text(MessageKey.InvalidTemplateCurriculum));
    }
    public static void ValidatePlan(TrainingTemplate template, PlanRequest request)
    {
        var curriculum = Read(template); if (curriculum is null) return;
        if (request.UseCustomPlan) { Ensure.That(request.TemplatePlanOptionKey is null, Messages.Text(MessageKey.ChooseTemplatePlanOption)); return; }
        var option = curriculum.PlanOptions.FirstOrDefault(o => o.Key == request.TemplatePlanOptionKey);
        Ensure.That(option is not null && option.Goal == request.Goal && option.Phase == request.Phase, Messages.Text(MessageKey.ChooseTemplatePlanOption));
    }
    public static void ValidateSession(TrainingTemplate template, SessionRequest request)
    {
        var curriculum = Read(template); if (curriculum is null) return;
        Ensure.That(RequiredTypes(template).Contains(request.TrainingType), Messages.Text(MessageKey.TrainerSpecialtyMismatch), 409, "trainer_specialty_mismatch");
        if (request.UseCustomExercise) { Ensure.That(request.TemplateExerciseKey is null, Messages.Text(MessageKey.ChooseTemplateSessionOption)); return; }
        var option = curriculum.SessionOptions.FirstOrDefault(o => o.Key == request.TemplateExerciseKey);
        Ensure.That(option is not null && option.TrainingType == request.TrainingType && option.DistanceMetres == request.DistanceMetres
            && option.Intensity == request.Intensity && option.Surface == request.Surface && option.DurationMinutes == request.DurationMinutes && option.Target == request.Target,
            Messages.Text(MessageKey.ChooseTemplateSessionOption));
    }
}
