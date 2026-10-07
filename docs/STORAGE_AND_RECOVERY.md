# Lưu file, bảo vệ khóa và khôi phục SQL Server

Dự án chỉ dùng SQL Server. Upload, rollback, giới hạn đường dẫn và certificate key protection được kiểm bằng integration tests. Native SQL backup/restore test khôi phục database mới rồi đọc record/file qua API; uploads/keys của drill dùng cùng thư mục nguồn, chưa chứng minh chuyển toàn bộ sang máy khác.

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

## SQL Server: runbook

1. Dừng API/workers; chốt cấu hình, migration, database và service identity. Dùng tài khoản backup có quyền phù hợp; SQL Server service account phải truy cập được đường dẫn backup.
2. BACKUP DATABASE database-nguồn TO DISK=đường-dẫn-mới WITH COPY_ONLY, CHECKSUM. Với recovery model FULL, DBA vẫn cần lịch full/differential/log backup và mục tiêu RPO/RTO; COPY_ONLY ở đây là snapshot/drill, không thay thế chiến lược đó.
3. Cùng thời điểm sao chép uploads và keys, giữ cấu trúc/tên file. Lưu certificate/password riêng; ghi manifest/hash, phiên bản app/schema, UTC thời điểm backup và cấu hình cần thiết. Hoàn tất bộ backup trước bật lại writer.
4. RESTORE VERIFYONLY WITH CHECKSUM; đọc RESTORE FILELISTONLY để lấy logical file names. RESTORE vào **database mới** với MOVE sang data/log path mới. Không dùng WITH REPLACE trên DB đang chạy.
5. Khôi phục bản sao uploads/keys sang thư mục mới, cấp ACL; cấu hình host drill kết nối DB mới và đường dẫn mới, workers tắt. Kiểm /health, đăng nhập, quyền tải file, file bytes và giải mã dữ liệu được bảo vệ. VERIFYONLY không thay thế thử restore/API.
6. Chỉ chuyển traffic sau khi kiểm chứng. Giữ bộ nguồn để rollback và ghi thời gian restore thực tế. Không đổi kết nối production chỉ vì một drill local pass.

Native SQL drill tự tạo/dọn database HRCMS_Test_<guid> và .bak HRCMS_Recovery_<guid>; không reset HorseClub_IntegrationTest hoặc DB cá nhân.

## Công việc còn lại

Kill-process đúng giữa file write/DB commit vẫn có thể để lại orphan. Chưa có background garbage collector: đối soát StorageName của Attachments và IncidentPhotos, chỉ xóa sau kiểm tra DB chắc chắn và khoảng chờ do vận hành quy định. Chưa tự xóa outbox/challenge/audit hoặc file theo retention policy chưa được xác nhận. Chưa có antivirus/full content validation. Chưa có offsite scheduler, backup encryption, certificate rotation hoặc chứng cứ restore sang máy production khác.

Tham khảo: [Data Protection encryption at rest](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest), [key storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0).
