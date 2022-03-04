using Microsoft.AspNetCore.HttpOverrides;
using AppInspectServices.Models;
using Microsoft.Extensions.Options;
using AppInspectServices;
using AppInspectServices.Hubs;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection($"AppInspect{nameof(DatabaseSettings)}"));

builder.Services.Configure<ServicesSettings>(
    builder.Configuration.GetSection($"AppInspect{nameof(ServicesSettings)}"));

builder.Services.AddSingleton<DatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);

builder.Services.AddSingleton<ServicesSettings>(sp =>
    sp.GetRequiredService<IOptions<ServicesSettings>>().Value);


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
});

builder.Services.AddControllers();
builder.Services.AddSignalR(hubOptions => {  hubOptions.MaximumReceiveMessageSize = 1048576; });
builder.Services.AddSingleton<AppInspectData>();


var app = builder.Build();
// Configure the HTTP request pipeline.

app.UseCors("AllowAll");

//app.UseCors(option => option.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthorization();

app.MapControllers();

app.MapHub<AppInspectRPCHub>("/rpc");

app.Run();
