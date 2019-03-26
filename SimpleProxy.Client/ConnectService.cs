using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace SimpleProxy.Client
{
    public class ConnectService : IHostedService
    {
        private readonly ILogger<ConnectService> _logger;

        public ConnectService(ILogger<ConnectService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            TcpClient proxyClient = new TcpClient();
            await proxyClient.ConnectAsync("cc3.xyz", 7777);
            NetworkStream proxyStream = proxyClient.GetStream();

            TcpClient localClient = new TcpClient();
            await localClient.ConnectAsync("127.0.0.1", 3389);
            NetworkStream localStream = proxyClient.GetStream();

            await RelayStreamAsync(proxyStream, localStream, cancellationToken);
            proxyClient.Dispose();
            localClient.Dispose();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task RelayStreamAsync(Stream proxyStream, Stream localStream, CancellationToken cancellationToken)
        {
            await Task.WhenAny(
                proxyStream.CopyToAsync(localStream, cancellationToken),
                localStream.CopyToAsync(proxyStream));

            proxyStream.Dispose();
            localStream.Dispose();
        }
    }
}
