using System;

namespace jasMIN.DynAzureDns
{
#nullable disable

    public class DynAzureDnsOptions
    {
        public const string SectionName = "DynAzureDns";
        public Uri ExternalIpProviderUrl { get; set; }
        public string AzureAdTenantId { get; set; }
        public string AzureSubscriptionId { get; set; }
        public string ServicePrincipalClientId { get; set; }
        public string ServicePrincipalClientSecret { get; set; }
        public string DnsResourceGroup { get; set; }
        public string DnsZoneName { get; set; }
        public string DnsRecordName { get; set; }
    }

#nullable restore
}
