using Microsoft.Extensions.Configuration;

namespace Roms.Web.Ai;

/// <summary>
/// Central fail-closed gate for the future AI feature.
///
/// The hold is intentionally independent from <c>Ai:Enabled</c>: an operator
/// cannot accidentally re-enable the assistant by carrying an old environment
/// variable into the current release.
/// </summary>
public static class AiFeatureGate
{
    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Ai:Enabled") &&
        !configuration.GetValue<bool>("Ai:Hold");
}
