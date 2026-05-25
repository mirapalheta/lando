using System;
using Lando.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.FunctionApp.Security.Tests;

/// <summary>
/// <see cref="TokenStoreFactory"/> is a thin wrapper that constructs a
/// <see cref="TokenStore"/> bound to a name + <see cref="ITokenClient"/>.
/// Tests pin the argument-guard surface (null/whitespace name, null
/// tokenClient) and that Create yields a usable store.
/// </summary>
public class TokenStoreFactoryTests
{
    private static TokenStoreFactory Build()
        => new(Mock.Of<ISecretClient>(), new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);

    [Fact]
    public void Create_returns_non_null_store_when_args_valid()
    {
        var sut = Build();
        var tokenClient = new Mock<ITokenClient>().Object;

        sut.Create("skill", tokenClient).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_throws_ArgumentException_for_blank_name(string? name)
    {
        var sut = Build();

        Should.Throw<ArgumentException>(() => sut.Create(name!, new Mock<ITokenClient>().Object));
    }

    [Fact]
    public void Create_throws_ArgumentNullException_for_null_tokenClient()
    {
        var sut = Build();

        Should.Throw<ArgumentNullException>(() => sut.Create("skill", null!));
    }
}
