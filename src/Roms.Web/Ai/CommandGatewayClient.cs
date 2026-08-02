using System.Net.Http.Json;
using Roms.Application.Commands;

namespace Roms.Web.Ai;

public sealed class CommandGatewayClient(HttpClient http) : ICommandGatewayClient
{
    public async Task<InterpretCommandResponse> InterpretAsync(
        InterpretCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "v1/interpret", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InterpretCommandResponse>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("The command gateway returned an empty response.");
    }
}
