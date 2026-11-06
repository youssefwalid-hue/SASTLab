# APP-07 PayGate XML - deploiement EB Windows (manifeste custom).
# IMPORTANT : fichier STRICTEMENT ASCII. PowerShell 5.1 lit un .ps1 sans BOM comme de l'ANSI
# (Windows-1252) ; des caracteres non-ASCII (accents, fleche) y cassent l'analyse des chaines.
# Copie l'application (Web Site : App_Code compile au runtime par IIS), active WCF (.svc), et monte
# le TLS 443. Mode letsencrypt = PFX wildcard lu depuis SSM ; sinon repli autosigne. Donnees FICTIVES.
# __CNAME_HOST__ / __TLS_CERT_MODE__ / __TLS_SSM_PREFIX__ / __TLS_SSM_REGION__ remplaces au packaging.
$ErrorActionPreference = 'Continue'

# EB execute les scripts du manifeste en PowerShell 32 bits ; New-SelfSignedCertificate et d'autres API
# ne sont pas enregistrees en 32 bits. On se relance en 64 bits via sysnative.
if (-not [Environment]::Is64BitProcess) {
  $ps64 = Join-Path $env:WINDIR 'sysnative\WindowsPowerShell\v1.0\powershell.exe'
  & $ps64 -ExecutionPolicy Unrestricted -NonInteractive -NoProfile -File $PSCommandPath
  exit $LASTEXITCODE
}

Import-Module WebAdministration

$root = Split-Path $PSScriptRoot -Parent
$dest = Join-Path $env:SystemDrive 'inetpub\wwwroot'

# ecrits ensuite dans wwwroot recoivent un alias 8.3 (PAYMEN~1.SVC...), exploitable par enumeration tilde.
& "$env:WINDIR\system32\fsutil.exe" behavior set disable8dot3 0 2>&1 | Out-Null

# 1) Nettoyer le site par defaut et y copier l'application (hors scripts de deploiement et manifeste).
Get-ChildItem $dest -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem $root -Force | Where-Object {
  $_.Name -notin @('win-deploy', 'aws-windows-deployment-manifest.json', 'fixtures')
} | ForEach-Object { Copy-Item $_.FullName -Destination $dest -Recurse -Force -ErrorAction SilentlyContinue }

# Fixtures du laboratoire HORS wwwroot (non servies par IIS) : cibles V001 (XXE) et V014 (traversee).
$fx = Join-Path $root 'fixtures'
if (Test-Path $fx) {
  New-Item -ItemType Directory -Force -Path 'C:\lab\fixtures' | Out-Null
  Copy-Item (Join-Path $fx '*') -Destination 'C:\lab\fixtures' -Recurse -Force -ErrorAction SilentlyContinue
}

# 2) Activation HTTP WCF (handler .svc) + ASP.NET 4.x (idempotent).
Install-WindowsFeature NET-Framework-45-Core, NET-WCF-HTTP-Activation45 -ErrorAction SilentlyContinue | Out-Null

# 2b) GT: APP07-V012 - WebDAV installe + authoring actif (OPTIONS annonce DAV, PROPFIND). Best-effort.
Install-WindowsFeature Web-DAV-Publishing -ErrorAction SilentlyContinue | Out-Null
$appcmd = Join-Path $env:WINDIR 'system32\inetsrv\appcmd.exe'
& $appcmd set config "Default Web Site" /section:system.webServer/webdav/authoring /enabled:true /commit:apphost 2>&1 | Out-Null
& $appcmd set config "Default Web Site" /section:system.webServer/webdav/authoringRules "/+[users='*',path='*',access='Read']" /commit:apphost 2>&1 | Out-Null

# 2c) GT: APP07-V008 - laisser le filtrage de requetes IIS transmettre la forme tilde (*~1*) jusqu'au
#     handler (sinon 400 d'emblee) : autoriser double-echappement et caracteres hauts. Avec la generation
#     8.3 active (ci-dessus) la reponse differe selon l'existence d'un alias court -> enumeration tilde.
& $appcmd set config "Default Web Site" /section:system.webServer/security/requestFiltering /allowDoubleEscaping:true /commit:apphost 2>&1 | Out-Null
& $appcmd set config "Default Web Site" /section:system.webServer/security/requestFiltering /allowHighBitCharacters:true /commit:apphost 2>&1 | Out-Null

# 3) Binding HTTPS 443.
#    - mode letsencrypt : PFX wildcard lu depuis SSM (pfx_b64 + pfx_pw), importe puis lie. Echec -> repli autosigne.
#    - mode selfsigned : certificat autosigne CN=<host>.
$cn        = '__CNAME_HOST__'
$mode      = '__TLS_CERT_MODE__'
$ssmPrefix = '__TLS_SSM_PREFIX__'
$ssmRegion = '__TLS_SSM_REGION__'

function Set-Https($thumb) {
  if (-not (Get-WebBinding -Name 'Default Web Site' -Protocol https -Port 443 -ErrorAction SilentlyContinue)) {
    New-WebBinding -Name 'Default Web Site' -Protocol https -Port 443 -IPAddress '*'
  }
  (Get-WebBinding -Name 'Default Web Site' -Protocol https).AddSslCertificate($thumb, 'My')
}
function Set-SelfSigned {
  $c = New-SelfSignedCertificate -DnsName $cn -CertStoreLocation Cert:\LocalMachine\My -KeyLength 2048 -NotAfter (Get-Date).AddYears(1)
  Set-Https $c.GetCertHashString()
  Write-Host ('[install] HTTPS binding autosigne OK, CN=' + $cn)
}
function Get-AwsCli {
  $c = Get-Command aws -ErrorAction SilentlyContinue
  if ($c) { return $c.Source }
  foreach ($p in @("$env:ProgramFiles\Amazon\AWSCLIV2\aws.exe", "${env:ProgramFiles(x86)}\Amazon\AWSCLIV2\aws.exe")) {
    if (Test-Path $p) { return $p }
  }
  return $null
}

$done = $false
if ($mode -eq 'letsencrypt') {
  try {
    Write-Host ('[install] mode letsencrypt : lecture du PFX depuis SSM ' + $ssmPrefix + ' (' + $ssmRegion + ')')
    $b64 = $null; $pw = $null
    $aws = Get-AwsCli
    if ($aws) {
      Write-Host ('[install] AWS CLI: ' + $aws)
      $b64 = (& $aws ssm get-parameter --region $ssmRegion --name "$ssmPrefix/pfx_b64" --with-decryption --query Parameter.Value --output text) 2>$null
      $pw  = (& $aws ssm get-parameter --region $ssmRegion --name "$ssmPrefix/pfx_pw"  --with-decryption --query Parameter.Value --output text) 2>$null
    } else {
      Write-Host '[install] AWS CLI absente, essai module AWS Tools PowerShell'
    }
    if (-not $b64) {
      foreach ($m in @('AWS.Tools.SimpleSystemsManagement','AWSPowerShell.NetCore','AWSPowerShell')) {
        if (Get-Module -ListAvailable -Name $m) { Import-Module $m -ErrorAction SilentlyContinue; Write-Host ('[install] module charge: ' + $m); break }
      }
      if (Get-Command Get-SSMParameter -ErrorAction SilentlyContinue) {
        try {
          $b64 = (Get-SSMParameter -Name "$ssmPrefix/pfx_b64" -WithDecryption $true -Region $ssmRegion).Value
          $pw  = (Get-SSMParameter -Name "$ssmPrefix/pfx_pw"  -WithDecryption $true -Region $ssmRegion).Value
        } catch { Write-Host ('[install] Get-SSMParameter KO: ' + $_.Exception.Message) }
      } else {
        Write-Host '[install] ni AWS CLI ni Get-SSMParameter disponibles'
      }
    }
    if ($b64 -and $pw) {
      $pfx = Join-Path $env:TEMP 'dastlab.pfx'
      [IO.File]::WriteAllBytes($pfx, [Convert]::FromBase64String($b64.Trim()))
      $sec = ConvertTo-SecureString -String $pw -AsPlainText -Force
      $imp = Import-PfxCertificate -FilePath $pfx -CertStoreLocation Cert:\LocalMachine\My -Password $sec
      Remove-Item $pfx -Force -ErrorAction SilentlyContinue
      $c = $imp | Select-Object -First 1
      if ($c -and $c.NotAfter -gt (Get-Date)) {
        Set-Https $c.Thumbprint
        Write-Host ('[install] HTTPS binding Lets Encrypt OK (exp ' + $c.NotAfter.ToString('yyyy-MM-dd') + ', thumb ' + $c.Thumbprint + ')')
        $done = $true
      }
    }
    if (-not $done) { Write-Host '[install] AVERTISSEMENT : PFX SSM indisponible/invalide -> REPLI autosigne' }
  } catch {
    Write-Host ('[install] erreur mode letsencrypt (' + $_.Exception.Message + ') -> REPLI autosigne')
  }
}
if (-not $done) {
  try { Set-SelfSigned } catch { Write-Host ('[install] HTTPS setup failed - ' + $_.Exception.Message) }
}

# 3b) Renouvellement quotidien du certificat (mode letsencrypt) : tache planifiee qui relit le PFX dans SSM.
if ($mode -eq 'letsencrypt') {
  try {
    New-Item -ItemType Directory -Force -Path 'C:\lab' | Out-Null
    $refreshSrc = Join-Path $PSScriptRoot 'tls-refresh.ps1'
    $refresh = 'C:\lab\tls-refresh.ps1'
    if (Test-Path $refreshSrc) { Copy-Item $refreshSrc $refresh -Force }
    if (Test-Path $refresh) {
      $act = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ('-ExecutionPolicy Bypass -NonInteractive -NoProfile -File "' + $refresh + '"')
      $trg = New-ScheduledTaskTrigger -Daily -At 3am
      Register-ScheduledTask -TaskName 'dastlab-tls-refresh' -Action $act -Trigger $trg -User 'SYSTEM' -RunLevel Highest -Force | Out-Null
      Write-Host '[install] tache planifiee dastlab-tls-refresh enregistree (quotidienne, 03:00)'
    }
  } catch { Write-Host ('[install] tache de renouvellement non enregistree - ' + $_.Exception.Message) }
}

# 3c) Garde d'hote canonique (mode letsencrypt) : rediriger l'ancienne URL EB
#     (Host *.elasticbeanstalk.com) vers l'hote canonique $cn (= app07.<LAB_DOMAIN>) en 301.
#     Regle URL Rewrite ecrite dans applicationHost.config (/commit:apphost), avec la syntaxe CORRECTE
#     (operateur ':' pour poser un attribut, pas '=' ; patternSyntax explicite). Puis AUTO-REPARATION :
#     on relit la section rewrite avec 'appcmd list config' ; si la config est invalide (ex. attribut
#     manquant), appcmd renvoie une erreur -> on retire la regle pour que le site NE PUISSE PAS rester
#     casse (au pire APP-07 revient a l'etat sans garde). Si le module URL Rewrite est absent, on ignore.
$sec = 'system.webServer/rewrite/rules'
$ruleName = 'canonical-host-guard'
if ($mode -eq 'letsencrypt') {
  $rewriteDll = Join-Path $env:WINDIR 'system32\inetsrv\rewrite.dll'
  if (Test-Path $rewriteDll) {
    try {
      # Idempotence : retirer une eventuelle regle du meme nom.
      & $appcmd set config "Default Web Site" /section:$sec "/-[name='$ruleName']" /commit:apphost 2>&1 | Out-Null
      # Creer la regle (syntaxe correcte : ':' pour chaque attribut, valeurs entre guillemets).
      & $appcmd set config "Default Web Site" /section:$sec ("/+[name='$ruleName',patternSyntax='ECMAScript',stopProcessing='true']") /commit:apphost 2>&1 | Out-Null
      & $appcmd set config "Default Web Site" /section:$sec ("/[name='$ruleName'].match.url:`"(.*)`"") /commit:apphost 2>&1 | Out-Null
      & $appcmd set config "Default Web Site" /section:$sec ("/+[name='$ruleName'].conditions.[input='{HTTP_HOST}',pattern='\.elasticbeanstalk\.com$']") /commit:apphost 2>&1 | Out-Null
      & $appcmd set config "Default Web Site" /section:$sec ("/[name='$ruleName'].action.type:`"Redirect`"") /commit:apphost 2>&1 | Out-Null
      & $appcmd set config "Default Web Site" /section:$sec ("/[name='$ruleName'].action.url:`"https://" + $cn + "/{R:1}`"") /commit:apphost 2>&1 | Out-Null
      & $appcmd set config "Default Web Site" /section:$sec ("/[name='$ruleName'].action.redirectType:`"Permanent`"") /commit:apphost 2>&1 | Out-Null

      # AUTO-REPARATION : valider la section. 'appcmd list config' renvoie un code <> 0 si invalide.
      & $appcmd list config "Default Web Site" /section:$sec 2>&1 | Out-Null
      if ($LASTEXITCODE -ne 0) {
        & $appcmd set config "Default Web Site" /section:$sec "/-[name='$ruleName']" /commit:apphost 2>&1 | Out-Null
        Write-Host '[install] garde d''hote canonique INVALIDE -> regle retiree (site preserve, APP-07 sans garde)'
      } else {
        Write-Host ('[install] garde d''hote canonique active et validee : ancienne URL EB -> https://' + $cn)
      }
    } catch {
      # En cas d'exception, tenter de retirer la regle pour ne pas laisser le site casse.
      try { & $appcmd set config "Default Web Site" /section:$sec "/-[name='$ruleName']" /commit:apphost 2>&1 | Out-Null } catch { }
      Write-Host ('[install] garde d''hote canonique non appliquee (regle retiree) - ' + $_.Exception.Message)
    }
  } else {
    Write-Host '[install] module URL Rewrite absent : garde d''hote canonique ignoree (ancienne URL EB sert encore l''app)'
  }
}
exit 0
