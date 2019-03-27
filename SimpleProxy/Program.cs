using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleProxy
{
    public static class Program
    {
        private static readonly Dictionary<string, string> s_switchMappings
            = new Dictionary<string, string>
            {
                ["-l"] = "listen",
                ["-s"] = "slave"
            };

        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "PREFIX_");
                    configHost.AddCommandLine(args, s_switchMappings);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                        $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                    configApp.AddCommandLine(args, s_switchMappings);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions().Configure<IPEndPointMapping>(portmapping => portmapping.Mapping = GetMapping(hostContext.Configuration));

                    services.AddHostedService<ListenerService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

        public static KeyValuePair<IPEndPoint, IPEndPoint> GetMapping(IConfiguration configuration)
        {
            var config = configuration.GetSection("listen");
            if (config.Value != null)
            {
                var ports = config.Value.Split("/");
                return new KeyValuePair<IPEndPoint, IPEndPoint>(new IPEndPoint(IPAddress.Any, int.Parse(ports[0])), new IPEndPoint(IPAddress.Any, int.Parse(ports[1])));
            }

            config = configuration.GetSection("slave");
            if (config.Value != null)
            {
                var ipEndPoints = config.Value.Split("/");
                var ipAddress0 = ipEndPoints[0].Split(":")[0];
                var ipAddress1 = ipEndPoints[1].Split(":")[0];
                var port0 = ipEndPoints[0].Split(":")[1];
                var port1 = ipEndPoints[1].Split(":")[1];

                return new KeyValuePair<IPEndPoint, IPEndPoint>(new IPEndPoint(IPAddress.Parse(ipAddress0), int.Parse(port0)), new IPEndPoint(IPAddress.Parse(ipAddress1), int.Parse(port1)));
            }

            throw new System.Exception("配置不能为空！");
        }
    }
}