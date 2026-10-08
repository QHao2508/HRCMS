> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../README.md).

# Dọn dẹp và chú thích source

## Đã dọn

- Frontend: CSS rỗng `src/App.css` và `src/index.css`, import CSS rỗng, SVG template `public/icons.svg` không được sử dụng.
- Xóa API client export `assignRider` không có caller; UI hiện giao người cưỡi qua `editSession`. Endpoint backend vẫn giữ nguyên.
- Xóa 8 khóa/nội dung message không có tham chiếu: `TOI_DA_20_FILE_MOI_FILE_10_MIB_ANH_PNG_JPEG_TAI_LIEU_PNG_JPEG_PDF`, `KET_QUA`, `CHAO_MUNG_DEN_HRCMS`, `GIAY_CHUNG_NHAN`, `LUU_BAN_NHAP_TRUOC_DE_TAI_FILE_LEN_HO_SO_THONG_TIN_CHUA_HOAN_CHINH_VAN_C`, `EACH_FILE_MUST_BE_10_MIB_OR_SMALLER`, `THE_REGISTRATION_HAS_REACHED_ITS_20_FILE_LIMIT`, `GROOM`.
- Xóa selector ô vuông logo cũ không còn dùng; không thay đổi palette hoặc xử lý nghiệp vụ.
- Backend: ảnh minh chứng thiết lập GitHub, thư mục merge backup cũ và thư mục Maintenance chỉ còn build artifact.
- Sau khi kiểm thử: dọn bin/obj của các project, dist/cache Vite và TestResults. Các file này có thể tạo lại.

## Giữ lại

Source API/BLL/DAL, frontend, dependency đang dùng, package lock, tests/CI, scripts Azure/SMTP/demo, migration SQL Server, snapshot, tài liệu baseline và cấu hình cần thiết. Không xóa .git, User Secrets, App_Data/keys, dữ liệu người dùng hoặc blob Azure. node_modules được giữ để npm run dev dùng ngay.

## Chú thích

- XML/JSDoc và [function reference](FUNCTION_REFERENCE.md) cho 226 function backend và 226 function frontend có tên.
- [Chức năng từng folder](../FOLDERS.md) là mục lục cấu trúc hiện tại; từng module có chi tiết trong docs/reference.
- Callback anonymous/getter được giải thích trong function bao ngoài; migration sinh tự động giữ nguyên. Không đổi code nghiệp vụ chỉ để thêm comment.

## Khôi phục và kiểm chứng

Bản sao phục hồi ngoài hai project: `C:/Users/Admin/AppData/Local/Temp/HRCMS-Cleanup-20261007-224501`. Bao gồm snapshot source hiện có và merge backup cũ. Kết quả kiểm thử/audit cuối được lưu trong thư mục verification của bản sao.

Đã so token executable trước/sau để xác nhận các chú thích không thay đổi function. Frontend chỉ loại function client không có caller nêu trên. Chạy lại dotnet build/test, npm test/lint/build trước khi dọn output. Khi chạy lại dotnet run, bin/obj được tạo lại; npm run dev tự tạo cache Vite.

## Kết quả kiểm tra cuối

Backend: 108 passed, 1 Azure smoke test opt-in không bật. Frontend: 160 passed. Build backend 0 cảnh báo/lỗi; lint và build frontend đạt. Bằng chứng nằm trong verification của bản sao phục hồi.
