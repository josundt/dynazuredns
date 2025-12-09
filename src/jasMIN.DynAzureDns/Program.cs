using jasMIN.DynAzureDns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

var host = CreateHostBuilder(args).Build();
var service = host.Services.GetService<IDynAzureDnsService>()!;
await service.UpdateDnsIfExternalIpChangedAsync();

static IHostBuilder CreateHostBuilder(string[] args) {
    return Host.CreateDefaultBuilder(args)
        .UseEnvironment(Debugger.IsAttached ? "Development" : "Production")
        .ConfigureLogging(builder =>
            builder.AddSimpleConsole(options => {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            })
        )
        .ConfigureServices((hostContext, services) => {
            services.Configure<DynAzureDnsOptions>(hostContext.Configuration.GetSection(DynAzureDnsOptions.SectionName));
            services.AddSingleton<IDynAzureDnsService, DynAzureDnsService>();
        });
}
