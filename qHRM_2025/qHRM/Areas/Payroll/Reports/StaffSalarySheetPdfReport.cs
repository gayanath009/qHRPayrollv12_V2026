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
    public static class StaffSalarySheetPdfReport
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
                    // 203x59 aspect ratio ~ 3.44
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

                // Show Staff Category in Report Header based on selection
                string categoryPrefix;
                if (sctn == 0)
                    categoryPrefix = "In-House Staff ";
                else if (sctn == 1)
                    categoryPrefix = "Outsourced Staff ";
                else
                    categoryPrefix = "Staff ";

                string reportSubtitle = $"{categoryPrefix}Salary Sheet for the Month of {monthName} - {year}";

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
                // Gross Salary reduced from 48 to 36; Remarks increased from 85 to 95.
                // Leaves a ~13 pt margin buffer before the printable boundary so the right border is never clipped!
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
                    36f,  // 15: Gross Salary (Reduced as requested)
                    30f,  // 16: Sl Adv / Loan
                    26f,  // 17: No Pay
                    26f,  // 18: Other Deduct
                    30f,  // 19: Total Deductions
                    40f,  // 20: Bank Transfer
                    32f,  // 21: Cash Payment
                    95f   // 22: Remarks (Increased as requested)
                };

                float tableTotalWidth = widths.Sum(); // 793 pt

                for (int i = 0; i < widths.Length; i++)
                {
                    PdfGridColumn col = grid.Columns.Add();
                    col.Width = widths[i];
                }

                // Fonts and styles
                PdfFont fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontBody = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Regular);
                PdfFont fontNameBold = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontDesigRegular = new PdfStandardFont(PdfFontFamily.Helvetica, 5.0f, PdfFontStyle.Regular);
                PdfFont fontTotal = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfPen penCell = new PdfPen(new PdfColor(0, 0, 0), 0.5f);
                PdfPaddings cellPadding = new PdfPaddings(1f, 1f, 1f, 1f);

                PdfStringFormat fmtLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtCenterCell = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                // Custom cell rendering to Bold Employee Name and ensure complete cell borders
                grid.BeginCellLayout += (sender, args) =>
                {
                    if (!args.IsHeaderRow && args.CellIndex == 2 && !string.IsNullOrWhiteSpace(args.Value))
                    {
                        // 1. Draw complete cell border on all 4 sides so the row separator line is fully drawn
                        args.Graphics.DrawRectangle(penCell, args.Bounds);

                        string[] parts = args.Value.Split(new[] { '\n' }, 2);
                        float padX = 1.5f;
                        float padY = 1.5f;
                        float x = args.Bounds.X + padX;
                        float y = args.Bounds.Y + padY;

                        // 2. Draw Name in Bold
                        args.Graphics.DrawString(parts[0], fontNameBold, PdfBrushes.Black, new PointF(x, y));

                        // 3. Draw Designation in Regular if present
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
                        else if (c == 0 || c == 1 || c == 3 || c == 7 || c == 16 || c == 20)
                            cell.StringFormat = fmtCenterCell;
                        else
                            cell.StringFormat = fmtCenterCell;
                    }
                }

                // Data Rows & Totals Accumulators
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

                foreach (var emp in list)
                {
                    decimal basic = emp.Basic;
                    decimal housing = emp.HrAlwnc;
                    decimal trans = emp.TrnsAlwnc;
                    decimal posit = emp.PstnAlwnc;
                    decimal mobile = emp.MobAlwnc;
                    decimal proj = emp.ProjAlwnc;
                    decimal otherAlwnc = emp.OthrAlwnc + emp.MedAlwnc + emp.SpcAlwnc;
                    decimal totalFixed = basic + housing + trans + posit + mobile + proj + otherAlwnc;
                    decimal incent = emp.IncntvAmt;
                    decimal ot = emp.OtAmt;
                    decimal otherPmt = emp.OthrPmt + emp.BonusAmt;
                    decimal gross = emp.GrsSlry > 0 ? emp.GrsSlry : (totalFixed + incent + ot + otherPmt);
                    decimal slAdvLoan = emp.SlryAdv + emp.StfLnPmt;
                    decimal noPay = emp.NoPayAmt;
                    decimal otherDeduct = emp.OthrRcvryAmt;
                    decimal totalDeduct = emp.TotDctAmt > 0 ? emp.TotDctAmt : (slAdvLoan + noPay + otherDeduct);
                    decimal bank = emp.PmtMod == 1 ? emp.NtSlry : (emp.BnkAmt > 0 ? emp.BnkAmt : 0);
                    decimal cash = emp.PmtMod == 0 ? emp.NtSlry : (emp.CshAmt > 0 ? emp.CshAmt : 0);

                    // Accumulate totals
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
                    row.Cells[3].Value = emp.AbFct > 0 ? emp.AbFct.ToString("0") : "0"; row.Cells[3].StringFormat = fmtCenterCell;
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

                // Grand Totals row in the table
                PdfGridRow totRow = grid.Rows.Add();
                for (int c = 0; c < 23; c++)
                {
                    totRow.Cells[c].Style.Font = fontTotal;
                    totRow.Cells[c].Style.CellPadding = cellPadding;
                    totRow.Cells[c].Style.Borders.All = penCell;
                    totRow.Cells[c].StringFormat = fmtRight;
                }
                totRow.Cells[0].Value = "";
                totRow.Cells[1].Value = "";
                totRow.Cells[2].Value = "";
                totRow.Cells[3].Value = "";
                totRow.Cells[4].Value = FormatNumber(totBasic);
                totRow.Cells[5].Value = FormatNumber(totHousing);
                totRow.Cells[6].Value = FormatNumber(totTrans);
                totRow.Cells[7].Value = FormatNumber(totPosit);
                totRow.Cells[8].Value = FormatNumber(totMobile);
                totRow.Cells[9].Value = FormatNumber(totProj);
                totRow.Cells[10].Value = FormatNumber(totOtherAlwnc);
                totRow.Cells[11].Value = FormatNumber(totFixedSalary);
                totRow.Cells[12].Value = FormatNumber(totIncent);
                totRow.Cells[13].Value = FormatNumber(totOt);
                totRow.Cells[14].Value = FormatNumber(totOtherPmt);
                totRow.Cells[15].Value = FormatNumber(totGross);
                totRow.Cells[16].Value = FormatNumber(totSlAdvLoan);
                totRow.Cells[17].Value = FormatNumber(totNoPay);
                totRow.Cells[18].Value = FormatNumber(totOtherDeduct);
                totRow.Cells[19].Value = FormatNumber(totDeductions);
                totRow.Cells[20].Value = FormatNumber(totBank);
                totRow.Cells[21].Value = FormatNumber(totCash);
                totRow.Cells[22].Value = "";

                // Draw Table with pagination
                PdfGridLayoutFormat layoutFormat = new PdfGridLayoutFormat
                {
                    Layout = PdfLayoutType.Paginate
                };
                PdfGridLayoutResult result = grid.Draw(page, new PointF(0, 0), layoutFormat);

                // Summary Box & Amount in Words on the last page (refer to Image 3)
                PdfPage lastPage = result.Page;
                PdfGraphics g = lastPage.Graphics;
                float curY = result.Bounds.Bottom + 4f;

                float boxW = 215f;
                // Align summary box with the right edge of the table
                float boxX = tableTotalWidth - boxW;
                float rowH = 14f;

                // Ensure enough space for summary box (3 rows) + words (~20pt) + margins
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
            if (value == 0) return "0";
            return (value % 1 == 0) ? value.ToString("#,##0") : value.ToString("#,##0.00");
        }
    }
}
