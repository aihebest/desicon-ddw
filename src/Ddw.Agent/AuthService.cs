using System.IO;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Ddw.Agent;

// Entra ID sign-in for the agent. Interactive once (system browser), then silent
// forever via a DPAPI-encrypted token cache. Works on domain and standalone PCs
// — all that's needed is the employee's Microsoft 365 account.
public static class AuthService
{
    private const string ClientId = "cdc21c25-679e-4f62-a157-86f438e57f85";
    private const string TenantId = "77e479f1-fefd-4238-a1a9-6b1f692f20b8";
    // Use the canonical App ID URI so the token audience matches the API (AzureAd__Audience=api://ddw-api).
    private static readonly string[] Scopes = { "api://ddw-api/access_as_user" };

    private static IPublicClientApplication? _app;

    private static async Task<IPublicClientApplication> GetAppAsync()
    {
        if (_app is not null) return _app;

        var app = PublicClientApplicationBuilder.Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
            .WithRedirectUri("http://localhost")
            .Build();

        // Persist tokens (encrypted) so the user only signs in once.
        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesiconAgent");
        Directory.CreateDirectory(cacheDir);
        var storage = new StorageCreationPropertiesBuilder("msal_cache.bin", cacheDir).Build();
        var helper = await MsalCacheHelper.CreateAsync(storage);
        helper.RegisterCache(app.UserTokenCache);

        _app = app;
        return _app;
    }

    /// <summary>Returns (access token, signed-in UPN). Interactive only when needed.</summary>
    public static async Task<(string token, string upn)> GetTokenAsync(bool allowInteractive)
    {
        var app = await GetAppAsync();
        var accounts = await app.GetAccountsAsync();
        AuthenticationResult result;
        try
        {
            result = await app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault()).ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            if (!allowInteractive) throw;
            result = await app.AcquireTokenInteractive(Scopes)
                .WithUseEmbeddedWebView(false) // system browser — works everywhere
                .ExecuteAsync();
        }
        return (result.AccessToken, result.Account?.Username ?? "");
    }
}
