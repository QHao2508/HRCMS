# Horse identifiers

## Canonical API fields

- `Horse.systemRegistrationCode`: club-issued, unique horse code (`HRC-000001`), allocated on approval. Clients cannot supply or change it. `Horse.id` remains the UUID used in routes and foreign keys.
- `Horse.externalRegistrationNumber`: optional document number from the approved registration. Returned on horse list and detail, without exposing the remainder of the registration. Never falls back to the system code.
- `HorseRegistration.externalRegistrationNumber`: optional document number supplied by the owner. Available on draft/create/edit/list/detail/review responses.
- `RegistrationDraftRequest.externalRegistrationNumber`: accepted on POST/PUT registrations. Max 100 characters, trimmed, blank becomes null. Leading zeroes and punctuation are preserved. Owner PUT remains replacement semantics.

The system code is allocated when approval creates a horse, not when saving a draft. Document numbers are not globally unique: different issuing authorities may use the same number. All reads/searches retain existing role/ownership/assignment scope.

## Compatibility

`registrationNumber` is deprecated, but remains supported:

| Object | Meaning of legacy registrationNumber |
| --- | --- |
| Horse | systemRegistrationCode |
| HorseRegistration / registration request | externalRegistrationNumber |

When two non-null request aliases are provided, their normalized values must match. A mismatch returns HTTP 400, title `registration_number_conflict`, messageKey `Invalid` with the field names. Supplying `systemRegistrationCode` on a registration request is rejected by the existing strict request-body contract.

Manager administrative edits retain existing omitted/null-field preservation. An explicitly blank string clears a document number. Owner PUT clears it with null/blank or omission.

New frontend forms send only the canonical document-number field. Read compatibility with legacy responses is limited to the same object type; arbitrary `identifier`/UUID and document numbers are never used as fallback system codes. Explicit canonical null takes priority over a legacy alias.

Search `/api/horses?search=...` matches horse name, system code and document number within the caller's existing scope. Search terms containing `/`, `+` or other reserved URL characters must be URL-encoded by the client.

## Persistence and release

Existing columns remain unchanged: `Horses.RegistrationNumber` stores the system code; `Registrations.RegistrationNumber` stores the document number. The new response properties are not mapped by EF. No database migration or copying of document numbers onto Horse is needed for this release. The sequence and unique index remain unchanged.

Run `tools/Audit-HorseRegistrationIdentifiers.sql` read-only against each target database before release. Never infer provenance solely from the `HRC-` prefix. Retain ambiguous legacy values and resolve them against registration/audit records before any repair. This change does not renumber existing horses.

Local audit on 28/10/2026: `.\SQLEXPRESS / HRCMS` contains 0 horses; no local data correction is needed. No Azure data was inspected or changed during implementation.

Release backend first, then frontend. Backend continues serving both aliases so the existing frontend works during rollout. Frontend must not be deployed ahead of backend because requests use the new field name.
