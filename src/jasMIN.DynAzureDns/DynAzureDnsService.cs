using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jasMIN.DynAzureDns
{
    public class DynAzureDnsService : IDynAzureDnsService
    {
        private readonly IOptions<DynAzureDnsOptions> _optionsWrapper;
        private readonly ILogger<DynAzureDnsService> _logger;

        public DynAzureDnsService(IOptions<DynAzureDnsOptions> optionsWrapper, ILogger<DynAzureDnsService> logger)
        {
            this._optionsWrapper = optionsWrapper;
            this._logger = logger;
        }

        public async Task<bool> UpdateDnsIfExternalIpChangedAsync(CancellationToken cancellationToken = default)
        {
            var stopWatch = Stopwatch.StartNew();
            var options = this._optionsWrapper.Value;
            this._logger.LogInformation($"[{DateTimeOffset.Now:g}] Starting Dynamic DNS Update ");
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
            this._logger.LogInformation($"[{DateTimeOffset.Now:g}] Ending Dynamic DNS Update (time taken: {stopWatch.ElapsedMilliseconds/1_000}s)");
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
}
