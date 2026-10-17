using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by HorseRegistrationService.</summary>
public interface IHorseRegistrationService
{
    Task<HorseRegistration> Create(RegistrationDraftRequest r);
    Task<HorseRegistration> Edit(Guid id, RegistrationDraftRequest r);
    Task Submit(Guid id);
    Task<RegistrationReviewResponse> Review(Guid id, ReviewRequest request);
    Task<OperationResult> CreateDraft(RegistrationDraftRequest request);
    Task<PageResponse<HorseRegistration>> ListRegistrations(RegistrationStatus? status, int? page, int? pageSize);
    Task<HorseRegistration> GetRegistration(Guid id);
    Task<HorseRegistration> UpdateDraft(Guid id, RegistrationDraftRequest request);
    Task<OperationResult> SubmitRegistration(Guid id);
    Task<RegistrationReviewResponse> ReviewRegistration(Guid id, ReviewRequest request);
    Task<OperationResult> CancelRegistration(Guid id);
}
