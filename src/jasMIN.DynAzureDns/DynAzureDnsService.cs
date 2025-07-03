using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace jasMIN.DynAzureDns;

public class DynAzureDnsService(
    IOptions<DynAzureDnsOptions> optionsWrapper,
    ILoggerFactory loggerFactory
)
    : IDynAzureDnsService
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("DynAzureDns");

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
    public async Task<bool> UpdateDnsIfExternalIpChangedAsync(CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();
        var options = optionsWrapper.Value;
        this._logger.LogInformation($"DYNAMIC DNS UPDATE STARTING");
        this._logger.LogInformation("Getting external/public IP Address...");
        var externalIp = await GetExternalIpAddressAsync(options.ExternalIpProviderUrl, cancellationToken);
        this._logger.LogInformation($"Public IP Address is {externalIp}");
        this._logger.LogInformation($"Updating Azure DNS record {options.DnsRecordName}.{options.DnsZoneName}...");
        var wasUpdated = await UpdateAzureDnsRecordAsync(options, externalIp, cancellationToken);
        stopWatch.Stop();
        if (wasUpdated)
        {
            this._logger.LogInformation($"Azure DNS record {options.DnsRecordName}.{options.DnsZoneName} was updated");
        }
        else
        {
            this._logger.LogInformation($"Azure DNS record {options.DnsRecordName}.{options.DnsZoneName} was not updated (ip address unchanged)");
        }
        this._logger.LogInformation($"DYNAMIC DNS UPDATE ENDED (time taken: {stopWatch.ElapsedMilliseconds / 1_000}s)");
        return wasUpdated;
    }

    private static async Task<string> GetExternalIpAddressAsync(Uri providerUrl, CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, providerUrl);
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return result;
    }

    private static async Task<bool> UpdateAzureDnsRecordAsync(DynAzureDnsOptions options, string ipAddress, CancellationToken cancellationToken)
    {
        var clientCredentials = new ServicePrincipalLoginInformation
        {
            ClientId = options.ServicePrincipalClientId,
            ClientSecret = options.ServicePrincipalClientSecret
        };
        var credentials = new AzureCredentials(clientCredentials, options.AzureAdTenantId, AzureEnvironment.AzureGlobalCloud);
        var zoneManager = DnsZoneManager.Authenticate(credentials, options.AzureSubscriptionId);

        // Switching to non-fluent-api
        var dnsClient = zoneManager.Inner;

        var recordSet = await dnsClient.RecordSets.GetAsync(
            options.DnsResourceGroup,
            options.DnsZoneName,
            options.DnsRecordName,
            RecordType.A,
            cancellationToken
        );

        if (recordSet.ARecords.Count == 1 && recordSet.ARecords[0].Ipv4Address == ipAddress)
        {
            return false;
        }
        else
        {
            recordSet.ARecords.Clear();
            recordSet.ARecords.Add(new ARecord(ipAddress));

            var beforeUpdateEtag = recordSet.Etag;

            // Note: ETAG check specified, update will be rejected if the record set has changed in the meantime
            recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(
                options.DnsResourceGroup,
                options.DnsZoneName,
                options.DnsRecordName,
                RecordType.A,
                recordSet,
                recordSet.Etag,
                null,
                cancellationToken
            );

            return recordSet.Etag != beforeUpdateEtag;
        }
    }

}
