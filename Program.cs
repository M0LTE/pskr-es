using pskr_es;
using pskr_es.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PskrListener>();

var host = builder.Build();
host.Run();
