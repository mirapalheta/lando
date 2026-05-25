using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace Lando.FunctionApp.Security;

/// <inheritdoc />
public class KeyVaultSecretClient(SecretClient client, ILogger<KeyVaultSecretClient> logger) : ISecretClient
{
    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await client
                .GetSecretAsync(secretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return token.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning(ex, "Secret '{SecretName}' not found in Key Vault", secretName);
            return default;
        }
    }

    /// <inheritdoc />
    public Task SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
        => client.SetSecretAsync(name, value, cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListKeysAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            yield return secretProperties.Name;
        }
    }
}
