# Logo HRCMS

Logo gốc được lưu trên Azure account hrcmsg2, container private hrcms-files, mục branding:

`branding/hrcms-logo-153411c4096984a2.png`

SHA-256: `153411c4096984a2e8e5c2e995fdcbb640534a1fcf6076c2cbf359876582de97`.

Frontend dùng `/api/branding/logo?v=153411c4096984a2`. Endpoint public chỉ stream blob cố định trong cấu hình `Branding:LogoBlobName`, không nhận tên blob từ người dùng, không xuất SAS/access key và không mở quyền public cho container chứa hồ sơ ngựa. Browser cache logo một ngày; query phiên bản thay đổi khi đổi logo.

Ảnh giữ nguyên bản gốc. Frontend có bản logo chuẩn tại `public/images/hrcms-logo.png`, đối soát đúng SHA-256 nêu trên từ endpoint branding đã deploy. Bản này giúp logo vẫn xuất hiện khi backend local chưa có credential Azure Storage. Logo tùy chỉnh từ quản trị website vẫn được ưu tiên; nếu tải thất bại, dùng lại bản chuẩn. Không sao chép ảnh ngựa hoặc hồ sơ private vào public assets.

Header chỉ dùng CSS để hiển thị phần huy hiệu của logo chuẩn; logo tùy chỉnh hiển thị đầy đủ theo tỷ lệ, không áp dụng khung cắt của ảnh chuẩn. Nếu mọi nguồn ảnh đều lỗi, dùng biểu tượng dự phòng thay vì lặp chữ HRCMS.
