using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using qHRM.Areas.Payroll.Models;
using qHRM.SharedSysFuncs;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace qHRM.Areas.Payroll.Reports
{
    public static class DepartmentSalarySheetPdfReport
    {
        public static byte[] GenerateReport(
            string companyName,
            int year,
            int month,
            IEnumerable<PrSlryShtVw> displayEmployees,
            decimal internalStaffNet,
            decimal outsourcedStaffNet,
            int sctn, // 0 = In-House, 1 = Outsourced, 2 = All
            string webRootPath)
        {
            using (PdfDocument document = new PdfDocument())
            {
                // A4 Landscape: 841.89 pt x 595.27 pt
                document.PageSettings.Size = PdfPageSize.A4;
                document.PageSettings.Orientation = PdfPageOrientation.Landscape;

                float leftMargin = 18f;
                float rightMargin = 18f;
                float topMargin = 15f;
                float bottomMargin = 15f;
                document.PageSettings.SetMargins(leftMargin, topMargin, rightMargin, bottomMargin);

                float printableWidth = 841.89f - (leftMargin + rightMargin); // ~805.89 pt

                // Top Header Template (Repeats cleanly at the top of every page)
                float topHeaderHeight = 44f;
                PdfPageTemplateElement topTemplate = new PdfPageTemplateElement(printableWidth, topHeaderHeight);

                // 1. QCS Logo on Top Left
                string logoPath = Path.Combine(webRootPath, "images", "qcs.png");
                FileStream logoStream = null;
                PdfBitmap logoBitmap = null;
                if (File.Exists(logoPath))
                {
                    try
                    {
                        logoStream = new FileStream(logoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        logoBitmap = new PdfBitmap(logoStream);
                    }
                    catch { }
                }

                if (logoBitmap != null)
                {
                    float lHeight = 30f;
                    float lWidth = lHeight * (203f / 59f);
                    topTemplate.Graphics.DrawImage(logoBitmap, 0f, 4f, lWidth, lHeight);
                }

                // 2. Header Titles (Centered)
                PdfFont fontCompany = new PdfStandardFont(PdfFontFamily.Helvetica, 13f, PdfFontStyle.Bold);
                PdfFont fontSubtitle = new PdfStandardFont(PdfFontFamily.Helvetica, 10f, PdfFontStyle.Bold);
                PdfStringFormat formatCenter = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Center,
                    LineAlignment = PdfVerticalAlignment.Middle
                };

                string compTitle = !string.IsNullOrWhiteSpace(companyName) ? companyName : "Qatar Computer Services WLL";
                string monthName = (month >= 1 && month <= 12)
                    ? new DateTime(year > 0 ? year : 2026, month, 1).ToString("MMMM")
                    : "September";

                string categoryPrefix;
                if (sctn == 0)
                    categoryPrefix = "In-House Staff ";
                else if (sctn == 1)
                    categoryPrefix = "Outsourced Staff ";
                else
                    categoryPrefix = "Staff ";

                string reportSubtitle = $"{categoryPrefix} - Department-wise Salary Sheet for the Month of {monthName} - {year}";

                topTemplate.Graphics.DrawString(compTitle, fontCompany, PdfBrushes.Black, new RectangleF(0, 4f, printableWidth, 18f), formatCenter);
                topTemplate.Graphics.DrawString(reportSubtitle, fontSubtitle, PdfBrushes.Black, new RectangleF(0, 23f, printableWidth, 16f), formatCenter);
                document.Template.Top = topTemplate;

                PdfPage page = document.Pages.Add();

                // Setup PdfGrid
                PdfGrid grid = new PdfGrid
                {
                    RepeatHeader = true,
                    AllowRowBreakAcrossPages = false
                };

                // 23 Columns with tuned widths summing to 793 pt:
                float[] widths = new float[]
                {
                    18f,  // 0: S. No
                    35f,  // 1: Code
                    105f, // 2: Name Designation
                    20f,  // 3: Absent
                    36f,  // 4: Basic Salary
                    30f,  // 5: Housing
                    25f,  // 6: Trans.
                    23f,  // 7: Posit.
                    23f,  // 8: Mobile
                    25f,  // 9: Project
                    23f,  // 10: Other (Allowance)
                    38f,  // 11: Total Fixed Salary
                    28f,  // 12: Incent
                    16f,  // 13: OT
                    28f,  // 14: Other Payment
                    36f,  // 15: Gross Salary
                    30f,  // 16: Sl Adv / Loan
                    26f,  // 17: No Pay
                    26f,  // 18: Other Deduct
                    30f,  // 19: Total Deductions
                    40f,  // 20: Bank Transfer
                    32f,  // 21: Cash Payment
                    95f   // 22: Remarks
                };

                float tableTotalWidth = widths.Sum(); // 793 pt

                for (int i = 0; i < widths.Length; i++)
                {
                    PdfGridColumn col = grid.Columns.Add();
                    col.Width = widths[i];
                }

                // Fonts and styles
                PdfFont fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontDeptHeader = new PdfStandardFont(PdfFontFamily.Helvetica, 6.0f, PdfFontStyle.Bold);
                PdfFont fontDeptTotal = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontGrandTotal = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontBody = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Regular);
                PdfFont fontNameBold = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontDesigRegular = new PdfStandardFont(PdfFontFamily.Helvetica, 5.0f, PdfFontStyle.Regular);

                PdfPen penCell = new PdfPen(new PdfColor(0, 0, 0), 0.5f);
                PdfPaddings cellPadding = new PdfPaddings(1f, 1f, 1f, 1f);

                PdfBrush brushDeptHeader = new PdfSolidBrush(new PdfColor(230, 238, 248)); // Soft blue-tinted banner
                PdfBrush brushDeptTotal = new PdfSolidBrush(new PdfColor(245, 247, 250));  // Light grey subtotal
                PdfBrush brushGrandTotal = new PdfSolidBrush(new PdfColor(225, 232, 244)); // Elegant summary row

                PdfStringFormat fmtLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtCenterCell = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                // Custom cell rendering to Bold Employee Name
                grid.BeginCellLayout += (sender, args) =>
                {
                    if (!args.IsHeaderRow && args.CellIndex == 2 && !string.IsNullOrWhiteSpace(args.Value))
                    {
                        if (args.Value.StartsWith("Department", StringComparison.OrdinalIgnoreCase) ||
                            args.Value.StartsWith("Grand Total", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        args.Graphics.DrawRectangle(penCell, args.Bounds);

                        string[] parts = args.Value.Split(new[] { '\n' }, 2);
                        float padX = 1.5f;
                        float padY = 1.5f;
                        float x = args.Bounds.X + padX;
                        float y = args.Bounds.Y + padY;

                        args.Graphics.DrawString(parts[0], fontNameBold, PdfBrushes.Black, new PointF(x, y));

                        if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                        {
                            float lineOffset = fontNameBold.Height + 0.5f;
                            args.Graphics.DrawString(parts[1], fontDesigRegular, PdfBrushes.Black, new PointF(x, y + lineOffset));
                        }

                        args.Skip = true;
                    }
                };

                // 2 Header Rows
                grid.Headers.Add(2);
                PdfGridRow h1 = grid.Headers[0];
                PdfGridRow h2 = grid.Headers[1];

                h1.Cells[0].Value = "S. No"; h1.Cells[0].RowSpan = 2;
                h1.Cells[1].Value = "Code"; h1.Cells[1].RowSpan = 2;
                h1.Cells[2].Value = "Name\nDesignation"; h1.Cells[2].RowSpan = 2;
                h1.Cells[3].Value = "Absent"; h1.Cells[3].RowSpan = 2;
                h1.Cells[4].Value = "Basic\nSalary"; h1.Cells[4].RowSpan = 2;
                h1.Cells[5].Value = "Housing"; h1.Cells[5].RowSpan = 2;
                h1.Cells[6].Value = "Trans."; h1.Cells[6].RowSpan = 2;

                // Allowance group (cols 7 - 10)
                h1.Cells[7].Value = "Allowance"; h1.Cells[7].ColumnSpan = 4;
                h2.Cells[7].Value = "Posit.";
                h2.Cells[8].Value = "Mobile";
                h2.Cells[9].Value = "Project";
                h2.Cells[10].Value = "Other";

                h1.Cells[11].Value = "Total\nFixed\nSalary"; h1.Cells[11].RowSpan = 2;
                h1.Cells[12].Value = "Incent"; h1.Cells[12].RowSpan = 2;
                h1.Cells[13].Value = "OT"; h1.Cells[13].RowSpan = 2;
                h1.Cells[14].Value = "Other\nPayment"; h1.Cells[14].RowSpan = 2;
                h1.Cells[15].Value = "Gross\nSalary"; h1.Cells[15].RowSpan = 2;

                // Deductions group (cols 16 - 19)
                h1.Cells[16].Value = "Deductions"; h1.Cells[16].ColumnSpan = 4;
                h2.Cells[16].Value = "Sl Adv /\nLoan";
                h2.Cells[17].Value = "No Pay";
                h2.Cells[18].Value = "Other\nDeduct";
                h2.Cells[19].Value = "Total";

                // Net Salary group (cols 20 - 21)
                h1.Cells[20].Value = "Net Salary"; h1.Cells[20].ColumnSpan = 2;
                h2.Cells[20].Value = "Bank\nTransfer";
                h2.Cells[21].Value = "Cash\nPayment";

                h1.Cells[22].Value = "Remarks"; h1.Cells[22].RowSpan = 2;

                // Format Header Cells
                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 23; c++)
                    {
                        PdfGridCell cell = grid.Headers[r].Cells[c];
                        cell.Style.Font = fontHeader;
                        cell.Style.CellPadding = cellPadding;
                        cell.Style.Borders.All = penCell;
                        if (c == 2 || c == 22)
                            cell.StringFormat = fmtLeft;
                        else
                            cell.StringFormat = fmtCenterCell;
                    }
                }

                // Grand Totals Accumulators
                decimal totBasic = 0;
                decimal totHousing = 0;
                decimal totTrans = 0;
                decimal totPosit = 0;
                decimal totMobile = 0;
                decimal totProj = 0;
                decimal totOtherAlwnc = 0;
                decimal totFixedSalary = 0;
                decimal totIncent = 0;
                decimal totOt = 0;
                decimal totOtherPmt = 0;
                decimal totGross = 0;
                decimal totSlAdvLoan = 0;
                decimal totNoPay = 0;
                decimal totOtherDeduct = 0;
                decimal totDeductions = 0;
                decimal totBank = 0;
                decimal totCash = 0;

                int sNo = 1;
                var list = displayEmployees?.ToList() ?? new List<PrSlryShtVw>();

                // Group by Department preserving Department Sort Order
                var deptGroups = list
                    .GroupBy(e => new
                    {
                        DeptId = e.DeptId,
                        DeptName = string.IsNullOrWhiteSpace(e.DeptName) ? "Other Department" : e.DeptName,
                        DeptSrtOrd = e.DeptSrtOrd > 0 ? e.DeptSrtOrd : 999
                    })
                    .OrderBy(g => g.Key.DeptSrtOrd)
                    .ThenBy(g => g.Key.DeptName);

                foreach (var grp in deptGroups)
                {
                    // 1. Department Header Row
                    PdfGridRow deptHdrRow = grid.Rows.Add();
                    deptHdrRow.Cells[0].ColumnSpan = 23;
                    deptHdrRow.Cells[0].Value = $"  DEPARTMENT: {grp.Key.DeptName.ToUpperInvariant()}";
                    deptHdrRow.Cells[0].StringFormat = fmtLeft;
                    for (int c = 0; c < 23; c++)
                    {
                        deptHdrRow.Cells[c].Style.Font = fontDeptHeader;
                        deptHdrRow.Cells[c].Style.BackgroundBrush = brushDeptHeader;
                        deptHdrRow.Cells[c].Style.CellPadding = new PdfPaddings(2f, 2f, 2f, 2f);
                        deptHdrRow.Cells[c].Style.Borders.All = penCell;
                    }

                    // Department Subtotals Accumulators
                    decimal deptBasic = 0;
                    decimal deptHousing = 0;
                    decimal deptTrans = 0;
                    decimal deptPosit = 0;
                    decimal deptMobile = 0;
                    decimal deptProj = 0;
                    decimal deptOtherAlwnc = 0;
                    decimal deptFixedSalary = 0;
                    decimal deptIncent = 0;
                    decimal deptOt = 0;
                    decimal deptOtherPmt = 0;
                    decimal deptGross = 0;
                    decimal deptSlAdvLoan = 0;
                    decimal deptNoPay = 0;
                    decimal deptOtherDeduct = 0;
                    decimal deptDeductions = 0;
                    decimal deptBank = 0;
                    decimal deptCash = 0;

                    // 2. Department Employee Rows
                    foreach (var emp in grp)
                    {
                        decimal basic = Math.Round(emp.Basic, 0, MidpointRounding.AwayFromZero);
                        decimal housing = Math.Round(emp.HrAlwnc, 0, MidpointRounding.AwayFromZero);
                        decimal trans = Math.Round(emp.TrnsAlwnc, 0, MidpointRounding.AwayFromZero);
                        decimal posit = Math.Round(emp.PstnAlwnc, 0, MidpointRounding.AwayFromZero);
                        decimal mobile = Math.Round(emp.MobAlwnc, 0, MidpointRounding.AwayFromZero);
                        decimal proj = Math.Round(emp.ProjAlwnc, 0, MidpointRounding.AwayFromZero);
                        decimal otherAlwnc = Math.Round(emp.OthrAlwnc + emp.MedAlwnc + emp.SpcAlwnc, 0, MidpointRounding.AwayFromZero);
                        decimal totalFixed = basic + housing + trans + posit + mobile + proj + otherAlwnc;
                        decimal incent = Math.Round(emp.IncntvAmt, 0, MidpointRounding.AwayFromZero);
                        decimal ot = Math.Round(emp.OtAmt, 0, MidpointRounding.AwayFromZero);
                        decimal otherPmt = Math.Round(emp.OthrPmt + emp.BonusAmt, 0, MidpointRounding.AwayFromZero);
                        decimal gross = emp.GrsSlry > 0 ? Math.Round(emp.GrsSlry, 0, MidpointRounding.AwayFromZero) : (totalFixed + incent + ot + otherPmt);
                        decimal slAdvLoan = Math.Round(emp.SlryAdv + emp.StfLnPmt, 0, MidpointRounding.AwayFromZero);
                        decimal noPay = Math.Round(emp.NoPayAmt, 0, MidpointRounding.AwayFromZero);
                        decimal otherDeduct = Math.Round(emp.OthrRcvryAmt, 0, MidpointRounding.AwayFromZero);
                        decimal totalDeduct = slAdvLoan + noPay + otherDeduct;
                        decimal bank = emp.PmtMod == 1 ? (gross - totalDeduct) : (emp.BnkAmt > 0 ? Math.Round(emp.BnkAmt, 0, MidpointRounding.AwayFromZero) : 0);
                        decimal cash = emp.PmtMod == 0 ? (gross - totalDeduct) : (emp.CshAmt > 0 ? Math.Round(emp.CshAmt, 0, MidpointRounding.AwayFromZero) : 0);

                        // Accumulate department totals
                        deptBasic += basic;
                        deptHousing += housing;
                        deptTrans += trans;
                        deptPosit += posit;
                        deptMobile += mobile;
                        deptProj += proj;
                        deptOtherAlwnc += otherAlwnc;
                        deptFixedSalary += totalFixed;
                        deptIncent += incent;
                        deptOt += ot;
                        deptOtherPmt += otherPmt;
                        deptGross += gross;
                        deptSlAdvLoan += slAdvLoan;
                        deptNoPay += noPay;
                        deptOtherDeduct += otherDeduct;
                        deptDeductions += totalDeduct;
                        deptBank += bank;
                        deptCash += cash;

                        // Accumulate grand totals
                        totBasic += basic;
                        totHousing += housing;
                        totTrans += trans;
                        totPosit += posit;
                        totMobile += mobile;
                        totProj += proj;
                        totOtherAlwnc += otherAlwnc;
                        totFixedSalary += totalFixed;
                        totIncent += incent;
                        totOt += ot;
                        totOtherPmt += otherPmt;
                        totGross += gross;
                        totSlAdvLoan += slAdvLoan;
                        totNoPay += noPay;
                        totOtherDeduct += otherDeduct;
                        totDeductions += totalDeduct;
                        totBank += bank;
                        totCash += cash;

                        PdfGridRow row = grid.Rows.Add();
                        row.Cells[0].Value = sNo.ToString(); row.Cells[0].StringFormat = fmtCenterCell;
                        row.Cells[1].Value = emp.EmpId ?? ""; row.Cells[1].StringFormat = fmtCenterCell;
                        row.Cells[2].Value = (emp.FullName ?? "") + (!string.IsNullOrWhiteSpace(emp.DesigName) ? "\n" + emp.DesigName : ""); row.Cells[2].StringFormat = fmtLeft;
                        row.Cells[3].Value = emp.AbFct > 0 ? (emp.AbFct % 1 == 0 ? emp.AbFct.ToString("0") : emp.AbFct.ToString("0.##")) : "0"; row.Cells[3].StringFormat = fmtCenterCell;
                        row.Cells[4].Value = FormatNumber(basic); row.Cells[4].StringFormat = fmtRight;
                        row.Cells[5].Value = FormatNumber(housing); row.Cells[5].StringFormat = fmtRight;
                        row.Cells[6].Value = FormatNumber(trans); row.Cells[6].StringFormat = fmtRight;
                        row.Cells[7].Value = FormatNumber(posit); row.Cells[7].StringFormat = fmtRight;
                        row.Cells[8].Value = FormatNumber(mobile); row.Cells[8].StringFormat = fmtRight;
                        row.Cells[9].Value = FormatNumber(proj); row.Cells[9].StringFormat = fmtRight;
                        row.Cells[10].Value = FormatNumber(otherAlwnc); row.Cells[10].StringFormat = fmtRight;
                        row.Cells[11].Value = FormatNumber(totalFixed); row.Cells[11].StringFormat = fmtRight;
                        row.Cells[12].Value = FormatNumber(incent); row.Cells[12].StringFormat = fmtRight;
                        row.Cells[13].Value = FormatNumber(ot); row.Cells[13].StringFormat = fmtRight;
                        row.Cells[14].Value = FormatNumber(otherPmt); row.Cells[14].StringFormat = fmtRight;
                        row.Cells[15].Value = FormatNumber(gross); row.Cells[15].StringFormat = fmtRight;
                        row.Cells[16].Value = FormatNumber(slAdvLoan); row.Cells[16].StringFormat = fmtRight;
                        row.Cells[17].Value = FormatNumber(noPay); row.Cells[17].StringFormat = fmtRight;
                        row.Cells[18].Value = FormatNumber(otherDeduct); row.Cells[18].StringFormat = fmtRight;
                        row.Cells[19].Value = FormatNumber(totalDeduct); row.Cells[19].StringFormat = fmtRight;
                        row.Cells[20].Value = FormatNumber(bank); row.Cells[20].StringFormat = fmtRight;
                        row.Cells[21].Value = FormatNumber(cash); row.Cells[21].StringFormat = fmtRight;
                        row.Cells[22].Value = emp.Rmks ?? ""; row.Cells[22].StringFormat = fmtLeft;

                        for (int c = 0; c < 23; c++)
                        {
                            row.Cells[c].Style.Font = fontBody;
                            row.Cells[c].Style.CellPadding = cellPadding;
                            row.Cells[c].Style.Borders.All = penCell;
                        }

                        sNo++;
                    }

                    // 3. Department Subtotal Row
                    PdfGridRow deptTotRow = grid.Rows.Add();
                    deptTotRow.Cells[0].ColumnSpan = 4;
                    deptTotRow.Cells[0].Value = $"Department Total ({grp.Count()} Staff)";
                    deptTotRow.Cells[0].StringFormat = fmtLeft;

                    for (int c = 0; c < 23; c++)
                    {
                        deptTotRow.Cells[c].Style.Font = fontDeptTotal;
                        deptTotRow.Cells[c].Style.BackgroundBrush = brushDeptTotal;
                        deptTotRow.Cells[c].Style.CellPadding = cellPadding;
                        deptTotRow.Cells[c].Style.Borders.All = penCell;
                        if (c >= 4)
                            deptTotRow.Cells[c].StringFormat = fmtRight;
                    }

                    deptTotRow.Cells[4].Value = FormatNumber(deptBasic);
                    deptTotRow.Cells[5].Value = FormatNumber(deptHousing);
                    deptTotRow.Cells[6].Value = FormatNumber(deptTrans);
                    deptTotRow.Cells[7].Value = FormatNumber(deptPosit);
                    deptTotRow.Cells[8].Value = FormatNumber(deptMobile);
                    deptTotRow.Cells[9].Value = FormatNumber(deptProj);
                    deptTotRow.Cells[10].Value = FormatNumber(deptOtherAlwnc);
                    deptTotRow.Cells[11].Value = FormatNumber(deptFixedSalary);
                    deptTotRow.Cells[12].Value = FormatNumber(deptIncent);
                    deptTotRow.Cells[13].Value = FormatNumber(deptOt);
                    deptTotRow.Cells[14].Value = FormatNumber(deptOtherPmt);
                    deptTotRow.Cells[15].Value = FormatNumber(deptGross);
                    deptTotRow.Cells[16].Value = FormatNumber(deptSlAdvLoan);
                    deptTotRow.Cells[17].Value = FormatNumber(deptNoPay);
                    deptTotRow.Cells[18].Value = FormatNumber(deptOtherDeduct);
                    deptTotRow.Cells[19].Value = FormatNumber(deptDeductions);
                    deptTotRow.Cells[20].Value = FormatNumber(deptBank);
                    deptTotRow.Cells[21].Value = FormatNumber(deptCash);
                    deptTotRow.Cells[22].Value = "";
                }

                // 4. Grand Totals Row
                PdfGridRow grandTotRow = grid.Rows.Add();
                grandTotRow.Cells[0].ColumnSpan = 4;
                grandTotRow.Cells[0].Value = $"Grand Total ({list.Count} Staff)";
                grandTotRow.Cells[0].StringFormat = fmtLeft;

                for (int c = 0; c < 23; c++)
                {
                    grandTotRow.Cells[c].Style.Font = fontGrandTotal;
                    grandTotRow.Cells[c].Style.BackgroundBrush = brushGrandTotal;
                    grandTotRow.Cells[c].Style.CellPadding = cellPadding;
                    grandTotRow.Cells[c].Style.Borders.All = penCell;
                    if (c >= 4)
                        grandTotRow.Cells[c].StringFormat = fmtRight;
                }

                grandTotRow.Cells[4].Value = FormatNumber(totBasic);
                grandTotRow.Cells[5].Value = FormatNumber(totHousing);
                grandTotRow.Cells[6].Value = FormatNumber(totTrans);
                grandTotRow.Cells[7].Value = FormatNumber(totPosit);
                grandTotRow.Cells[8].Value = FormatNumber(totMobile);
                grandTotRow.Cells[9].Value = FormatNumber(totProj);
                grandTotRow.Cells[10].Value = FormatNumber(totOtherAlwnc);
                grandTotRow.Cells[11].Value = FormatNumber(totFixedSalary);
                grandTotRow.Cells[12].Value = FormatNumber(totIncent);
                grandTotRow.Cells[13].Value = FormatNumber(totOt);
                grandTotRow.Cells[14].Value = FormatNumber(totOtherPmt);
                grandTotRow.Cells[15].Value = FormatNumber(totGross);
                grandTotRow.Cells[16].Value = FormatNumber(totSlAdvLoan);
                grandTotRow.Cells[17].Value = FormatNumber(totNoPay);
                grandTotRow.Cells[18].Value = FormatNumber(totOtherDeduct);
                grandTotRow.Cells[19].Value = FormatNumber(totDeductions);
                grandTotRow.Cells[20].Value = FormatNumber(totBank);
                grandTotRow.Cells[21].Value = FormatNumber(totCash);
                grandTotRow.Cells[22].Value = "";

                // Draw Table with pagination
                PdfGridLayoutFormat layoutFormat = new PdfGridLayoutFormat
                {
                    Layout = PdfLayoutType.Paginate
                };
                PdfGridLayoutResult result = grid.Draw(page, new PointF(0, 0), layoutFormat);

                // Summary Box & Amount in Words on the last page
                PdfPage lastPage = result.Page;
                PdfGraphics g = lastPage.Graphics;
                float curY = result.Bounds.Bottom + 4f;

                float boxW = 215f;
                float boxX = tableTotalWidth - boxW;
                float rowH = 14f;

                if (curY + (rowH * 3) + 30f > lastPage.GetClientSize().Height)
                {
                    lastPage = document.Pages.Add();
                    g = lastPage.Graphics;
                    curY = 15f;
                }

                PdfFont fontBoxReg = new PdfStandardFont(PdfFontFamily.Helvetica, 7.5f, PdfFontStyle.Regular);
                PdfFont fontBoxBold = new PdfStandardFont(PdfFontFamily.Helvetica, 7.5f, PdfFontStyle.Bold);

                decimal grandTotalNet = internalStaffNet + outsourcedStaffNet;

                // Summary Row 1: Internal Staff Total Net Salary
                g.DrawRectangle(penCell, boxX, curY, boxW, rowH);
                g.DrawString("Internal Staff Total Net Salary", fontBoxReg, PdfBrushes.Black, new RectangleF(boxX + 4f, curY, boxW - 65f, rowH), fmtLeft);
                g.DrawString(FormatNumber(internalStaffNet), fontBoxBold, PdfBrushes.Black, new RectangleF(boxX + boxW - 65f, curY, 61f, rowH), fmtRight);

                // Summary Row 2: Outsourced Staff Total Net Salary
                curY += rowH;
                g.DrawRectangle(penCell, boxX, curY, boxW, rowH);
                g.DrawString("Outsourced Staff Total Net Salary", fontBoxReg, PdfBrushes.Black, new RectangleF(boxX + 4f, curY, boxW - 65f, rowH), fmtLeft);
                g.DrawString(FormatNumber(outsourcedStaffNet), fontBoxBold, PdfBrushes.Black, new RectangleF(boxX + boxW - 65f, curY, 61f, rowH), fmtRight);

                // Summary Row 3: Total Net Salary (QR)
                curY += rowH;
                g.DrawRectangle(penCell, boxX, curY, boxW, rowH);
                g.DrawString("Total Net Salary (QR)", fontBoxBold, PdfBrushes.Black, new RectangleF(boxX + 4f, curY, boxW - 65f, rowH), fmtLeft);
                g.DrawString(FormatNumber(grandTotalNet), fontBoxBold, PdfBrushes.Black, new RectangleF(boxX + boxW - 65f, curY, 61f, rowH), fmtRight);

                // Amount in Words Centered
                curY += rowH + 12f;
                PdfFont fontWords = new PdfStandardFont(PdfFontFamily.Helvetica, 8.5f, PdfFontStyle.Bold);
                string amountInWords;
                try
                {
                    amountInWords = "Qatari Riyal " + (grandTotalNet > 0 ? NumberToWords.convert(Convert.ToInt32(grandTotalNet)) : "Zero");
                }
                catch
                {
                    amountInWords = $"Qatari Riyal {FormatNumber(grandTotalNet)}";
                }
                g.DrawString(amountInWords, fontWords, PdfBrushes.Black, new RectangleF(0, curY, printableWidth, 16f), formatCenter);

                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    document.Save(ms);
                    bytes = ms.ToArray();
                }

                logoStream?.Dispose();
                return bytes;
            }
        }

        private static string FormatNumber(decimal value)
        {
            decimal rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
            if (rounded == 0) return "0";
            return rounded.ToString("#,##0");
        }
    }
}
