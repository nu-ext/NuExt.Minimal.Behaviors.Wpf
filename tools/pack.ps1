$csproj = "$PSScriptRoot\..\src\NuExt.Minimal.Behaviors.Wpf.csproj"
$outDir = $PSScriptRoot

$tasks = @(
    @{ Config = "Release" },
    @{ Config = "Sources" }
)

foreach ($t in $tasks) {
    $cfg  = $t.Config

    Write-Host "==> $csproj ($cfg)"

    dotnet clean $csproj -c $cfg
    dotnet pack  $csproj -c $cfg -o $outDir
}