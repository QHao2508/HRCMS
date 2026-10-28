-- Read-only audit. Run against each target environment before a release.
-- Format is a review signal, not proof of origin; never rewrite codes based only on HRC-.
SET NOCOUNT ON;

SELECT COUNT(*) AS HorseCount,
    COALESCE(SUM(CASE WHEN h.RegistrationNumber IS NULL OR LTRIM(RTRIM(h.RegistrationNumber)) = N'' THEN 1 ELSE 0 END), 0) AS MissingSystemCode,
    COALESCE(SUM(CASE WHEN h.RegistrationNumber IS NOT NULL AND h.RegistrationNumber NOT LIKE N'HRC-%' THEN 1 ELSE 0 END), 0) AS LegacyCodeToReview,
    COALESCE(SUM(CASE WHEN r.RegistrationNumber IS NOT NULL AND LTRIM(RTRIM(r.RegistrationNumber)) <> N'' THEN 1 ELSE 0 END), 0) AS WithDocumentNumber
FROM Horses h LEFT JOIN Registrations r ON r.Id = h.RegistrationId;

SELECT h.Id AS HorseId, h.RegistrationId, h.RegistrationNumber AS SystemRegistrationCode,
    r.RegistrationNumber AS ExternalRegistrationNumber,
    CASE WHEN r.Id IS NULL THEN N'Missing registration'
         WHEN h.RegistrationNumber IS NULL OR LTRIM(RTRIM(h.RegistrationNumber)) = N'' THEN N'Missing system code'
         WHEN h.RegistrationNumber = r.RegistrationNumber THEN N'Equal values: verify provenance'
         ELSE N'Legacy system code: verify provenance' END AS ReviewReason
FROM Horses h LEFT JOIN Registrations r ON r.Id = h.RegistrationId
WHERE r.Id IS NULL OR h.RegistrationNumber IS NULL OR LTRIM(RTRIM(h.RegistrationNumber)) = N''
    OR h.RegistrationNumber NOT LIKE N'HRC-%' OR h.RegistrationNumber = r.RegistrationNumber;

SELECT RegistrationNumber AS DuplicateSystemCode, COUNT(*) AS HorseCount
FROM Horses WHERE RegistrationNumber IS NOT NULL
GROUP BY RegistrationNumber HAVING COUNT(*) > 1;
