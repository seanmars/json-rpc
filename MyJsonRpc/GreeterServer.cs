using JsonRpcContract;

namespace MyJsonRpc;

public class GreeterServer : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
}