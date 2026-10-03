# Thiết lập GitHub cho nhóm

Repository đã được quan sát trong phiên GitHub của người dùng: [QHao2508/HRCMS](https://github.com/QHao2508/HRCMS). Dùng một monorepo FE/BE/docs. Chưa xác minh visibility, collaborators hay branch protection; đề xuất Private cho nhóm.

## Sau khi có repository

Thêm remote URL thực tế và push main. Không copy nguyên thư mục chứa bin/obj/.vs hoặc secrets lên web. Chỉ push file đã kiểm tra bằng git status và git diff --cached.

```powershell
git remote add origin https://github.com/QHao2508/HRCMS.git
git push -u origin main
```

Chỉ thêm remote khi chưa có origin; dùng `git remote -v` để kiểm tra trước. Nhóm sử dụng tài khoản riêng, không chia sẻ mật khẩu/token.

## Cấu hình nhóm

1. Mời username thực tế qua Settings → Collaborators, quyền Write cho người phát triển; chỉ lead/owner có quyền quản trị cần thiết.
2. Bật ruleset/branch protection cho main nếu gói GitHub hỗ trợ: PR bắt buộc, một approval, dismiss stale approvals, resolve conversations và check `backend-build`; chặn force push/deletion.
3. Tạo milestones W1 Foundations, W2 Auth Intake, W3 Approval Assignment, W4 Planning Medical Guards, W5 Execution Clearance, W6 Care Notifications, W7 Reports Integration, W8 Release.
4. Tạo labels: `priority:P0`, `priority:P1`, `area:auth`, `area:horses`, `area:training`, `area:medical`, `area:care`, `area:reports`, `area:infra`, `type:bug`, `type:feature`, `blocked`.
5. Tạo Project board: Backlog → Ready → In Progress → In Review → Done. Mỗi issue lấy tiêu chí từ BACKLOG.md, gán assignee/reviewer và milestone.
6. Khi frontend/tests đã tồn tại, thêm lint/build/tests vào CI và mới đưa check đó vào required checks. Workflow hiện tại chỉ build backend, chưa thay thế business tests.

Các bước trên là cấu hình đề xuất, chưa được xác nhận đã thực hiện trên GitHub. Thiết lập chính sách quyền và account theo chủ sở hữu repository và khả năng gói GitHub thực tế.
