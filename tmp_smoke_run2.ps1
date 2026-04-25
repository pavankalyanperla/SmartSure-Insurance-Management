$ErrorActionPreference='Stop'

function Call-Api {
  param(
    [string]$Method,
    [string]$Url,
    [hashtable]$Headers = $null,
    $Body = $null
  )

  try {
    $params = @{
      Uri = $Url
      Method = $Method
      UseBasicParsing = $true
      TimeoutSec = 20
    }

    if ($Headers) { $params['Headers'] = $Headers }
    if ($Body -ne $null) {
      $params['ContentType'] = 'application/json'
      $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
    }

    $r = Invoke-WebRequest @params
    return [pscustomobject]@{ Status=[int]$r.StatusCode; Content=$r.Content }
  } catch {
    $status = -1
    $content = ''
    if ($_.Exception.Response) {
      try { $status = [int]$_.Exception.Response.StatusCode.value__ } catch { $status = -1 }
      try {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $content = $sr.ReadToEnd()
      } catch {}
    }
    return [pscustomobject]@{ Status=$status; Content=$content }
  }
}

function Run-Matrix {
  param(
    [string]$Mode,
    [string]$AuthBase,
    [string]$PolicyBase,
    [string]$ClaimsBase,
    [string]$AdminBase
  )

  $results = @{}
  $stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff')
  $customerEmail = "smoke.$Mode.$stamp@example.com"
  $customerPassword = 'Cust@12345'

  $reg = Call-Api -Method 'POST' -Url "$AuthBase/register" -Body @{ fullName='Smoke User'; email=$customerEmail; password=$customerPassword }
  $results['Auth register (customer)'] = $reg.Status

  $adminLogin = Call-Api -Method 'POST' -Url "$AuthBase/login" -Body @{ email='admin@smartsure.com'; password='Admin@123' }
  $results['Auth login (admin)'] = $adminLogin.Status

  $adminToken = $null
  if ($adminLogin.Content) {
    try { $adminToken = ($adminLogin.Content | ConvertFrom-Json).token } catch {}
  }
  $adminHeaders = if ($adminToken) { @{ Authorization = "Bearer $adminToken" } } else { @{} }

  $customerToken = $null
  $custLogin = Call-Api -Method 'POST' -Url "$AuthBase/login" -Body @{ email='john@example.com'; password='Customer@123' }
  if ($custLogin.Content) {
    try { $customerToken = ($custLogin.Content | ConvertFrom-Json).token } catch {}
  }

  if (-not $customerToken) {
    $custLoginFallback = Call-Api -Method 'POST' -Url "$AuthBase/login" -Body @{ email=$customerEmail; password=$customerPassword }
    if ($custLoginFallback.Content) {
      try { $customerToken = ($custLoginFallback.Content | ConvertFrom-Json).token } catch {}
    }
  }
  $customerHeaders = if ($customerToken) { @{ Authorization = "Bearer $customerToken" } } else { @{} }

  $profile = Call-Api -Method 'GET' -Url "$AuthBase/profile" -Headers $customerHeaders
  $results['Identity profile (customer)'] = $profile.Status

  $idUsers = Call-Api -Method 'GET' -Url "$AuthBase/admin/users" -Headers $adminHeaders
  $results['Identity admin users list'] = $idUsers.Status

  $idCount = Call-Api -Method 'GET' -Url "$AuthBase/admin/users/count" -Headers $adminHeaders
  $results['Identity admin users count'] = $idCount.Status

  $targetUserId = 2
  if ($idUsers.Content) {
    try {
      $users = $idUsers.Content | ConvertFrom-Json
      foreach($u in $users){ if($u.email -eq $customerEmail){ $targetUserId = $u.id; break } }
    } catch {}
  }

  $idDisable = Call-Api -Method 'PUT' -Url "$AuthBase/admin/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$false }
  $results['Identity admin update user status (disable)'] = $idDisable.Status
  $idEnable = Call-Api -Method 'PUT' -Url "$AuthBase/admin/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$true }
  $results['Identity admin update user status (enable)'] = $idEnable.Status

  $types = Call-Api -Method 'GET' -Url "$PolicyBase/types" -Headers $customerHeaders
  $results['Policy types list'] = $types.Status

  $policyTypeId = 1
  if ($types.Content) {
    try {
      $t = $types.Content | ConvertFrom-Json
      if ($t.Count -gt 0) { $policyTypeId = [int]$t[0].id }
    } catch {}
  }

  $start = (Get-Date).Date.AddDays(1).ToString('o')
  $end = (Get-Date).Date.AddYears(1).ToString('o')

  $premium = Call-Api -Method 'POST' -Url "$PolicyBase/calculate-premium" -Headers $customerHeaders -Body @{ policyTypeId=$policyTypeId; age=30; startDate=$start; endDate=$end }
  $results['Policy premium calculate (customer)'] = $premium.Status

  $createPolicy = Call-Api -Method 'POST' -Url "$PolicyBase" -Headers $customerHeaders -Body @{ policyTypeId=$policyTypeId; age=30; startDate=$start; endDate=$end }
  $results['Policy create (customer)'] = $createPolicy.Status

  $myPolicies = Call-Api -Method 'GET' -Url "$PolicyBase/my" -Headers $customerHeaders
  $results['Policy my list (customer)'] = $myPolicies.Status

  $policyId = 1
  if ($createPolicy.Content) {
    try { $policyId = [int](($createPolicy.Content | ConvertFrom-Json).id) } catch {}
  }

  $createClaim = Call-Api -Method 'POST' -Url "$ClaimsBase" -Headers $customerHeaders -Body @{ policyId=$policyId; incidentDate=(Get-Date).Date.AddDays(-1).ToString('o'); description='Smoke claim' }
  $results['Claim create (customer)'] = $createClaim.Status

  $claimId = 1
  if ($createClaim.Content) {
    try { $claimId = [int](($createClaim.Content | ConvertFrom-Json).id) } catch {}
  }

  $submitClaim = Call-Api -Method 'POST' -Url "$ClaimsBase/$claimId/submit" -Headers $customerHeaders -Body @{}
  $results['Claim submit (customer)'] = $submitClaim.Status

  $claimsList = Call-Api -Method 'GET' -Url "$ClaimsBase" -Headers $adminHeaders
  $results['Claims admin list'] = $claimsList.Status

  $claimUpdate = Call-Api -Method 'PUT' -Url "$ClaimsBase/$claimId/status" -Headers $adminHeaders -Body @{ status='UnderReview'; adminNote='Smoke review' }
  $results['Claim status update (admin)'] = $claimUpdate.Status

  $admDash = Call-Api -Method 'GET' -Url "$AdminBase/dashboard" -Headers $adminHeaders
  $results['Admin dashboard'] = $admDash.Status

  $admClaims = Call-Api -Method 'GET' -Url "$AdminBase/claims" -Headers $adminHeaders
  $results['Admin claims list'] = $admClaims.Status

  $admUsers = Call-Api -Method 'GET' -Url "$AdminBase/users" -Headers $adminHeaders
  $results['Admin users list'] = $admUsers.Status

  $admDisable = Call-Api -Method 'PUT' -Url "$AdminBase/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$false }
  $results['Admin user status update (disable)'] = $admDisable.Status
  $admEnable = Call-Api -Method 'PUT' -Url "$AdminBase/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$true }
  $results['Admin user status update (enable)'] = $admEnable.Status

  $admTypes = Call-Api -Method 'GET' -Url "$PolicyBase/admin/types" -Headers $adminHeaders
  $results['Admin policy types list'] = $admTypes.Status

  $typeName = "SmokeType-$Mode-$stamp"
  $admCreateType = Call-Api -Method 'POST' -Url "$PolicyBase/admin/types" -Headers $adminHeaders -Body @{ name=$typeName; description='Smoke policy type'; baseAmount=1234 }
  $results['Admin policy type create'] = $admCreateType.Status

  $newTypeId = 1
  if ($admCreateType.Content) {
    try { $newTypeId = [int](($admCreateType.Content | ConvertFrom-Json).id) } catch {}
  }

  $admUpdateType = Call-Api -Method 'PUT' -Url "$PolicyBase/admin/types/$newTypeId" -Headers $adminHeaders -Body @{ name="$typeName-upd"; description='Updated'; baseAmount=1500 }
  $results['Admin policy type update'] = $admUpdateType.Status

  $admDeleteType = Call-Api -Method 'DELETE' -Url "$PolicyBase/admin/types/$newTypeId" -Headers $adminHeaders
  $results['Admin policy type delete'] = $admDeleteType.Status

  return $results
}

$labels = @(
  'Admin claims list','Admin dashboard','Admin policy type create','Admin policy type delete','Admin policy type update','Admin policy types list','Admin user status update (disable)','Admin user status update (enable)','Admin users list','Auth login (admin)','Auth register (customer)','Claim create (customer)','Claim status update (admin)','Claim submit (customer)','Claims admin list','Identity admin update user status (disable)','Identity admin update user status (enable)','Identity admin users count','Identity admin users list','Identity profile (customer)','Policy create (customer)','Policy my list (customer)','Policy premium calculate (customer)','Policy types list'
)

$gw = Run-Matrix -Mode 'gateway' -AuthBase 'http://localhost:5000/gateway/auth' -PolicyBase 'http://localhost:5000/gateway/policies' -ClaimsBase 'http://localhost:5000/gateway/claims' -AdminBase 'http://localhost:5000/gateway/admin'
$dr = Run-Matrix -Mode 'direct' -AuthBase 'http://localhost:5265/api/auth' -PolicyBase 'http://localhost:5145/api/policies' -ClaimsBase 'http://localhost:5084/api/claims' -AdminBase 'http://localhost:5073/api/admin'

function Txt([int]$s){ if($s -ge 200 -and $s -lt 300){"$s (PASS)"} else {"$s (FAIL)"} }

$gwPass=0; $gwFail=0; $drPass=0; $drFail=0
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('=== SIDE-BY-SIDE AUTHENTICATED FUNCTIONAL SMOKE ===')
foreach($label in $labels){
  $gs=[int]$gw[$label]; $ds=[int]$dr[$label]
  if($gs -ge 200 -and $gs -lt 300){$gwPass++}else{$gwFail++}
  if($ds -ge 200 -and $ds -lt 300){$drPass++}else{$drFail++}
}
$lines.Add("Gateway: pass=$gwPass fail=$gwFail")
$lines.Add("Direct: pass=$drPass fail=$drFail")
$lines.Add('Label|Gateway|Direct')
$lines.Add('---|---|---')
$fails = New-Object System.Collections.Generic.List[string]
foreach($label in $labels){
  $gs=[int]$gw[$label]; $ds=[int]$dr[$label]
  $lines.Add("$label|$(Txt $gs)|$(Txt $ds)")
  if(($gs -lt 200 -or $gs -ge 300) -or ($ds -lt 200 -or $ds -ge 300)){ $fails.Add("$label => Gateway:$gs, Direct:$ds") }
}
$lines.Add('')
$lines.Add('=== FAILURES ===')
if($fails.Count -eq 0){$lines.Add('None')} else { foreach($f in $fails){$lines.Add($f)} }
[System.IO.File]::WriteAllLines('c:\Users\Pavan\Desktop\SmartSure\smoke_side_by_side_report.txt', $lines)
Write-Output "Report written"
Write-Output "Gateway pass=$gwPass fail=$gwFail"
Write-Output "Direct pass=$drPass fail=$drFail"
$ErrorActionPreference='Stop'

function Call-Api {
  param(
    [string]$Method,
    [string]$Url,
    [hashtable]$Headers = $null,
    $Body = $null
  )

  try {
    $params = @{
      Uri = $Url
      Method = $Method
      UseBasicParsing = $true
      TimeoutSec = 20
    }

    if ($Headers) { $params['Headers'] = $Headers }
    if ($Body -ne $null) {
      $params['ContentType'] = 'application/json'
      $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
    }

    $r = Invoke-WebRequest @params
    return [pscustomobject]@{ Status=[int]$r.StatusCode; Content=$r.Content }
  } catch {
    $status = -1
    $content = ''
    if ($_.Exception.Response) {
      try { $status = [int]$_.Exception.Response.StatusCode.value__ } catch { $status = -1 }
      try {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $content = $sr.ReadToEnd()
      } catch {}
    }
    return [pscustomobject]@{ Status=$status; Content=$content }
  }
}

function Run-Matrix {
  param(
    [string]$Mode,
    [string]$AuthBase,
    [string]$PolicyBase,
    [string]$ClaimsBase,
    [string]$AdminBase
  )

  $results = @{}
  $stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff')
  $customerEmail = "smoke.$Mode.$stamp@example.com"
  $customerPassword = 'Cust@12345'

  $reg = Call-Api -Method 'POST' -Url "$AuthBase/register" -Body @{ fullName='Smoke User'; email=$customerEmail; password=$customerPassword }
  $results['Auth register (customer)'] = $reg.Status

  $adminLogin = Call-Api -Method 'POST' -Url "$AuthBase/login" -Body @{ email='admin@smartsure.com'; password='Admin@123' }
  $results['Auth login (admin)'] = $adminLogin.Status

  $adminToken = $null
  if ($adminLogin.Content) {
    try { $adminToken = ($adminLogin.Content | ConvertFrom-Json).token } catch {}
  }
  $adminHeaders = if ($adminToken) { @{ Authorization = "Bearer $adminToken" } } else { @{} }

  $customerToken = $null
  if ($reg.Content) {
    try { $customerToken = ($reg.Content | ConvertFrom-Json).token } catch {}
  }

  if (-not $customerToken) {
    $custLogin = Call-Api -Method 'POST' -Url "$AuthBase/login" -Body @{ email=$customerEmail; password=$customerPassword }
    if ($custLogin.Content) {
      try { $customerToken = ($custLogin.Content | ConvertFrom-Json).token } catch {}
    }
  }
  $customerHeaders = if ($customerToken) { @{ Authorization = "Bearer $customerToken" } } else { @{} }

  $profile = Call-Api -Method 'GET' -Url "$AuthBase/profile" -Headers $customerHeaders
  $results['Identity profile (customer)'] = $profile.Status

  $idUsers = Call-Api -Method 'GET' -Url "$AuthBase/admin/users" -Headers $adminHeaders
  $results['Identity admin users list'] = $idUsers.Status

  $idCount = Call-Api -Method 'GET' -Url "$AuthBase/admin/users/count" -Headers $adminHeaders
  $results['Identity admin users count'] = $idCount.Status

  $targetUserId = 2
  if ($idUsers.Content) {
    try {
      $users = $idUsers.Content | ConvertFrom-Json
      foreach($u in $users){ if($u.email -eq $customerEmail){ $targetUserId = $u.id; break } }
    } catch {}
  }

  $idDisable = Call-Api -Method 'PUT' -Url "$AuthBase/admin/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$false }
  $results['Identity admin update user status (disable)'] = $idDisable.Status
  $idEnable = Call-Api -Method 'PUT' -Url "$AuthBase/admin/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$true }
  $results['Identity admin update user status (enable)'] = $idEnable.Status

  $types = Call-Api -Method 'GET' -Url "$PolicyBase/types" -Headers $customerHeaders
  $results['Policy types list'] = $types.Status

  $policyTypeId = 1
  if ($types.Content) {
    try {
      $t = $types.Content | ConvertFrom-Json
      if ($t.Count -gt 0) { $policyTypeId = [int]$t[0].id }
    } catch {}
  }

  $start = (Get-Date).Date.AddDays(1).ToString('o')
  $end = (Get-Date).Date.AddYears(1).ToString('o')

  $premium = Call-Api -Method 'POST' -Url "$PolicyBase/calculate-premium" -Headers $customerHeaders -Body @{ policyTypeId=$policyTypeId; age=30; startDate=$start; endDate=$end }
  $results['Policy premium calculate (customer)'] = $premium.Status

  $createPolicy = Call-Api -Method 'POST' -Url "$PolicyBase" -Headers $customerHeaders -Body @{ policyTypeId=$policyTypeId; age=30; startDate=$start; endDate=$end }
  $results['Policy create (customer)'] = $createPolicy.Status

  $myPolicies = Call-Api -Method 'GET' -Url "$PolicyBase/my" -Headers $customerHeaders
  $results['Policy my list (customer)'] = $myPolicies.Status

  $policyId = 1
  if ($createPolicy.Content) {
    try { $policyId = [int](($createPolicy.Content | ConvertFrom-Json).id) } catch {}
  }

  $createClaim = Call-Api -Method 'POST' -Url "$ClaimsBase" -Headers $customerHeaders -Body @{ policyId=$policyId; incidentDate=(Get-Date).Date.AddDays(-1).ToString('o'); description='Smoke claim' }
  $results['Claim create (customer)'] = $createClaim.Status

  $claimId = 1
  if ($createClaim.Content) {
    try { $claimId = [int](($createClaim.Content | ConvertFrom-Json).id) } catch {}
  }

  $submitClaim = Call-Api -Method 'POST' -Url "$ClaimsBase/$claimId/submit" -Headers $customerHeaders -Body @{}
  $results['Claim submit (customer)'] = $submitClaim.Status

  $claimsList = Call-Api -Method 'GET' -Url "$ClaimsBase" -Headers $adminHeaders
  $results['Claims admin list'] = $claimsList.Status

  $claimUpdate = Call-Api -Method 'PUT' -Url "$ClaimsBase/$claimId/status" -Headers $adminHeaders -Body @{ status='UnderReview'; adminNote='Smoke review' }
  $results['Claim status update (admin)'] = $claimUpdate.Status

  $admDash = Call-Api -Method 'GET' -Url "$AdminBase/dashboard" -Headers $adminHeaders
  $results['Admin dashboard'] = $admDash.Status

  $admClaims = Call-Api -Method 'GET' -Url "$AdminBase/claims" -Headers $adminHeaders
  $results['Admin claims list'] = $admClaims.Status

  $admUsers = Call-Api -Method 'GET' -Url "$AdminBase/users" -Headers $adminHeaders
  $results['Admin users list'] = $admUsers.Status

  $admDisable = Call-Api -Method 'PUT' -Url "$AdminBase/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$false }
  $results['Admin user status update (disable)'] = $admDisable.Status
  $admEnable = Call-Api -Method 'PUT' -Url "$AdminBase/users/$targetUserId/status" -Headers $adminHeaders -Body @{ isActive=$true }
  $results['Admin user status update (enable)'] = $admEnable.Status

  $admTypes = Call-Api -Method 'GET' -Url "$PolicyBase/admin/types" -Headers $adminHeaders
  $results['Admin policy types list'] = $admTypes.Status

  $typeName = "SmokeType-$Mode-$stamp"
  $admCreateType = Call-Api -Method 'POST' -Url "$PolicyBase/admin/types" -Headers $adminHeaders -Body @{ name=$typeName; description='Smoke policy type'; baseAmount=1234 }
  $results['Admin policy type create'] = $admCreateType.Status

  $newTypeId = 1
  if ($admCreateType.Content) {
    try { $newTypeId = [int](($admCreateType.Content | ConvertFrom-Json).id) } catch {}
  }

  $admUpdateType = Call-Api -Method 'PUT' -Url "$PolicyBase/admin/types/$newTypeId" -Headers $adminHeaders -Body @{ name="$typeName-upd"; description='Updated'; baseAmount=1500 }
  $results['Admin policy type update'] = $admUpdateType.Status

  $admDeleteType = Call-Api -Method 'DELETE' -Url "$PolicyBase/admin/types/$newTypeId" -Headers $adminHeaders
  $results['Admin policy type delete'] = $admDeleteType.Status

  return $results
}

$labels = @(
  'Admin claims list','Admin dashboard','Admin policy type create','Admin policy type delete','Admin policy type update','Admin policy types list','Admin user status update (disable)','Admin user status update (enable)','Admin users list','Auth login (admin)','Auth register (customer)','Claim create (customer)','Claim status update (admin)','Claim submit (customer)','Claims admin list','Identity admin update user status (disable)','Identity admin update user status (enable)','Identity admin users count','Identity admin users list','Identity profile (customer)','Policy create (customer)','Policy my list (customer)','Policy premium calculate (customer)','Policy types list'
)

$gw = Run-Matrix -Mode 'gateway' -AuthBase 'http://localhost:5000/gateway/auth' -PolicyBase 'http://localhost:5000/gateway/policies' -ClaimsBase 'http://localhost:5000/gateway/claims' -AdminBase 'http://localhost:5000/gateway/admin'
$dr = Run-Matrix -Mode 'direct' -AuthBase 'http://localhost:5265/api/auth' -PolicyBase 'http://localhost:5145/api/policies' -ClaimsBase 'http://localhost:5084/api/claims' -AdminBase 'http://localhost:5073/api/admin'

function Txt([int]$s){ if($s -ge 200 -and $s -lt 300){"$s (PASS)"} else {"$s (FAIL)"} }

$gwPass=0; $gwFail=0; $drPass=0; $drFail=0
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('=== SIDE-BY-SIDE AUTHENTICATED FUNCTIONAL SMOKE ===')
foreach($label in $labels){
  $gs=[int]$gw[$label]; $ds=[int]$dr[$label]
  if($gs -ge 200 -and $gs -lt 300){$gwPass++}else{$gwFail++}
  if($ds -ge 200 -and $ds -lt 300){$drPass++}else{$drFail++}
}
$lines.Add("Gateway: pass=$gwPass fail=$gwFail")
$lines.Add("Direct: pass=$drPass fail=$drFail")
$lines.Add('Label|Gateway|Direct')
$lines.Add('---|---|---')
$fails = New-Object System.Collections.Generic.List[string]
foreach($label in $labels){
  $gs=[int]$gw[$label]; $ds=[int]$dr[$label]
  $lines.Add("$label|$(Txt $gs)|$(Txt $ds)")
  if(($gs -lt 200 -or $gs -ge 300) -or ($ds -lt 200 -or $ds -ge 300)){ $fails.Add("$label => Gateway:$gs, Direct:$ds") }
}
$lines.Add('')
$lines.Add('=== FAILURES ===')
if($fails.Count -eq 0){$lines.Add('None')} else { foreach($f in $fails){$lines.Add($f)} }
[System.IO.File]::WriteAllLines('c:\Users\Pavan\Desktop\SmartSure\smoke_side_by_side_report.txt', $lines)
Write-Output "Report written"
Write-Output "Gateway pass=$gwPass fail=$gwFail"
Write-Output "Direct pass=$drPass fail=$drFail"