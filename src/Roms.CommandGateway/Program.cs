using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Roms.Application.Commands;
using Roms.CommandGateway;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<OllamaOptions>>().Value);
builder.Services.AddSingleton<CommandProposalValidator>();
builder.Services.AddHttpClient<ICommandInterpretationService,
    OllamaCommandInterpretationService>((sp, client) =>
    {
        var baseUrl = sp.GetRequiredService<IConfiguration>()["Ollama:BaseUrl"]
            ?? "http://ollama:11434/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(45);
    });

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    schemaVersion = RestaurantCommandProtocol.CurrentSchemaVersion
}));
app.MapPost("/v1/interpret", async (
    InterpretCommandRequest request,
    ICommandInterpretationService interpreter,
    CancellationToken cancellationToken) =>
    Results.Ok(await interpreter.InterpretAsync(request, cancellationToken)));
app.Run();

public partial class Program;
