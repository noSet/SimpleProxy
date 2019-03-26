using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleProxy
{
    public class ListenerService : IHostedService
    {
        private readonly ILogger<ListenerService> _logger;

        public ListenerService(ILogger<ListenerService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 7777);
            tcpListener.Start();
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                _ = ProcessTcpClient(tcpClient, cancellationToken);
            }
        }

        private async Task ProcessTcpClient(TcpClient clientClient, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await Task.CompletedTask;
            }

            TcpListener proxyListener = new TcpListener(IPAddress.Any, 9974);
            TcpClient proxyClient = await proxyListener.AcceptTcpClientAsync();

            NetworkStream proxyStream = proxyClient.GetStream();
            NetworkStream clientStream = clientClient.GetStream();

            await RelayStreamAsync(proxyStream, clientStream, cancellationToken);

            proxyClient.Dispose();
            clientClient.Dispose();
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

