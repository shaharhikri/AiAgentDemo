using Raven.Client.Documents;

namespace AiAgentDemo;

internal class RavenUserProfileResolver(IDocumentStore store)
{
    public async Task<string> GetOrCreateUserIdAsync(long telegramUserId, CancellationToken token = default)
    {
        using var session = store.OpenAsyncSession();

        var user = await session
            .Query<User>()
            .FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId, token);

        if (user is not null)
            return user.Id;

        user = new User
        {
            Id = "Users/" + Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            FirstName = "Telegram",
            LastName = "User",
            Age = 30,
            HeightCm = 170,
            WeightKg = 70,
            Gender = Gender.Other,
            FitnessGoals = null
        };

        await session.StoreAsync(user, token);
        await session.SaveChangesAsync(token);

        return user.Id;
    }
}
