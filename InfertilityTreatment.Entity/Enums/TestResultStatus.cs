namespace InfertilityTreatment.Entity.Enums
{
    public enum TestResultStatus : byte
    {
        Pending = 0,          // Đã được chỉ định, nhưng chưa thực hiện
        InProgress = 1,       // Đang được thực hiện
        Completed = 2,        // Đã có kết quả, nhưng chưa được bác sĩ xem
        Reviewed = 3,         // Bác sĩ đã xem và đưa ra nhận xét
        RequiresRetest = 4,   // Kết quả không rõ ràng hoặc lỗi, cần làm lại
        Cancelled = 5         // Bị hủy (ví dụ: bệnh nhân không đến, hủy điều trị)
    }
}