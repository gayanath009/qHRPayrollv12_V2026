using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using qHRM.Models;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace qHRM.Areas.Attendance.Reports
{
    public static class AttendancePresentAbsentPdfReport
    {
        public static byte[] GenerateReport(
            DateTime fromDate,
            DateTime toDate,
            int filterEmpSysId,
            int filterDeptId,
            qHRMasterContext hrmContext,
            string webRootPath = null,
            bool onlyAbsent = false)
        {
            if (toDate < fromDate)
            {
                var temp = fromDate;
                fromDate = toDate;
                toDate = temp;
            }

            // Build list of dates in the range
            var dateRange = new List<DateTime>();
            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                dateRange.Add(d);
            }

            int dayCount = dateRange.Count;

            // 1. Query Active In-House Employees (WrktypId == 1 && CurrStat == 0)
            var empQuery = hrmContext.HrEmpGenInfo
                .Where(g => g.WrktypId == 1 && g.CurrStat == 0);

            if (filterEmpSysId > 0)
            {
                empQuery = empQuery.Where(g => g.EmpSysId == filterEmpSysId);
            }

            var employeesRaw = empQuery
                .Select(g => new
                {
                    g.EmpSysId,
                    EmpId = g.EmpId != null ? g.EmpId.Trim() : "",
                    EmpName = (g.PstName ?? (g.FirstName + " " + g.LastName)).Trim(),
                    SrtOrd = g.SrtOrd ?? 1000
                })
                .ToList();

            var empSysIds = employeesRaw.Select(e => e.EmpSysId).ToList();

            // 2. Resolve Department & Designation for each employee
            var jobs = hrmContext.HrEmpJobAssign
                .Where(j => empSysIds.Contains(j.EmpSysId) && j.WkStat == 1)
                .Select(j => new
                {
                    j.EmpSysId,
                    DeptId = j.DeptId,
                    DesigId = j.DesigId
                })
                .ToList();

            var deptIds = jobs.Select(j => j.DeptId).Distinct().ToList();
            var desigIds = jobs.Select(j => j.DesigId).Distinct().ToList();

            var deptMap = hrmContext.OrgDepartment
                .Where(d => deptIds.Contains(d.DeptId))
                .ToDictionary(d => d.DeptId, d => d.NameEng?.Trim() ?? "");

            var desigMap = hrmContext.OrgDesignation
                .Where(ds => desigIds.Contains(ds.DesigId))
                .ToDictionary(ds => ds.DesigId, ds => ds.NameEng?.Trim() ?? "");

            var jobLookup = jobs
                .GroupBy(j => j.EmpSysId)
                .ToDictionary(g => g.Key, g => g.First());

            // Filter by department if selected
            var employeeList = new List<(int EmpSysId, string EmpId, string EmpName, string DeptDesig, int SrtOrd)>();

            foreach (var emp in employeesRaw)
            {
                int dId = 0;
                int dsId = 0;
                if (jobLookup.TryGetValue(emp.EmpSysId, out var job))
                {
                    dId = job.DeptId;
                    dsId = job.DesigId;
                }

                if (filterDeptId > 0 && dId != filterDeptId)
                {
                    continue;
                }

                deptMap.TryGetValue(dId, out string dName);
                desigMap.TryGetValue(dsId, out string dsName);

                string deptDesig;
                if (!string.IsNullOrWhiteSpace(dName) && !string.IsNullOrWhiteSpace(dsName))
                    deptDesig = $"{dName} / {dsName}";
                else if (!string.IsNullOrWhiteSpace(dName))
                    deptDesig = dName;
                else if (!string.IsNullOrWhiteSpace(dsName))
                    deptDesig = dsName;
                else
                    deptDesig = "-";

                employeeList.Add((emp.EmpSysId, emp.EmpId, emp.EmpName, deptDesig, emp.SrtOrd));
            }

            // Order employees according to the sort order field, then numerically by EmpId where possible
            employeeList = employeeList
                .OrderBy(e => e.SrtOrd)
                .ThenBy(e => int.TryParse(e.EmpId, out int idNum) ? idNum : 999999)
                .ThenBy(e => e.EmpId)
                .ToList();

            var finalEmpSysIds = employeeList.Select(e => e.EmpSysId).ToList();

            // 3. Query Shift Attendance for the date range
            var shifts = hrmContext.HrEmpAtndcSft
                .Where(s => finalEmpSysIds.Contains(s.EmpSysId) 
                            && s.AtndcDate >= fromDate.Date 
                            && s.AtndcDate <= toDate.Date 
                            && s.RecStat == 1)
                .Select(s => new
                {
                    s.EmpSysId,
                    s.AtndcDate,
                    s.AtndcStatId,
                    s.InTime,
                    s.OutTime,
                    s.InSource,
                    s.WrkMnt
                })
                .ToList();

            var shiftLookup = shifts
                .GroupBy(s => $"{s.EmpSysId}_{s.AtndcDate:yyyyMMdd}")
                .ToDictionary(g => g.Key, g => g.First());

            // 4. Query Leaves for the date range
            var leaves = (from r in hrmContext.HrLvRegister
                          where finalEmpSysIds.Contains(r.EmpSysId.Value)
                                && r.RecStat == 1
                                && r.EndDate >= fromDate.Date
                                && r.StDate <= toDate.Date
                                && (r.WfStat == null || (!r.WfStat.Contains("Reject") && !r.WfStat.Contains("Cancel")))
                          join t in hrmContext.HrLvType on r.LvTypId equals t.LvTypId
                          select new
                          {
                              EmpSysId = r.EmpSysId.Value,
                              r.StDate,
                              r.EndDate,
                              t.LvTypCode,
                              t.NameEng
                          })
                          .ToList();

            // 4.1 Query active rosters to check "Consider for Attendance" (CndrAtndc)
            var empRosters = hrmContext.HrEmpWkRoster
                .Where(r => finalEmpSysIds.Contains(r.EmpSysId) && r.RecStat == 1)
                .Select(r => new
                {
                    r.EmpSysId,
                    r.StDate,
                    r.EndDate,
                    r.CndrAtndc
                })
                .ToList();

            bool IsNotConsideredForAttendance(int empSysId, DateTime d)
            {
                return empRosters.Any(r => r.EmpSysId == empSysId
                    && r.CndrAtndc == false
                    && d >= r.StDate.Date
                    && (!r.EndDate.HasValue || d <= r.EndDate.Value.Date));
            }

            // Filter for employees absent on ANY day in this cycle/period if requested
            if (onlyAbsent)
            {
                var absentEmpSysIds = new HashSet<int>();
                foreach (var emp in employeeList)
                {
                    bool hasAbsentDay = false;
                    for (int i = 0; i < dayCount; i++)
                    {
                        var d = dateRange[i];
                        bool isWeekend = (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday);
                        if (isWeekend || d > DateTime.Today) continue;

                        string shiftKey = $"{emp.EmpSysId}_{d:yyyyMMdd}";
                        shiftLookup.TryGetValue(shiftKey, out var sft);

                        var empLeave = leaves.FirstOrDefault(l => l.EmpSysId == emp.EmpSysId && d >= l.StDate.Date && d <= l.EndDate.Date);

                        bool isPresentOrExcused = false;
                        if (IsNotConsideredForAttendance(emp.EmpSysId, d))
                        {
                            isPresentOrExcused = true;
                        }
                        else if (empLeave != null)
                        {
                            isPresentOrExcused = true;
                        }
                        else if (sft != null && (sft.AtndcStatId == 2 || sft.AtndcStatId == 3))
                        {
                            isPresentOrExcused = true;
                        }
                        else if (sft != null && (sft.InSource == "REGULARIZED" || sft.WrkMnt > 0 || sft.InTime.HasValue || new[] { 1, 11, 12, 112, 13, 14, 51, 61 }.Contains(sft.AtndcStatId)))
                        {
                            isPresentOrExcused = true;
                        }

                        if (!isPresentOrExcused)
                        {
                            hasAbsentDay = true;
                            break;
                        }
                    }

                    if (hasAbsentDay)
                    {
                        absentEmpSysIds.Add(emp.EmpSysId);
                    }
                }

                employeeList = employeeList.Where(e => absentEmpSysIds.Contains(e.EmpSysId)).ToList();
            }

            // 5. Generate PDF Document
            using (PdfDocument document = new PdfDocument())
            {
                document.PageSettings.Orientation = PdfPageOrientation.Landscape;

                // For ranges longer than 32 days, use A3 Landscape to guarantee readability
                if (dayCount > 32)
                {
                    document.PageSettings.Size = PdfPageSize.A3;
                }
                else
                {
                    document.PageSettings.Size = PdfPageSize.A4;
                }

                float leftMargin = 15f;
                float rightMargin = 15f;
                float topMargin = 50f;
                float bottomMargin = 15f;
                document.PageSettings.SetMargins(leftMargin, topMargin, rightMargin, bottomMargin);

                float pageWidth = document.PageSettings.Size.Width;
                float printableWidth = pageWidth - (leftMargin + rightMargin);

                // Setup Top Page Header Template (Repeats cleanly on every page)
                PdfPageTemplateElement topTemplate = new PdfPageTemplateElement(printableWidth, 46f);

                PdfFont fontTitle = new PdfStandardFont(PdfFontFamily.Helvetica, 11f, PdfFontStyle.Bold);
                PdfFont fontSubtitle = new PdfStandardFont(PdfFontFamily.Helvetica, 7.5f, PdfFontStyle.Regular);
                PdfFont fontLegend = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f, PdfFontStyle.Regular);
                PdfStringFormat fmtHeaderCenter = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtLegendRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                float curY = 0f;

                // 1. Centered Heading in Title Case
                string headingText = onlyAbsent
                    ? "Day Wise Employee Present/Absent Status (Absent Employees Only)"
                    : "Day Wise Employee Present/Absent Status";
                topTemplate.Graphics.DrawString(headingText, fontTitle, PdfBrushes.Black, new RectangleF(0, curY, printableWidth, 14f), fmtHeaderCenter);
                curY += 14f;

                // 2. Centered Date Range
                string subtitleText = onlyAbsent
                    ? $"Start Date : {fromDate:dd-MMM-yyyy}        End Date : {toDate:dd-MMM-yyyy}        [Filter: Absent Any Day in Cycle]"
                    : $"Start Date : {fromDate:dd-MMM-yyyy}        End Date : {toDate:dd-MMM-yyyy}";
                topTemplate.Graphics.DrawString(subtitleText, fontSubtitle, PdfBrushes.Black, new RectangleF(0, curY, printableWidth, 10f), fmtHeaderCenter);
                curY += 10f;

                // 3. Right-Aligned Legends
                topTemplate.Graphics.DrawString("P - Present, A - Absent, W - Weekend, H - Bank Holiday", fontLegend, PdfBrushes.Black, new RectangleF(0, curY, printableWidth, 8.5f), fmtLegendRight);
                curY += 8.5f;

                topTemplate.Graphics.DrawString("AL - Annual Leave - Based Joining Month, CL - Casual Leave, ML - Maternity Leave, HL - Hajj Leave, EL - Emergency Leave, SL - Sick Leave", fontLegend, PdfBrushes.Black, new RectangleF(0, curY, printableWidth, 8.5f), fmtLegendRight);
                curY += 9f;

                document.Template.Top = topTemplate;

                PdfPage page = document.Pages.Add();

                // Setup PdfGrid
                PdfGrid grid = new PdfGrid
                {
                    RepeatHeader = true
                };

                // Column definitions:
                // Col 0: Employee Id (~42pt)
                // Col 1: Employee Name (~92pt)
                // Col 2: Department, Designation (~110pt)
                // Cols 3 to (dayCount + 2): Each Day
                float idColWidth = 42f;
                float nameColWidth = 92f;
                float deptDesigColWidth = 110f;
                float fixedColsWidth = idColWidth + nameColWidth + deptDesigColWidth;

                float dayColWidth = Math.Max(13f, (printableWidth - fixedColsWidth) / Math.Max(1, dayCount));

                // Add grid columns
                grid.Columns.Add(); grid.Columns[0].Width = idColWidth;
                grid.Columns.Add(); grid.Columns[1].Width = nameColWidth;
                grid.Columns.Add(); grid.Columns[2].Width = deptDesigColWidth;

                for (int i = 0; i < dayCount; i++)
                {
                    PdfGridColumn col = grid.Columns.Add();
                    col.Width = dayColWidth;
                }

                // Fonts and Brushes
                PdfFont fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontBody = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Regular);
                PdfFont fontBodyBold = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontDayCell = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);

                PdfPen penBorder = new PdfPen(new PdfColor(203, 213, 225), 0.5f); // #cbd5e1 light gray border
                PdfPaddings cellPadding = new PdfPaddings(1f, 1f, 1f, 1f);

                PdfBrush brushGreen = new PdfSolidBrush(new PdfColor(22, 163, 74));   // Present: #16a34a Green
                PdfBrush brushRed = new PdfSolidBrush(new PdfColor(220, 38, 38));     // Absent: #dc2626 Red
                PdfBrush brushGray = new PdfSolidBrush(new PdfColor(107, 114, 128));  // Weekend: #6b7280 Gray
                PdfBrush brushBlue = new PdfSolidBrush(new PdfColor(29, 78, 216));    // All Leaves: #1d4ed8 Blue
                PdfBrush brushMuted = new PdfSolidBrush(new PdfColor(148, 163, 184)); // Future/Unworked: #94a3b8
                PdfBrush brushPurple = new PdfSolidBrush(new PdfColor(147, 51, 234)); // Bank Holiday: #9333ea

                PdfStringFormat fmtLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtCenter = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };

                // Header Row
                grid.Headers.Add(1);
                PdfGridRow headerRow = grid.Headers[0];

                headerRow.Cells[0].Value = "Employee Id"; headerRow.Cells[0].StringFormat = fmtLeft;
                headerRow.Cells[1].Value = "Employee Name"; headerRow.Cells[1].StringFormat = fmtLeft;
                headerRow.Cells[2].Value = "Department, Designation"; headerRow.Cells[2].StringFormat = fmtLeft;

                for (int i = 0; i < dayCount; i++)
                {
                    headerRow.Cells[3 + i].Value = $"{dateRange[i]:dd - MMM}";
                    headerRow.Cells[3 + i].StringFormat = fmtCenter;
                }

                for (int c = 0; c < 3 + dayCount; c++)
                {
                    headerRow.Cells[c].Style.Font = fontHeader;
                    headerRow.Cells[c].Style.CellPadding = cellPadding;
                    headerRow.Cells[c].Style.Borders.All = penBorder;
                }

                // Data Rows
                if (employeeList.Count == 0)
                {
                    PdfGridRow emptyRow = grid.Rows.Add();
                    emptyRow.Cells[0].Value = "No employees found matching filter (No absent records within this period).";
                    emptyRow.Cells[0].ColumnSpan = 3 + dayCount;
                    emptyRow.Cells[0].StringFormat = fmtCenter;
                    emptyRow.Cells[0].Style.Font = fontBodyBold;
                    emptyRow.Cells[0].Style.CellPadding = cellPadding;
                    emptyRow.Cells[0].Style.Borders.All = penBorder;
                }

                foreach (var emp in employeeList)
                {
                    PdfGridRow row = grid.Rows.Add();

                    row.Cells[0].Value = emp.EmpId;
                    row.Cells[0].StringFormat = fmtLeft;
                    row.Cells[0].Style.Font = fontBodyBold;

                    row.Cells[1].Value = emp.EmpName;
                    row.Cells[1].StringFormat = fmtLeft;
                    row.Cells[1].Style.Font = fontBody;

                    row.Cells[2].Value = emp.DeptDesig;
                    row.Cells[2].StringFormat = fmtLeft;
                    row.Cells[2].Style.Font = fontBody;

                    for (int c = 0; c < 3; c++)
                    {
                        row.Cells[c].Style.CellPadding = cellPadding;
                        row.Cells[c].Style.Borders.All = penBorder;
                    }

                    // Populate Daily Attendance Status
                    for (int i = 0; i < dayCount; i++)
                    {
                        var d = dateRange[i];
                        bool isWeekend = (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday);
                        string shiftKey = $"{emp.EmpSysId}_{d:yyyyMMdd}";
                        shiftLookup.TryGetValue(shiftKey, out var sft);

                        // Check registered leave
                        var empLeave = leaves.FirstOrDefault(l => l.EmpSysId == emp.EmpSysId && d >= l.StDate.Date && d <= l.EndDate.Date);

                        string dayStatus;

                        if (isWeekend)
                        {
                            // Weekend check: did employee work on weekend?
                            if (sft != null && (sft.WrkMnt > 0 || sft.InTime.HasValue || sft.OutTime.HasValue || sft.AtndcStatId == 41))
                            {
                                dayStatus = "W/P";
                            }
                            else
                            {
                                dayStatus = "W";
                            }
                        }
                        else
                        {
                            // Working day (Sunday - Thursday)
                            if (empLeave != null)
                            {
                                // 4. AL, CL, ML, HL, EL, SL in blue
                                string code = (empLeave.LvTypCode ?? "").ToUpperInvariant().Trim();
                                if (code == "LV_ANL" || code.Contains("ANL") || code.Contains("ANNUAL"))
                                    dayStatus = "AL";
                                else if (code == "LV_CSL" || code.Contains("CSL") || code.Contains("CASUAL"))
                                    dayStatus = "CL";
                                else if (code == "LV_MTN" || code.Contains("MTN") || code.Contains("MATERN"))
                                    dayStatus = "ML";
                                else if (code == "LV_HAJ" || code.Contains("HAJ"))
                                    dayStatus = "HL";
                                else if (code == "LV_EMG_PD" || code == "LV_EMG_UPD" || code.Contains("EMG") || code.Contains("EMERG"))
                                    dayStatus = "EL";
                                else if (code == "LV_SICK" || code.Contains("SICK"))
                                    dayStatus = "SL";
                                else
                                    dayStatus = "AL";
                            }
                            else if (sft != null && sft.AtndcStatId == 2)
                            {
                                dayStatus = "AL";
                            }
                            else if (sft != null && sft.AtndcStatId == 3)
                            {
                                dayStatus = "H";
                            }
                            else if (IsNotConsideredForAttendance(emp.EmpSysId, d))
                            {
                                dayStatus = "P";
                            }
                            else if (sft != null && (sft.InSource == "REGULARIZED" || sft.WrkMnt > 0 || sft.InTime.HasValue || new[] { 1, 11, 12, 112, 13, 14, 51, 61 }.Contains(sft.AtndcStatId)))
                            {
                                dayStatus = "P";
                            }
                            else if (d > DateTime.Today)
                            {
                                dayStatus = "-";
                            }
                            else
                            {
                                dayStatus = "A";
                            }
                        }

                        int cellCol = 3 + i;
                        row.Cells[cellCol].Value = dayStatus;
                        row.Cells[cellCol].StringFormat = fmtCenter;
                        row.Cells[cellCol].Style.Font = fontDayCell;
                        row.Cells[cellCol].Style.CellPadding = cellPadding;
                        row.Cells[cellCol].Style.Borders.All = penBorder;
                    }
                }

                // Custom cell rendering to guarantee exact colors for each status
                grid.BeginCellLayout += (sender, args) =>
                {
                    if (args.IsHeaderRow) return;

                    if (args.CellIndex >= 3)
                    {
                        // 1. Draw cell border
                        args.Graphics.DrawRectangle(penBorder, args.Bounds);

                        // 2. Select exact brush based on status code
                        string val = args.Value ?? "";
                        if (!string.IsNullOrEmpty(val))
                        {
                            PdfBrush brush;

                            if (val == "P" || val.Contains("/P"))
                            {
                                // 1. P - Present -> Green
                                brush = brushGreen;
                            }
                            else if (val == "A")
                            {
                                // 2. A - Absent -> Red
                                brush = brushRed;
                            }
                            else if (val == "W")
                            {
                                // 3. W - Weekend -> Gray
                                brush = brushGray;
                            }
                            else if (val == "AL" || val == "CL" || val == "ML" || val == "HL" || val == "EL" || val == "SL")
                            {
                                // 4. All leave types -> Blue
                                brush = brushBlue;
                            }
                            else if (val == "H")
                            {
                                brush = brushPurple;
                            }
                            else
                            {
                                brush = brushMuted;
                            }

                            args.Graphics.DrawString(val, fontDayCell, brush, args.Bounds, fmtCenter);
                        }

                        args.Skip = true;
                    }
                };

                // Draw the grid on the document
                grid.Draw(page, new PointF(0, 0));

                // Save to PDF byte array
                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    document.Save(ms);
                    bytes = ms.ToArray();
                }

                return bytes;
            }
        }
    }
}

