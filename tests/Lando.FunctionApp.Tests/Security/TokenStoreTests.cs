using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lando.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.FunctionApp.Security.Tests;

public sealed class TokenStoreTests : IDisposable
{
    private const string StoreName = "skill";
    private const string UserId = "user1";
    private const string SecretName = "skill--user1";

    private readonly Mock<ITokenClient> _tokenClient = new();
    private readonly Mock<ISecretClient> _secretClient = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly TokenStore _sut;

    public TokenStoreTests()
    {
        _sut = new TokenStore(StoreName, _tokenClient.Object, _secretClient.Object,
            _cache, NullLogger<TokenStore>.Instance);
    }

    public void Dispose() => _cache.Dispose();

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Token MakeToken(string access, int expiresIn = 3600, string? refresh = null)
        => new() { AccessToken = access, ExpiresIn = expiresIn, RefreshToken = refresh };

    private static async IAsyncEnumerable<string> AsyncFrom(
        [EnumeratorCancellation] CancellationToken _ = default,
        params string[] items)
    {
        foreach (var item in items)
            yield return item;
    }

    private static IAsyncEnumerable<string> Keys(params string[] keys)
        => AsyncFrom(items: keys);

    // ── Client ───────────────────────────────────────────────────────────────

    [Fact]
    public void Client_ReturnsInjectedTokenClient()
    {
        _sut.Client.ShouldBeSameAs(_tokenClient.Object);
    }

    // ── SaveAsync ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_BlankId_ThrowsArgumentException(string? id)
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.SaveAsync(id!, "tok", default));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_BlankRefreshToken_ThrowsArgumentException(string? token)
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.SaveAsync(UserId, token!, default));
    }

    [Fact]
    public async Task SaveAsync_WritesSecretToKeyVault()
    {
        await _sut.SaveAsync(UserId, "refresh-token", default);

        _secretClient.Verify(c => c.SetSecretAsync(SecretName, "refresh-token",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_SanitizesSpecialCharsInId()
    {
        await _sut.SaveAsync("amzn1.account.ABC", "tok", default);

        _secretClient.Verify(c => c.SetSecretAsync("skill--amzn1-account-ABC", "tok",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_InvalidatesListCache()
    {
        _cache.Set("TokenStore->List", Array.Empty<(string userId, string value)>());

        await _sut.SaveAsync(UserId, "tok", default);

        _cache.TryGetValue("TokenStore->List", out _).ShouldBeFalse();
    }

    // ── GetAsync ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_BlankId_ThrowsArgumentException(string? id)
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.GetAsync(id!, default));
    }

    [Fact]
    public async Task GetAsync_ReturnsAccessTokenFromTokenClient()
    {
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");
        _tokenClient.Setup(c => c.RefreshAsync("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access-token", 3600));

        var (value, expiresIn) = await _sut.GetAsync(UserId, default);

        value.ShouldBe("access-token");
        expiresIn.ShouldBe(3600);
    }

    [Fact]
    public async Task GetAsync_MissingSecret_ThrowsInvalidOperationException()
    {
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.GetAsync(UserId, default));

        ex.Message.ShouldContain("No refresh token found");
        ex.Message.ShouldContain(UserId);
    }

    [Fact]
    public async Task GetAsync_CachesAccessToken_SecondCallSkipsKeyVaultAndTokenClient()
    {
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");
        _tokenClient.Setup(c => c.RefreshAsync("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access-token", 3600));

        await _sut.GetAsync(UserId, default);
        var (value, _) = await _sut.GetAsync(UserId, default);

        value.ShouldBe("access-token");
        _tokenClient.Verify(c => c.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_RotatedRefreshToken_PersistsNewRefreshToken()
    {
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-refresh");
        _tokenClient.Setup(c => c.RefreshAsync("old-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access", refresh: "new-refresh"));

        await _sut.GetAsync(UserId, default);

        _secretClient.Verify(c => c.SetSecretAsync(SecretName, "new-refresh",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_UnchangedRefreshToken_DoesNotPersist()
    {
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh");
        _tokenClient.Setup(c => c.RefreshAsync("refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access", refresh: "refresh")); // same value returned

        await _sut.GetAsync(UserId, default);

        _secretClient.Verify(c => c.SetSecretAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_NullRefreshToken_DoesNotPersist()
    {
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh");
        _tokenClient.Setup(c => c.RefreshAsync("refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access", refresh: null));

        await _sut.GetAsync(UserId, default);

        _secretClient.Verify(c => c.SetSecretAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_ExpiresInAtOrBelowBuffer_DoesNotSetCacheExpiration()
    {
        // Token with ExpiresIn == 60 (equal to AccessTokenSafetyBuffer) should not set expiration;
        // the entry stays in cache indefinitely. Verify by ensuring a second call still uses cache.
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh");
        _tokenClient.Setup(c => c.RefreshAsync("refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access", expiresIn: 60));

        await _sut.GetAsync(UserId, default);
        await _sut.GetAsync(UserId, default);

        _tokenClient.Verify(c => c.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_EmptyVault_ReturnsEmptyArray()
    {
        _secretClient.Setup(c => c.ListKeysAsync(It.IsAny<CancellationToken>()))
            .Returns(Keys());

        var result = await _sut.ListAsync(default);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_FiltersToStoreName()
    {
        _secretClient.Setup(c => c.ListKeysAsync(It.IsAny<CancellationToken>()))
            .Returns(Keys("skill--user1", "other--userX", "skill--user2"));
        _secretClient.Setup(c => c.GetSecretAsync("skill--user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh1");
        _secretClient.Setup(c => c.GetSecretAsync("skill--user2", It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh2");
        _tokenClient.Setup(c => c.RefreshAsync("refresh1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access1"));
        _tokenClient.Setup(c => c.RefreshAsync("refresh2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access2"));

        var result = await _sut.ListAsync(default);

        result.Length.ShouldBe(2);
        result.ShouldContain(("user1", "access1"));
        result.ShouldContain(("user2", "access2"));
    }

    [Fact]
    public async Task ListAsync_CachesResult_SecondCallSkipsKeyVault()
    {
        _secretClient.Setup(c => c.ListKeysAsync(It.IsAny<CancellationToken>()))
            .Returns(Keys("skill--user1"));
        _secretClient.Setup(c => c.GetSecretAsync(SecretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh");
        _tokenClient.Setup(c => c.RefreshAsync("refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeToken("access"));

        await _sut.ListAsync(default);
        await _sut.ListAsync(default);

        _secretClient.Verify(c => c.ListKeysAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
