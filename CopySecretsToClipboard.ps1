#  .\CopySecretsToClipboard.ps1 "D:\BGRS\Code\AzureSupportSecureConfiguration\AzureSupportSecureConfiguration\AzueProductionSettings.json"  $true

    Param
    (
         [Parameter(Mandatory=$true, Position=0)]
         [string] $FileName,
         [Parameter(Mandatory=$false, Position=1)]
         [bool] $CopyToClipboard = $true
    )

    #NOTE: DO NOT call ReadAllBytes because that will inject a (Byte Order Mark) BOM at the start 
    #      of the text. This is an invisible character that will cause probles when we decode
    #      the contents of the secret.
    #      https://stackoverflow.com/questions/26101859/why-is-file-readallbytes-result-different-than-when-using-file-readalltext
    $fileText =	[IO.File]::ReadAllText($FileName);
    $fileCharacters = [System.Text.Encoding]::UTF8.GetBytes($fileText );
    $base64string = [Convert]::ToBase64String($fileCharacters);

    if($CopyToClipboard -eq $true)
    {
	Set-Clipboard -Value $base64string 
    }

    return $base64string;
