using System.Threading.Tasks;

/// <summary>
/// API service cho Inventory feature.
/// GET /inventory/{characterId}
/// </summary>
public class InventoryApiService
{
    public async Task<string> GetInventoryAsync(string characterId)
    {
        return await ApiClient.Instance.GetRawAsync("/inventory/" + characterId);
    }
}
