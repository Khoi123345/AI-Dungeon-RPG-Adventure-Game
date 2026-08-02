namespace GameBackend.Core.Config;

public class BedrockOptions
{
    public string Region { get; set; } = "";

    public string ModelId { get; set; } = "";

    public float Temperature { get; set; } = 0.7f;

    public int MaxTokens { get; set; } = 1200;

    public float TopP { get; set; } = 0.9f;
}
