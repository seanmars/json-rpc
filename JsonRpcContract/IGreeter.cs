namespace JsonRpcContract;

public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request);
}