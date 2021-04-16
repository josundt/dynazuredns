using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace jasMIN.DynAzureDns
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors", Justification = "<Pending>")]
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var service = host.Services.GetService<IDynAzureDnsService>()!;
            await service.UpdateDnsIfExternalIpChangedAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<DynAzureDnsOptions>(hostContext.Configuration.GetSection(DynAzureDnsOptions.SectionName));
                    services.AddSingleton<IDynAzureDnsService, DynAzureDnsService>();
                });

    }
}
