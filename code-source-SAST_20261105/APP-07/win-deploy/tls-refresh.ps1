# APP-07 PayGate XML -- renouvellement TLS (tache planifiee quotidienne, installee par install.ps1 dans C:\lab).
# Recupere le PFX wildcard *.<LAB_DOMAIN> (Let's Encrypt) republie dans SSM par deploy/tls-issue.sh,
# l'importe dans LocalMachine\My et re-bind le site par defaut sur 443 UNIQUEMENT si le certificat SSM
# est plus recent que celui deja lie (idempotent, best-effort). Donnees 100 % FICTIVES -- usage defensif.
# Les placeholders __...__ sont remplaces au packaging (package.sh).
$ErrorActionPreference = 'Continue'
$cn        = '__CNAME_HOST__'
$ssmPrefix = '__TLS_SSM_PREFIX__'
$ssmRegion = '__TLS_SSM_REGION__'

Import-Module WebAdministration -ErrorAction SilentlyContinue

function Get-AwsCli {
  $c = Get-Command aws -ErrorAction SilentlyContinue
  if ($c) { return $c.Source }
  foreach ($p in @("$env:ProgramFiles\Amazon\AWSCLIV2\aws.exe", "${env:ProgramFiles(x86)}\Amazon\AWSCLIV2\aws.exe")) {
    if (Test-Path $p) { return $p }
  }
  return $null
}

try {
  $b64 = $null; $pw = $null
  $aws = Get-AwsCli
  if ($aws) {
    $b64 = (& $aws ssm get-parameter --region $ssmRegion --name "$ssmPrefix/pfx_b64" --with-decryption --query Parameter.Value --output text) 2>$null
    $pw  = (& $aws ssm get-parameter --region $ssmRegion --name "$ssmPrefix/pfx_pw"  --with-decryption --query Parameter.Value --output text) 2>$null
  }
  if ((-not $b64) -and (Get-Module -ListAvailable -Name 'AWS.Tools.SimpleSystemsManagement','AWSPowerShell','AWSPowerShell.NetCore' -ErrorAction SilentlyContinue)) {
    Import-Module AWS.Tools.SimpleSystemsManagement -ErrorAction SilentlyContinue
    Import-Module AWSPowerShell -ErrorAction SilentlyContinue
    $b64 = (Get-SSMParameter -Name "$ssmPrefix/pfx_b64" -WithDecryption $true -Region $ssmRegion -ErrorAction SilentlyContinue).Value
    $pw  = (Get-SSMParameter -Name "$ssmPrefix/pfx_pw"  -WithDecryption $true -Region $ssmRegion -ErrorAction SilentlyContinue).Value
  }
  if (-not ($b64 -and $pw)) { Write-Host '[tls-refresh] PFX SSM indisponible, aucune action'; exit 0 }

  $pfx = Join-Path $env:TEMP 'dastlab-refresh.pfx'
  [IO.File]::WriteAllBytes($pfx, [Convert]::FromBase64String($b64.Trim()))
  $sec = ConvertTo-SecureString -String $pw -AsPlainText -Force
  # Empreinte du PFX SSM, sans l'importer d'abord (comparaison avec le binding courant).
  $incoming = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $pfx, $sec, 'MachineKeySet'
  $bind = Get-WebBinding -Name 'Default Web Site' -Protocol https -Port 443 -ErrorAction SilentlyContinue
  $current = $null
  if ($bind) {
    $h = ($bind.certificateHash)
    if ($h) { $current = Get-Item ("Cert:\LocalMachine\My\$h") -ErrorAction SilentlyContinue }
  }
  if ($current -and $current.Thumbprint -eq $incoming.Thumbprint) {
    Remove-Item $pfx -Force -ErrorAction SilentlyContinue
    Write-Host '[tls-refresh] certificat deja a jour, aucune action'; exit 0
  }
  $imp = Import-PfxCertificate -FilePath $pfx -CertStoreLocation Cert:\LocalMachine\My -Password $sec
  Remove-Item $pfx -Force -ErrorAction SilentlyContinue
  $c = $imp | Select-Object -First 1
  if ($c -and $c.NotAfter -gt (Get-Date)) {
    if (-not $bind) { New-WebBinding -Name 'Default Web Site' -Protocol https -Port 443 -IPAddress '*' }
    (Get-WebBinding -Name 'Default Web Site' -Protocol https).AddSslCertificate($c.Thumbprint, 'My')
    Write-Host ('[tls-refresh] certificat renouvele lie (exp ' + $c.NotAfter.ToString('yyyy-MM-dd') + ')')
  }
} catch {
  Write-Host ('[tls-refresh] erreur - ' + $_.Exception.Message)
}
exit 0
