using System.Threading;
using System.Threading.Tasks;

namespace jasMIN.DynAzureDns
{
    interface IDynAzureDnsService
    {
        Task<bool> UpdateDnsIfExternalIpChangedAsync(CancellationToken cancellationToken = default);
    }
}