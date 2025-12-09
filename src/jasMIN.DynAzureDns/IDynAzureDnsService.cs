namespace jasMIN.DynAzureDns;

internal interface IDynAzureDnsService {
    Task<bool> UpdateDnsIfExternalIpChangedAsync(CancellationToken cancellationToken = default);
}
