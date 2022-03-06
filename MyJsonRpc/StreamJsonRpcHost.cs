using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Connections;
using StreamJsonRpc;

namespace MyJsonRpc;

public class StreamJsonRpcHost : BackgroundService
{
    private readonly ILogger<StreamJsonRpcHost> _logger;
    private readonly IConnectionListenerFactory _connectionListenerFactory;
    private readonly ConcurrentDictionary<string, (ConnectionContext Context, Task ExecutionTask)> _connections = new();

    private IConnectionListener _connectionListener = null!;

    public StreamJsonRpcHost(ILogger<StreamJsonRpcHost> logger, IConnectionListenerFactory connectionListenerFactory)
    {
        _logger = logger;
        _connectionListenerFactory = connectionListenerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = await Dns.GetHostEntryAsync("localhost", stoppingToken);
        var ipAddress = host.AddressList[0];
        var ipe = new IPEndPoint(ipAddress, 11000);
        var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(ipe, stoppingToken);

        socket.Listen();
        _logger.LogInformation("Listening for connections");

        while (true)
        {
            var connectionContext = await socket.AcceptAsync(stoppingToken);
            _logger.LogInformation("Accepted connection");

            _connections[connectionContext.ConnectionId] = (connectionContext, AcceptAsync(connectionContext));
        }

        var connectionsExecutionTasks = new List<Task>(_connections.Count);
        foreach (var (_, (context, executionTask)) in _connections)
        {
            connectionsExecutionTasks.Add(executionTask);
            context.Abort();
        }

        await Task.WhenAll(connectionsExecutionTasks);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _connectionListener.DisposeAsync();
    }

    private async Task AcceptAsync(ConnectionContext connectionContext)
    {
        try
        {
            await Task.Yield();

            var jsonRpcMessageFormatter = new JsonMessageFormatter(Encoding.UTF8);
            var jsonRpcMessageHandler = new LengthHeaderMessageHandler(
                connectionContext.Transport,
                jsonRpcMessageFormatter);

            using var jsonRpc = new JsonRpc(jsonRpcMessageHandler, new GreeterServer());
            jsonRpc.StartListening();

            await jsonRpc.Completion;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Connection {ConnectionId} threw an exception", connectionContext.ConnectionId);
        }
        finally
        {
            await connectionContext.DisposeAsync();
            _connections.TryRemove(connectionContext.ConnectionId, out _);
        }
    }
}