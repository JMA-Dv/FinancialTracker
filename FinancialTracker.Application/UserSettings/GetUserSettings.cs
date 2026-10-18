namespace FinancialTracker.Application.UserSettings;

public sealed class GetUserSettings(IUserSettingsStore store)
{
    public async Task<Settings> ExecuteAsync(Guid userId) =>
        await store.GetAsync(userId)
            ?? throw new InvalidOperationException($"Signed-in User {userId} does not exist.");
}
