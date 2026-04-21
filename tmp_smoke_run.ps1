$ErrorActionPreference='Stop'

function Invoke-Api {
  param(
    [string]$Method,
    [string]$Url,
    [string]$Token,
    $Body = $null,
    [string]$ContentType = 'application/json'
  )

  try {
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }

    $params = @{
      Uri = $Url
      Method = $Method
      Headers = $headers
      UseBasicParsing = $true
      TimeoutSec = 30
    }

    if ($Body -ne $null) {
      if ($ContentType -eq 'application/json') {
        $params['Body'] = ($Body | ConvertTo-Json -Depth 12)
      } else {
        $params['Body'] = $Body
      }
      $params['ContentType'] = $ContentType
    }

    $resp = Invoke-WebRequest @params
    $parsed = $null
    if ($resp.Content) {
      try { $parsed = $resp.Content | ConvertFrom-Json -Depth 20 } catch { $parsed = $null }
    }

    return [pscustomobject]@{ Status = [int]$resp.StatusCode; Json = $parsed; Raw = $resp.Content }
  }
  catch {
    $status = -1
    $raw = $_.Exception.Message
    if ($_.Exception.Response) {
      try { $status = [int]$_.Exception.Response.StatusCode.value__ } catch { $status = -1 }
      try {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $raw = $sr.ReadToEnd()
      } catch {}
    }

    $parsed = $null
    if ($raw) { try { $parsed = $raw | ConvertFrom-Json -Depth 20 } catch {} }

    return [pscustomobject]@{ Status = $status; Json = $parsed; Raw = $raw }
  }
}

function Status-Text {
  param([int]$Status)
  if ($Status -ge 200 -and $Status -lt 300) { return "$Status (PASS)" }
  return "$Status (FAIL)"
}

function Run-Smoke {
  param(
    [string]$Mode,
    [hashtable]$Base
  )

  $results = @{}
  $stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff')
  $customerEmail = "smoke.$Mode.$stamp@example.com".ToLower()
  $customerPassword = 'Cust@12345'
  $adminEmail = 'admin@smartsure.com'
  $adminPassword = 'Admin@123'

  $registerBody = @{ fullName = "Smoke $Mode Customer"; email = $customerEmail; password = $customerPassword }
  $rRegister = Invoke-Api -Method 'POST' -Url "$($Base.auth)/register" -Body $registerBody
  $results['Auth register (customer)'] = $rRegister.Status

  $rAdminLogin = Invoke-Api -Method 'POST' -Url "$($Base.auth)/login" -Body @{ email = $adminEmail; password = $adminPassword }
  $results['Auth login (admin)'] = $rAdminLogin.Status
  $adminToken = $rAdminLogin.Json.token

  $customerToken = $null
  if ($rRegister.Json -and $rRegister.Json.token) { $customerToken = $rRegister.Json.token }
  if (-not $customerToken) {
    $rCustomerLogin = Invoke-Api -Method 'POST' -Url "$($Base.auth)/login" -Body @{ email = $customerEmail; password = $customerPassword }
    $customerToken = $rCustomerLogin.Json.token
  }

  $rProfile = Invoke-Api -Method 'GET' -Url "$($Base.auth)/profile" -Token $customerToken
  $results['Identity profile (customer)'] = $rProfile.Status

  $rIdentityUsers = Invoke-Api -Method 'GET' -Url "$($Base.auth)/admin/users" -Token $adminToken
  $results['Identity admin users list'] = $rIdentityUsers.Status

  $rIdentityCount = Invoke-Api -Method 'GET' -Url "$($Base.auth)/admin/users/count" -Token $adminToken
  $results['Identity admin users count'] = $rIdentityCount.Status

  $targetUserId = $null
  if ($rIdentityUsers.Json) {
    foreach ($u in $rIdentityUsers.Json) {
      if ($u.email -eq $customerEmail) { $targetUserId = $u.id; break }
    }
    if (-not $targetUserId) {
      foreach ($u in $rIdentityUsers.Json) {
        if (($u.email -ne $adminEmail) -and $u.id) { $targetUserId = $u.id; break }
      }
    }
  }
  if (-not $targetUserId) { $targetUserId = 2 }

  $rIdentityDisable = Invoke-Api -Method 'PUT' -Url "$($Base.auth)/admin/users/$targetUserId/status" -Token $adminToken -Body @{ isActive = $false }
  $results['Identity admin update user status (disable)'] = $rIdentityDisable.Status

  $rIdentityEnable = Invoke-Api -Method 'PUT' -Url "$($Base.auth)/admin/users/$targetUserId/status" -Token $adminToken -Body @{ isActive = $true }
  $results['Identity admin update user status (enable)'] = $rIdentityEnable.Status

  $rPolicyTypes = Invoke-Api -Method 'GET' -Url "$($Base.policy)/types" -Token $customerToken
  $results['Policy types list'] = $rPolicyTypes.Status

  $policyTypeId = $null
  if ($rPolicyTypes.Json -and $rPolicyTypes.Json.Count -gt 0) { $policyTypeId = [int]$rPolicyTypes.Json[0].id }
  if (-not $policyTypeId) { $policyTypeId = 1 }

  $startDate = (Get-Date).Date.AddDays(1)
  $endDate = $startDate.AddYears(1)

  $premiumBody = @{ policyTypeId = $policyTypeId; age = 30; startDate = $startDate.ToString('o'); endDate = $endDate.ToString('o') }
  $rPremium = Invoke-Api -Method 'POST' -Url "$($Base.policy)/calculate-premium" -Token $customerToken -Body $premiumBody
  $results['Policy premium calculate (customer)'] = $rPremium.Status

  $createPolicyBody = @{ policyTypeId = $policyTypeId; age = 30; startDate = $startDate.ToString('o'); endDate = $endDate.ToString('o') }
  $rPolicyCreate = Invoke-Api -Method 'POST' -Url "$($Base.policy)" -Token $customerToken -Body $createPolicyBody
  $results['Policy create (customer)'] = $rPolicyCreate.Status

  $policyId = $null
  if ($rPolicyCreate.Json -and $rPolicyCreate.Json.id) { $policyId = [int]$rPolicyCreate.Json.id }

  $rMyPolicies = Invoke-Api -Method 'GET' -Url "$($Base.policy)/my" -Token $customerToken
  $results['Policy my list (customer)'] = $rMyPolicies.Status

  if (-not $policyId -and $rMyPolicies.Json -and $rMyPolicies.Json.Count -gt 0) { $policyId = [int]$rMyPolicies.Json[0].id }
  if (-not $policyId) { $policyId = 1 }

  $claimBody = @{ policyId = $policyId; incidentDate = (Get-Date).Date.AddDays(-2).ToString('o'); description = "Smoke test claim $Mode $stamp" }
  $rClaimCreate = Invoke-Api -Method 'POST' -Url "$($Base.claims)" -Token $customerToken -Body $claimBody
  $results['Claim create (customer)'] = $rClaimCreate.Status

  $claimId = $null
  if ($rClaimCreate.Json -and $rClaimCreate.Json.id) { $claimId = [int]$rClaimCreate.Json.id }
  if (-not $claimId) {
    $rMyClaims = Invoke-Api -Method 'GET' -Url "$($Base.claims)/my" -Token $customerToken
    if ($rMyClaims.Json -and $rMyClaims.Json.Count -gt 0) { $claimId = [int]$rMyClaims.Json[0].id }
  }
  if (-not $claimId) { $claimId = 1 }

  $rClaimSubmit = Invoke-Api -Method 'POST' -Url "$($Base.claims)/$claimId/submit" -Token $customerToken -Body @{}
  $results['Claim submit (customer)'] = $rClaimSubmit.Status

  $rClaimsAdminList = Invoke-Api -Method 'GET' -Url "$($Base.claims)" -Token $adminToken
  $results['Claims admin list'] = $rClaimsAdminList.Status

  $rClaimStatusUpdate = Invoke-Api -Method 'PUT' -Url "$($Base.claims)/$claimId/status" -Token $adminToken -Body @{ status = 'Approved'; adminNote = "Approved in smoke $Mode" }
  $results['Claim status update (admin)'] = $rClaimStatusUpdate.Status

  $rAdminDashboard = Invoke-Api -Method 'GET' -Url "$($Base.admin)/dashboard" -Token $adminToken
  $results['Admin dashboard'] = $rAdminDashboard.Status

  $rAdminClaims = Invoke-Api -Method 'GET' -Url "$($Base.admin)/claims" -Token $adminToken
  $results['Admin claims list'] = $rAdminClaims.Status

  $rAdminUsers = Invoke-Api -Method 'GET' -Url "$($Base.admin)/users" -Token $adminToken
  $results['Admin users list'] = $rAdminUsers.Status

  $adminTargetUserId = $targetUserId
  if (-not $adminTargetUserId -and $rAdminUsers.Json) {
    foreach ($u in $rAdminUsers.Json) {
      if (($u.email -ne $adminEmail) -and $u.userId) { $adminTargetUserId = $u.userId; break }
    }
  }
  if (-not $adminTargetUserId) { $adminTargetUserId = 2 }

  $rAdminDisable = Invoke-Api -Method 'PUT' -Url "$($Base.admin)/users/$adminTargetUserId/status" -Token $adminToken -Body @{ isActive = $false }
  $results['Admin user status update (disable)'] = $rAdminDisable.Status

  $rAdminEnable = Invoke-Api -Method 'PUT' -Url "$($Base.admin)/users/$adminTargetUserId/status" -Token $adminToken -Body @{ isActive = $true }
  $results['Admin user status update (enable)'] = $rAdminEnable.Status

  $rAdminPolicyTypes = Invoke-Api -Method 'GET' -Url "$($Base.policy)/admin/types" -Token $adminToken
  $results['Admin policy types list'] = $rAdminPolicyTypes.Status

  $newTypeBody = @{ name = "SmokeType $Mode $stamp"; description = "Smoke policy type"; baseAmount = 1234 }
  $rAdminPolicyCreate = Invoke-Api -Method 'POST' -Url "$($Base.policy)/admin/types" -Token $adminToken -Body $newTypeBody
  $results['Admin policy type create'] = $rAdminPolicyCreate.Status

  $newTypeId = $null
  if ($rAdminPolicyCreate.Json -and $rAdminPolicyCreate.Json.id) { $newTypeId = [int]$rAdminPolicyCreate.Json.id }
  if (-not $newTypeId) {
    $rAdminTypesRefresh = Invoke-Api -Method 'GET' -Url "$($Base.policy)/admin/types" -Token $adminToken
    if ($rAdminTypesRefresh.Json -and $rAdminTypesRefresh.Json.Count -gt 0) {
      foreach ($t in $rAdminTypesRefresh.Json) {
        if ($t.name -eq $newTypeBody.name) { $newTypeId = [int]$t.id; break }
      }
    }
  }
  if (-not $newTypeId) { $newTypeId = 1 }

  $updateTypeBody = @{ name = "$($newTypeBody.name) Updated"; description = 'Updated by smoke'; baseAmount = 1500 }
  $rAdminPolicyUpdate = Invoke-Api -Method 'PUT' -Url "$($Base.policy)/admin/types/$newTypeId" -Token $adminToken -Body $updateTypeBody
  $results['Admin policy type update'] = $rAdminPolicyUpdate.Status

  $rAdminPolicyDelete = Invoke-Api -Method 'DELETE' -Url "$($Base.policy)/admin/types/$newTypeId" -Token $adminToken
  $results['Admin policy type delete'] = $rAdminPolicyDelete.Status

  return $results
}

$labels = @(
  'Admin claims list',
  'Admin dashboard',
  'Admin policy type create',
  'Admin policy type delete',
  'Admin policy type update',
  'Admin policy types list',
  'Admin user status update (disable)',
  'Admin user status update (enable)',
  'Admin users list',
  'Auth login (admin)',
  'Auth register (customer)',
  'Claim create (customer)',
  'Claim status update (admin)',
  'Claim submit (customer)',
  'Claims admin list',
  'Identity admin update user status (disable)',
  'Identity admin update user status (enable)',
  'Identity admin users count',
  'Identity admin users list',
  'Identity profile (customer)',
  'Policy create (customer)',
  'Policy my list (customer)',
  'Policy premium calculate (customer)',
  'Policy types list'
)

$gatewayBase = @{ auth='http://localhost:5000/gateway/auth'; policy='http://localhost:5000/gateway/policies'; claims='http://localhost:5000/gateway/claims'; admin='http://localhost:5000/gateway/admin' }
$directBase = @{ auth='http://localhost:5265/api/auth'; policy='http://localhost:5145/api/policies'; claims='http://localhost:5084/api/claims'; admin='http://localhost:5073/api/admin' }

$gw = Run-Smoke -Mode 'gateway' -Base $gatewayBase
$dr = Run-Smoke -Mode 'direct' -Base $directBase

$gwPass = 0; $gwFail = 0; $drPass = 0; $drFail = 0
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('=== SIDE-BY-SIDE AUTHENTICATED FUNCTIONAL SMOKE ===')

foreach($label in $labels){
  $gs = [int]($gw[$label])
  $ds = [int]($dr[$label])
  if($gs -ge 200 -and $gs -lt 300){$gwPass++} else {$gwFail++}
  if($ds -ge 200 -and $ds -lt 300){$drPass++} else {$drFail++}
}

$lines.Add("Gateway: pass=$gwPass fail=$gwFail")
$lines.Add("Direct: pass=$drPass fail=$drFail")
$lines.Add('Label|Gateway|Direct')
$lines.Add('---|---|---')

$failures = New-Object System.Collections.Generic.List[string]
foreach($label in $labels){
  $gs = [int]($gw[$label])
  $ds = [int]($dr[$label])
  $gt = Status-Text -Status $gs
  $dt = Status-Text -Status $ds
  $lines.Add("$label|$gt|$dt")
  if(($gs -lt 200 -or $gs -ge 300) -or ($ds -lt 200 -or $ds -ge 300)) {
    $failures.Add("$label => Gateway:$gs, Direct:$ds")
  }
}

$lines.Add('')
$lines.Add('=== FAILURES ===')
if($failures.Count -eq 0){
  $lines.Add('None')
} else {
  foreach($f in $failures){ $lines.Add($f) }
}

$reportPath = 'c:\Users\Pavan\Desktop\SmartSure\smoke_side_by_side_report.txt'
[System.IO.File]::WriteAllLines($reportPath, $lines)

Write-Output "Report written: $reportPath"
Write-Output "Gateway pass=$gwPass fail=$gwFail"
Write-Output "Direct  pass=$drPass fail=$drFail"