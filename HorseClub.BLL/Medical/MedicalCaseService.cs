using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Medical;

public sealed partial class MedicalService
{
    private static readonly JsonSerializerOptions SnapshotOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
    private static MedicalVisitSnapshot Snapshot(MedicalRecord record) => new(record.ExaminationAt, record.Reason, record.Symptoms,
        record.Findings, record.Diagnosis, record.HealthStatus, record.Notes, record.VeterinarianId);
    private static string SnapshotJson(MedicalRecord record) => JsonSerializer.Serialize(Snapshot(record), SnapshotOptions);
    private static MedicalRecord CaseHead(HashSet<Guid> ids, List<MedicalRecord> records)
    {
        var replaced = records.Where(x => x.SupersedesRecordId.HasValue).Select(x => x.SupersedesRecordId!.Value).ToHashSet();
        return records.Where(x => ids.Contains(x.Id) && !replaced.Contains(x.Id)).OrderByDescending(x => x.ExaminationAt)
            .ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).First();
    }

    /// <summary>Vet explicitly joins duplicate records of the same episode; no clinical rows are deleted.</summary>
    public async Task<MedicalRecord> MergeCases(Guid horseId, Guid id, MergeMedicalCaseRequest request)
    {
        await access.Vet(horseId);
        Ensure.That(request.RecordIds is { Length: > 0 and <= 25 } && request.RecordIds.Distinct().Count() == request.RecordIds.Length
            && !string.IsNullOrWhiteSpace(request.Reason), Messages.Get(MessageKey.InvalidRequestBody));
        var records = await repository.GetTrackedRecordsAsync(horseId);
        var target = Ensure.Found(records.FirstOrDefault(x => x.Id == id));
        Ensure.That(request.RecordIds.All(recordId => records.Any(x => x.Id == recordId)), Messages.Get(MessageKey.MedicalRecordDoesNotBelongToThisHorse));
        var followUps = await repository.GetFollowUpsAsync(horseId);
        var ids = CaseIds(target.Id, records, followUps);
        var resolved = followUps.Any(x => x.Clearance && (ids.Contains(x.PreviousRecordId) || ids.Contains(x.CurrentRecordId)));
        foreach (var other in request.RecordIds)
        {
            var otherIds = CaseIds(other, records, followUps);
            // Different resolved/open episodes must not become one case through an accidental merge.
            Ensure.That(resolved == followUps.Any(x => x.Clearance && (otherIds.Contains(x.PreviousRecordId) || otherIds.Contains(x.CurrentRecordId))),
                Messages.Get(MessageKey.InvalidRequestBody));
            ids.UnionWith(otherIds);
        }
        foreach (var record in records.Where(x => ids.Contains(x.Id)))
        {
            if (string.IsNullOrEmpty(record.InitialSnapshotJson)) record.InitialSnapshotJson = await RecoverInitialSnapshot(record, followUps);
            record.CaseRootId = target.Id;
        }
        await events.Audit(AuditAction.MedicalRecordCorrected, target.Id,
            JsonSerializer.Serialize(new { Operation = "MergeMedicalCases", SourceRecordIds = request.RecordIds, MergedRecordCount = ids.Count, Reason = request.Reason.Trim() }));
        await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync();
        return CaseHead(ids, records);
    }

    /// <summary>Clinical history, including old linked records and snapshots retained before in-place follow-ups.</summary>
    public async Task<MedicalCaseHistoryResponse> GetCaseHistory(Guid horseId, Guid id)
    {
        await access.MedicalDetails(horseId);
        var records = await repository.GetRecordsAsync(horseId);
        Ensure.Found(records.FirstOrDefault(x => x.Id == id));
        var followUps = await repository.GetFollowUpsAsync(horseId);
        var ids = CaseIds(id, records, followUps);
        var visits = followUps.Where(x => ids.Contains(x.PreviousRecordId) || ids.Contains(x.CurrentRecordId)).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToList();
        var audits = await repository.GetCaseAuditsAsync(visits.Select(x => x.Id));
        var result = new List<MedicalCaseVisitResponse>();
        foreach (var record in records.Where(x => ids.Contains(x.Id)))
        {
            if (visits.Any(x => x.CurrentRecordId == record.Id && x.PreviousRecordId != record.Id)) continue;
            var firstVisit = visits.FirstOrDefault(x => x.PreviousRecordId == record.Id && x.CurrentRecordId == record.Id);
            var initial = ReadSnapshot(record.InitialSnapshotJson) ?? BeforeSnapshot(firstVisit, audits, record) ?? Snapshot(record);
            result.Add(new(record.Id, record.Id, record.SupersedesRecordId.HasValue ? "Correction" : "Examination", initial, "", false, record.CreatedAt));
        }
        for (var i = 0; i < visits.Count; i++)
        {
            var visit = visits[i];
            var record = records.FirstOrDefault(x => x.Id == visit.CurrentRecordId);
            if (record is null) continue;
            var next = visits.Skip(i + 1).FirstOrDefault(x => x.PreviousRecordId == visit.CurrentRecordId && x.CurrentRecordId == visit.CurrentRecordId);
            var assessment = ReadSnapshot(visit.CurrentSnapshotJson) ?? BeforeSnapshot(next, audits, record) ?? Snapshot(record);
            result.Add(new(visit.Id, record.Id, "FollowUp", assessment, visit.Outcome, visit.Clearance, visit.CreatedAt));
        }
        return new(CaseHead(ids, records).Id, ids.ToArray(), result.OrderBy(x => x.Examination.ExaminationAt).ThenBy(x => x.RecordedAt).ToArray());
    }

    private async Task<string> RecoverInitialSnapshot(MedicalRecord record, List<MedicalFollowUp> followUps)
    {
        var visits = followUps.Where(x => x.PreviousRecordId == record.Id && x.CurrentRecordId == record.Id).OrderBy(x => x.CreatedAt).ToList();
        var audits = await repository.GetCaseAuditsAsync(visits.Select(x => x.Id));
        return JsonSerializer.Serialize(BeforeSnapshot(visits.FirstOrDefault(), audits, record) ?? Snapshot(record), SnapshotOptions);
    }

    private static MedicalVisitSnapshot? ReadSnapshot(string? json) => string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<MedicalVisitSnapshot>(json, SnapshotOptions);
    private static MedicalVisitSnapshot? BeforeSnapshot(MedicalFollowUp? visit, List<AuditEvent> audits, MedicalRecord fallback)
    {
        if (visit is null) return null;
        var stored = ReadSnapshot(visit.PreviousSnapshotJson);
        if (stored is not null) return stored;
        foreach (var audit in audits.Where(x => x.ReferenceId == visit.Id && !string.IsNullOrEmpty(x.Detail)))
        {
            using var document = JsonDocument.Parse(audit.Detail);
            if (!document.RootElement.TryGetProperty("Before", out var before)) continue;
            var json = before.ValueKind == JsonValueKind.String ? before.GetString() : before.GetRawText();
            var snapshot = ReadSnapshot(json);
            if (snapshot is not null) return snapshot with { Reason = snapshot.Reason ?? fallback.Reason, VeterinarianId = snapshot.VeterinarianId == Guid.Empty ? fallback.VeterinarianId : snapshot.VeterinarianId };
        }
        return null;
    }
}
