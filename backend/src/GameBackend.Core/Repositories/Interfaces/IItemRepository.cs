namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IItemRepository
    {
        Task<bool> ExistsAsync(string itemId);
    }
}
