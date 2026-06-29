namespace GameBackend.Core.Repositories.Interfaces
{
    public interface ILootRepository
    {
        Task<bool> CanDropItemAtLocationAsync(string itemId, string locationId);
    }
}
