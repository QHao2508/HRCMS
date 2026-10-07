# Giải thích function: HorseClub.DAL-Data

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.DAL/Data/ClubDbContext.cs

DbContext SQL Server, design-time factory, nhận diện lỗi DB và application lock cho worker.

### ClubDbContext.OnModelCreating(model)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Áp dụng cấu hình bảng, quan hệ, kiểu dữ liệu, ràng buộc và index cho toàn bộ model SQL Server.

| Đầu vào | Ý nghĩa |
|---|---|
| `model` | Giá trị kiểu ModelBuilder dùng trong OnModelCreating. |

Lời gọi chính: `ClubModelConfiguration.Configure`.

### ClubDbContext.SaveChangesAsync(cancellationToken)

**Kết quả:** `Task<int>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tăng Version cho entity bị sửa trước khi lưu để phát hiện cập nhật đồng thời bằng optimistic concurrency.

| Đầu vào | Ý nghĩa |
|---|---|
| `cancellationToken` | Giá trị kiểu CancellationToken dùng trong SaveChangesAsync. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `base.SaveChangesAsync`.

## HorseClub.DAL/Data/DatabaseFailures.cs

DbContext SQL Server, design-time factory, nhận diện lỗi DB và application lock cho worker.

### DatabaseFailures.IsDeadlock(error)

**Kết quả:** `bool`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Nhận diện mã deadlock SQL Server trong chuỗi exception để API trả xung đột có thể tải lại.

| Đầu vào | Ý nghĩa |
|---|---|
| `error` | Lỗi từ HTTP/network/ghi dữ liệu cần chuẩn hóa hoặc trình bày an toàn. |

## HorseClub.DAL/Data/SqlServerDesignFactory.cs

DbContext SQL Server, design-time factory, nhận diện lỗi DB và application lock cho worker.

### SqlServerDesignFactory.CreateDbContext(args)

**Kết quả:** `SqlServerClubDbContext`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo DbContext design-time cho dotnet-ef từ cấu hình; chỉ dùng để tạo/áp dụng migration SQL Server.

| Đầu vào | Ý nghĩa |
|---|---|
| `args` | Giá trị kiểu string[] dùng trong CreateDbContext. |

## HorseClub.DAL/Data/WorkerDatabaseLock.cs

DbContext SQL Server, design-time factory, nhận diện lỗi DB và application lock cho worker.

### WorkerDatabaseLock.TryAcquire(db, resource, token)

**Kết quả:** `Task<bool>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lấy application lock của SQL Server trong transaction để nhiều instance không xử lý cùng lô công việc; trả false khi instance khác đang giữ khóa.

| Đầu vào | Ý nghĩa |
|---|---|
| `db` | Giá trị kiểu ClubDbContext dùng trong TryAcquire. |
| `resource` | Giá trị kiểu string dùng trong TryAcquire. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

Lời gọi chính: `Database.GetDbConnection`, `command.CreateParameter`, `Parameters.Add`, `Convert.ToInt32`, `command.ExecuteScalarAsync`.

## HorseClub.DAL/Data/Configurations/ClubModelConfiguration.cs

Quan hệ restrictive, chuyển UTC ticks, concurrency token và index tối ưu truy vấn.

### ClubModelConfiguration.Configure(b)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Định nghĩa khóa, quan hệ restrictive, độ dài/precision, version và chuyển DateTimeOffset sang UTC ticks theo schema hiện có.

| Đầu vào | Ý nghĩa |
|---|---|
| `b` | Giá trị kiểu ModelBuilder dùng trong Configure. |

Lời gọi chính: `Assembly.GetTypes`, `t.IsSubclassOf`, `b.Entity`, `entity.HasBaseType`, `entity.HasKey`, `entity.Property`, `Model.GetEntityTypes`, `t.FindProperty`, `t.GetProperties`, `p.SetMaxLength`, `p.SetPrecision`, `p.SetScale`, `p.SetValueConverter`, `ReadPerformanceConfiguration.Configure`.

## HorseClub.DAL/Data/Configurations/ReadPerformanceConfiguration.cs

Quan hệ restrictive, chuyển UTC ticks, concurrency token và index tối ưu truy vấn.

### ReadPerformanceConfiguration.Configure(model)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Định nghĩa index phục vụ truy vấn danh sách/phạm vi/lịch, giữ truy vấn phổ biến tránh scan không cần thiết.

| Đầu vào | Ý nghĩa |
|---|---|
| `model` | Giá trị kiểu ModelBuilder dùng trong Configure. |
