PowerShell 7.7.0-preview.3
PS D:\ARCWorks_Restaurant Suite> dotnet add package Microsoft.AspNetCore.Mvc.Testing
Could not find any project in `D:\ARCWorks_Restaurant Suite\`.
PS D:\ARCWorks_Restaurant Suite> dotnet add ".\YourTestProjectFolder" package Microsoft.AspNetCore.Mvc.Testing
Could not find project or directory `.\YourTestProjectFolder`.
PS D:\ARCWorks_Restaurant Suite> dotnet add ".\YourTestProjectFolder\YourTestProject.csproj" package Microsoft.AspNetCore.Mvc.Testing
Could not find project or directory `.\YourTestProjectFolder\YourTestProject.csproj`.
PS D:\ARCWorks_Restaurant Suite> cd ".\YourTestProjectFolder"
Set-Location: Cannot find path 'D:\ARCWorks_Restaurant Suite\YourTestProjectFolder' because it does not exist.
PS D:\ARCWorks_Restaurant Suite> dotnet add package Microsoft.AspNetCore.Mvc.Testing
Could not find any project in `D:\ARCWorks_Restaurant Suite\`.
PS D:\ARCWorks_Restaurant Suite> Get-ChildItem -Filter *.csproj -Recurse | Select-Object Name, DirectoryName

Name                    DirectoryName
----                    -------------
template-compare.csproj D:\ARCWorks_Restaurant Suite\.artifacts\template-compare
Roms.Application.csproj D:\ARCWorks_Restaurant Suite\src\Roms.Application
Roms.CommandGateway.cs… D:\ARCWorks_Restaurant Suite\src\Roms.CommandGateway
Roms.Domain.csproj      D:\ARCWorks_Restaurant Suite\src\Roms.Domain
Roms.Infrastructure.cs… D:\ARCWorks_Restaurant Suite\src\Roms.Infrastructure
Roms.Web.csproj         D:\ARCWorks_Restaurant Suite\src\Roms.Web
Roms.CommandGateway.Te… D:\ARCWorks_Restaurant Suite\tests\Roms.CommandGateway.…
Roms.Domain.Tests.cspr… D:\ARCWorks_Restaurant Suite\tests\Roms.Domain.Tests
Roms.IntegrationTests.… D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests

PS D:\ARCWorks_Restaurant Suite> dotnet add ".\tests\Roms.IntegrationTests" package Microsoft.AspNetCore.Mvc.Testing
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.AspNetCore.Mvc.Testing' into project 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/index.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/index.json 208ms
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/0.0.1-alpha/3.1.30.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/0.0.1-alpha/3.1.30.json 200ms
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/3.1.31/6.0.25.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/3.1.31/6.0.25.json 201ms
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/6.0.26/8.0.16.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/6.0.26/8.0.16.json 204ms
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/8.0.17/11.0.0-preview.3.26207.106.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/8.0.17/11.0.0-preview.3.26207.106.json 196ms
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/11.0.0-preview.4.26230.115/11.0.0-preview.6.26359.118.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/11.0.0-preview.4.26230.115/11.0.0-preview.6.26359.118.json 197ms
info : Restoring packages for D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj...
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.mvc.testing/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.mvc.testing/index.json 214ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.mvc.testing/10.0.10/microsoft.aspnetcore.mvc.testing.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.mvc.testing/10.0.10/microsoft.aspnetcore.mvc.testing.10.0.10.nupkg 13ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.testhost/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.testhost/index.json 211ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.testhost/10.0.10/microsoft.aspnetcore.testhost.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.testhost/10.0.10/microsoft.aspnetcore.testhost.10.0.10.nupkg 14ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting/index.json 287ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting/10.0.10/microsoft.extensions.hosting.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting/10.0.10/microsoft.extensions.hosting.10.0.10.nupkg 21ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.commandline/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.fileextensions/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.environmentvariables/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.json/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.usersecrets/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.abstractions/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.physical/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting.abstractions/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.configuration/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.console/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.debug/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventlog/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventsource/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.fileextensions/index.json 222ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.fileextensions/10.0.10/microsoft.extensions.configuration.fileextensions.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.environmentvariables/index.json 227ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.environmentvariables/10.0.10/microsoft.extensions.configuration.environmentvariables.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.physical/index.json 227ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.physical/10.0.10/microsoft.extensions.fileproviders.physical.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventlog/index.json 227ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventlog/10.0.10/microsoft.extensions.logging.eventlog.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.console/index.json 234ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting.abstractions/index.json 237ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.debug/index.json 236ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.console/10.0.10/microsoft.extensions.logging.console.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.environmentvariables/10.0.10/microsoft.extensions.configuration.environmentvariables.10.0.10.nupkg 12ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting.abstractions/10.0.10/microsoft.extensions.hosting.abstractions.10.0.10.nupkg
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.debug/10.0.10/microsoft.extensions.logging.debug.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.configuration/index.json 239ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.usersecrets/index.json 244ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.configuration/10.0.10/microsoft.extensions.logging.configuration.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.physical/10.0.10/microsoft.extensions.fileproviders.physical.10.0.10.nupkg 13ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.usersecrets/10.0.10/microsoft.extensions.configuration.usersecrets.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.fileextensions/10.0.10/microsoft.extensions.configuration.fileextensions.10.0.10.nupkg 23ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventlog/10.0.10/microsoft.extensions.logging.eventlog.10.0.10.nupkg 16ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.hosting.abstractions/10.0.10/microsoft.extensions.hosting.abstractions.10.0.10.nupkg 14ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.commandline/index.json 257ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.configuration/10.0.10/microsoft.extensions.logging.configuration.10.0.10.nupkg 14ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.commandline/10.0.10/microsoft.extensions.configuration.commandline.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.console/10.0.10/microsoft.extensions.logging.console.10.0.10.nupkg 19ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.usersecrets/10.0.10/microsoft.extensions.configuration.usersecrets.10.0.10.nupkg 13ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.commandline/10.0.10/microsoft.extensions.configuration.commandline.10.0.10.nupkg 12ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.debug/10.0.10/microsoft.extensions.logging.debug.10.0.10.nupkg 31ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventsource/index.json 322ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventsource/10.0.10/microsoft.extensions.logging.eventsource.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.eventsource/10.0.10/microsoft.extensions.logging.eventsource.10.0.10.nupkg 13ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.abstractions/index.json 443ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.abstractions/10.0.10/microsoft.extensions.fileproviders.abstractions.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.json/index.json 449ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.json/10.0.10/microsoft.extensions.configuration.json.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.fileproviders.abstractions/10.0.10/microsoft.extensions.fileproviders.abstractions.10.0.10.nupkg 12ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.json/10.0.10/microsoft.extensions.configuration.json.10.0.10.nupkg 30ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.filesystemglobbing/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/system.diagnostics.eventlog/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/system.diagnostics.eventlog/index.json 200ms
info :   GET https://api.nuget.org/v3-flatcontainer/system.diagnostics.eventlog/10.0.10/system.diagnostics.eventlog.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.filesystemglobbing/index.json 232ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.extensions.filesystemglobbing/10.0.10/microsoft.extensions.filesystemglobbing.10.0.10.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/system.diagnostics.eventlog/10.0.10/system.diagnostics.eventlog.10.0.10.nupkg 30ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.extensions.filesystemglobbing/10.0.10/microsoft.extensions.filesystemglobbing.10.0.10.nupkg 13ms
info : Installed Microsoft.AspNetCore.TestHost 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.aspnetcore.testhost\10.0.10 with content hash Kks+OpQlP/eWQhnTjkiv0H9kc9Uqa7ieAzNV62yJpZ8Ips/WwpfpwrwZUKzV83CBJaBl5OI7J2XxVVTC8vah/Q==.
info : Installed Microsoft.AspNetCore.Mvc.Testing 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.aspnetcore.mvc.testing\10.0.10 with content hash pTWE4RtRbb9sl/U9QjZA5oapEZ01ZEMfRilZvvh55ZW97caTQ2XuAF6sgc+7ojKWBbR2qrcWVo5P80gMPuW/tQ==.
info : Installed Microsoft.Extensions.Configuration.FileExtensions 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.configuration.fileextensions\10.0.10 with content hash ZOhZYwvbXGTgGVRwswIirofEMVHuWdxjdh0JeUZXwaF9cgcjXdz/t0ELtgaevw7ezTyv47yPNCgGreWtLkn3IQ==.
info : Installed Microsoft.Extensions.Logging.EventSource 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.logging.eventsource\10.0.10 with content hash 85SAPwXhJtdBInzN2k7SChiFiBGh3KOWay5AfoY+GREF6P7oZA98+ST2p7Z9384iLKYjkZSKIZ/FqIO5aojtNw==.
info : Installed Microsoft.Extensions.Logging.EventLog 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.logging.eventlog\10.0.10 with content hash 0RE4951AzQ+YD4gVrvbq0BhdsiBgSDo44yM7+QBZ2mrmMJeNjY+teCIYfUjqDPVYnKs0HR6SkkhgrX1YgXZq3Q==.
info : Installed Microsoft.Extensions.Configuration.CommandLine 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.configuration.commandline\10.0.10 with content hash 33cBeR2HRbzHUTtmcmLdNOApneNGcymwwL4arHuotgVK9Frba8kcDTrvVTj7cSCmF1R9OiSbZH0KxNOwab3HUg==.
info : Installed Microsoft.Extensions.Logging.Debug 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.logging.debug\10.0.10 with content hash 8+TZBnV5fgBXoVNJ5ROSErUwYogk4hOgV7c2HWK1u5cqKGmiUTUn7+KqZ35iQu8e/B7Ykccyz5OTjdXcidNZ9g==.
info : Installed Microsoft.Extensions.Configuration.EnvironmentVariables 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.configuration.environmentvariables\10.0.10 with content hash KRfFSSCV58vEdU7mPED/YMzeovIWF5P0g8s9K8n9HEfy0/WzMq37SrPdXdFN5/dFT/rPMHpF7AvpoXHckbcBFg==.
info : Installed Microsoft.Extensions.FileProviders.Abstractions 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.fileproviders.abstractions\10.0.10 with content hash c5zqFCY9DiIpMovLd7/d/CTiEtrMOuQ639dhv3PABtKQIKNQikSHwQt8+N679uii9q+B55lgK28Uv64FOwEu8w==.
info : Installed Microsoft.Extensions.FileSystemGlobbing 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.filesystemglobbing\10.0.10 with content hash jSOCVxEwCd4Aq925kJVz1kSO1EpX2OHYKL04qVREXkDU7Ce3pVDdHPYm+fEy8y/th2kJf/DAstRHpJAqoNWP8w==.
info : Installed Microsoft.Extensions.Logging.Console 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.logging.console\10.0.10 with content hash VIlNzPwPS0GeQVSmCqqo36ugryX3LpE9ul6gEkks5VLET3weH/XMLeWmclwfoGn4Nxi2mwVibB+OZBVJ9tDqvg==.
info : Installed Microsoft.Extensions.Configuration.Json 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.configuration.json\10.0.10 with content hash uvJ6sHwjgrkMEJOgiC76G0mcZGXerwyyWkwX34EOjCbxKG6TCtfAoqDKAMsCvEBf9HxjlGQEgqsSMOGCmGBf+A==.
info : Installed Microsoft.Extensions.Hosting 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.hosting\10.0.10 with content hash tL9FkfV64GPUDSPvwrgyw42LVzsnVAnyrqJEuZVJbODgrQ3eL63zmzEcVWoCHzfgqUhWggzbgAyUCnz/zfI3Pg==.
info : Installed Microsoft.Extensions.Hosting.Abstractions 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.hosting.abstractions\10.0.10 with content hash 5LugpYGHk+mkn0a8IZgcyfBca8PCTAU9RQFoMrTdtOOidq88M2SI5f3px6ugnzgxC+eTkvYYJi8pzlUnG5xdAQ==.
info : Installed Microsoft.Extensions.Configuration.UserSecrets 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.configuration.usersecrets\10.0.10 with content hash 1s1sKFTk/Foam64JY6+m/diH8drL3Wx6V3gtSd5v1IEZtszZYyc1pW8uRnMblzpNiR0l0t8gGk7tXj3xHzFgdg==.
info : Installed System.Diagnostics.EventLog 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\system.diagnostics.eventlog\10.0.10 with content hash OvGz3PrzuAI/Sj7LTcXcCe3FClRI1IyRMZjNONcZtFh+Ww7nAtSh4kh08r8KVe/xxkXJPjR0Y1jF7H+N42d4xQ==.
info : Installed Microsoft.Extensions.Logging.Configuration 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.logging.configuration\10.0.10 with content hash cLrqxkuEfcilZ8SjK+9KAnpLk9lOoMPaOokF+wRUYie+iUEcdX4/p/+gJkt0BYgWLthjpBUCkVTBI6Kxg0nsOw==.
info : Installed Microsoft.Extensions.FileProviders.Physical 10.0.10 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.extensions.fileproviders.physical\10.0.10 with content hash jhJAyo38kSrH3ARvWUk0h8itogVnQu2DCZuPo+s0Z+tXes0ugTxMPaHYzap85785eHQmPFqD9TYERqBbtGxn/w==.
info :   GET https://api.nuget.org/v3/vulnerabilities/index.json
info :   OK https://api.nuget.org/v3/vulnerabilities/index.json 194ms
info :   GET https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/vulnerability.base.json
info :   GET https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/2026.07.29.05.43.51/vulnerability.update.json
info :   OK https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/vulnerability.base.json 195ms
info :   OK https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/2026.07.29.05.43.51/vulnerability.update.json 194ms
info : Package 'Microsoft.AspNetCore.Mvc.Testing' is compatible with all the specified frameworks in project 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info : PackageReference for package 'Microsoft.AspNetCore.Mvc.Testing' version '10.0.10' added to file 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info : Generating MSBuild file D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\obj\Roms.IntegrationTests.csproj.nuget.g.props.
info : Generating MSBuild file D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\obj\Roms.IntegrationTests.csproj.nuget.g.targets.
info : Writing assets file to disk. Path: D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\obj\project.assets.json
log  : Restored D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj (in 2.58 sec).
PS D:\ARCWorks_Restaurant Suite> dotnet add ".\tests\Roms.IntegrationTests" package Microsoft.AspNetCore.Mvc.Testing
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.AspNetCore.Mvc.Testing' into project 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/index.json
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/0.0.1-alpha/3.1.30.json
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/3.1.31/6.0.25.json
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/6.0.26/8.0.16.json
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/8.0.17/11.0.0-preview.3.26207.106.json
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/page/11.0.0-preview.4.26230.115/11.0.0-preview.6.26359.118.json
info : Restoring packages for D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/2026.07.29.05.43.51/vulnerability.update.json
info : Package 'Microsoft.AspNetCore.Mvc.Testing' is compatible with all the specified frameworks in project 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info : PackageReference for package 'Microsoft.AspNetCore.Mvc.Testing' version '10.0.10' updated in file 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info : Assets file has not changed. Skipping assets file writing. Path: D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\obj\project.assets.json
log  : Restored D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj (in 278 ms).
PS D:\ARCWorks_Restaurant Suite> dotnet add ".\tests\Roms.IntegrationTests" package Microsoft.Playwright.NUnit
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Playwright.NUnit' into project 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info :   GET https://api.nuget.org/v3/registration5-gz-semver2/microsoft.playwright.nunit/index.json
info :   OK https://api.nuget.org/v3/registration5-gz-semver2/microsoft.playwright.nunit/index.json 556ms
info : Restoring packages for D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj...
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.playwright.nunit/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.playwright.nunit/index.json 275ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.playwright.nunit/1.61.0/microsoft.playwright.nunit.1.61.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.playwright.nunit/1.61.0/microsoft.playwright.nunit.1.61.0.nupkg 233ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.playwright.testadapter/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.playwright/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/nunit/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/nunit3testadapter/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.playwright.testadapter/index.json 213ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.playwright.testadapter/1.61.0/microsoft.playwright.testadapter.1.61.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/nunit/index.json 231ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.playwright/index.json 232ms
info :   GET https://api.nuget.org/v3-flatcontainer/nunit/3.13.2/nunit.3.13.2.nupkg
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.playwright/1.61.0/microsoft.playwright.1.61.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/nunit3testadapter/index.json 241ms
info :   GET https://api.nuget.org/v3-flatcontainer/nunit3testadapter/4.0.0/nunit3testadapter.4.0.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/nunit/3.13.2/nunit.3.13.2.nupkg 21ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.playwright/1.61.0/microsoft.playwright.1.61.0.nupkg 24ms
info :   OK https://api.nuget.org/v3-flatcontainer/nunit3testadapter/4.0.0/nunit3testadapter.4.0.0.nupkg 35ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.playwright.testadapter/1.61.0/microsoft.playwright.testadapter.1.61.0.nupkg 267ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.testplatform.objectmodel/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.testplatform.objectmodel/index.json 248ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.testplatform.objectmodel/17.3.0/microsoft.testplatform.objectmodel.17.3.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.testplatform.objectmodel/17.3.0/microsoft.testplatform.objectmodel.17.3.0.nupkg 260ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.bcl.asyncinterfaces/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/netstandard.library/index.json
info :   GET https://api.nuget.org/v3-flatcontainer/system.componentmodel.annotations/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/system.componentmodel.annotations/index.json 202ms
info :   GET https://api.nuget.org/v3-flatcontainer/system.componentmodel.annotations/5.0.0/system.componentmodel.annotations.5.0.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/system.componentmodel.annotations/5.0.0/system.componentmodel.annotations.5.0.0.nupkg 14ms
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.bcl.asyncinterfaces/index.json 246ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.bcl.asyncinterfaces/6.0.0/microsoft.bcl.asyncinterfaces.6.0.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.bcl.asyncinterfaces/6.0.0/microsoft.bcl.asyncinterfaces.6.0.0.nupkg 20ms
info :   OK https://api.nuget.org/v3-flatcontainer/netstandard.library/index.json 363ms
info :   GET https://api.nuget.org/v3-flatcontainer/netstandard.library/2.0.0/netstandard.library.2.0.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/netstandard.library/2.0.0/netstandard.library.2.0.0.nupkg 16ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.netcore.platforms/index.json
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.netcore.platforms/index.json 244ms
info :   GET https://api.nuget.org/v3-flatcontainer/microsoft.netcore.platforms/1.1.0/microsoft.netcore.platforms.1.1.0.nupkg
info :   OK https://api.nuget.org/v3-flatcontainer/microsoft.netcore.platforms/1.1.0/microsoft.netcore.platforms.1.1.0.nupkg 15ms
info : Installed Microsoft.Playwright.NUnit 1.61.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.playwright.nunit\1.61.0 with content hash idTN839z5n0ekDDxCrD4HZM3TtI0TpTnHhJyADIqan4yhK5PU4dAxmzXlQeHsR+IVUFpiIThjxRldURKzNgCaA==.
info : Installed Microsoft.Playwright.TestAdapter 1.61.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.playwright.testadapter\1.61.0 with content hash ExBrT7i3beXIEc44BeS0DvEPKhvazHqzRodh8+xW7AFH+i9I7thI9i0csi92qhXcBYQ1WuHxijylJtEvCuAOHg==.
info : Installed Microsoft.Bcl.AsyncInterfaces 6.0.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.bcl.asyncinterfaces\6.0.0 with content hash UcSjPsst+DfAdJGVDsu346FX0ci0ah+lw3WRtn18NUwEqRt70HaOQ7lI72vy3+1LxtqI3T5GWwV39rQSrCzAeg==.
info : Installed NUnit3TestAdapter 4.0.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\nunit3testadapter\4.0.0 with content hash 5gCkytgQWL93M5s9Rnl4CfSUhn95VsuMVLYjmZe8K7dNRF7kzrBMPsBTyLNCga5qLW7RM/o591J+HVrV1QMaVQ==.
info : Installed NUnit 3.13.2 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\nunit\3.13.2 with content hash u+fz/lXyR4vlamySNAEMrXvh+GhAQiB6/aVZtU5WjivR5zF26Ui0tfteDtWqT90k9D8y6g8rFKYQC97Z7d195w==.
info : Installed Microsoft.NETCore.Platforms 1.1.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.netcore.platforms\1.1.0 with content hash kz0PEW2lhqygehI/d6XsPCQzD7ff7gUJaVGPVETX611eadGsA3A877GdSlU0LRVMCTH/+P3o2iDTak+S08V2+A==.
info : Installed System.ComponentModel.Annotations 5.0.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\system.componentmodel.annotations\5.0.0 with content hash dMkqfy2el8A8/I76n2Hi1oBFEbG1SfxD2l5nhwXV3XjlnOmwxJlQbYpJH4W51odnU9sARCSAgv7S3CyAFMkpYg==.
info : Installed NETStandard.Library 2.0.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\netstandard.library\2.0.0 with content hash 7jnbRU+L08FXKMxqUflxEXtVymWvNOrS8yHgu9s6EM8Anr6T/wIX4nZ08j/u3Asz+tCufp3YVwFSEvFTPYmBPA==.
info : Installed Microsoft.TestPlatform.ObjectModel 17.3.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.testplatform.objectmodel\17.3.0 with content hash 6NRzi6QbmWV49Psf8A9z1LTJU4nBrlJdCcDOUyD4Ttm1J2wvksu98GlV+52CkxtpgNsUjGr9Mv1Rbb1/dB06yQ==.
info : Installed Microsoft.Playwright 1.61.0 from https://api.nuget.org/v3/index.json to C:\Users\GBServerPH\.nuget\packages\microsoft.playwright\1.61.0 with content hash VM149rQ2Pu+3xAzrO2gvh2WRgrWNbIl0jL9LG2oMC9xKUVYDnUrsh+E/5S+NQToVV4S+yhZ3NHsa6kf1PdHeag==.
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/2026.07.29.05.43.51/vulnerability.update.json
info : Package 'Microsoft.Playwright.NUnit' is compatible with all the specified frameworks in project 'D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info : PackageReference for package 'Microsoft.Playwright.NUnit' version '1.61.0' added to file 'D:\ARCWorks_RestaurantSuite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj'.
info : Generating MSBuild file D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\obj\Roms.IntegrationTests.csproj.nuget.g.targets.
info : Writing assets file to disk. Path: D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\obj\project.assets.json
log  : Restored D:\ARCWorks_Restaurant Suite\tests\Roms.IntegrationTests\Roms.IntegrationTests.csproj (in 8.28 sec).
PS D:\ARCWorks_Restaurant Suite>
