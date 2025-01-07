Push-Location
Set-Location $PSScriptRoot
gsudo {
    Get-Process -Name PowerToys* | Stop-Process
    Start-Sleep -Seconds 1
	$ptPath = 'C:\Program Files\PowerToys'
	$projectName = 'UniversalSearchSuggestions'
	$safeProjectName = 'UniversalSearchSuggestions'
	$debug = '.\bin\x64\Debug\net9.0-windows'
	$dest = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\$projectName"
	$files = @(
		"Community.PowerToys.Run.Plugin.$safeProjectName.deps.json",
		"Community.PowerToys.Run.Plugin.$safeProjectName.dll",
        "Community.PowerToys.Run.Plugin.Update.dll",
        "update.ps1",    
        "ExCSS.dll",
        "Svg.dll",
		'plugin.json',
		'Images'
	)

	Set-Location $debug
	mkdir $dest -Force -ErrorAction Ignore | Out-Null
	Copy-Item $files $dest -Force -Recurse

	& "$ptPath\PowerToys.exe"
}
Pop-Location
