# Lưu file, bảo vệ khóa và khôi phục — bước 6

Ngày kiểm chứng: 04/10/2026, .NET 10, SQLite và SQL Server Express local. Đây là kiểm chứng backend; chưa nghiệm thu frontend hoặc production.

## Thay đổi và kết quả

- Upload dùng chung UploadStorage, tên lưu là GUID 32 ký tự hex; không cho traversal hoặc symlink/junction, không ghi đè file đã tồn tại. Download tiếp tục kiểm quyền, trả 404 nếu thiếu file, force download và có nosniff.
- File mới được theo dõi trong request. Khi transaction rollback/cancel, cleanup chỉ xóa file chưa được DB tham chiếu. Nếu trạng thái DB không xác định hoặc không truy cập được, giữ file và log để đối soát, tránh xóa file đã commit.
- Cấu hình Data Protection được đọc khi khởi tạo service, nên host override đúng đường dẫn keys. Có None, WindowsDpapi và Certificate; application name giữ nguyên HorseClub.
- Thêm CLI SQLite backup/verify/restore đồng bộ database, uploads và keys. Backup dùng SQLite Backup API, chuyển snapshot sang journal DELETE để không phụ thuộc WAL sidecar. Manifest SHA-256/length, integrity_check, foreign_key_check và tham chiếu file được kiểm trước restore. Không ghi đè destination đã tồn tại.
- Thêm 10 test. Toàn suite 61 ca: SQL Server 60 pass/1 SQLite-only skip; SQLite 48 pass/13 SQL-only skip. Build Release 0 warning/0 error.

CLI cũng đã chạy đủ backup-sqlite → verify-sqlite → restore-sqlite trên fixture test; artifact ở TestResults/step6/cli (Git ignored). Sau drill không còn database HRCMS_Test_% trên instance local; HorseClub_IntegrationTest của bước 1 vẫn tồn tại. Các ca storage SQLite được chạy lại 7/7 pass sau kiểm tra link cho đường dẫn đích.

Test bao gồm download/scope/missing file/traversal, rollback do SQL trigger cho cả attachment và incident photo, file write cancellation, phát hiện bundle hỏng, certificate qua host mới và mất certificate. SQLite được khôi phục cả bộ dữ liệu sang thư mục mới, đăng nhập qua API, đọc file và giải mã NationalId. SQL Server được BACKUP COPY_ONLY/CHECKSUM, VERIFYONLY và RESTORE vào database test mới, sau đó đọc record/file qua API; phần uploads/keys của drill SQL dùng cùng thư mục nguồn, chưa phải drill sao chép toàn bộ sang máy khác.

## Cấu hình khóa

Development mặc định None. Ngoài Development, mặc định RequireEncryptedKeys=true; host từ chối chạy None. Cần cấu hình trước khi triển khai:

```powershell
$env:Storage__Path='D:\HRCMSData\uploads'
$env:DataProtection__Path='D:\HRCMSData\keys'
$env:DataProtection__KeyEncryption='Certificate'
$env:DataProtection__CertificatePath='D:\HRCMSSecrets\key-protection.pfx'
# Cấp DataProtection__CertificatePassword qua secret store của môi trường chạy.
```

Certificate phải có private key. PFX/password không đưa vào Git, bundle hoặc command history; sao lưu riêng trong kho bảo mật và kiểm khả năng cấp lại khi restore. Giữ certificate cũ trong khi keys cũ còn cần dùng. Phiên bản hiện tại chỉ nạp một certificate từ cấu hình, chưa hỗ trợ danh sách certificate cho rotation; phải chuẩn bị hỗ trợ migration/rotation trước khi đổi certificate.

WindowsDpapi dùng identity Windows hiện tại. Khôi phục sang máy hoặc identity khác không được đảm bảo; Certificate phù hợp hơn nếu cần di chuyển. Chọn None với RequireEncryptedKeys=false là opt-out rõ ràng, không phù hợp baseline production. Bật encryption chỉ bảo vệ keys được tạo mới; không tự chuyển các XML key plaintext đã có. Không xóa key ring cũ: NationalId đã lưu và dữ liệu Data Protection còn cần khóa tương ứng.

Giới hạn ACL cho thư mục dữ liệu/keys/certificate, chỉ cấp service account và người vận hành cần thiết. Việc chặn link ở code không thay thế ACL; tránh cho process khác thay đổi đường dẫn giữa lúc kiểm tra và mở file.

## SQLite: backup và restore

Chốt thời điểm bảo trì và **dừng tất cả API/worker ghi dữ liệu**. Cờ --offline là xác nhận của người vận hành, CLI không tự phát hiện hoặc dừng process. SQLite snapshot nhất quán cho DB nhưng việc sao chép uploads/keys cần cùng thời điểm không có writer.

Build CLI từ solution, sau đó chạy với đường dẫn thực tế và destination chưa tồn tại:

```powershell
dotnet build HorseClub.slnx -c Release
dotnet run --project tools/HorseClub.Maintenance -c Release --no-build -- backup-sqlite 'D:\HRCMSData\horseclub.db' 'D:\HRCMSData\uploads' 'D:\HRCMSData\keys' 'D:\HRCMSBackups\snapshot-20261004' --offline
dotnet run --project tools/HorseClub.Maintenance -c Release --no-build -- verify-sqlite 'D:\HRCMSBackups\snapshot-20261004'
dotnet run --project tools/HorseClub.Maintenance -c Release --no-build -- restore-sqlite 'D:\HRCMSBackups\snapshot-20261004' 'D:\HRCMSRestore\drill-20261004'
```

Restore tạo database.db, uploads, keys. Cấu hình host kiểm chứng sang các đường dẫn đó bằng Database:Provider=Sqlite, ConnectionStrings:Sqlite (Data Source=...database.db), Storage:Path và DataProtection:Path. Giữ HorseClub application name và certificate/DPAPI tương ứng. Kiểm /health, đăng nhập, quyền download, file bytes, dữ liệu cá nhân, notification và migration trước chuyển traffic. Không khởi chạy worker gửi email thật trong drill. Không trộn một DB backup với files/keys của thời điểm khác.

Bundle có dữ liệu nhạy cảm; SHA-256 phát hiện lỗi file, không phải chữ ký xác thực hay mã hóa backup. Lưu trong kho backup được mã hóa, kiểm soát quyền và retention. Backup thất bại có thể để lại thư mục dở dang; không dùng nó nếu verify chưa pass. Không có lệnh xóa hoặc overwrite tự động.

## SQL Server: runbook

1. Dừng API/workers; chốt cấu hình, migration, database và service identity. Dùng tài khoản backup có quyền phù hợp; SQL Server service account phải truy cập được đường dẫn backup.
2. BACKUP DATABASE database-nguồn TO DISK=đường-dẫn-mới WITH COPY_ONLY, CHECKSUM. Với recovery model FULL, DBA vẫn cần lịch full/differential/log backup và mục tiêu RPO/RTO; COPY_ONLY ở đây là snapshot/drill, không thay thế chiến lược đó.
3. Cùng thời điểm sao chép uploads và keys, giữ cấu trúc/tên file. Lưu certificate/password riêng; ghi manifest/hash, phiên bản app/schema, UTC thời điểm backup và cấu hình cần thiết. Hoàn tất bộ backup trước bật lại writer.
4. RESTORE VERIFYONLY WITH CHECKSUM; đọc RESTORE FILELISTONLY để lấy logical file names. RESTORE vào **database mới** với MOVE sang data/log path mới. Không dùng WITH REPLACE trên DB đang chạy.
5. Khôi phục bản sao uploads/keys sang thư mục mới, cấp ACL; cấu hình host drill kết nối DB mới và đường dẫn mới, workers tắt. Kiểm dữ liệu và quyền như SQLite. VERIFYONLY không thay thế thử restore/API.
6. Chỉ chuyển traffic sau khi kiểm chứng. Giữ bộ nguồn để rollback và ghi thời gian restore thực tế. Không đổi kết nối production chỉ vì một drill local pass.

Native SQL drill tự tạo/dọn database HRCMS_Test_<guid> và .bak HRCMS_Recovery_<guid>; không reset HorseClub_IntegrationTest hoặc DB cá nhân.

## Công việc còn lại

Kill-process đúng giữa file write/DB commit vẫn có thể để lại orphan. Chưa có background garbage collector: đối soát StorageName của Attachments và IncidentPhotos, chỉ xóa sau kiểm tra DB chắc chắn và khoảng chờ do vận hành quy định. Chưa tự xóa outbox/challenge/audit hoặc file theo retention policy chưa được xác nhận. Chưa có antivirus/full content validation. Chưa có offsite scheduler, backup encryption của CLI, certificate rotation hoặc chứng cứ restore sang máy production khác.

Tham khảo: [Data Protection encryption at rest](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest), [key storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0), [SQLite Backup API](https://www.sqlite.org/backup.html).
