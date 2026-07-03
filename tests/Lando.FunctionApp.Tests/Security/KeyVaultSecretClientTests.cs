using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.FunctionApp.Security.Tests;

/// <summary>
/// <see cref="KeyVaultSecretClient"/> wraps the Azure SDK <see cref="SecretClient"/>:
/// unwraps the response envelope, swallows a 404 into a null return (so callers
/// don't need to catch Azure exceptions themselves), and forwards writes/listing
/// through unchanged.
/// </summary>
public class KeyVaultSecretClientTests
{
    private static KeyVaultSecretClient Sut(Mock<SecretClient> client)
        => new(client.Object, NullLogger<KeyVaultSecretClient>.Instance);

    [Fact]
    public async Task GetSecretAsync_ReturnsValue_WhenFound()
    {
        var client = new Mock<SecretClient>(MockBehavior.Strict);
        var secret = new KeyVaultSecret("my-secret", "the-value");
        client.Setup(c => c.GetSecretAsync("my-secret", It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(secret, Mock.Of<Response>()));

        var value = await Sut(client).GetSecretAsync("my-secret", CancellationToken.None);

        value.ShouldBe("the-value");
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsNull_When404()
    {
        var client = new Mock<SecretClient>(MockBehavior.Strict);
        client.Setup(c => c.GetSecretAsync("missing", It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "not found"));

        var value = await Sut(client).GetSecretAsync("missing", CancellationToken.None);

        value.ShouldBeNull();
    }

    [Fact]
    public async Task GetSecretAsync_RethrowsNon404Failures()
    {
        var client = new Mock<SecretClient>(MockBehavior.Strict);
        client.Setup(c => c.GetSecretAsync("broken", It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "server error"));

        await Should.ThrowAsync<RequestFailedException>(
            () => Sut(client).GetSecretAsync("broken", CancellationToken.None));
    }

    [Fact]
    public async Task SetSecretAsync_ForwardsToClient()
    {
        var client = new Mock<SecretClient>();
        var secret = new KeyVaultSecret("name", "value");
        client.Setup(c => c.SetSecretAsync("name", "value", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(secret, Mock.Of<Response>()));

        await Sut(client).SetSecretAsync("name", "value", CancellationToken.None);

        client.Verify(c => c.SetSecretAsync("name", "value", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListKeysAsync_YieldsNamesFromEachPage()
    {
        var client = new Mock<SecretClient>();
        var page = Page<SecretProperties>.FromValues(
            [new SecretProperties("secret-one"), new SecretProperties("secret-two")],
            continuationToken: null,
            response: Mock.Of<Response>());
        client.Setup(c => c.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<SecretProperties>.FromPages([page]));

        var keys = new List<string>();
        await foreach (var key in Sut(client).ListKeysAsync(CancellationToken.None))
            keys.Add(key);

        keys.ShouldBe(["secret-one", "secret-two"]);
    }
}
