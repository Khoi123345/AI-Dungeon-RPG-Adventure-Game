using System.Collections.Generic;
using System.Threading.Tasks;
using GameShared.Models;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IStorySummaryService
    {
        /// <summary>
        /// Kiểm tra và thực hiện tóm tắt lại storySummary nếu đã đạt mốc turn (mỗi 20 turn).
        /// Sử dụng Claude AI (IBedrockService) để nén toàn bộ cốt truyện xuống khoảng 300 từ.
        /// </summary>
        /// <param name="session">Story session hiện tại</param>
        /// <param name="currentTurnCount">Số turn hiện tại trong session</param>
        /// <param name="recentActions">Danh sách các hành động gần đây</param>
        /// <returns>Bản tóm tắt storySummary mới (nếu được tóm tắt) hoặc giữ nguyên cũ</returns>
        Task<string> CondenseSummaryIfNeededAsync(StorySession session, int currentTurnCount, List<StoryAction> recentActions);
    }
}
