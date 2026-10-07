# Logo HRCMS

Logo gốc được lưu trên Azure account hrcmsg2, container private hrcms-files, mục branding:

`branding/hrcms-logo-153411c4096984a2.png`

SHA-256: `153411c4096984a2e8e5c2e995fdcbb640534a1fcf6076c2cbf359876582de97`.

Frontend dùng `/api/branding/logo?v=153411c4096984a2`. Endpoint public chỉ stream blob cố định trong cấu hình `Branding:LogoBlobName`, không nhận tên blob từ người dùng, không xuất SAS/access key và không mở quyền public cho container chứa hồ sơ ngựa. Browser cache logo một ngày; query phiên bản thay đổi khi đổi logo.

Ảnh giữ nguyên bản gốc, không sao chép vào thư mục ảnh local của backend/frontend. Home dùng CSS để trình bày logo trong khung nhận diện và header. Khi đổi logo, upload blob mới theo tên chứa hash rồi cập nhật cấu hình và hằng số frontend.
