using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using TxGrpcClient.v1;
using static System.Net.WebRequestMethods;
using static TxGrpcClient.v1.ProductService;
//using static TxGrpcClient.v1.ProductService;

var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Trace);
});

//// The port number must match the port of the gRPC server.
var host1 = "http://20.78.57.37:80";
var host2 = "http://localhost:5117";
//AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
//var httpHandler = new HttpClientHandler();
//// Return `true` to allow certificates that are untrusted/invalid
//httpHandler.ServerCertificateCustomValidationCallback =
//    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
//using var channel = GrpcChannel.ForAddress("http://20.78.57.37:80");
using var channel = GrpcChannel.ForAddress(host1, new
GrpcChannelOptions
{
    //LoggerFactory = loggerFactory,
});

var productClient = new ProductServiceClient(channel);

Console.WriteLine("Create..............");
using var bidirectionnalStreamingCall = productClient.Create();
var productsToCreate = new List<ProductCreationRequest>
{
    new ProductCreationRequest {
        Name = "P1",
        Description = "P1 Description",
        Price = 1.00
    },
    new ProductCreationRequest {
        Name = "P2",
        Description = "P2 Description",
        Price = 2.00
    }
};

foreach (var productToCreate in productsToCreate)
{
    await bidirectionnalStreamingCall.RequestStream.WriteAsync
    (productToCreate);
    Console.WriteLine($"Product {productToCreate.Name} set for creation");
}

await bidirectionnalStreamingCall.RequestStream.CompleteAsync();

await foreach (var createdProduct in bidirectionnalStreamingCall.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"{createdProduct.Name} has been created with Id:{createdProduct.Id}");
}

var bidirectionnalStreamingCallHeaders = await bidirectionnalStreamingCall.ResponseHeadersAsync;
var bidirectionnalStreamingCallTrailers = bidirectionnalStreamingCall.GetTrailers();


Console.WriteLine("Get..............");

var productCall = productClient.GetAsync(new ProductIdRequest { Id = 1 });
var product = await productCall.ResponseAsync;
Console.WriteLine($"{product.Id}: {product.Name} => {product.Price}");

var productCallHeaders = await productCall.ResponseHeadersAsync;
var productCallTrailers = productCall.GetTrailers();
Console.WriteLine("Update..............");
var productUpdateRequest = new ProductUpdateRequest
{
    Id = product.Id,
    Name = "New Name",
    Description = "Change desc",
    Price = 9.99
};
var productUpdateCall = productClient.UpdateAsync(productUpdateRequest);
var productUpdate = await productCall.ResponseAsync;

var productUpdateCallHeaders = await productCall.ResponseHeadersAsync;
var productUpdateCallTrailers = productCall.GetTrailers();
Console.WriteLine("GetAll.............");
using var serverStreamingCall = productClient.GetAll(new Empty());
var ids = new List<int>();
await foreach (var response in serverStreamingCall.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"{response.Name}: {response.Description} => {response.Price}");
    ids.Add(response.Id);
}
var serverStreamingCallHeaders = await serverStreamingCall.ResponseHeadersAsync;
var serverStreamingCallTrailers = serverStreamingCall.GetTrailers();


Console.WriteLine("Delete..............");
using var clientStreamingCall = productClient.Delete();
foreach (var id in ids)
{
    await clientStreamingCall.RequestStream.WriteAsync(new ProductIdRequest { Id = id });
    Console.WriteLine($"Product Id {id} Will be deleted.");
}

await clientStreamingCall.RequestStream.CompleteAsync();
var emptyResponse = await clientStreamingCall.ResponseAsync;

var clientStreamingCallHeaders = await clientStreamingCall.ResponseHeadersAsync;
var clientStreamingCallTrailers = clientStreamingCall.GetTrailers();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

