using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.DAL.Data;

// Club-owned illustrative choices, not externally published exercise prescriptions.
public static class DefaultTrainingTemplates
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static object Exercise(string key, string label, TrainingType type, decimal distance, Intensity intensity, int minutes, string target)
        => new { key, label, trainingType = type, distanceMetres = distance, intensity, surface = "Cát", durationMinutes = minutes, target };
    private static TrainingTemplate Make(int index, string key, string name, string goal, string phase, int level, TrainingType[] types, object[] exercises, decimal distance)
        => new() {
            Id = Guid.Parse($"9b590cbd-cc33-4f4d-8f03-{index:000000000000}"), CreatedAt = new DateTimeOffset(2026,10,13,0,0,0,TimeSpan.Zero),
            Name = name, Goal = goal, Phase = phase, DistanceMetres = distance, Intensity = Intensity.Light, Surface = "Cát", FrequencyPerWeek = 3,
            IsSystemDefault = true, SystemKey = key, MinimumExpertiseLevel = level, RequiredTrainingTypesJson = JsonSerializer.Serialize(types, Json),
            CurriculumJson = JsonSerializer.Serialize(new { planOptions = new[] { new { key = "standard", label = name, goal, phase } }, sessionOptions = exercises }, Json),
            Notes = "Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.",
            SourceUrl = "https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/"
        };
    public static IReadOnlyList<TrainingTemplate> All => [
        Make(1,"foundation","Xây dựng thể lực nền tảng","Xây thể lực và làm quen nhịp vận động từng bước.","Base",1,[TrainingType.Walk,TrainingType.Trot], [
            Exercise("walk-easy","Đi bộ nhẹ",TrainingType.Walk,400,Intensity.Light,15,"Giữ bước đi đều và phản hồi ổn định với người cưỡi."),
            Exercise("trot-base","Nước kiệu nền tảng",TrainingType.Trot,800,Intensity.Light,20,"Duy trì nhịp nước kiệu nhẹ và theo dõi hồi phục.")],800),
        Make(2,"steady-canter","Duy trì nhịp chạy ổn định","Phát triển sức bền và khả năng giữ nhịp canter.","Build",2,[TrainingType.Canter],[
            Exercise("canter-light","Canter nhẹ",TrainingType.Canter,800,Intensity.Light,20,"Duy trì nhịp chạy ổn định và kiểm soát hướng vận động."),
            Exercise("canter-steady","Canter đều",TrainingType.Canter,1200,Intensity.Moderate,25,"Giữ nhịp canter đều, khởi động và thả lỏng đầy đủ.")],1200),
        Make(3,"speed-intervals","Phát triển tốc độ theo từng đoạn","Rèn khả năng tăng tốc có kiểm soát xen kẽ hồi phục.","Peak",3,[TrainingType.Gallop,TrainingType.Sprint],[
            Exercise("gallop-interval","Đoạn gallop kiểm soát",TrainingType.Gallop,600,Intensity.Moderate,20,"Vận động theo từng đoạn, xen kẽ hồi phục và quan sát phản ứng."),
            Exercise("sprint-short","Đoạn tăng tốc ngắn",TrainingType.Sprint,300,Intensity.Heavy,15,"Tăng tốc ngắn theo hướng dẫn chuyên môn, không áp dụng khi có hạn chế sức khỏe.")],600),
        Make(4,"starting-gate","Làm quen cổng xuất phát","Xây sự bình tĩnh và phản hồi khi vào, ra cổng xuất phát.","Technique",2,[TrainingType.StartingGate],[
            Exercise("gate-familiar","Làm quen cổng",TrainingType.StartingGate,100,Intensity.Light,15,"Làm quen khu vực cổng và giữ phản hồi bình tĩnh."),
            Exercise("gate-exit","Phản hồi ra cổng",TrainingType.StartingGate,200,Intensity.Light,20,"Thực hành vào và ra cổng theo tín hiệu của người hướng dẫn.")],100),
        Make(5,"light-recovery","Vận động nhẹ giữa các buổi nặng","Cân bằng khối lượng huấn luyện bằng vận động nhẹ.","Recovery",1,[TrainingType.Walk,TrainingType.Recovery],[
            Exercise("recovery-easy","Vận động nhẹ",TrainingType.Recovery,300,Intensity.Light,15,"Theo dõi vận động nhẹ theo đánh giá của Trainer và bác sĩ."),
            Exercise("walk-recovery","Đi bộ thả lỏng",TrainingType.Walk,500,Intensity.Light,20,"Đi bộ đều để thả lỏng, ghi nhận phản ứng sau buổi tập.")],300)
    ];
}
