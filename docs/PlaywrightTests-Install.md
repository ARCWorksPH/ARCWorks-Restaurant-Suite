PS D:\ARCWorks_Restaurant Suite> dotnet new nunit -n PlaywrightTests
The template "NUnit Test Project" was created successfully.

Processing post-creation actions...
Restoring D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj:
Restore succeeded.


PS D:\ARCWorks_Restaurant Suite> cd PlaywrightTests
PS D:\ARCWorks_Restaurant Suite\PlaywrightTests> dotnet add package Microsoft.Playwright.NUnit
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Playwright.NUnit' into project 'D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj'.
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.playwright.nunit/index.json
info : Restoring packages for D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/2026.07.29.05.43.51/vulnerability.update.json
info : Package 'Microsoft.Playwright.NUnit' is compatible with all the specified frameworks in project 'D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj'.
info : PackageReference for package 'Microsoft.Playwright.NUnit' version '1.61.0' added to file 'D:\ARCWorks_RestaurantSuite\PlaywrightTests\PlaywrightTests.csproj'.
info : Generating MSBuild file D:\ARCWorks_Restaurant Suite\PlaywrightTests\obj\PlaywrightTests.csproj.nuget.g.targets.
info : Writing assets file to disk. Path: D:\ARCWorks_Restaurant Suite\PlaywrightTests\obj\project.assets.json
log  : Restored D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj (in 200 ms).
PS D:\ARCWorks_Restaurant Suite\PlaywrightTests> dotnet add package Microsoft.Playwright.NUnit
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Playwright.NUnit' into project 'D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj'.
info :   CACHE https://api.nuget.org/v3/registration5-gz-semver2/microsoft.playwright.nunit/index.json
info : Restoring packages for D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.07.29.05.43.51/2026.07.29.05.43.51/vulnerability.update.json
info : Package 'Microsoft.Playwright.NUnit' is compatible with all the specified frameworks in project 'D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj'.
info : PackageReference for package 'Microsoft.Playwright.NUnit' version '1.61.0' updated in file 'D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj'.
info : Assets file has not changed. Skipping assets file writing. Path: D:\ARCWorks_Restaurant Suite\PlaywrightTests\obj\project.assets.json
log  : Restored D:\ARCWorks_Restaurant Suite\PlaywrightTests\PlaywrightTests.csproj (in 202 ms).
PS D:\ARCWorks_Restaurant Suite\PlaywrightTests> dotnet build
Restore complete (0.3s)
  PlaywrightTests net10.0 succeeded (3.2s) → bin\Debug\net10.0\PlaywrightTests.dll

Build succeeded in 4.0s
PS D:\ARCWorks_Restaurant Suite\PlaywrightTests> pwsh bin/Debug/net8.0/playwright.ps1 install
The argument 'bin/Debug/net8.0/playwright.ps1' is not recognized as the name of a script file. Check the spelling of the name, or if a path was included, verify that the path is correct and try again.

Usage: pwsh[.exe] [-Login] [[-File] <filePath> [args]]
                  [-Command { - | <script-block> [-args <arg-array>]
                                | <string> [<CommandParameters>] } ]
                  [-CommandWithArgs <string> [<CommandParameters>]
                  [-ConfigurationName <string>] [-ConfigurationFile <filePath>]
                  [-CustomPipeName <string>] [-EncodedCommand <Base64EncodedCommand>]
                  [-ExecutionPolicy <ExecutionPolicy>] [-InputFormat {Text | XML}]
                  [-Interactive] [-MTA] [-NoExit] [-NoLogo] [-NonInteractive] [-NoProfile]
                  [-NoProfileLoadTime] [-OutputFormat {Text | XML}]
                  [-SettingsFile <filePath>] [-SSHServerMode] [-STA]
                  [-Version] [-WindowStyle <style>]
                  [-WorkingDirectory <directoryPath>]

       pwsh[.exe] -h | -Help | -? | /?

PowerShell Online Help https://aka.ms/powershell-docs

All parameters are case-insensitive.
PS D:\ARCWorks_Restaurant Suite\PlaywrightTests>
