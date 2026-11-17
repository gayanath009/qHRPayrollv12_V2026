using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using qHRM.Areas.Payroll.Models;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace qHRM.Areas.Payroll.Reports
{
    public static class StaffLeaveTicketEncashPdfReport
    {
        public static byte[] GenerateReport(
            string companyName,
            int year,
            int month,
            IEnumerable<HrPrStaffLvTktEncashVw> records,
            string webRootPath)
        {
            using (PdfDocument document = new PdfDocument())
            {
                // A4 Landscape: 841.89 pt x 595.27 pt
                document.PageSettings.Size = PdfPageSize.A4;
                document.PageSettings.Orientation = PdfPageOrientation.Landscape;

                float leftMargin = 16f;
                float rightMargin = 16f;
                float topMargin = 12f;
                float bottomMargin = 16f;
                document.PageSettings.SetMargins(leftMargin, topMargin, rightMargin, bottomMargin);

                float printableWidth = 841.89f - (leftMargin + rightMargin); // ~809.89 pt

                int prevMonth = month == 1 ? 12 : month - 1;
                int prevYear = month == 1 ? year - 1 : year;
                string monthName = new DateTime(year, month, 1).ToString("MMMM");
                string prevYY = (prevYear % 100).ToString("D2");
                string curYY = (year % 100).ToString("D2");
                string prevMM = prevMonth.ToString("D2");
                string curMM = month.ToString("D2");

                string cutoffPeriodText = $"Cut off period 21/{prevMM}/{prevYY} to 20/{curMM}/{curYY}";
                string compTitle = !string.IsNullOrWhiteSpace(companyName) ? companyName : "Qatar Computer Services WLL";
                string reportSubtitle = $"Internal/Outsource Staff Leave Salary & Air Ticket Encashment - {monthName} {year}";

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
                PdfFont fontSubtitle = new PdfStandardFont(PdfFontFamily.Helvetica, 9.5f, PdfFontStyle.Bold);
                PdfFont fontCutoff = new PdfStandardFont(PdfFontFamily.Helvetica, 7.5f, PdfFontStyle.Regular);
                PdfStringFormat formatCenter = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Center,
                    LineAlignment = PdfVerticalAlignment.Middle
                };

                topTemplate.Graphics.DrawString(compTitle, fontCompany, PdfBrushes.Black, new RectangleF(0, 2f, printableWidth, 16f), formatCenter);
                topTemplate.Graphics.DrawString(reportSubtitle, fontSubtitle, PdfBrushes.Black, new RectangleF(0, 18f, printableWidth, 14f), formatCenter);
                topTemplate.Graphics.DrawString($"({cutoffPeriodText})", fontCutoff, PdfBrushes.Black, new RectangleF(0, 31f, printableWidth, 12f), formatCenter);
                document.Template.Top = topTemplate;

                // Bottom Footer Template: Page Number and Timestamp
                PdfPageTemplateElement bottomTemplate = new PdfPageTemplateElement(printableWidth, 14f);
                PdfFont fontFooter = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f, PdfFontStyle.Regular);
                PdfStringFormat fmtFootLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtFootRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                string printTime = "Printed on: " + DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
                bottomTemplate.Graphics.DrawString(printTime, fontFooter, PdfBrushes.DarkGray, new RectangleF(0, 0, printableWidth / 2, 14f), fmtFootLeft);

                PdfCompositeField pageNumberField = new PdfCompositeField(fontFooter, PdfBrushes.DarkGray, "Page {0} of {1}", new PdfPageNumberField(), new PdfPageCountField());
                pageNumberField.Bounds = new RectangleF(printableWidth - 100f, 0, 100f, 14f);
                pageNumberField.StringFormat = fmtFootRight;
                pageNumberField.Draw(bottomTemplate.Graphics, new PointF(printableWidth - 100f, 0));

                document.Template.Bottom = bottomTemplate;

                PdfPage page = document.Pages.Add();

                // Setup PdfGrid
                PdfGrid grid = new PdfGrid
                {
                    RepeatHeader = true,
                    AllowRowBreakAcrossPages = false
                };

                // 17 Columns with optimal widths summing to 792 pt
                float[] widths = new float[]
                {
                    36f,  // 0: Emp ID
                    18f,  // 1: Title
                    75f,  // 2: Full Name
                    65f,  // 3: Department
                    65f,  // 4: Designation
                    40f,  // 5: Joined Date
                    42f,  // 6: Vacation Return
                    42f,  // 7: Nationality
                    40f,  // 8: Next Reset
                    22f,  // 9: App. Days
                    30f,  // 10: Elig. Days
                    24f,  // 11: Net Days
                    44f,  // 12: Monthly Sal.
                    45f,  // 13: Leave Sal.
                    42f,  // 14: Air Ticket
                    48f,  // 15: Total Sett.
                    114f  // 16: Remarks
                };

                for (int i = 0; i < widths.Length; i++)
                {
                    PdfGridColumn col = grid.Columns.Add();
                    col.Width = widths[i];
                }

                // Fonts and styles
                PdfFont fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica, 5.5f, PdfFontStyle.Bold);
                PdfFont fontBody = new PdfStandardFont(PdfFontFamily.Helvetica, 5.2f, PdfFontStyle.Regular);
                PdfFont fontBodyBold = new PdfStandardFont(PdfFontFamily.Helvetica, 5.2f, PdfFontStyle.Bold);
                PdfFont fontTotal = new PdfStandardFont(PdfFontFamily.Helvetica, 5.8f, PdfFontStyle.Bold);
                PdfFont fontGrand = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f, PdfFontStyle.Bold);

                PdfPen penCell = new PdfPen(new PdfColor(120, 120, 120), 0.5f);
                PdfPen penTotal = new PdfPen(new PdfColor(0, 0, 0), 0.75f);
                PdfPaddings cellPadding = new PdfPaddings(1f, 1f, 1f, 1f);

                PdfStringFormat fmtLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtCenter = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat fmtRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                PdfBrush brushHdrBg = new PdfSolidBrush(new PdfColor(240, 240, 240));
                PdfBrush brushSubtotalBg = new PdfSolidBrush(new PdfColor(250, 250, 250));
                PdfBrush brushGrandBg = new PdfSolidBrush(new PdfColor(235, 235, 235));
                PdfBrush brushRedText = new PdfSolidBrush(new PdfColor(185, 28, 28));

                // 1 Header Row
                grid.Headers.Add(1);
                PdfGridRow hdr = grid.Headers[0];
                string[] headerTitles = new string[]
                {
                    "Employee\nID",
                    "Title",
                    "Full Name",
                    "Department",
                    "Designation",
                    "Joined\nDate",
                    "Vacation\nReturn",
                    "Nationality",
                    "Next Reset\nDate",
                    "App.\nDays",
                    "Eligible\nDays",
                    "Net Leave\nDays",
                    "Monthly\nSalary",
                    "Total Leave\nSalary",
                    "Air Ticket\nValue",
                    "Total\nSettlement",
                    "Remarks"
                };

                for (int c = 0; c < headerTitles.Length; c++)
                {
                    hdr.Cells[c].Value = headerTitles[c];
                    hdr.Cells[c].Style.Font = fontHeader;
                    hdr.Cells[c].Style.BackgroundBrush = brushHdrBg;
                    hdr.Cells[c].Style.StringFormat = fmtCenter;
                    hdr.Cells[c].Style.CellPadding = cellPadding;
                    hdr.Cells[c].Style.Borders.All = penCell;
                }
                hdr.Cells[11].Style.TextBrush = brushRedText;

                var list = records != null ? records.ToList() : new List<HrPrStaffLvTktEncashVw>();
                var internalStaff = list.Where(x => x.StaffCategory == "INTERNAL").ToList();
                var outsourceStaff = list.Where(x => x.StaffCategory == "OUTSOURCE").ToList();

                decimal totInternalLeave = 0m;
                decimal totInternalTicket = 0m;
                decimal totInternalSettlement = 0m;

                decimal totOutsourceLeave = 0m;
                decimal totOutsourceTicket = 0m;
                decimal totOutsourceSettlement = 0m;

                // 1. Internal Staff Section
                if (internalStaff.Any())
                {
                    foreach (var emp in internalStaff)
                    {
                        PdfGridRow row = grid.Rows.Add();
                        FillDataRow(row, emp, fontBody, fontBodyBold, penCell, cellPadding, fmtLeft, fmtCenter, fmtRight, brushRedText);

                        totInternalLeave += emp.TotalLeaveSalary;
                        totInternalTicket += emp.AirTicketValue;
                        totInternalSettlement += emp.TotalSettlement;
                    }

                    // Internal Subtotal Row
                    PdfGridRow subRow = grid.Rows.Add();
                    subRow.Cells[0].Value = "Total Leave Salary & Air Ticket Encashment for Internal Staff:";
                    subRow.Cells[0].ColumnSpan = 13;
                    subRow.Cells[0].Style.Font = fontTotal;
                    subRow.Cells[0].Style.StringFormat = fmtRight;
                    subRow.Cells[0].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[0].Style.CellPadding = cellPadding;
                    subRow.Cells[0].Style.Borders.All = penTotal;

                    for (int c = 1; c <= 12; c++)
                    {
                        subRow.Cells[c].Style.BackgroundBrush = brushSubtotalBg;
                        subRow.Cells[c].Style.Borders.All = penTotal;
                    }

                    subRow.Cells[13].Value = FormatNumber(totInternalLeave);
                    subRow.Cells[13].Style.Font = fontTotal;
                    subRow.Cells[13].Style.StringFormat = fmtRight;
                    subRow.Cells[13].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[13].Style.CellPadding = cellPadding;
                    subRow.Cells[13].Style.Borders.All = penTotal;

                    subRow.Cells[14].Value = FormatNumber(totInternalTicket);
                    subRow.Cells[14].Style.Font = fontTotal;
                    subRow.Cells[14].Style.StringFormat = fmtRight;
                    subRow.Cells[14].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[14].Style.CellPadding = cellPadding;
                    subRow.Cells[14].Style.Borders.All = penTotal;

                    subRow.Cells[15].Value = FormatNumber(totInternalSettlement);
                    subRow.Cells[15].Style.Font = fontTotal;
                    subRow.Cells[15].Style.StringFormat = fmtRight;
                    subRow.Cells[15].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[15].Style.CellPadding = cellPadding;
                    subRow.Cells[15].Style.Borders.All = penTotal;

                    subRow.Cells[16].Value = "";
                    subRow.Cells[16].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[16].Style.CellPadding = cellPadding;
                    subRow.Cells[16].Style.Borders.All = penTotal;
                }

                // 2. Outsource Staff Section
                if (outsourceStaff.Any())
                {
                    foreach (var emp in outsourceStaff)
                    {
                        PdfGridRow row = grid.Rows.Add();
                        FillDataRow(row, emp, fontBody, fontBodyBold, penCell, cellPadding, fmtLeft, fmtCenter, fmtRight, brushRedText);

                        totOutsourceLeave += emp.TotalLeaveSalary;
                        totOutsourceTicket += emp.AirTicketValue;
                        totOutsourceSettlement += emp.TotalSettlement;
                    }

                    // Outsource Subtotal Row
                    PdfGridRow subRow = grid.Rows.Add();
                    subRow.Cells[0].Value = "Total Leave Salary & Air Ticket Encashment for Outsource Staff:";
                    subRow.Cells[0].ColumnSpan = 13;
                    subRow.Cells[0].Style.Font = fontTotal;
                    subRow.Cells[0].Style.StringFormat = fmtRight;
                    subRow.Cells[0].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[0].Style.CellPadding = cellPadding;
                    subRow.Cells[0].Style.Borders.All = penTotal;

                    for (int c = 1; c <= 12; c++)
                    {
                        subRow.Cells[c].Style.BackgroundBrush = brushSubtotalBg;
                        subRow.Cells[c].Style.Borders.All = penTotal;
                    }

                    subRow.Cells[13].Value = FormatNumber(totOutsourceLeave);
                    subRow.Cells[13].Style.Font = fontTotal;
                    subRow.Cells[13].Style.StringFormat = fmtRight;
                    subRow.Cells[13].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[13].Style.CellPadding = cellPadding;
                    subRow.Cells[13].Style.Borders.All = penTotal;

                    subRow.Cells[14].Value = FormatNumber(totOutsourceTicket);
                    subRow.Cells[14].Style.Font = fontTotal;
                    subRow.Cells[14].Style.StringFormat = fmtRight;
                    subRow.Cells[14].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[14].Style.CellPadding = cellPadding;
                    subRow.Cells[14].Style.Borders.All = penTotal;

                    subRow.Cells[15].Value = FormatNumber(totOutsourceSettlement);
                    subRow.Cells[15].Style.Font = fontTotal;
                    subRow.Cells[15].Style.StringFormat = fmtRight;
                    subRow.Cells[15].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[15].Style.CellPadding = cellPadding;
                    subRow.Cells[15].Style.Borders.All = penTotal;

                    subRow.Cells[16].Value = "";
                    subRow.Cells[16].Style.BackgroundBrush = brushSubtotalBg;
                    subRow.Cells[16].Style.CellPadding = cellPadding;
                    subRow.Cells[16].Style.Borders.All = penTotal;
                }

                // 3. Grand Total Row
                decimal grandTotal = totInternalSettlement + totOutsourceSettlement;
                PdfGridRow grandRow = grid.Rows.Add();
                grandRow.Cells[0].Value = "Total:";
                grandRow.Cells[0].ColumnSpan = 15;
                grandRow.Cells[0].Style.Font = fontGrand;
                grandRow.Cells[0].Style.StringFormat = fmtRight;
                grandRow.Cells[0].Style.BackgroundBrush = brushGrandBg;
                grandRow.Cells[0].Style.CellPadding = cellPadding;
                grandRow.Cells[0].Style.Borders.All = penTotal;

                for (int c = 1; c <= 14; c++)
                {
                    grandRow.Cells[c].Style.BackgroundBrush = brushGrandBg;
                    grandRow.Cells[c].Style.Borders.All = penTotal;
                }

                grandRow.Cells[15].Value = FormatNumber(grandTotal);
                grandRow.Cells[15].Style.Font = fontGrand;
                grandRow.Cells[15].Style.StringFormat = fmtRight;
                grandRow.Cells[15].Style.BackgroundBrush = brushGrandBg;
                grandRow.Cells[15].Style.CellPadding = cellPadding;
                grandRow.Cells[15].Style.Borders.All = penTotal;

                grandRow.Cells[16].Value = "";
                grandRow.Cells[16].Style.BackgroundBrush = brushGrandBg;
                grandRow.Cells[16].Style.CellPadding = cellPadding;
                grandRow.Cells[16].Style.Borders.All = penTotal;

                // Draw Table with pagination
                PdfGridLayoutFormat layoutFormat = new PdfGridLayoutFormat
                {
                    Layout = PdfLayoutType.Paginate
                };
                grid.Draw(page, new PointF(0, 0), layoutFormat);

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

        private static void FillDataRow(
            PdfGridRow row,
            HrPrStaffLvTktEncashVw emp,
            PdfFont fontBody,
            PdfFont fontBodyBold,
            PdfPen penCell,
            PdfPaddings padding,
            PdfStringFormat fmtLeft,
            PdfStringFormat fmtCenter,
            PdfStringFormat fmtRight,
            PdfBrush brushRedText)
        {
            // 0: Emp ID
            SetCell(row.Cells[0], emp.EmpId, fontBodyBold, fmtCenter, penCell, padding, null);

            // 1: Courtesy Title
            SetCell(row.Cells[1], emp.CourtesyTitle ?? "", fontBody, fmtCenter, penCell, padding, null);

            // 2: Full Name
            SetCell(row.Cells[2], emp.FullName ?? "", fontBodyBold, fmtLeft, penCell, padding, null);

            // 3: Department
            SetCell(row.Cells[3], emp.Department ?? "", fontBody, fmtLeft, penCell, padding, null);

            // 4: Designation
            SetCell(row.Cells[4], emp.Designation ?? "", fontBody, fmtLeft, penCell, padding, null);

            // 5: Joined Date
            SetCell(row.Cells[5], emp.JoinedDateStr, fontBody, fmtCenter, penCell, padding, null);

            // 6: Vacation Return Date
            SetCell(row.Cells[6], emp.VacationReturnDateStr ?? "-", fontBody, fmtCenter, penCell, padding, null);

            // 7: Nationality
            SetCell(row.Cells[7], emp.Nationality ?? "", fontBody, fmtCenter, penCell, padding, null);

            // 8: Next Reset Date
            SetCell(row.Cells[8], emp.NextResetDateStr, fontBody, fmtCenter, penCell, padding, null);

            // 9: Applicable Days
            SetCell(row.Cells[9], emp.ApplicableDays.ToString(), fontBody, fmtCenter, penCell, padding, null);

            // 10: Eligible Leave Days
            SetCell(row.Cells[10], emp.EligibleLeaveDays.ToString("0.#####"), fontBody, fmtRight, penCell, padding, null);

            // 11: Net Leave Days (Red, Bold)
            SetCell(row.Cells[11], emp.NetLeaveDays.ToString(), fontBodyBold, fmtCenter, penCell, padding, brushRedText);

            // 12: Monthly Salary
            SetCell(row.Cells[12], FormatNumber(emp.MonthlySalary), fontBody, fmtRight, penCell, padding, null);

            // 13: Total Leave Salary
            SetCell(row.Cells[13], emp.TotalLeaveSalary > 0 ? FormatNumber(emp.TotalLeaveSalary) : "-", fontBody, fmtRight, penCell, padding, null);

            // 14: Air Ticket Value
            SetCell(row.Cells[14], emp.AirTicketValue > 0 ? FormatNumber(emp.AirTicketValue) : "-", fontBody, fmtRight, penCell, padding, null);

            // 15: Total Settlement
            SetCell(row.Cells[15], FormatNumber(emp.TotalSettlement), fontBodyBold, fmtRight, penCell, padding, null);

            // 16: Remarks
            SetCell(row.Cells[16], emp.Remarks ?? "-", fontBody, fmtLeft, penCell, padding, null);
        }

        private static void SetCell(
            PdfGridCell cell,
            string value,
            PdfFont font,
            PdfStringFormat fmt,
            PdfPen border,
            PdfPaddings padding,
            PdfBrush textBrush)
        {
            cell.Value = value;
            cell.Style.Font = font;
            cell.Style.StringFormat = fmt;
            cell.Style.Borders.All = border;
            cell.Style.CellPadding = padding;
            if (textBrush != null)
            {
                cell.Style.TextBrush = textBrush;
            }
        }

        private static string FormatNumber(decimal value)
        {
            if (value == 0) return "0";
            return value.ToString("#,##0.00");
        }
    }
}
