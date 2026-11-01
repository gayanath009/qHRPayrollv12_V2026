using System;
using System.Collections.Generic;
using System.IO;
using qHRM.Areas.Payroll.Models;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Security;

namespace qHRM.Areas.Payroll.Reports
{
    public static class PaySlipPdfReport
    {
        public static byte[] GeneratePaySlipPdf(IEnumerable<PrSalarySheetView> slips, string webRootPath, string customPeriod = null, string password = null)
        {
            using (PdfDocument document = new PdfDocument())
            {
                // Page settings: A4 Portrait, zero default margins so we control our coordinate space
                document.PageSettings.Size = PdfPageSize.A4;
                document.PageSettings.Orientation = PdfPageOrientation.Portrait;
                document.PageSettings.Margins.All = 0;

                // PDF document security (if password provided)
                if (!string.IsNullOrWhiteSpace(password))
                {
                    document.Security.KeySize = PdfEncryptionKeySize.Key256BitRevision6;
                    document.Security.Algorithm = PdfEncryptionAlgorithm.AES;
                    document.Security.Permissions = PdfPermissionsFlags.Print | PdfPermissionsFlags.FullQualityPrint;
                    document.Security.UserPassword = password.Trim();
                    document.Security.OwnerPassword = password.Trim();
                }

                string logoPath = Path.Combine(webRootPath, "images", "qcs.png");
                string stampPath = Path.Combine(webRootPath, "images", "rubbrStmp.png");

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

                FileStream stampStream = null;
                PdfBitmap stampBitmap = null;
                if (File.Exists(stampPath))
                {
                    try
                    {
                        stampStream = new FileStream(stampPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        stampBitmap = new PdfBitmap(stampStream);
                    }
                    catch { }
                }

                // Fonts (increased by 1 size across the pay slip)
                PdfFont fontTitle = new PdfStandardFont(PdfFontFamily.Helvetica, 17, PdfFontStyle.Regular);
                PdfFont fontPeriod = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Regular);
                PdfFont fontEmpInfo = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Regular);
                PdfFont fontEmpInfoBold = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                
                PdfFont fontTableHdr = new PdfStandardFont(PdfFontFamily.Helvetica, 10.5f, PdfFontStyle.Bold);
                PdfFont fontTableSubHdr = new PdfStandardFont(PdfFontFamily.Helvetica, 10.5f, PdfFontStyle.Bold | PdfFontStyle.Italic);
                PdfFont fontTableRow = new PdfStandardFont(PdfFontFamily.Helvetica, 10f, PdfFontStyle.Regular);
                PdfFont fontTableRowBold = new PdfStandardFont(PdfFontFamily.Helvetica, 10.5f, PdfFontStyle.Bold);
                
                PdfFont fontFootBold = new PdfStandardFont(PdfFontFamily.Helvetica, 9.5f, PdfFontStyle.Bold);
                PdfFont fontFootItalic = new PdfStandardFont(PdfFontFamily.Helvetica, 9f, PdfFontStyle.Italic);

                // Brushes & Pens
                PdfBrush brushBlack = PdfBrushes.Black;
                PdfBrush brushBannerBg = new PdfSolidBrush(new PdfColor(226, 230, 234)); // Light gray header bar
                PdfBrush brushCatBg = new PdfSolidBrush(new PdfColor(233, 236, 239)); // Category row light gray
                PdfPen penBorder = new PdfPen(new PdfColor(0, 0, 0), 0.75f);
                PdfPen penOuterBorder = new PdfPen(new PdfColor(0, 0, 0), 1.0f);

                // Alignments
                PdfStringFormat formatLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat formatCenter = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat formatRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                float marginX = 40f;
                float contentWidth = 515f; // 595 - 80

                foreach (var slip in slips)
                {
                    PdfPage page = document.Pages.Add();
                    PdfGraphics g = page.Graphics;

                    float y = 35f;

                    // 1. Top Header Banner
                    float bannerHeight = 44f;
                    g.DrawRectangle(brushBannerBg, marginX, y, contentWidth, bannerHeight);

                    // Logo on left of banner
                    if (logoBitmap != null)
                    {
                        // 203x59 aspect ratio ~ 3.44
                        float lHeight = 24f;
                        float lWidth = lHeight * (203f / 59f);
                        float lY = y + (bannerHeight - lHeight) / 2f;
                        g.DrawImage(logoBitmap, marginX + 10f, lY, lWidth, lHeight);
                    }

                    // "Pay Slip" centered in banner
                    RectangleF titleRect = new RectangleF(marginX, y, contentWidth, bannerHeight);
                    g.DrawString("Pay Slip", fontTitle, brushBlack, titleRect, formatCenter);

                    // Period on right of banner
                    string periodTxt = !string.IsNullOrWhiteSpace(customPeriod)
                        ? customPeriod
                        : (!string.IsNullOrWhiteSpace(slip.YrMntTxt)
                            ? slip.YrMntTxt
                            : (slip.EftYr > 0 && slip.EftMnt > 0 ? new DateTime(slip.EftYr, slip.EftMnt, 1).ToString("MMMM yyyy") : ""));
                    RectangleF periodRect = new RectangleF(marginX, y, contentWidth - 12f, bannerHeight);
                    g.DrawString(periodTxt, fontPeriod, brushBlack, periodRect, formatRight);

                    y += bannerHeight + 18f;

                    // 2. Employee Details
                    float labelW = 95f;
                    float colonW = 10f;
                    float valW = contentWidth - labelW - colonW;
                    float infoLineHeight = 17f;

                    DrawInfoLine(g, "Employee Id", slip.Emp_Id ?? "", marginX, y, labelW, colonW, valW, infoLineHeight, fontEmpInfo, fontEmpInfo, brushBlack, formatLeft);
                    y += infoLineHeight;
                    DrawInfoLine(g, "Name", slip.Full_Name ?? "", marginX, y, labelW, colonW, valW, infoLineHeight, fontEmpInfo, fontEmpInfoBold, brushBlack, formatLeft);
                    y += infoLineHeight;
                    DrawInfoLine(g, "Title", slip.Designation ?? "", marginX, y, labelW, colonW, valW, infoLineHeight, fontEmpInfo, fontEmpInfo, brushBlack, formatLeft);
                    y += infoLineHeight;
                    DrawInfoLine(g, "Department", slip.Department ?? "", marginX, y, labelW, colonW, valW, infoLineHeight, fontEmpInfo, fontEmpInfo, brushBlack, formatLeft);
                    y += infoLineHeight + 12f;

                    // 3. Table
                    float col1W = 38f;
                    float col3W = 105f;
                    float col2W = contentWidth - col1W - col3W;
                    float rowH = 19.5f;

                    // Header
                    DrawTableRow(g, "SI", "Description", "Amount (QR)", marginX, y, col1W, col2W, col3W, rowH,
                        fontTableHdr, fontTableHdr, fontTableHdr, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;

                    // Row 1: Basic Salary
                    DrawTableRow(g, "1.", "Basic Salary", FormatAmount(slip.BasicSalary), marginX, y, col1W, col2W, col3W, rowH,
                        fontTableRowBold, fontTableRowBold, fontTableRowBold, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;

                    // Category: Allowances
                    DrawCategoryRow(g, "Allowances", marginX, y, col1W, col2W, col3W, rowH,
                        fontTableSubHdr, brushBlack, brushCatBg, penBorder, formatLeft);
                    y += rowH;

                    // Allowances items
                    DrawTableRow(g, "2.", "House Rent", FormatAmount(slip.HouseRent), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "3.", "Transport", FormatAmount(slip.Transport), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "4.", "Position", FormatAmount(slip.Position), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "5.", "Mobile", FormatAmount(slip.Mobile), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "6.", "Medical", FormatAmount(slip.MedicalAllowance), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "7.", "Project", FormatAmount(slip.ProjectAllowance), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "8.", "Special", FormatAmount(slip.SpcAllowance), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "9.", "Other", FormatAmount(slip.OthrAlwnc), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "10.", "Incentive", FormatAmount(slip.Incentives), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "11.", "Overtime", FormatAmount(slip.Overtime), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "12.", "Other Adjustments (Payments)", FormatAmount(slip.OthrPmt), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;

                    // Row 13: Gross Salary
                    DrawTableRow(g, "13.", "Gross Salary", FormatAmount(slip.GrossSalary), marginX, y, col1W, col2W, col3W, rowH,
                        fontTableRowBold, fontTableRowBold, fontTableRowBold, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;

                    // Category: Deductions
                    DrawCategoryRow(g, "Deductions", marginX, y, col1W, col2W, col3W, rowH,
                        fontTableSubHdr, brushBlack, brushCatBg, penBorder, formatLeft);
                    y += rowH;

                    // Deductions items
                    DrawTableRow(g, "14.", "Salary Advance", FormatAmount(slip.SalaryAdvance), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "15.", "Staff Loan", FormatAmount(slip.StaffLoans), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "16.", "No Pay (Absent)", FormatAmount(slip.NoPay), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;
                    DrawTableRow(g, "17.", "Other Adjustment (Deductions)", FormatAmount(slip.OthrRcvryAmt), marginX, y, col1W, col2W, col3W, rowH, fontTableRow, fontTableRow, fontTableRow, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;

                    // Row 18: Total Deductions
                    DrawTableRow(g, "18.", "Total Deductions", FormatAmount(slip.TotalDeductions), marginX, y, col1W, col2W, col3W, rowH,
                        fontTableRowBold, fontTableRowBold, fontTableRowBold, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH;

                    // Row 19: Net Salary
                    DrawTableRow(g, "19.", "Net Salary", FormatAmount(slip.NtSlry), marginX, y, col1W, col2W, col3W, rowH,
                        fontTableRowBold, fontTableRowBold, fontTableRowBold, brushBlack, null, penBorder, formatCenter, formatLeft, formatRight);
                    y += rowH + 28f;

                    // 4. Footer Section
                    float footLeftW = contentWidth - 110f;
                    RectangleF footTextRect = new RectangleF(marginX, y, footLeftW, 16f);
                    g.DrawString("This is a system generated document. No signature required.", fontFootBold, brushBlack, footTextRect, formatLeft);

                    string printTime = !string.IsNullOrWhiteSpace(slip.PrntTime)
                        ? slip.PrntTime
                        : DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");
                    RectangleF timeRect = new RectangleF(marginX, y + 18f, footLeftW, 14f);
                    g.DrawString(printTime, fontFootItalic, brushBlack, timeRect, formatLeft);

                    // Rubber stamp on right
                    if (stampBitmap != null)
                    {
                        float sSize = 88f;
                        float sX = marginX + contentWidth - sSize - 4f;
                        float sY = y - 10f;
                        g.DrawImage(stampBitmap, sX, sY, sSize, sSize);
                    }
                }

                if (document.Pages.Count == 0)
                {
                    document.Pages.Add();
                }

                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    document.Save(ms);
                    bytes = ms.ToArray();
                }

                logoStream?.Dispose();
                stampStream?.Dispose();
                return bytes;
            }
        }

        private static string FormatAmount(decimal amt)
        {
            return amt % 1 == 0 ? amt.ToString("#,##0") : amt.ToString("#,##0.00");
        }

        private static void DrawInfoLine(PdfGraphics g, string label, string value, float x, float y, float labelW, float colonW, float valW, float h,
            PdfFont fLabel, PdfFont fVal, PdfBrush brush, PdfStringFormat fmt)
        {
            g.DrawString(label, fLabel, brush, new RectangleF(x, y, labelW, h), fmt);
            g.DrawString(":", fLabel, brush, new RectangleF(x + labelW, y, colonW, h), fmt);
            g.DrawString(value, fVal, brush, new RectangleF(x + labelW + colonW, y, valW, h), fmt);
        }

        private static void DrawTableRow(PdfGraphics g, string col1, string col2, string col3,
            float x, float y, float w1, float w2, float w3, float h,
            PdfFont f1, PdfFont f2, PdfFont f3,
            PdfBrush textBrush, PdfBrush bgBrush, PdfPen pen,
            PdfStringFormat fmt1, PdfStringFormat fmt2, PdfStringFormat fmt3)
        {
            if (bgBrush != null)
            {
                g.DrawRectangle(bgBrush, x, y, w1 + w2 + w3, h);
            }

            // Cell borders
            g.DrawRectangle(pen, x, y, w1, h);
            g.DrawRectangle(pen, x + w1, y, w2, h);
            g.DrawRectangle(pen, x + w1 + w2, y, w3, h);

            // Cell texts
            g.DrawString(col1, f1, textBrush, new RectangleF(x, y, w1, h), fmt1);
            g.DrawString(col2, f2, textBrush, new RectangleF(x + w1 + 6f, y, w2 - 12f, h), fmt2);
            g.DrawString(col3, f3, textBrush, new RectangleF(x + w1 + w2 + 6f, y, w3 - 12f, h), fmt3);
        }

        private static void DrawCategoryRow(PdfGraphics g, string catTitle,
            float x, float y, float w1, float w2, float w3, float h,
            PdfFont font, PdfBrush textBrush, PdfBrush bgBrush, PdfPen pen, PdfStringFormat fmt)
        {
            float totalW = w1 + w2 + w3;
            // Draw background spanning col2 and col3 or entire row
            g.DrawRectangle(bgBrush, x + w1, y, w2 + w3, h);

            // Borders
            g.DrawRectangle(pen, x, y, w1, h);
            g.DrawRectangle(pen, x + w1, y, w2 + w3, h);

            // Title in col2
            g.DrawString(catTitle, font, textBrush, new RectangleF(x + w1 + 6f, y, w2 + w3 - 12f, h), fmt);
        }
    }
}
