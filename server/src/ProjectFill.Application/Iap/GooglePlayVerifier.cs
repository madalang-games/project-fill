using System.Text.Json;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectFill.Application.Common;

namespace ProjectFill.Application.Iap;

/// <summary>
/// Verifies a Google Play purchase receipt server-side and acknowledges it.
/// Returns the store-authoritative order id and purchase token; never trusts a client-supplied id.
/// Transient store/API outages surface <see cref="ErrorCodes.IapVerifyPending"/> so the client can
/// retry without losing the purchase; permanent failures surface <see cref="ErrorCodes.IapVerificationFailed"/>.
/// </summary>
public sealed class GooglePlayVerifier
{
    private readonly string _packageName;
    private readonly string _serviceAccountJson;
    private readonly ILogger<GooglePlayVerifier> _logger;

    public GooglePlayVerifier(IConfiguration config, ILogger<GooglePlayVerifier> logger)
    {
        _packageName        = config["GooglePlay:PackageName"]        ?? "";
        _serviceAccountJson = config["GooglePlay:ServiceAccountJson"] ?? "";
        _logger             = logger;
    }

    public async Task<(string orderId, string purchaseToken)> VerifyAndAcknowledgeAsync(
        string productSku, string receiptData, CancellationToken ct)
    {
        var (purchaseToken, orderId) = ParseReceipt(receiptData);

        if (string.IsNullOrEmpty(_serviceAccountJson))
            throw new GameApiException(ErrorCodes.IapVerificationFailed, "Google Play service account not configured.");

        try
        {
            var credential = CredentialFactory
                .FromJson<ServiceAccountCredential>(_serviceAccountJson)
                .ToGoogleCredential()
                .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

            using var service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName       = "ProjectFill"
            });

            var purchase = await service.Purchases.Products
                .Get(_packageName, productSku, purchaseToken)
                .ExecuteAsync(ct);

            if (purchase.PurchaseState != 0)
                throw new GameApiException(ErrorCodes.IapVerificationFailed, $"Purchase state is not PURCHASED: {purchase.PurchaseState}");

            if (purchase.AcknowledgementState == 0)
            {
                await service.Purchases.Products
                    .Acknowledge(new ProductPurchasesAcknowledgeRequest(), _packageName, productSku, purchaseToken)
                    .ExecuteAsync(ct);
            }
        }
        catch (GameApiException)
        {
            throw;
        }
        catch (Google.GoogleApiException ex) when (IsTransient(ex.HttpStatusCode))
        {
            // Google API unreachable/disabled/throttled (e.g. androidpublisher API not enabled,
            // 5xx, rate limit). The purchase may be valid; do not reject it. Client keeps the
            // transaction pending and re-verifies on relaunch once the outage clears.
            _logger.LogWarning(ex, "Google Play verification pending for sku={Sku}, status={Status}", productSku, ex.HttpStatusCode);
            throw new GameApiException(ErrorCodes.IapVerifyPending, "Google Play verification pending.");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Google Play verification network failure for sku={Sku}", productSku);
            throw new GameApiException(ErrorCodes.IapVerifyPending, "Google Play verification pending.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Google Play verification timeout for sku={Sku}", productSku);
            throw new GameApiException(ErrorCodes.IapVerifyPending, "Google Play verification pending.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Play verification failed for sku={Sku}", productSku);
            throw new GameApiException(ErrorCodes.IapVerificationFailed, ex.Message);
        }

        return (orderId, purchaseToken);
    }

    private static bool IsTransient(System.Net.HttpStatusCode status)
        => status == System.Net.HttpStatusCode.Forbidden          // 403 — API not enabled / propagation delay
        || status == System.Net.HttpStatusCode.RequestTimeout     // 408
        || status == System.Net.HttpStatusCode.TooManyRequests    // 429
        || (int)status >= 500;                                    // 5xx

    private static (string purchaseToken, string orderId) ParseReceipt(string receiptData)
    {
        try
        {
            using var outer    = JsonDocument.Parse(receiptData);
            var payloadStr     = outer.RootElement.GetProperty("Payload").GetString()
                ?? throw new GameApiException(ErrorCodes.IapVerificationFailed, "Receipt missing Payload.");
            using var payload  = JsonDocument.Parse(payloadStr);
            var jsonStr        = payload.RootElement.GetProperty("json").GetString()
                ?? throw new GameApiException(ErrorCodes.IapVerificationFailed, "Payload missing json field.");
            using var inner    = JsonDocument.Parse(jsonStr);
            var purchaseToken  = inner.RootElement.GetProperty("purchaseToken").GetString()
                ?? throw new GameApiException(ErrorCodes.IapVerificationFailed, "Missing purchaseToken.");
            var orderId        = inner.RootElement.GetProperty("orderId").GetString()
                ?? throw new GameApiException(ErrorCodes.IapVerificationFailed, "Missing orderId.");
            return (purchaseToken, orderId);
        }
        catch (GameApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GameApiException(ErrorCodes.IapVerificationFailed, $"Receipt parse error: {ex.Message}");
        }
    }
}
