#  .\RetrieveSecretFromKeyVault.ps1 'KeyVault-ReloAccessDemo' 'ReloAccessSecrets'

    Param
    (
         [Parameter(Mandatory=$true, Position=0)]
         [string] $VaultName,
         [Parameter(Mandatory=$true, Position=1)]
         [string] $SecretName
    )

    $secret = Get-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName

    $rawSecretValue = "";
    $ssPtr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret.SecretValue);
    try 
    {
       $secretValueText = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ssPtr);
       $rawSecretValue = [Text.Encoding]::Utf8.GetString([Convert]::FromBase64String($secretValueText))
    } 
    finally
    {
       [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ssPtr);
    }

    return $rawSecretValue;
