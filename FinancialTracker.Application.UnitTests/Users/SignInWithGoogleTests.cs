using FinancialTracker.Application.Users;
using FinancialTracker.Domain.Users;

namespace FinancialTracker.Application.UnitTests.Users;

public sealed class SignInWithGoogleTests
{
    private readonly InMemoryGoogleUserStore store = new();
    private readonly SignInWithGoogle signIn;

    public SignInWithGoogleTests() => signIn = new SignInWithGoogle(store);

    [Fact]
    public async Task A_first_sign_in_creates_a_User_with_the_Google_email()
    {
        var userId = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana@example.test"));

        Assert.NotNull(userId);
        Assert.Equal(new GoogleUser(userId.Value, "ana@example.test"), await store.FindAsync("sub-1"));
    }

    [Fact]
    public async Task Signing_in_again_opens_the_same_User()
    {
        var first = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana@example.test"));

        var again = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana@example.test"));

        Assert.Equal(first, again);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task Two_Google_accounts_with_the_same_email_are_two_Users()
    {
        var first = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "shared@example.test"));

        var second = await signIn.ExecuteAsync(new GoogleAccount("sub-2", "shared@example.test"));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task A_first_sign_in_without_an_email_creates_nothing()
    {
        var userId = await signIn.ExecuteAsync(new GoogleAccount("sub-1", email: null));

        Assert.Null(userId);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task A_later_sign_in_without_an_email_keeps_the_email_the_User_had()
    {
        var first = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana@example.test"));

        var again = await signIn.ExecuteAsync(new GoogleAccount("sub-1", email: null));

        Assert.Equal(first, again);
        Assert.Equal("ana@example.test", (await store.FindAsync("sub-1"))?.Email);
    }

    [Fact]
    public async Task A_changed_Google_email_replaces_the_Users_email()
    {
        var first = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana@example.test"));

        var again = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana.new@example.test"));

        Assert.Equal(first, again);
        Assert.Equal("ana.new@example.test", (await store.FindAsync("sub-1"))?.Email);
    }

    [Fact]
    public async Task Losing_a_first_sign_in_race_signs_in_as_the_User_the_other_sign_in_created()
    {
        var winner = Guid.NewGuid();
        store.BeforeNextCreate = () => store.Add("sub-1", winner, "ana@example.test");

        var userId = await signIn.ExecuteAsync(new GoogleAccount("sub-1", "ana@example.test"));

        Assert.Equal(winner, userId);
        Assert.Equal(1, store.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_Google_account_needs_a_subject(string subject) =>
        Assert.ThrowsAny<ArgumentException>(() => new GoogleAccount(subject, "ana@example.test"));

    [Fact]
    public void A_blank_Google_email_counts_as_none() =>
        Assert.Null(new GoogleAccount("sub-1", " ").Email);

    private sealed class InMemoryGoogleUserStore : IGoogleUserStore
    {
        private readonly Dictionary<string, GoogleUser> usersBySubject = [];

        /// <summary>Runs inside the next create, as if another sign-in committed first.</summary>
        public Action? BeforeNextCreate { get; set; }

        public int Count => usersBySubject.Count;

        public void Add(string googleSubject, Guid id, string email) =>
            usersBySubject.Add(googleSubject, new GoogleUser(id, email));

        public Task<GoogleUser?> FindAsync(string googleSubject) =>
            Task.FromResult(usersBySubject.GetValueOrDefault(googleSubject));

        public Task<Guid?> TryCreateAsync(string googleSubject, string email)
        {
            BeforeNextCreate?.Invoke();
            BeforeNextCreate = null;

            var id = Guid.NewGuid();
            return Task.FromResult<Guid?>(usersBySubject.TryAdd(googleSubject, new GoogleUser(id, email)) ? id : null);
        }

        public Task UpdateEmailAsync(Guid userId, string email)
        {
            var (subject, user) = usersBySubject.Single(entry => entry.Value.Id == userId);
            usersBySubject[subject] = user with { Email = email };
            return Task.CompletedTask;
        }
    }
}
