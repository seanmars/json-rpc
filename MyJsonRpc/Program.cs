using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyJsonRpc;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.TryAddSingleton<ITransportFactory, SocketTransportFactory>();
        services.AddHostedService<StreamJsonRpcHost>();
    })
    .Build();

await host.RunAsync();