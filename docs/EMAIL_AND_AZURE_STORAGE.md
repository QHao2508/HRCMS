# Gmail và Azure Blob Storage

## Trạng thái

Backend hỗ trợ Gmail SMTP gửi mail xác thực qua hàng đợi hiện có và Azure Blob Storage cho ảnh/tài liệu. Đăng ký Owner, reset mật khẩu và kích hoạt nhân viên đều dùng OTP 6 chữ số, không dùng link; xem [quy tắc và hướng dẫn test](EMAIL_OTP_TEST_GUIDE.md). Cần lưu thông tin dịch vụ trong User Secrets rồi khởi động lại API để kích hoạt. SQL nghiệp vụ vẫn ở Azure SQL; Blob chứa nội dung file, SQL giữ metadata và quyền truy cập.

`Storage:Provider=AzureBlob` upload/download trực tiếp bằng SDK, không ghi file vào `App_Data/uploads` và không tự fallback về local khi Azure lỗi. Container phải private; frontend tải qua API có kiểm tra quyền. Tên blob là ID ngẫu nhiên; upload không ghi đè. Khi transaction SQL thất bại, upload chưa được tham chiếu sẽ được dọn; nếu không kiểm chứng được SQL, giữ blob để đối soát.

`Local` được giữ cho test và cấu hình cũ trong thời gian chuyển đổi. File đang có ở local chưa tự chuyển lên Azure. Không xóa file cũ trước khi chuyển đúng `StorageName` và xác nhận tải lại. Data Protection key ring là cấu hình độc lập; chuyển attachment sang Blob không tự chuyển các khóa mã hóa.

## 1. Gmail

Bạn có thể cấu hình trực tiếp bằng [các lệnh dotnet user-secrets](GMAIL_USER_SECRETS.md), theo cấu trúc `Email:Provider`, `Email:FromAddress` và `Email:Smtp:*`. Script dưới đây là lựa chọn nhập ẩn bổ sung, không bắt buộc.

Địa chỉ hiện tại: `lequochao12a2@gmail.com`. Đây là Gmail; địa chỉ tên miền công ty cần Google Workspace hoặc cấu hình tên miền phù hợp.

1. Bật [2-Step Verification](https://myaccount.google.com/security).
2. Tạo [App Password](https://myaccount.google.com/apppasswords), tên ứng dụng `HRCMS`. Không dùng mật khẩu đăng nhập Google thông thường. Nếu chính sách tài khoản không cho App Password, cần cấu hình OAuth hoặc SMTP relay được quản trị viên cho phép.
3. Trong PowerShell:

```powershell
cd E:\SWP391\HorseClub
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Set-GmailEmail.ps1 -Email "lequochao12a2@gmail.com"
```

Nhập App Password ở prompt ẩn. Script lưu SMTP `smtp.gmail.com`, cổng `587`, TLS, người gửi và thông tin đăng nhập vào User Secrets, bật worker. Không gửi App Password vào chat hoặc commit vào Git.

Development hiện chọn SMTP thật; không mặc định ghi mail vào file nữa. Kiểm tra thông tin đăng nhập, không gửi email thử:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-GmailConnection.ps1
```

Nếu Gmail trả `535 5.7.8 BadCredentials`, tạo lại App Password cho đúng tài khoản gửi và chạy lại `Set-GmailEmail.ps1`. Kết nối TLS thành công không có nghĩa Gmail đã chấp nhận mật khẩu. Không dùng mật khẩu đăng nhập Google thông thường.

Khi khởi động lại, các email còn trong hàng đợi cũng có thể được worker gửi. Thử đăng ký bằng email do bạn kiểm soát, nhận mã xác thực và hoàn tất xác thực. Chưa đánh dấu kiểm chứng gửi thật cho đến khi nhận được email.

Nguồn: [Google App Password](https://support.google.com/accounts/answer/185833), [Gmail SMTP](https://support.google.com/mail/answer/7104828).

## 2. Azure Blob

Azure Portal → Storage Account → Containers: tạo/chọn container với anonymous access `Private`. Account nên tắt `Allow Blob anonymous access`. Không cấu hình Static Website cho kho hồ sơ.

Lấy URL `https://<account>.blob.core.windows.net` và tên container. Với connection string:

```powershell
cd E:\SWP391\HorseClub
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Set-AzureBlobStorage.ps1 -ServiceUri "https://<account>.blob.core.windows.net" -Container "hrcms-files"
```

Script hỏi connection string ẩn (Portal → Access keys), kiểm tra account trùng URL và lưu vào User Secrets. Không đưa connection string vào frontend.

Ưu tiên identity khi deploy Azure: bật managed identity và gán `Storage Blob Data Contributor` cho identity ở container. Local dùng `az login` với tài khoản được gán cùng role:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Set-AzureBlobStorage.ps1 -ServiceUri "https://<account>.blob.core.windows.net" -Container "hrcms-files" -UseAzureIdentity
```

Script chọn `AzureCliCredential` trên máy local để dùng phiên `az login`, tránh dò managed identity của máy Azure. Khi deploy Azure, thêm `-IdentityCredential Default` để dùng `DefaultAzureCredential` và managed identity của ứng dụng. Container phải có sẵn; ứng dụng không tự tạo hoặc thay đổi quyền public. Firewall của account phải cho phép nơi chạy backend kết nối.

### Account của dự án

- Storage Account: `hrcmsg2`, resource group `g2-HRCMS`.
- Blob endpoint: `https://hrcmsg2.blob.core.windows.net/`.
- Container: `hrcms-files`, private; account tắt anonymous Blob access.
- Local User Secrets đã chọn `AzureBlob` và `AzureCli`, không lưu access key.
- Tài khoản Azure đang đăng nhập được cấp `Storage Blob Data Contributor` chỉ trong container này.

Nếu Azure CLI hết phiên đăng nhập, chạy `az login` rồi khởi động lại API. Quyền mới trên Azure có thể cần vài phút để có hiệu lực.

Nguồn: [Azure Blob SDK và identity](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet).

## 3. Chạy và kiểm tra

```powershell
cd E:\SWP391\HorseClub\Horse_BackEnd
dotnet run
```

Trong terminal khác chạy frontend như trước. Upload PNG/JPEG/PDF trong hồ sơ được phép, xác nhận blob xuất hiện trên Azure, download qua API trả đúng nội dung; tài khoản ngoài phạm vi phải bị từ chối. Dừng/chạy lại API và tải lại để kiểm tra file không phụ thuộc ổ local.

Unit test Azure dùng transport giả của SDK (không gọi Azure thật), test SQL dùng database tạm. Kiểm chứng Azure thật cần account/container/credential thực tế.
