# Script PowerShell pour renommer automatiquement les images Resources/Images
# - Convertit les noms en lowercase
# - Remplace les caractères non ascii/diacritiques
# - Remplace caractères non alphanumériques par underscore
# - Assure que le nom commence et termine par une lettre

$imagesPath = Join-Path $PSScriptRoot "..\Resources\Images"
$imagesPath = [System.IO.Path]::GetFullPath($imagesPath)
Write-Host "Images path: $imagesPath"

if (-not (Test-Path $imagesPath)) {
	Write-Error "Le dossier d'images n'existe pas: $imagesPath"
	exit 1
}

# Collect mappings
$mappings = @()
Get-ChildItem -Path $imagesPath -File | ForEach-Object {
	$file = $_
	$oldName = $file.Name
	$ext = $file.Extension.ToLower()
	$name = [System.IO.Path]::GetFileNameWithoutExtension($oldName)

	# Remove diacritics
	$norm = $name.Normalize([Text.NormalizationForm]::FormD)
	$sb = New-Object System.Text.StringBuilder
	foreach ($ch in $norm.ToCharArray()) {
		$cat = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch)
		if ($cat -ne [Globalization.UnicodeCategory]::NonSpacingMark) {
			[void]$sb.Append($ch)
		}
	}
	$clean = $sb.ToString().Normalize([Text.NormalizationForm]::FormC)

	# Lowercase and replace non alphanum by underscore
	$clean = $clean.ToLower()
	$clean = $clean -replace "[^a-z0-9]", "_"
	$clean = $clean.Trim("_")

	# Ensure start with letter
	if ($clean -notmatch '^[a-z]') { $clean = "img_" + $clean }
	if ($clean -notmatch '[a-z]$') { $clean = $clean + "_img" }

	$newName = $clean + $ext

	if ($newName -ne $oldName) {
		$mappings += [PSCustomObject]@{Old=$oldName; New=$newName; FullPath=$file.FullName}
	}
}

if ($mappings.Count -eq 0) {
	Write-Host "Aucun fichier à renommer."
	exit 0
}

# Apply renames
foreach ($m in $mappings) {
	$src = Join-Path $imagesPath $m.Old
	$dst = Join-Path $imagesPath $m.New
	if (Test-Path $dst) {
		Write-Warning "Destination existe déjà, saut: $($m.Old) -> $($m.New)"
		continue
	}
	Write-Host "Renommage: $($m.Old) -> $($m.New)"
	Rename-Item -Path $src -NewName $m.New
}

# Update references in project files (XAML, CS, JSON, XML, csproj)
$projectRoot = Join-Path $PSScriptRoot ".."
$projectRoot = [System.IO.Path]::GetFullPath($projectRoot)
$filesToUpdate = Get-ChildItem -Path $projectRoot -Recurse -File -Include *.xaml,*.cs,*.json,*.xml,*.csproj,*.xaml.cs | Where-Object { $_.FullName -notlike "$imagesPath*" }

foreach ($m in $mappings) {
	$old = [regex]::Escape($m.Old)
	$new = $m.New
	foreach ($f in $filesToUpdate) {
		try {
			$text = Get-Content -Raw -Encoding UTF8 -Path $f.FullName
		} catch {
			continue
		}
		if ($text -match $old) {
			$newText = $text -replace $old, $new
			Set-Content -Path $f.FullName -Value $newText -Encoding UTF8
			Write-Host "Updated references in: $($f.FullName) -> $($m.Old) -> $($m.New)"
		}
	}
}

Write-Host "Renommage terminé. Mappings :"
$mappings | Format-Table -AutoSize

return 0
