# Dữ liệu tham chiếu thật đã lưu vào Azure SQL

Ngày 13/10/2026, theo yêu cầu người dùng: thêm migration `20261013062223_PublicReferenceData`, tạo `dbo.PublicReferenceRecords` trên `hrcms.database.windows.net / HRCMS`. Schema hiện tại: 37 bảng ứng dụng, 11 migration.

Đã commit transaction nhập 13 bản ghi và đọc lại bằng connection mới để kiểm tra số lượng và SHA-256:

| Kind | Số bản ghi |
|---|---:|
| VeterinaryObservation | 8 |
| Product | 4 |
| Facility | 1 |

Mỗi bản ghi chứa `DisplayName`, `SourceUrl`, `CollectedOn`, `SourceAsOf` khi có, `SourceMetadataJson`, `PayloadJson`, `ContentSha256`. `ExternalKey` duy nhất gồm nguồn, loại, danh tính bên ngoài và ngày thu thập. Lưu JSON để bảo toàn null, số đo, cấu hình cơ sở và trường ngày của nguồn. Không có foreign key đến ngựa hoặc nhân viên CLB.

Những dữ liệu này là thư viện tham chiếu trong database. Các trang chăm sóc, bệnh án, kho và chuồng hiện tại vẫn đọc bảng nghiệp vụ của CLB; chưa có giao diện đọc bảng tham chiếu mới. Tồn kho/giá/liều chưa biết vẫn null; không tạo phiếu nhập/xuất, occupancy, bệnh án hoặc task giả.

## Truy vấn

```sql
SELECT Kind, COUNT(*) AS RecordCount
FROM dbo.PublicReferenceRecords
GROUP BY Kind;

SELECT DisplayName, SourceUrl, CollectedOn, SourceAsOf, PayloadJson
FROM dbo.PublicReferenceRecords
ORDER BY Kind, DisplayName;

SELECT DisplayName,
       JSON_VALUE(PayloadJson, '$.horseCode') AS HorseCode,
       JSON_VALUE(PayloadJson, '$.observedDate') AS ObservedDate,
       JSON_VALUE(PayloadJson, '$.summaryVi') AS Observation,
       JSON_VALUE(PayloadJson, '$.sourcePassedOnDate') AS SourcePassedOnDate
FROM dbo.PublicReferenceRecords
WHERE Kind = 'VeterinaryObservation';
```

Không chuyển `sourcePassedOnDate` thành clearance của HRCMS. Ngày thu thập là 13/10; riêng snapshot thú y đã mở có ngày nguồn 10/10, được lưu riêng.

## Chạy lại

Từ repository BE:

```powershell
dotnet build tools/HorseClub.ReferenceImport/HorseClub.ReferenceImport.csproj -c Release
# Chỉ kiểm tra JSON, không kết nối hoặc ghi database:
dotnet tools/HorseClub.ReferenceImport/bin/Release/net10.0/HorseClub.ReferenceImport.dll
# Ghi Azure theo connection đang lưu trong User Secrets:
dotnet tools/HorseClub.ReferenceImport/bin/Release/net10.0/HorseClub.ReferenceImport.dll --apply
```

Importer khóa target Azure HRCMS và yêu cầu TLS hợp lệ. Bỏ qua bản ghi đã có cùng fingerprint; không ghi đè snapshot thay đổi nội dung. Muốn thu thập snapshot mới phải dùng ngày mới. Migration SQL additive tại [PUBLIC_REFERENCE_MIGRATION_2026-10-13.sql](PUBLIC_REFERENCE_MIGRATION_2026-10-13.sql). Chạy bằng script Azure hiện hành; importer không tự migrate database.

Kiểm thử mới: 3 pass trên SQL Server tạm, bao gồm nhập hai lần, nguồn gốc/unknowns, từ chối stock suy đoán và snapshot bị đổi. Toàn bộ regression BE: 173 pass, 1 skip (Azure Blob live chưa bật), 0 fail. Release build: 0 lỗi/cảnh báo. Lần nhập Azure thứ hai trả Inserted=0, Unchanged=13 và kiểm tra lại đủ 13 bản ghi. Không khởi động worker hoặc restart API đang chạy để nhập dữ liệu.
