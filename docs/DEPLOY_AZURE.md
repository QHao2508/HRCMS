# HRCMS: deploy tiết kiệm credit Students

Phương án hiện tại: FE Azure Static Web Apps Free, BE Docker trên Azure Container Apps Consumption,
image riêng tư trên GitHub Container Registry (GHCR), Azure SQL/Blob hiện có.
Không tạo ACR hoặc App Service Plan B1. Chỉ chuẩn bị local; bạn tự tạo tài nguyên trên Portal.

## 0. Cấu hình đang chạy (16/10/2026)

Bản triển khai thực tế đơn giản hơn các mục bên dưới; các mục 3+ là phương án đầy đủ để tham khảo.

| Thành phần | Giá trị |
| --- | --- |
| BE | Container App `hrcms-api` (RG `g2-HRCMS`, Japan East), chạy trong environment dùng chung `smartcinema-env` vì Students giới hạn 1 environment/region |
| URL BE | `https://hrcms-api.ashysand-0e4a958a.japaneast.azurecontainerapps.io` (`/health`) |
| FE | Static Web App `https://orange-ocean-04488bd00.1.azurestaticapps.net` |
| Data Protection | `KeyEncryption=None`, `RequireEncryptedKeys=false`, `Path=/tmp/hrcms-keys`: không dùng Azure Files/certificate; restart BE thì người dùng phải đăng nhập lại |
| Probes | Startup/Liveness/Readiness đều TCP 8080 (HTTP probe bị AllowedHosts trả 400) |
| Scale | min 0 / max 1, HTTP rule 10 concurrent requests: không có truy cập ~5 phút thì về 0 replica, worker ngừng truy vấn nên SQL tự pause sau 1 giờ; lần mở đầu tiên chờ 30–60 giây (cold start BE + SQL resume) |
| Worker | Email/Reminder/AccountCleanup chỉ truy vấn SQL khi có request người dùng trong `Workers:IdleAfterMinutes` (mặc định 10 phút; `/health` không tính). Hết khoảng đó worker ngừng chạm SQL nên SQL tự pause sau 1 giờ dù backend còn chạy (kể cả `dotnet run` ở máy dev) |
| Database | `HRCMS-free` (Free offer, serverless 0.5–1 vCore, auto-pause 1 giờ, hết hạn mức thì Auto-pause tới tháng sau). `HRCMS` cũ hết free amount tháng 10/2026, giữ dữ liệu cũ, chạy lại từ 01/11 |
| Secrets | `sql-connection`, `smtp-password`, secret password GHCR do Portal tạo |

Deploy tự động:

- FE: push `main` của HRCMS-Frontend → test, build với `VITE_API_BASE_URL`, upload Static Web App.
- BE: push `main` (trừ docs/tests/*.md) → workflow `Publish backend image to GHCR` build image `sha-<commit>`,
  đăng nhập Azure bằng OIDC (managed identity `hrcms-github-deploy`, role Contributor trên `hrcms-api`),
  `az containerapp update --image` và chờ revision mới + `/health`. Secrets repo: `AZURE_CLIENT_ID`,
  `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. Rollback: Run workflow trên commit cũ hoặc đổi image tag trên Portal.
- Migration không tự chạy (`AutoMigrate=false`): đặt `$env:HRCMS_SQL_DATABASE = 'HRCMS-free'` rồi chạy `./tools/Update-AzureSqlDatabase.ps1` trước khi push code cần schema mới.
- Tài khoản Quản lý đầu tiên tạo bởi `Bootstrap__ManagerEmail` + secret `Bootstrap__ManagerPassword` (bỏ qua nếu đã có Manager).
- FE: ảnh/link tải từ BE phải dùng `apiUrl('/api/...')`, không viết `'/api/...'` trực tiếp.


## 1. Kiểm tra ngân sách trước

- Trong subscription Azure for Students, kiểm tra credit còn lại và giữ spending limit. Không tự nâng Pay-As-You-Go.
- Xem Cost Management theo resource để biết SQL, storage, logs và SmartCinema đang tiêu thụ bao nhiêu credit.
- Container Apps free grant dùng chung subscription. Consumption không bảo đảm tổng hóa đơn bằng 0.
- SQL HRCMS hiện là serverless: kiểm tra free offer trong Portal; nếu được hỗ trợ, xem tùy chọn pause khi hết free limit.
  Không tự chuyển database hoặc tạo database thay thế chỉ để đổi gói; phải có backup/kế hoạch dữ liệu riêng.
  Kiểm tra read-only ngày 16/10/2026 cho thấy `useFreeLimit=true`, `freeLimitExhaustionBehavior=AutoPause`,
  `autoPauseDelay=60`. Đây là trạng thái cấu hình, không phải số credit/free allowance còn lại.
- Đặt budget alert; alert chỉ thông báo, không phải cơ chế tự dừng. Azure Files, Blob và logs vẫn có thể tính phí.
- Chọn region được subscription cho phép, ưu tiên gần SQL/Blob. Không tự dùng/sửa environment SmartCinema.

## 2. Đưa source và image lên GitHub

Các sửa đổi local chưa được commit/push. Review thay đổi BE/FE trước khi đưa lên hai repo tương ứng.
Không commit `deploy/azure-appsettings.local.json`, PFX, keys hoặc các file `*.private.json`.

Repo BE → Actions → **Backend CI** phải đạt. Sau đó chạy thủ công
**Publish backend image to GHCR → Run workflow**, branch `main`.
Workflow `.github/workflows/publish-container.yml` dùng `GITHUB_TOKEN` với `packages:write`, không cần PAT để build/push.
Image Linux amd64 có tên `ghcr.io/qhao2508/hrcms-api`, tags `sha-<full-commit-sha>` và `latest`.
Deploy bằng tag SHA để rollback rõ ràng. Workflow chỉ publish image, không tạo/cập nhật Azure.

Giữ package private; không chuyển public nếu chưa quyết định công khai image.
Trong GitHub Packages, xác nhận repo HRCMS có quyền Actions truy cập package nếu package đã tồn tại từ trước.
Để Azure pull private image, tạo PAT classic có `read:packages` của tài khoản có quyền đọc package,
authorize SSO nếu tổ chức yêu cầu, lưu trực tiếp trong Container Apps secret `ghcr-read-token`.
Không đặt PAT vào source hoặc lịch sử lệnh. Đặt thời hạn phù hợp và thay token trước khi hết hạn.

## 3. Chuẩn bị Azure Files và keys

Bộ riêng tư đã chuẩn bị nằm ở đường dẫn ghi trong `%LOCALAPPDATA%\HRCMS\deployment\latest-path.txt`.
Gồm `keys/`, `keys.zip`, `data-protection.pfx`, `certificate-settings.private.json` và bản cấu hình riêng tư.
Key ring copy đã mã hóa; original dev keys không bị đổi. Không tạo lại certificate sau khi đã sử dụng production.

Trong Storage account hỗ trợ classic Azure Files (ưu tiên cùng region), tạo file share riêng `hrcms-keys`.
Upload các file XML từ `keys/` vào root share; không upload ZIP chưa giải nén hoặc lồng thêm thư mục keys.
Không đặt PFX/password vào Blob public hoặc web root. Giữ backup riêng an toàn.

Tạo Container Apps environment riêng cho HRCMS dùng Consumption, không chọn Dedicated workload profile.
Xem lựa chọn logs/retention, tránh cấp thêm tài nguyên giám sát trả phí không cần thiết.
Trong **Container Apps environment → Azure Files**, đăng ký share, storage name `hrcms-keys`, Read/Write.
Cấp storage account key trực tiếp tại Portal; key này khác với quyền Managed Identity dùng cho Blob.
Mount file share vào container ở `/mnt/hrcms-keys`. Filesystem container mặc định không giữ keys qua restart.

## 4. Tạo BE Container App

Tạo `hrcms-api` trong resource group HRCMS, chọn environment HRCMS ở bước trên.
Deployment source Container image, không bật Optimize for Azure Functions.

| Mục | Giá trị |
| --- | --- |
| Registry | Other/private registry, `ghcr.io` |
| Username | Tài khoản GitHub được cấp quyền đọc package |
| Password | PAT read:packages; lưu secret `ghcr-read-token` |
| Image | `ghcr.io/qhao2508/hrcms-api:sha-<commit>` |
| Workload profile | Consumption |
| CPU / Memory | 0.25 vCPU / 0.5 GiB để bắt đầu; tăng nếu có OOM |
| Ingress | Enabled, external HTTP, target port 8080, không cho insecure HTTP |
| Revision mode | Single |
| Min / Max replicas | 1 / 1 khi sử dụng |
| Command / Arguments | Để mặc định Docker image |

Cấu hình đầy đủ nằm trong `deploy/containerapp.example.yaml`.
Nếu Portal cần tạo app trước khi thêm volume/secrets, revision đầu có thể chưa chạy; chưa mở sử dụng chính thức.
Thêm Secrets, Environment variables và Volume mounts, rồi tạo revision hoàn chỉnh.

### Secrets và biến môi trường

Lấy giá trị từ `deploy/azure-appsettings.local.json` đã được chuẩn bị riêng trên máy.
Không cần Azure Key Vault. Đặt các secret sau trong Container App → Secrets:

| Secret | Giá trị từ cấu hình riêng tư |
| --- | --- |
| `sql-connection` | `ConnectionStrings__SqlServer` |
| `smtp-password` | `Email__Smtp__Password` |
| `dp-certificate-base64` | `DataProtection__CertificateBase64` |
| `dp-certificate-password` | `DataProtection__CertificatePassword` |
| `ghcr-read-token` | PAT GitHub read:packages |

Trong Container → Environment variables, các tên .NET tương ứng phải chọn **Secret reference**,
không phải nhập chữ `secretref:...` như giá trị literal trên Portal.
Các biến không nhạy cảm theo YAML: Production, port 8080, Workers enabled, SQL AutoMigrate=false,
Storage AzureBlob/Default, SMTP SSL 587, certificate encryption và `DataProtection__Path=/mnt/hrcms-keys`.
Điền SMTP sender/username/host đúng provider hiện có; YAML minh họa Gmail.

Lấy FQDN thật từ Overview sau khi tạo app. Đặt `AllowedHosts` bằng hostname đó, không có `https://`.
Đặt `Cors__Origins__0` bằng FE origin thật sau bước deploy FE, không có dấu `/` cuối.
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` chỉ dùng sau ingress Azure được quản lý,
không dùng cấu hình này cho container public trực tiếp không có proxy tin cậy.

### Blob, SQL và probes

- Bật System assigned managed identity của Container App; cấp `Storage Blob Data Contributor`
  ở scope private container `hrcms-files`. Không cần storage connection string cho Blob.
- Cho phép outbound IP BE kết nối Azure SQL hoặc cấu hình private networking phù hợp.
  Chỉ bỏ rule `AllowAllInternet` sau khi kiểm đường kết nối thay thế; outbound IP có thể thay khi đổi môi trường.
- Source đã khớp 13 migration trên Azure ở lần kiểm gần nhất. Không chạy AutoMigrate hoặc seed demo khi startup.
- Startup/liveness dùng TCP 8080; readiness dùng HTTP `/health` với Host đúng FQDN và X-Forwarded-Proto=https.
  Đừng dùng SQL readiness làm liveness để tránh restart vòng lặp khi SQL đang resume.
- Kiểm CPU/memory, startup logs, `/health` và gửi OTP thật. 0.5 GiB là mức thử ban đầu, không phải bảo đảm đủ tải.

### Dùng YAML tùy chọn

Copy mẫu thành `deploy/containerapp.local.yaml`, thay tất cả `REPLACE_WITH_...`.
Mẫu dành cho **update app đã có secrets** và storage registration, không phải lệnh tạo app hoàn chỉnh một bước.
Không đưa secret values vào YAML. Trước khi update, xác nhận các secret được YAML tham chiếu vẫn tồn tại.

```powershell
az containerapp update --resource-group g2-HRCMS --name hrcms-api --yaml deploy/containerapp.local.yaml --output none
```

Lệnh này thay đổi cloud; chỉ chạy sau khi đã hoàn thành cấu hình Portal và review mẫu đã điền.
Nếu Portal đặt registry password secret bằng tên khác, đổi registry passwordSecretRef cho khớp.

## 5. FE Static Web Apps Free

Hướng dẫn chi tiết ở `docs/DEPLOY_AZURE.md` trong repo `HRCMS-Frontend`.
Tạo Static Web App Free, thêm GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN`, repository variables
`VITE_API_BASE_URL=https://<BE-FQDN-thực-tế>` và `VITE_REALTIME_ENABLED=false`.
Workflow FE test/build rồi upload dist. Không cần Docker hoặc Nginx cho FE.
Lấy URL FE và cập nhật CORS BE. Các API calls dùng trực tiếp HTTPS BE, không cần linked API của Static Web Apps.

## 6. Sử dụng theo giờ để tiết kiệm

Trước demo: Container App Overview → Start nếu đang stopped, đợi readiness và `/health` đạt.
Trong lúc dùng, giữ min=1/max=1 để worker OTP/reminder chạy đúng. Không chọn min=0 như một cách bảo đảm worker.
Sau demo: Overview → Stop; khi stopped, login/OTP/reminder/API đều không hoạt động, FE vẫn mở được.
Kiểm Revision management không còn revision cũ giữ replica ngoài dự kiến.

SQL có thể auto-pause sau khi hết kết nối; worker/readiness trong lúc BE chạy có thể giữ SQL thức.
Storage vẫn lưu dữ liệu và có thể tính phí dù BE stopped. Kiểm tra cả SmartCinema vì free grant/credit dùng chung.

## 7. Nghiệm thu và rollback

1. SQL/Blob/SMTP thật, login/refresh/logout/OTP, quyền từng role, upload/download.
2. Refresh trực tiếp trang con FE không 404; FE gọi đúng BE qua HTTPS và preflight CORS đạt.
3. Restart/redeploy BE; keys còn đủ, dữ liệu mã hóa vẫn đọc được; không tự thay certificate.
4. Dừng/bật BE thử, kiểm lại `/health` và OTP; xem Cost Analysis sau khi dữ liệu chi phí cập nhật.
5. Rollback bằng image SHA trước, giữ keys/secrets/mount. Không tự rollback schema SQL.

Chạy local không đổi: BE `dotnet run --project Horse_BackEnd --launch-profile http`, FE `npm run dev`.
Không import biến production vào terminal dev. Giữ `VITE_API_BASE_URL=` trống để dùng proxy local.

Tài liệu: [Container Apps storage](https://learn.microsoft.com/azure/container-apps/storage-mounts),
[secrets](https://learn.microsoft.com/azure/container-apps/manage-secrets),
[billing](https://learn.microsoft.com/azure/container-apps/billing),
[GHCR](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry).
