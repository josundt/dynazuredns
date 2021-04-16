using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
                .UseEnvironment(Debugger.IsAttached ? "Development" : "Production")
                .ConfigureLogging(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                    })
                )
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<DynAzureDnsOptions>(hostContext.Configuration.GetSection(DynAzureDnsOptions.SectionName));
                    services.AddSingleton<IDynAzureDnsService, DynAzureDnsService>();
                });

    }
}
