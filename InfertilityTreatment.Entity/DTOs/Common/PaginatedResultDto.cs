using System.Collections.Generic;

namespace InfertilityTreatment.Entity.DTOs.Common
{
    public class PaginatedResultDto<T>
    {
        public List<T> Items { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public PaginatedResultDto(List<T> items, int count, int pageNumber, int pageSize)
        {
            // Gán mặc định nếu đầu vào không hợp lệ
            PageNumber = pageNumber > 0 ? pageNumber : 1;
            PageSize = pageSize > 0 ? pageSize : 100;

            Items = items ?? new List<T>();
            TotalCount = count;

            // Tránh chia cho 0
            TotalPages = PageSize > 0
                ? (int)Math.Ceiling(count / (double)PageSize)
                : 0;
        }
    }
}
