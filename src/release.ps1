Push-Location
Set-Location $PSScriptRoot

$name = 'UniversalSearchSuggestions'
$assembly = "Community.PowerToys.Run.Plugin.$name"
$version = "v$((Get-Content ./plugin.json | ConvertFrom-Json).Version)"
$archs = @('x64', 'arm64')
$tempDir = './out/UniversalSearchSuggestions'

git tag $version
git push --tags

Remove-Item ./out/*.zip -Recurse -Force -ErrorAction Ignore
foreach ($arch in $archs) {
    $releasePath = "./bin/$arch/Release/net9.0-windows"

    dotnet build -c Release /p:Platform=$arch

    Remove-Item "$tempDir/*" -Recurse -Force -ErrorAction Ignore
    mkdir "$tempDir" -ErrorAction Ignore

    # Ensure these files/folders exist in your Release output folder so they can be copied:
    $items = @(
        "$releasePath/$assembly.deps.json",
        "$releasePath/$assembly.dll",
        "$releasePath/Community.PowerToys.Run.Plugin.Update.dll",  # NEW
        "$releasePath/update.ps1",                                # NEW
        "$releasePath/ExCSS.dll",
        "$releasePath/Svg.dll",
        "$releasePath/plugin.json",
        "$releasePath/Images"
    )

    Copy-Item $items "$tempDir" -Recurse -Force

    Compress-Archive "$tempDir" "./out/$name-$version-$arch.zip" -Force
}

$notes = ""
Write-Host "Enter release notes (end with an empty line):"
do {
    $line = Read-Host
    if ($line -ne "") {
        $notes += $line + "`n"
    }
} while ($line -ne "")

gh release create $version (Get-ChildItem ./out/*.zip) --title "Universal Search Suggestions $version" --notes "Universal Search Suggestions ${version}: `n$notes"

Pop-Location
