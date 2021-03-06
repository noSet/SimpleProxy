﻿using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleProxy
{
    public class ListenerService : IHostedService
    {
        private readonly ILogger<ListenerService> _logger;
        private readonly IPEndPointMapping _portMapping;

        public ListenerService(ILogger<ListenerService> logger, IOptions<IPEndPointMapping> portMapping)
        {
            _logger = logger;
            _portMapping = portMapping.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            (IPEndPoint ipEndPoint0, IPEndPoint ipEndPoint1) = _portMapping.Mapping;
            MappingType mappingType = _portMapping.MappingType;

            if (mappingType == MappingType.Prot2Prot)
            {
                TcpListener tcpListener0 = new TcpListener(ipEndPoint0);
                TcpListener tcpListener1 = new TcpListener(ipEndPoint1);
                tcpListener0.Start();
                tcpListener1.Start();

                Task<TcpClient> task0 = tcpListener0.AcceptTcpClientAsync();
                Task<TcpClient> task1 = tcpListener1.AcceptTcpClientAsync();

                TcpClient[] tcpClients = await Task.WhenAll(task0, task1);

                _ = ForwardStreamAsync(tcpClients[0].GetStream(), tcpClients[1].GetStream(), cancellationToken);
            }
            else
            {
                TcpClient tcpClient0 = new TcpClient();
                TcpClient tcpClient1 = new TcpClient();
                Task task0 = tcpClient0.ConnectAsync(ipEndPoint0.Address, ipEndPoint0.Port);
                Task task1 = tcpClient1.ConnectAsync(ipEndPoint1.Address, ipEndPoint1.Port);

                await Task.WhenAll(task0, task1);

                _ = ForwardStreamAsync(tcpClient0.GetStream(), tcpClient1.GetStream(), cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task ForwardStreamAsync(Stream proxyStream, Stream localStream, CancellationToken cancellationToken)
        {
            await Task.WhenAny(
                proxyStream.CopyToAsync(localStream, cancellationToken),
                localStream.CopyToAsync(proxyStream));

            proxyStream.Dispose();
            localStream.Dispose();
        }
    }
}

