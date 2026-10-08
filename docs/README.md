# Tài liệu HRCMS

Cập nhật 08/10/2026. Tài liệu hiện hành nằm bên dưới; kế hoạch và snapshot cũ nằm trong [archive](archive/README.md).

## Báo cáo và kiến trúc

- [Bảng cấu trúc, luồng nghiệp vụ và trả lời vấn đáp](BAO_CAO_CAU_TRUC_VA_LUONG_NGHIEP_VU.md).
- [Kiến trúc ba layer](ARCHITECTURE.md), [chức năng các folder](FOLDERS.md), [layer và message](THREE_LAYER_AND_MESSAGES.md).
- [Interface, repository, Unit of Work và SignalR: triển khai và kiểm thử](REPOSITORY_SIGNALR_IMPLEMENTATION.md).

## Cấu hình và kiểm thử

- [Chạy backend](BACKEND_GUIDE.md), [Azure SQL](AZURE_SQL_SETUP.md), [schema SQL](DATABASE_SQL.md), [kiểm thử SQL Server](SQLSERVER_TESTING.md).
- [Email và Azure Blob](EMAIL_AND_AZURE_STORAGE.md), [Gmail User Secrets](GMAIL_USER_SECRETS.md), [test OTP](EMAIL_OTP_TEST_GUIDE.md).
- [Storage và recovery](STORAGE_AND_RECOVERY.md), [ảnh ngựa](HORSE_PHOTOS.md), [branding](BRANDING.md).
- [Nhân viên mẫu](STAFF_SAMPLE_DATA.md), [demo BE02](BE02_DEMO_SETUP.md), [GitHub](GITHUB_SETUP.md).

## API và nghiệp vụ

- [API contract](API_CONTRACT.md), [intake và phân công](BE-003_INTAKE_AND_ASSIGNMENT.md), [training](BE-004_TRAINING.md).
- [Policy baseline](T02_POLICY_BASELINE.md), [đăng ký 24 giờ](PENDING_REGISTRATION.md), [email worker](WORKER_RELIABILITY.md).

## Dữ liệu hỗ trợ

| Vị trí | Nội dung |
|---|---|
| `contracts/` | OpenAPI, endpoint inventory và audit; baseline phục vụ đối chiếu tương thích. |
| `demo/` | Dữ liệu tài khoản mà công cụ seed tham chiếu. |
| `http/` | Request mẫu intake và training. |
| `azure-sql-schema.sql` | Schema hiện hành, đủ 7 migration. |
| `database.sql` | Schema cũ gồm 3 migration; giữ để tra lịch sử. |
| `Export-ContractInventory.ps1` | Công cụ xuất contract inventory. |
| `archive/` | Kế hoạch, báo cáo và function reference trước refactor. |

Ưu tiên source và kiểm thử khi tài liệu lịch sử khác hành vi hiện tại. Không đưa mật khẩu hoặc token vào tài liệu.
