namespace HorseClub.BLL.Workers;

public interface IClubMailSender {
    /// <summary>
    /// Hợp đồng gửi thư cho worker: recipient/subject/body và cancellation token; implementation chịu trách nhiệm SMTP hoặc provider kiểm thử.
    /// </summary>
    /// <param name="recipient">Giá trị kiểu string dùng trong Send.</param>
    /// <param name="subject">Giá trị kiểu string dùng trong Send.</param>
    /// <param name="body">Giá trị kiểu string dùng trong Send.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    Task Send(string recipient, string subject, string body, CancellationToken token); }
