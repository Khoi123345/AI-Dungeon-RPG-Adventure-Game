namespace GameBackend.Core.Services.Validation
{
    public interface IGameRuleSubValidator
    {
        Task ValidateAsync(GameRuleValidationContext context);
    }
}
