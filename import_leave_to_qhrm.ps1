param(
    [string]$TargetServer = "SOFT-NB-095",
    [string]$Database = "qHRMasterV27",
    [string]$User = "sa",
    [string]$Password = "sa#12345678"
)

Write-Host "=========================================================="
Write-Host "Starting Leave Import to $TargetServer / $Database"
Write-Host "=========================================================="

$excelPath = "qHRM_2025\qHRM\wwwroot\leave\Leave View.xlsx"
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path $excelPath)) {
    Write-Error "Excel file not found at $excelPath"
    exit 1
}

$zip = [System.IO.Compression.ZipFile]::OpenRead($excelPath)
$sheetEntry = $zip.GetEntry("xl/worksheets/sheet1.xml")
$sheetStream = $sheetEntry.Open()
$xml = [xml](New-Object System.IO.StreamReader($sheetStream)).ReadToEnd()
$sheetStream.Dispose()
$zip.Dispose()

$rows = $xml.worksheet.sheetData.row
function Get-CellValue($c) {
    if (-not $c) { return "" }
    if ($c.t -eq "inlineStr" -or $c.is) { return $c.is.InnerText }
    return $c.v
}

# 1. Parse Excel Leave Records (From >= 2026-08-01, LeaveType != 'Absent')
$records = @()
for ($i = 1; $i -lt $rows.Count; $i++) {
    $r = $rows[$i]
    $cells = @{}
    foreach ($c in $r.c) {
        $colLetter = $c.r -replace '\d+',''
        $cells[$colLetter] = Get-CellValue $c
    }

    $leaveType = $cells["C"].Trim()
    if ($leaveType -ne "Absent") {
        $fromOa = [double]$cells["D"]
        $toOa = [double]$cells["E"]
        if ($fromOa -gt 0) {
            $fromDt = [DateTime]::FromOADate($fromOa)
            $toDt = [DateTime]::FromOADate($toOa)
            if ($fromDt -ge [DateTime]"2026-08-01") {
                $addedTime = [DateTime]::Now
                $addedOa = [double]$cells["I"]
                if ($addedOa -gt 0) {
                    try { $addedTime = [DateTime]::FromOADate($addedOa) } catch {}
                }

                $records += [PSCustomObject]@{
                    ZohoId = $cells["A"].Trim()
                    EmpStr = $cells["B"].Trim()
                    LeaveType = $leaveType
                    From = $fromDt.Date
                    To = $toDt.Date
                    Days = [int][Math]::Round([double]$cells["F"])
                    Reason = $cells["G"]
                    Status = $cells["Q"].Trim()
                    AddedTime = $addedTime
                    PaidType = $cells["T"]
                }
            }
        }
    }
}

Write-Host "Found $($records.Count) leave applications since 2026-08-01 in Excel."

# 2. Connect to Database
$connStr = "Server=$TargetServer;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

# 3. Fetch Employee Lookup
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Emp_Sys_Id, Emp_Id, Wrktyp_Id FROM HR_EmpGenInfo"
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dtEmp = New-Object System.Data.DataTable
$adapter.Fill($dtEmp) | Out-Null

$empMap = @{}
foreach ($row in $dtEmp.Rows) {
    $empMap[$row["Emp_Id"].ToString().Trim()] = $row["Emp_Sys_Id"]
}

# 4. Leave Type ID Mapping
$lvTypeMap = @{
    "Annual Leave - Based Joining Month" = 1
    "Sick Leave"                         = 3
    "Casual Leave"                       = 4
    "Emergency Leave"                    = 14
    "Maternity Leave"                    = 12
}

# 5. Incident Tracker (per Emp_Sys_Id)
$incidentTracker = @{}
$cmd.CommandText = "SELECT Emp_Sys_Id, MAX(Incident) AS MaxInc FROM HR_LvRegister GROUP BY Emp_Sys_Id"
$dtInc = New-Object System.Data.DataTable
$adapter.Fill($dtInc) | Out-Null
foreach ($row in $dtInc.Rows) {
    $incidentTracker[[int]$row["Emp_Sys_Id"]] = [int]$row["MaxInc"]
}

# Sort records chronologically so incidents are ordered
$records = $records | Sort-Object From, AddedTime

# 6. Process Records in a Transaction
$tran = $conn.BeginTransaction("LeaveImport")
$cmd.Transaction = $tran

$inserted = 0
$updated = 0
$skipped = 0
$approvedCount = 0

try {
    foreach ($rec in $records) {
        # Match Employee
        $tokens = $rec.EmpStr -split '\s+'
        $candidateId = $tokens[-1]
        if (-not $empMap.ContainsKey($candidateId)) {
            foreach ($t in $tokens) {
                if ($empMap.ContainsKey($t)) {
                    $candidateId = $t
                    break
                }
            }
        }

        if (-not $empMap.ContainsKey($candidateId)) {
            Write-Warning "Could not match employee for $($rec.EmpStr) (ZohoId: $($rec.ZohoId))"
            $skipped++
            continue
        }

        $empSysId = [int]$empMap[$candidateId]
        $lvTypId = [int]$lvTypeMap[$rec.LeaveType]
        if ($lvTypId -eq 0) {
            Write-Warning "Unknown leave type: $($rec.LeaveType)"
            $skipped++
            continue
        }

        $lvYear = $rec.From.Year

        # Approval Status & Rec_Stat
        $isApproved = ($rec.Status -eq "Approved")
        $recStat = if ($isApproved) { 1 } else { 0 }
        $wfStat = $rec.Status
        if ($isApproved) { $approvedCount++ }

        # Note with ZohoId
        $zohoTag = " [ZohoId:$($rec.ZohoId)]"
        $reasonStr = if ($rec.Reason) { $rec.Reason.Trim() } else { "$($rec.LeaveType) (Zoho Import)" }
        $maxReasonLen = 250 - $zohoTag.Length
        if ($reasonStr.Length -gt $maxReasonLen) {
            $reasonStr = $reasonStr.Substring(0, $maxReasonLen)
        }
        $note = $reasonStr + $zohoTag

        # Check if already exists by ZohoId
        $cmd.CommandText = "SELECT Lv_Id, Incident FROM HR_LvRegister WHERE CHARINDEX(@ZohoId, ISNULL(Note, '')) > 0"
        $cmd.Parameters.Clear()
        $cmd.Parameters.AddWithValue("@ZohoId", $rec.ZohoId) | Out-Null
        $reader = $cmd.ExecuteReader()
        $existingLvId = $null
        $existingInc = $null
        if ($reader.Read()) {
            $existingLvId = [int]$reader["Lv_Id"]
            $existingInc = [int]$reader["Incident"]
        }
        $reader.Close()

        if ($existingLvId) {
            # Update
            $cmd.CommandText = @"
UPDATE HR_LvRegister 
SET St_Date = @St_Date,
    End_Date = @End_Date,
    Days = @Days,
    Rec_Stat = @Rec_Stat,
    WfStat = @WfStat,
    Note = @Note
WHERE Lv_Id = @Lv_Id
"@
            $cmd.Parameters.Clear()
            $cmd.Parameters.AddWithValue("@St_Date", $rec.From) | Out-Null
            $cmd.Parameters.AddWithValue("@End_Date", $rec.To) | Out-Null
            $cmd.Parameters.AddWithValue("@Days", $rec.Days) | Out-Null
            $cmd.Parameters.AddWithValue("@Rec_Stat", $recStat) | Out-Null
            $cmd.Parameters.AddWithValue("@WfStat", $wfStat) | Out-Null
            $cmd.Parameters.AddWithValue("@Note", $note) | Out-Null
            $cmd.Parameters.AddWithValue("@Lv_Id", $existingLvId) | Out-Null
            $cmd.ExecuteNonQuery() | Out-Null
            $updated++
        } else {
            # Insert
            $currInc = if ($incidentTracker.ContainsKey($empSysId)) { $incidentTracker[$empSysId] + 1 } else { 1 }
            $incidentTracker[$empSysId] = $currInc

            $cmd.CommandText = @"
INSERT INTO HR_LvRegister (
    Emp_Sys_Id, LvTyp_Id, LvYear, Incident, St_Date, End_Date, Days, Curr_Avbl_Days,
    LvSdl_Id, Doc_Id, Rjn_Date, Rjn_Stat, Note, Rec_Stat, Crt_Id, Crt_DateTime, WfStat, WfInstId
) VALUES (
    @Emp_Sys_Id, @LvTyp_Id, @LvYear, @Incident, @St_Date, @End_Date, @Days, 0,
    NULL, NULL, NULL, -1, @Note, @Rec_Stat, 1, @Crt_DateTime, @WfStat, NULL
)
"@
            $cmd.Parameters.Clear()
            $cmd.Parameters.AddWithValue("@Emp_Sys_Id", $empSysId) | Out-Null
            $cmd.Parameters.AddWithValue("@LvTyp_Id", $lvTypId) | Out-Null
            $cmd.Parameters.AddWithValue("@LvYear", $lvYear) | Out-Null
            $cmd.Parameters.AddWithValue("@Incident", $currInc) | Out-Null
            $cmd.Parameters.AddWithValue("@St_Date", $rec.From) | Out-Null
            $cmd.Parameters.AddWithValue("@End_Date", $rec.To) | Out-Null
            $cmd.Parameters.AddWithValue("@Days", $rec.Days) | Out-Null
            $cmd.Parameters.AddWithValue("@Note", $note) | Out-Null
            $cmd.Parameters.AddWithValue("@Rec_Stat", $recStat) | Out-Null
            $cmd.Parameters.AddWithValue("@Crt_DateTime", $rec.AddedTime) | Out-Null
            $cmd.Parameters.AddWithValue("@WfStat", $wfStat) | Out-Null
            $cmd.ExecuteNonQuery() | Out-Null
            $inserted++
        }
    }

    # 7. Synchronize Approved Leaves to Shift Attendance (HR_EmpAtndcSft)
    Write-Host "Synchronizing shift attendance (HR_EmpAtndcSft) for approved leaves..."
    $cmd.CommandText = @"
UPDATE sft
SET sft.Atndc_Stat_Id = 2, -- Leave
    sft.Slry_Dct_Fct = 0
FROM HR_EmpAtndcSft sft
INNER JOIN HR_LvRegister lr 
    ON sft.Emp_Sys_Id = lr.Emp_Sys_Id
    AND sft.Atndc_Date BETWEEN lr.St_Date AND lr.End_Date
WHERE lr.Rec_Stat = 1
  AND lr.St_Date >= '2026-08-01'
  AND (sft.Atndc_Stat_Id = 0 OR (sft.In_Time IS NULL AND sft.Out_Time IS NULL AND sft.Atndc_Stat_Id != 4))
"@
    $cmd.Parameters.Clear()
    $sftUpdated = $cmd.ExecuteNonQuery()
    Write-Host "Updated $sftUpdated shift records in HR_EmpAtndcSft to Leave (Atndc_Stat_Id = 2)."

    $tran.Commit()
    Write-Host "`nImport successfully committed!"
    Write-Host "Inserted: $inserted records"
    Write-Host "Updated:  $updated records"
    Write-Host "Skipped:  $skipped records"
    Write-Host "Total Approved: $approvedCount records (affect payroll & attendance)"
    Write-Host "Total Non-Approved: $($records.Count - $approvedCount) records (will NOT affect payroll)"
} catch {
    $tran.Rollback()
    Write-Error "Error during import: $_"
} finally {
    $conn.Close()
}
