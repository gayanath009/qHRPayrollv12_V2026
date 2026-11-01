using System;
using System.IO;
using qHRM.SharedSysFuncs;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace qHRM.Areas.Payroll.Reports
{
    public static class SalarySheetSummaryPdfReport
    {
        public static byte[] GenerateReport(
            string companyName,
            int year,
            int month,
            decimal internalStaffNet,
            decimal outsourcedStaffNet,
            string webRootPath)
        {
            using (PdfDocument document = new PdfDocument())
            {
                // A4 Landscape: 841.89 x 595.27 points
                document.PageSettings.Size = PdfPageSize.A4;
                document.PageSettings.Orientation = PdfPageOrientation.Landscape;
                document.PageSettings.Margins.All = 0;

                PdfPage page = document.Pages.Add();
                PdfGraphics g = page.Graphics;

                float pageWidth = page.Size.Width;   // 841.89
                float pageHeight = page.Size.Height; // 595.27

                // 1. Logo
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
                    float lHeight = 34f;
                    float lWidth = lHeight * (203f / 59f);
                    g.DrawImage(logoBitmap, 50f, 36f, lWidth, lHeight);
                }

                // 2. Header Texts (Centered)
                PdfFont fontCompany = new PdfStandardFont(PdfFontFamily.Helvetica, 17, PdfFontStyle.Bold);
                PdfFont fontSubtitle = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                PdfBrush brushText = PdfBrushes.Black;
                PdfStringFormat formatCenter = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Center,
                    LineAlignment = PdfVerticalAlignment.Middle
                };

                string compTitle = !string.IsNullOrWhiteSpace(companyName) ? companyName : "Qatar Computer Services WLL";
                string monthName = (month >= 1 && month <= 12)
                    ? new DateTime(year > 0 ? year : 2026, month, 1).ToString("MMMM")
                    : "December";
                string reportSubtitle = $"Monthly Salary Sheet Summary for the Month of  {monthName} - {year}";

                g.DrawString(compTitle, fontCompany, brushText, new RectangleF(0, 36f, pageWidth, 24f), formatCenter);
                g.DrawString(reportSubtitle, fontSubtitle, brushText, new RectangleF(0, 64f, pageWidth, 20f), formatCenter);

                // 3. Summary Table
                float tableW = 420f;
                float tableX = (pageWidth - tableW) / 2f;
                float tableY = 165f;
                float rowH = 26f;
                float col1W = 265f;
                float col2W = tableW - col1W; // 155f

                PdfPen penTable = new PdfPen(new PdfColor(0, 0, 0), 1.0f);
                PdfFont fontRowRegular = new PdfStandardFont(PdfFontFamily.Helvetica, 10.5f, PdfFontStyle.Regular);
                PdfFont fontRowBold = new PdfStandardFont(PdfFontFamily.Helvetica, 10.5f, PdfFontStyle.Bold);
                PdfFont fontTotalBold = new PdfStandardFont(PdfFontFamily.Helvetica, 11f, PdfFontStyle.Bold);

                PdfStringFormat formatCellLeft = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Left,
                    LineAlignment = PdfVerticalAlignment.Middle
                };
                PdfStringFormat formatCellRight = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Right,
                    LineAlignment = PdfVerticalAlignment.Middle
                };

                decimal totalNet = internalStaffNet + outsourcedStaffNet;

                // Row 1: Internal Staff
                DrawTableRow(g, tableX, tableY, col1W, col2W, rowH,
                    "Internal Staff Total Net Salary", FormatAmount(internalStaffNet),
                    fontRowRegular, fontRowBold, brushText, null, penTable, formatCellLeft, formatCellRight);

                // Row 2: Outsourced Staff
                DrawTableRow(g, tableX, tableY + rowH, col1W, col2W, rowH,
                    "Outsourced Staff Total Net Salary", FormatAmount(outsourcedStaffNet),
                    fontRowRegular, fontRowBold, brushText, null, penTable, formatCellLeft, formatCellRight);

                // Row 3: Total Net Salary (QR)
                PdfBrush brushTotalBg = new PdfSolidBrush(new PdfColor(250, 250, 250));
                DrawTableRow(g, tableX, tableY + (rowH * 2), col1W, col2W, rowH,
                    "Total Net Salary (QR)", FormatAmount(totalNet),
                    fontRowBold, fontTotalBold, brushText, brushTotalBg, penTable, formatCellLeft, formatCellRight);

                // 4. Amount in Words
                string amountInWords;
                try
                {
                    amountInWords = "Qatari Riyal " + (totalNet > 0 ? NumberToWords.convert(Convert.ToInt32(totalNet)) : "Zero");
                }
                catch
                {
                    amountInWords = $"Qatari Riyal {FormatAmount(totalNet)}";
                }

                float wordsY = tableY + (rowH * 3) + 45f;
                PdfFont fontWords = new PdfStandardFont(PdfFontFamily.Helvetica, 10.5f, PdfFontStyle.Bold);
                g.DrawString(amountInWords, fontWords, brushText, new RectangleF(40f, wordsY, pageWidth - 80f, 22f), formatCenter);

                // 5. Signatures Section
                float sigY = 475f;
                float sigLineWidth = 130f;
                float marginSig = 60f;
                float availableSigW = pageWidth - (marginSig * 2);
                float sigSpacing = (availableSigW - (4 * sigLineWidth)) / 3f;

                string[] sigTitles = new[] { "HR Officer", "Manager Accounts", "Chief Executive", "Managing Director" };
                PdfFont fontSig = new PdfStandardFont(PdfFontFamily.Helvetica, 9.5f, PdfFontStyle.Bold | PdfFontStyle.Underline);
                PdfPen penDash = new PdfPen(new PdfColor(0, 0, 0), 1.0f) { DashStyle = PdfDashStyle.Dash };

                for (int i = 0; i < sigTitles.Length; i++)
                {
                    float sigX = marginSig + i * (sigLineWidth + sigSpacing);

                    // Dashed line
                    g.DrawLine(penDash, sigX, sigY, sigX + sigLineWidth, sigY);

                    // Title below dashed line
                    RectangleF sigTextRect = new RectangleF(sigX - 15f, sigY + 6f, sigLineWidth + 30f, 18f);
                    g.DrawString(sigTitles[i], fontSig, brushText, sigTextRect, formatCenter);
                }

                // 6. Page Footer
                float footY = 542f;
                PdfFont fontFoot = new PdfStandardFont(PdfFontFamily.Helvetica, 8.5f, PdfFontStyle.Regular);
                PdfStringFormat formatFootLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
                PdfStringFormat formatFootRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };

                string printDate = $"Printed on:  {DateTime.Now:dd MMM yyyy}";
                g.DrawString(printDate, fontFoot, brushText, new RectangleF(50f, footY, 300f, 15f), formatFootLeft);
                g.DrawString("Page 1 of 1", fontFoot, brushText, new RectangleF(pageWidth - 350f, footY, 300f, 15f), formatFootRight);

                // Bottom line
                PdfPen penBottom = new PdfPen(new PdfColor(0, 0, 0), 1.5f);
                g.DrawLine(penBottom, 0, pageHeight - 10f, pageWidth, pageHeight - 10f);

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

        private static string FormatAmount(decimal amt)
        {
            return amt % 1 == 0 ? amt.ToString("#,##0") : amt.ToString("#,##0.00");
        }

        private static void DrawTableRow(PdfGraphics g, float x, float y, float w1, float w2, float h,
            string col1, string col2,
            PdfFont f1, PdfFont f2,
            PdfBrush textBrush, PdfBrush bgBrush, PdfPen pen,
            PdfStringFormat fmt1, PdfStringFormat fmt2)
        {
            if (bgBrush != null)
            {
                g.DrawRectangle(bgBrush, x, y, w1 + w2, h);
            }

            // Cell borders
            g.DrawRectangle(pen, x, y, w1, h);
            g.DrawRectangle(pen, x + w1, y, w2, h);

            // Cell contents
            g.DrawString(col1, f1, textBrush, new RectangleF(x + 10f, y, w1 - 20f, h), fmt1);
            g.DrawString(col2, f2, textBrush, new RectangleF(x + w1 + 10f, y, w2 - 22f, h), fmt2);
        }
    }
}
