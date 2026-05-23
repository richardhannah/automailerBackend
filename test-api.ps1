##############################################################################
# AutoMailer API - Comprehensive Endpoint Test Script
# Logs in as admin/admin, then tests every endpoint (CRUD + edge cases)
##############################################################################

param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

# -- Helpers -----------------------------------------------------------------

$script:passed = 0
$script:failed = 0
$script:total  = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int[]]$ExpectedStatus = @(200),
        [scriptblock]$Validate = $null
    )

    $script:total++
    $uri = "$BaseUrl$Url"

    $params = @{
        Method  = $Method
        Uri     = $uri
        Headers = $Headers
        ContentType = "application/json"
        ErrorAction = "Stop"
    }

    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    try {
        $response = Invoke-WebRequest @params -UseBasicParsing
        $status = $response.StatusCode
        $json = $null
        if ($response.Content) {
            try { $json = $response.Content | ConvertFrom-Json } catch {}
        }

        if ($ExpectedStatus -contains $status) {
            $valid = $true
            if ($Validate) {
                $valid = & $Validate $json
            }
            if ($valid) {
                Write-Host "  PASS  " -ForegroundColor Green -NoNewline
                Write-Host " $Name ($Method $Url) -> $status"
                $script:passed++
            } else {
                Write-Host "  FAIL  " -ForegroundColor Red -NoNewline
                Write-Host " $Name - validation failed. Response: $($response.Content)"
                $script:failed++
            }
        } else {
            Write-Host "  FAIL  " -ForegroundColor Red -NoNewline
            Write-Host " $Name - expected $($ExpectedStatus -join ',') got $status"
            $script:failed++
        }

        return $json
    }
    catch {
        $errStatus = 0
        $errBody = ""
        if ($_.Exception.Response) {
            $errStatus = [int]$_.Exception.Response.StatusCode
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $errBody = $reader.ReadToEnd()
            } catch {}
        }

        if ($ExpectedStatus -contains $errStatus) {
            Write-Host "  PASS  " -ForegroundColor Green -NoNewline
            Write-Host " $Name ($Method $Url) -> $errStatus"
            $script:passed++
            try { return ($errBody | ConvertFrom-Json) } catch { return $null }
        } else {
            Write-Host "  FAIL  " -ForegroundColor Red -NoNewline
            Write-Host " $Name ($Method $Url) -> $errStatus $($_.Exception.Message)"
            $script:failed++
            return $null
        }
    }
}

function Auth-Headers {
    return @{ "Authorization" = "Bearer $script:token" }
}

# -- Start -------------------------------------------------------------------

Write-Host ""
Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host "  AutoMailer API Test Suite" -ForegroundColor Cyan
Write-Host "  Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# LOGIN
# ===========================================================================

Write-Host "-- Login --------------------------------------------------" -ForegroundColor Yellow

$loginResult = Test-Endpoint -Name "Login with admin/admin" `
    -Method POST -Url "/api/login" `
    -Body @{ username = "admin"; password = "admin" } `
    -Validate { param($r) $r.token -and $r.role -eq "Admin" }

$script:token = $loginResult.token

Test-Endpoint -Name "Login with bad credentials" `
    -Method POST -Url "/api/login" `
    -Body @{ username = "admin"; password = "wrong" } `
    -ExpectedStatus @(401)

# -- Login /me ---------------------------------------------------------------

Test-Endpoint -Name "GET /me (authenticated)" `
    -Method GET -Url "/api/login/me" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.username -eq "admin" -and $r.role -eq "Admin" }

Test-Endpoint -Name "GET /me (no token)" `
    -Method GET -Url "/api/login/me" `
    -ExpectedStatus @(401)

# ===========================================================================
# REGISTER
# ===========================================================================

Write-Host ""
Write-Host "-- Register -----------------------------------------------" -ForegroundColor Yellow

$testUser = "testuser_$(Get-Random -Minimum 10000 -Maximum 99999)"

$regResult = Test-Endpoint -Name "Register new user" `
    -Method POST -Url "/api/login/register" `
    -Body @{ username = $testUser; password = "Test123!"; email = "$testUser@test.com"; phone = "1234567890" } `
    -Validate { param($r) $r.token -and $r.username -eq $testUser }

Test-Endpoint -Name "Register duplicate username" `
    -Method POST -Url "/api/login/register" `
    -Body @{ username = $testUser; password = "Test123!"; email = "$testUser@test.com" } `
    -ExpectedStatus @(409)

# ===========================================================================
# USERS
# ===========================================================================

Write-Host ""
Write-Host "-- Users --------------------------------------------------" -ForegroundColor Yellow

$users = Test-Endpoint -Name "GET all users" `
    -Method GET -Url "/api/users" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

# Find the test user we just registered
$testUserObj = $users | Where-Object { $_.username -eq $testUser }

if ($testUserObj) {
    Test-Endpoint -Name "Update user role to Admin" `
        -Method PUT -Url "/api/users/$($testUserObj.userId)/role" `
        -Headers (Auth-Headers) `
        -Body @{ role = "Admin" } `
        -Validate { param($r) $r.role -eq "Admin" }

    Test-Endpoint -Name "Update user role back to User" `
        -Method PUT -Url "/api/users/$($testUserObj.userId)/role" `
        -Headers (Auth-Headers) `
        -Body @{ role = "User" } `
        -Validate { param($r) $r.role -eq "User" }

    Test-Endpoint -Name "Update role with invalid value" `
        -Method PUT -Url "/api/users/$($testUserObj.userId)/role" `
        -Headers (Auth-Headers) `
        -Body @{ role = "SuperAdmin" } `
        -ExpectedStatus @(400)

    Test-Endpoint -Name "Delete test user" `
        -Method DELETE -Url "/api/users/$($testUserObj.userId)" `
        -Headers (Auth-Headers) `
        -Validate { param($r) $r.message -eq "User deleted" }
}

Test-Endpoint -Name "Update role for non-existent user" `
    -Method PUT -Url "/api/users/00000000-0000-0000-0000-000000099999/role" `
    -Headers (Auth-Headers) `
    -Body @{ role = "Admin" } `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete non-existent user" `
    -Method DELETE -Url "/api/users/00000000-0000-0000-0000-000000099999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "GET users without auth" `
    -Method GET -Url "/api/users" `
    -ExpectedStatus @(401)

# ===========================================================================
# CUSTOMERS
# ===========================================================================

Write-Host ""
Write-Host "-- Customers ----------------------------------------------" -ForegroundColor Yellow

$custResult = Test-Endpoint -Name "Create customer" `
    -Method POST -Url "/api/customers" `
    -Headers (Auth-Headers) `
    -Body @{
        firstName = "John"; lastName = "Doe"; email = "john@test.com"
        phone = "555-1234"; iptvUser = "johndoe"; iptvPassword = "secret"
        notes = "Test customer"; expirationDate = "2026-12-31"; followUp = $true
    } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.firstName -eq "John" }

$custId = $custResult.customerId

Test-Endpoint -Name "GET all customers" `
    -Method GET -Url "/api/customers" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "GET customer by ID" `
    -Method GET -Url "/api/customers/$custId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.firstName -eq "John" -and $r.lastName -eq "Doe" }

Test-Endpoint -Name "Update customer" `
    -Method PUT -Url "/api/customers/$custId" `
    -Headers (Auth-Headers) `
    -Body @{
        firstName = "Jane"; lastName = "Doe"; email = "jane@test.com"
        phone = "555-5678"; iptvUser = "janedoe"; iptvPassword = "newsecret"
        notes = "Updated"; expirationDate = "2027-06-30"; followUp = $false
    } `
    -Validate { param($r) $r.firstName -eq "Jane" }

Test-Endpoint -Name "GET non-existent customer" `
    -Method GET -Url "/api/customers/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Update non-existent customer" `
    -Method PUT -Url "/api/customers/999999" `
    -Headers (Auth-Headers) `
    -Body @{
        firstName = "X"; lastName = "Y"; email = "x@y.com"
        iptvUser = "xy"
    } `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete customer" `
    -Method DELETE -Url "/api/customers/$custId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Customer deleted" }

Test-Endpoint -Name "Delete non-existent customer" `
    -Method DELETE -Url "/api/customers/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "GET customers without auth" `
    -Method GET -Url "/api/customers" `
    -ExpectedStatus @(401)

# ===========================================================================
# EMAIL TEMPLATES
# ===========================================================================

Write-Host ""
Write-Host "-- Email Templates ----------------------------------------" -ForegroundColor Yellow

$tmplResult = Test-Endpoint -Name "Create email template" `
    -Method POST -Url "/api/emailtemplates" `
    -Headers (Auth-Headers) `
    -Body @{
        templateName = "Test Template"
        bodyText = "Hello {{name}}"
        bodyHtml = '<h1>Hello {{name}}</h1>'
    } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.templateName -eq "Test Template" }

$tmplGuid = $tmplResult.emailTemplateGuid

Test-Endpoint -Name "GET all email templates" `
    -Method GET -Url "/api/emailtemplates" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "GET email template by GUID" `
    -Method GET -Url "/api/emailtemplates/$tmplGuid" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.templateName -eq "Test Template" }

Test-Endpoint -Name "Update email template" `
    -Method PUT -Url "/api/emailtemplates/$tmplGuid" `
    -Headers (Auth-Headers) `
    -Body @{
        templateName = "Updated Template"
        bodyText = "Hi {{name}}"
        bodyHtml = '<p>Hi {{name}}</p>'
    } `
    -Validate { param($r) $r.templateName -eq "Updated Template" }

$fakeGuid = "00000000-0000-0000-0000-000000099999"

Test-Endpoint -Name "GET non-existent template" `
    -Method GET -Url "/api/emailtemplates/$fakeGuid" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Update non-existent template" `
    -Method PUT -Url "/api/emailtemplates/$fakeGuid" `
    -Headers (Auth-Headers) `
    -Body @{ templateName = "X"; bodyText = ""; bodyHtml = "" } `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete email template" `
    -Method DELETE -Url "/api/emailtemplates/$tmplGuid" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Template deleted" }

Test-Endpoint -Name "Delete non-existent template" `
    -Method DELETE -Url "/api/emailtemplates/$fakeGuid" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "GET templates without auth" `
    -Method GET -Url "/api/emailtemplates" `
    -ExpectedStatus @(401)

# ===========================================================================
# IPTV PACKAGES
# ===========================================================================

Write-Host ""
Write-Host "-- IPTV Packages ------------------------------------------" -ForegroundColor Yellow

$pkgResult = Test-Endpoint -Name "Create IPTV package" `
    -Method POST -Url "/api/iptvpackages" `
    -Headers (Auth-Headers) `
    -Body @{
        packageName = "Basic Plan"
        price = 9.99
        billingPeriod = 0  # Monthly
    } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.packageName -eq "Basic Plan" }

$pkgGuid = $pkgResult.iptvPackageGuid

Test-Endpoint -Name "GET all IPTV packages" `
    -Method GET -Url "/api/iptvpackages" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "GET IPTV package by GUID" `
    -Method GET -Url "/api/iptvpackages/$pkgGuid" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.packageName -eq "Basic Plan" }

Test-Endpoint -Name "Update IPTV package" `
    -Method PUT -Url "/api/iptvpackages/$pkgGuid" `
    -Headers (Auth-Headers) `
    -Body @{
        packageName = "Premium Plan"
        price = 19.99
        billingPeriod = 1  # Annual
    } `
    -Validate { param($r) $r.packageName -eq "Premium Plan" }

Test-Endpoint -Name "GET non-existent package" `
    -Method GET -Url "/api/iptvpackages/$fakeGuid" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Update non-existent package" `
    -Method PUT -Url "/api/iptvpackages/$fakeGuid" `
    -Headers (Auth-Headers) `
    -Body @{ packageName = "X"; price = 1; billingPeriod = 0 } `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete IPTV package" `
    -Method DELETE -Url "/api/iptvpackages/$pkgGuid" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Package deleted" }

Test-Endpoint -Name "Delete non-existent package" `
    -Method DELETE -Url "/api/iptvpackages/$fakeGuid" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

# ===========================================================================
# REPORT SETTINGS
# ===========================================================================

Write-Host ""
Write-Host "-- Report Settings ----------------------------------------" -ForegroundColor Yellow

# Create a template first for FK reference
$rptTmpl = Test-Endpoint -Name "Create template for report setting" `
    -Method POST -Url "/api/emailtemplates" `
    -Headers (Auth-Headers) `
    -Body @{
        templateName = "Report Template"
        bodyText = "Report body"
        bodyHtml = '<p>Report</p>'
    } `
    -ExpectedStatus @(201)

$rptTmplId = $rptTmpl.emailTemplateId

$rptResult = Test-Endpoint -Name "Create report setting" `
    -Method POST -Url "/api/reportsettings" `
    -Headers (Auth-Headers) `
    -Body @{
        name = "Daily Report"
        emailAddress = "reports@test.com"
        emailTemplateId = $rptTmplId
    } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.name -eq "Daily Report" }

$rptId = $rptResult.reportingSettingId

Test-Endpoint -Name "Create report setting with invalid template" `
    -Method POST -Url "/api/reportsettings" `
    -Headers (Auth-Headers) `
    -Body @{
        name = "Bad Report"
        emailAddress = "bad@test.com"
        emailTemplateId = 999999
    } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "Create report setting without template" `
    -Method POST -Url "/api/reportsettings" `
    -Headers (Auth-Headers) `
    -Body @{
        name = "No Template Report"
        emailAddress = "notemplate@test.com"
    } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.name -eq "No Template Report" }

Test-Endpoint -Name "GET all report settings" `
    -Method GET -Url "/api/reportsettings" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "GET report setting by ID" `
    -Method GET -Url "/api/reportsettings/$rptId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.name -eq "Daily Report" -and $r.emailTemplateName -eq "Report Template" }

Test-Endpoint -Name "Update report setting" `
    -Method PUT -Url "/api/reportsettings/$rptId" `
    -Headers (Auth-Headers) `
    -Body @{
        name = "Weekly Report"
        emailAddress = "weekly@test.com"
        emailTemplateId = $rptTmplId
    } `
    -Validate { param($r) $r.name -eq "Weekly Report" }

Test-Endpoint -Name "Update report setting with invalid template" `
    -Method PUT -Url "/api/reportsettings/$rptId" `
    -Headers (Auth-Headers) `
    -Body @{
        name = "Weekly Report"
        emailAddress = "weekly@test.com"
        emailTemplateId = 999999
    } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "GET non-existent report setting" `
    -Method GET -Url "/api/reportsettings/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Update non-existent report setting" `
    -Method PUT -Url "/api/reportsettings/999999" `
    -Headers (Auth-Headers) `
    -Body @{ name = "X"; emailAddress = "x@x.com" } `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete report setting" `
    -Method DELETE -Url "/api/reportsettings/$rptId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Report setting deleted" }

Test-Endpoint -Name "Delete non-existent report setting" `
    -Method DELETE -Url "/api/reportsettings/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "GET report settings without auth" `
    -Method GET -Url "/api/reportsettings" `
    -ExpectedStatus @(401)

# Clean up the template we created for report settings
Test-Endpoint -Name "Cleanup: delete report template" `
    -Method DELETE -Url "/api/emailtemplates/$($rptTmpl.emailTemplateGuid)" `
    -Headers (Auth-Headers)

# ===========================================================================
# ENQUIRIES
# ===========================================================================

Write-Host ""
Write-Host "-- Enquiries ----------------------------------------------" -ForegroundColor Yellow

$enqResult = Test-Endpoint -Name "Create enquiry (public)" `
    -Method POST -Url "/api/enquiries" `
    -Body @{
        email = "visitor@test.com"
        phoneNumber = "555-9999"
        message = "I want to know more about your plans"
    } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.email -eq "visitor@test.com" }

$enqId = $enqResult.enquiryId

Test-Endpoint -Name "GET all enquiries (admin)" `
    -Method GET -Url "/api/enquiries" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "GET enquiry by ID (admin)" `
    -Method GET -Url "/api/enquiries/$enqId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.email -eq "visitor@test.com" }

Test-Endpoint -Name "GET non-existent enquiry" `
    -Method GET -Url "/api/enquiries/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete enquiry" `
    -Method DELETE -Url "/api/enquiries/$enqId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Enquiry deleted" }

Test-Endpoint -Name "Delete non-existent enquiry" `
    -Method DELETE -Url "/api/enquiries/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "GET enquiries without auth" `
    -Method GET -Url "/api/enquiries" `
    -ExpectedStatus @(401)

# ===========================================================================
# CLEANUP: delete the extra report setting we created without a template
# ===========================================================================

$allRptSettings = Test-Endpoint -Name "Cleanup: GET all report settings" `
    -Method GET -Url "/api/reportsettings" `
    -Headers (Auth-Headers)

$noTmplRpt = $allRptSettings | Where-Object { $_.name -eq "No Template Report" }
if ($noTmplRpt) {
    Test-Endpoint -Name "Cleanup: delete no-template report setting" `
        -Method DELETE -Url "/api/reportsettings/$($noTmplRpt.reportingSettingId)" `
        -Headers (Auth-Headers)
}

# ===========================================================================
# USERS - EMAIL UPDATE
# ===========================================================================

Write-Host ""
Write-Host "-- Users Email --------------------------------------------" -ForegroundColor Yellow

# Register a user to test email update
$emailTestUser = "emailtest_$(Get-Random -Minimum 10000 -Maximum 99999)"
$emailRegResult = Test-Endpoint -Name "Register user for email test" `
    -Method POST -Url "/api/login/register" `
    -Body @{ username = $emailTestUser; password = "Test123!"; email = "$emailTestUser@test.com" } `
    -Validate { param($r) $r.token }

$allUsersForEmail = Test-Endpoint -Name "GET users to find email test user" `
    -Method GET -Url "/api/users" `
    -Headers (Auth-Headers)

$emailTestUserObj = $allUsersForEmail | Where-Object { $_.username -eq $emailTestUser }

if ($emailTestUserObj) {
    Test-Endpoint -Name "Update user email" `
        -Method PUT -Url "/api/users/$($emailTestUserObj.userId)/email" `
        -Headers (Auth-Headers) `
        -Body @{ email = "updated@test.com" } `
        -Validate { param($r) $r.email -eq "updated@test.com" }

    Test-Endpoint -Name "Update email for non-existent user" `
        -Method PUT -Url "/api/users/00000000-0000-0000-0000-000000099999/email" `
        -Headers (Auth-Headers) `
        -Body @{ email = "x@x.com" } `
        -ExpectedStatus @(404)

    # Clean up
    Test-Endpoint -Name "Cleanup: delete email test user" `
        -Method DELETE -Url "/api/users/$($emailTestUserObj.userId)" `
        -Headers (Auth-Headers)
}

# ===========================================================================
# WORKFLOW EMAIL SETTINGS
# ===========================================================================

Write-Host ""
Write-Host "-- Workflow Email Settings --------------------------------" -ForegroundColor Yellow

# Create a template for workflow settings
$wfTmpl = Test-Endpoint -Name "Create template for workflow setting" `
    -Method POST -Url "/api/emailtemplates" `
    -Headers (Auth-Headers) `
    -Body @{
        templateName = "Workflow Template"
        bodyText = "Hello {{customer.name}}"
        bodyHtml = '<p>Hello {{customer.name}}</p>'
    } `
    -ExpectedStatus @(201)

$wfTmplId = $wfTmpl.emailTemplateId

Test-Endpoint -Name "GET workflow settings for Enquiry (initially empty)" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry" `
    -Headers (Auth-Headers)

Test-Endpoint -Name "Upsert Enquiry/Customer workflow setting" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/Customer" `
    -Headers (Auth-Headers) `
    -Body @{ emailTemplateId = $wfTmplId } `
    -Validate { param($r) $r.workflowType -eq "Enquiry" -and $r.recipientType -eq "Customer" -and $r.emailTemplateId -eq $wfTmplId }

Test-Endpoint -Name "Upsert Enquiry/Admin workflow setting" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/Admin" `
    -Headers (Auth-Headers) `
    -Body @{ emailTemplateId = $wfTmplId } `
    -Validate { param($r) $r.recipientType -eq "Admin" }

Test-Endpoint -Name "GET workflow settings for Enquiry (now 2)" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -eq 2 }

Test-Endpoint -Name "Upsert with invalid template" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/Customer" `
    -Headers (Auth-Headers) `
    -Body @{ emailTemplateId = 999999 } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "Clear workflow setting (null template)" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/Customer" `
    -Headers (Auth-Headers) `
    -Body @{} `
    -Validate { param($r) $null -eq $r.emailTemplateId }

Test-Endpoint -Name "Upsert Subscription/Customer workflow setting" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Subscription/Customer" `
    -Headers (Auth-Headers) `
    -Body @{ emailTemplateId = $wfTmplId } `
    -Validate { param($r) $r.workflowType -eq "Subscription" }

Test-Endpoint -Name "GET workflow settings without auth" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry" `
    -ExpectedStatus @(401)

# -- Workflow Notification Recipients --

Write-Host ""
Write-Host "-- Workflow Notification Recipients ------------------------" -ForegroundColor Yellow

# Get admin user ID
$adminUserObj = $allUsersForEmail | Where-Object { $_.username -eq "admin" }
$adminUserId = $adminUserObj.userId

# Clean up any stale recipients first
$staleRecipients = Test-Endpoint -Name "GET existing Enquiry recipients (cleanup)" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry/recipients" `
    -Headers (Auth-Headers)

if ($staleRecipients) {
    foreach ($rid in $staleRecipients) {
        Test-Endpoint -Name "Cleanup: remove stale recipient $rid" `
            -Method DELETE -Url "/api/WorkflowEmailSettings/Enquiry/recipients/$rid" `
            -Headers (Auth-Headers)
    }
}

Test-Endpoint -Name "GET Enquiry recipients (empty after cleanup)" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry/recipients" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -eq 0 }

Test-Endpoint -Name "Add admin as Enquiry recipient" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/recipients/$adminUserId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Recipient added" }

Test-Endpoint -Name "Add admin again (idempotent)" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/recipients/$adminUserId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Recipient added" }

$recipients = Test-Endpoint -Name "GET Enquiry recipients (now 1)" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry/recipients" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -eq 1 }

Test-Endpoint -Name "Remove admin as Enquiry recipient" `
    -Method DELETE -Url "/api/WorkflowEmailSettings/Enquiry/recipients/$adminUserId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Recipient removed" }

Test-Endpoint -Name "GET Enquiry recipients (back to 0)" `
    -Method GET -Url "/api/WorkflowEmailSettings/Enquiry/recipients" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -eq 0 }

# ===========================================================================
# SUBSCRIPTIONS (Admin CRUD)
# ===========================================================================

Write-Host ""
Write-Host "-- Subscriptions ------------------------------------------" -ForegroundColor Yellow

# Create a customer and package for subscription tests
$subCust = Test-Endpoint -Name "Create customer for subscription test" `
    -Method POST -Url "/api/customers" `
    -Headers (Auth-Headers) `
    -Body @{
        firstName = "Sub"; lastName = "Tester"; email = "sub@test.com"
        iptvUser = "subtester"
    } `
    -ExpectedStatus @(201)

$subCustId = $subCust.customerId

$subPkg = Test-Endpoint -Name "Create package for subscription test" `
    -Method POST -Url "/api/iptvpackages" `
    -Headers (Auth-Headers) `
    -Body @{ packageName = "Sub Test Plan"; price = 14.99; billingPeriod = 0 } `
    -ExpectedStatus @(201)

$subPkgId = $subPkg.iptvPackageId
$subPkgGuid = $subPkg.iptvPackageGuid

$subResult = Test-Endpoint -Name "Create subscription" `
    -Method POST -Url "/api/subscriptions" `
    -Headers (Auth-Headers) `
    -Body @{ customerId = $subCustId; iptvPackageId = $subPkgId; status = "Pending" } `
    -ExpectedStatus @(201) `
    -Validate { param($r) $r.status -eq "Pending" -and $r.packageName -eq "Sub Test Plan" }

$subId = $subResult.subscriptionId

Test-Endpoint -Name "Create subscription with invalid customer" `
    -Method POST -Url "/api/subscriptions" `
    -Headers (Auth-Headers) `
    -Body @{ customerId = 999999; iptvPackageId = $subPkgId } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "Create subscription with invalid package" `
    -Method POST -Url "/api/subscriptions" `
    -Headers (Auth-Headers) `
    -Body @{ customerId = $subCustId; iptvPackageId = 999999 } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "GET all subscriptions" `
    -Method GET -Url "/api/subscriptions" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "GET subscription by ID" `
    -Method GET -Url "/api/subscriptions/$subId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.subscriptionId -eq $subId -and $r.links }

Test-Endpoint -Name "GET subscriptions by customer" `
    -Method GET -Url "/api/subscriptions/customer/$subCustId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.Count -ge 1 }

Test-Endpoint -Name "Update subscription status to Subscribed" `
    -Method PUT -Url "/api/subscriptions/$subId" `
    -Headers (Auth-Headers) `
    -Body @{ status = "Subscribed" } `
    -Validate { param($r) $r.status -eq "Subscribed" }

Test-Endpoint -Name "Update subscription with invalid status" `
    -Method PUT -Url "/api/subscriptions/$subId" `
    -Headers (Auth-Headers) `
    -Body @{ status = "Bogus" } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "GET non-existent subscription" `
    -Method GET -Url "/api/subscriptions/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Update non-existent subscription" `
    -Method PUT -Url "/api/subscriptions/999999" `
    -Headers (Auth-Headers) `
    -Body @{ status = "Cancelled" } `
    -ExpectedStatus @(404)

Test-Endpoint -Name "Delete subscription" `
    -Method DELETE -Url "/api/subscriptions/$subId" `
    -Headers (Auth-Headers) `
    -Validate { param($r) $r.message -eq "Subscription deleted" }

Test-Endpoint -Name "Delete non-existent subscription" `
    -Method DELETE -Url "/api/subscriptions/999999" `
    -Headers (Auth-Headers) `
    -ExpectedStatus @(404)

Test-Endpoint -Name "GET subscriptions without auth" `
    -Method GET -Url "/api/subscriptions" `
    -ExpectedStatus @(401)

# ===========================================================================
# SUBSCRIBE (User-facing endpoint)
# ===========================================================================

Write-Host ""
Write-Host "-- Subscribe (user-facing) --------------------------------" -ForegroundColor Yellow

# Register a new user, then subscribe as that user
$subUser = "subuser_$(Get-Random -Minimum 10000 -Maximum 99999)"
$subUserReg = Test-Endpoint -Name "Register user for subscribe test" `
    -Method POST -Url "/api/login/register" `
    -Body @{ username = $subUser; password = "Test123!"; email = "$subUser@test.com" } `
    -Validate { param($r) $r.token }

$subUserToken = $subUserReg.token
$subUserHeaders = @{ "Authorization" = "Bearer $subUserToken" }

Test-Endpoint -Name "Subscribe to package as user" `
    -Method POST -Url "/api/subscribe" `
    -Headers $subUserHeaders `
    -Body @{ iptvPackageId = $subPkgId } `
    -Validate { param($r) $r.status -eq "Pending" -and $r.packageName -eq "Sub Test Plan" }

Test-Endpoint -Name "Subscribe to same package again (duplicate)" `
    -Method POST -Url "/api/subscribe" `
    -Headers $subUserHeaders `
    -Body @{ iptvPackageId = $subPkgId } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "Subscribe to non-existent package" `
    -Method POST -Url "/api/subscribe" `
    -Headers $subUserHeaders `
    -Body @{ iptvPackageId = 999999 } `
    -ExpectedStatus @(400)

Test-Endpoint -Name "Subscribe without auth" `
    -Method POST -Url "/api/subscribe" `
    -Body @{ iptvPackageId = $subPkgId } `
    -ExpectedStatus @(401)

# Clean up: delete the test user's subscription, customer, user, and package
$subUserAllUsers = Test-Endpoint -Name "Cleanup: GET users for subscribe cleanup" `
    -Method GET -Url "/api/users" `
    -Headers (Auth-Headers)

$subUserObj = $subUserAllUsers | Where-Object { $_.username -eq $subUser }
if ($subUserObj) {
    Test-Endpoint -Name "Cleanup: delete subscribe test user" `
        -Method DELETE -Url "/api/users/$($subUserObj.userId)" `
        -Headers (Auth-Headers)
}

# Clean up subscription test data
Test-Endpoint -Name "Cleanup: delete subscription test customer" `
    -Method DELETE -Url "/api/customers/$subCustId" `
    -Headers (Auth-Headers)

Test-Endpoint -Name "Cleanup: delete subscription test package" `
    -Method DELETE -Url "/api/iptvpackages/$subPkgGuid" `
    -Headers (Auth-Headers)

# Clean up workflow settings before deleting the template (FK constraint)
Test-Endpoint -Name "Cleanup: clear Enquiry/Customer workflow setting" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/Customer" `
    -Headers (Auth-Headers) `
    -Body @{}

Test-Endpoint -Name "Cleanup: clear Enquiry/Admin workflow setting" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Enquiry/Admin" `
    -Headers (Auth-Headers) `
    -Body @{}

Test-Endpoint -Name "Cleanup: clear Subscription/Customer workflow setting" `
    -Method PUT -Url "/api/WorkflowEmailSettings/Subscription/Customer" `
    -Headers (Auth-Headers) `
    -Body @{}

Test-Endpoint -Name "Cleanup: delete workflow template" `
    -Method DELETE -Url "/api/emailtemplates/$($wfTmpl.emailTemplateGuid)" `
    -Headers (Auth-Headers)

# ===========================================================================
# RESULTS
# ===========================================================================

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
$resultColor = if ($script:failed -eq 0) { "Green" } else { "Red" }
Write-Host "  Results: $($script:passed) passed, $($script:failed) failed, $($script:total) total" -ForegroundColor $resultColor
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

exit $script:failed
