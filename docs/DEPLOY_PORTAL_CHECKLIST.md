# Checklist: Static Web Apps Free + Container Apps Consumption

## Đã chuẩn bị local

- Dockerfile BE .NET 10, HTTP 8080, không nhúng secrets.
- Workflow GHCR riêng tư: `.github/workflows/publish-container.yml`, chạy thủ công trên main.
- `deploy/containerapp.example.yaml`: ingress, probes, 0.25 vCPU/0.5 GiB, min=max=1, Azure Files keys mount.
- `deploy/azure-appsettings.local.json`: cấu hình riêng tư đã gitignore; domain BE/FE cần điền sau khi tạo app.
- Certificate/key ring đã mã hóa nằm ngoài repo; đường dẫn bộ hiện tại trong `%LOCALAPPDATA%\HRCMS\deployment\latest-path.txt`.
- FE có workflow Static Web Apps, SPA fallback; không cần Docker FE.

## Bạn thực hiện theo thứ tự

1. Kiểm credit/spending limit, SQL free offer và chi phí SmartCinema; không tạo B1/ACR.
2. Review rồi commit/push các sửa đổi BE và FE. Chưa có commit/push tự động.
3. Khi Backend CI đạt, chạy workflow Publish backend image to GHCR; giữ package private, ghi tag SHA.
4. Tạo Azure Files share riêng và upload XML keys đã mã hóa. Tạo environment HRCMS Consumption, đăng ký share.
5. Tạo Container App `hrcms-api`, image GHCR private; thêm secrets, mount `/mnt/hrcms-keys`, env/probes theo mẫu.
6. Bật Managed Identity cho Blob, cấu hình SQL network, kiểm `/health` và OTP.
7. Tạo FE Static Web Apps Free, nhập token/URL BE trong GitHub, chạy workflow deploy FE.
8. Cập nhật CORS BE bằng domain FE thật; chạy test workflow/API/upload/refresh và restart BE.
9. Thu hẹp SQL firewall sau khi xác minh kết nối. Theo dõi chi phí; Stop BE khi không dùng, Start trước demo.

Đã chuẩn bị cấu hình, chưa tạo tài nguyên/publish hoặc thay database/firewall.
Không bảo đảm phí 0: SQL, Azure Files/Blob, logs và mức vượt free grant vẫn có thể trừ credit Students.
Hướng dẫn đầy đủ: [DEPLOY_AZURE.md](DEPLOY_AZURE.md).
