using Whisprr.Infrastructure.MessageBroker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddMessageBroker();

var host = builder.Build();
host.Run();
