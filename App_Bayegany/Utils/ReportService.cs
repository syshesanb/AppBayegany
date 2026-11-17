using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using BayeganyApp.Models;

namespace BayeganyApp.Utils
{
    public static class ReportService
    {
        /// <summary>
        /// صدور فایل اکسل/CSV استاندارد با انکودینگ UTF-8 BOM جهت باز شدن بی‌نقص در Microsoft Excel
        /// </summary>
        public static void ExportToCsv(List<DocumentItem> items, string filePath)
        {
            var sb = new StringBuilder();

            // ردیف هدر
            sb.AppendLine("ردیف,کد بایگانی,شماره مدرک,تاریخ مدرک,نوع مدرک,موضوع اصلی,موضوع فرعی ۱,موضوع فرعی ۲,شخص اصلی,شخص فرعی ۱,شخص فرعی ۲,تعداد صفحات,یادداشت‌ها");

            for (int i = 0; i < items.Count; i++)
            {
                var doc = items[i];
                string row = string.Join(",",
                    i + 1,
                    EscapeCsv(doc.CodBayegany),
                    EscapeCsv(doc.DocNumber ?? ""),
                    EscapeCsv(doc.DocDate ?? ""),
                    EscapeCsv(doc.DocType ?? ""),
                    EscapeCsv(doc.MainSubjectTitle),
                    EscapeCsv(doc.SubSubject1Title),
                    EscapeCsv(doc.SubSubject2Title),
                    EscapeCsv(doc.MainPersonName),
                    EscapeCsv(doc.SubPerson1Name),
                    EscapeCsv(doc.SubPerson2Name),
                    doc.PageCount,
                    EscapeCsv(doc.Notes ?? "")
                );
                sb.AppendLine(row);
            }

            // نوشتن با UTF-8 BOM
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "\"\"";
            string clean = text.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return $"\"{clean}\"";
        }

        /// <summary>
        /// تولید گزارش چاپی و رسمی HTML برای مشاهده و پرینت مستقیم (Save as PDF) با قالب اداری فاخر
        /// </summary>
        public static string GenerateHtmlReport(
            List<DocumentItem> items,
            string reportTitle,
            string filterDescription,
            int totalPages,
            string minDate,
            string maxDate)
        {
            string dateNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"fa\" dir=\"rtl\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"utf-8\">");
            sb.AppendLine($"  <title>{reportTitle}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: Tahoma, 'Segoe UI', Arial, sans-serif; margin: 20px; color: #1e293b; background: #fff; }");
            sb.AppendLine("    .header { text-align: center; border-bottom: 2px solid #1e3a8a; padding-bottom: 12px; margin-bottom: 16px; }");
            sb.AppendLine("    .title { font-size: 20px; font-weight: bold; color: #1e3a8a; margin: 0; }");
            sb.AppendLine("    .subtitle { font-size: 13px; color: #64748b; margin-top: 5px; }");
            sb.AppendLine("    .kpi-container { display: flex; justify-content: space-between; margin-bottom: 16px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; }");
            sb.AppendLine("    .kpi-box { text-align: center; flex: 1; }");
            sb.AppendLine("    .kpi-value { font-size: 16px; font-weight: bold; color: #1e3a8a; }");
            sb.AppendLine("    .kpi-label { font-size: 12px; color: #64748b; margin-top: 3px; }");
            sb.AppendLine("    .filter-info { font-size: 12.5px; background: #eff6ff; border-right: 4px solid #3b82f6; padding: 8px 12px; margin-bottom: 16px; border-radius: 0 4px 4px 0; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 11.5px; margin-bottom: 20px; }");
            sb.AppendLine("    th { background-color: #1e3a8a; color: #fff; font-weight: bold; padding: 8px 6px; border: 1px solid #1e3a8a; text-align: center; }");
            sb.AppendLine("    td { padding: 6px; border: 1px solid #cbd5e1; text-align: center; vertical-align: middle; }");
            sb.AppendLine("    tr:nth-child(even) { background-color: #f8fafc; }");
            sb.AppendLine("    .footer { text-align: center; font-size: 11px; color: #94a3b8; margin-top: 30px; border-top: 1px solid #e2e8f0; padding-top: 8px; }");
            sb.AppendLine("    @media print { body { margin: 10mm; } .no-print { display: none; } }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // دکمه پرینت در نمای وب
            sb.AppendLine("  <div class=\"no-print\" style=\"text-align: left; margin-bottom: 10px;\">");
            sb.AppendLine("    <button onclick=\"window.print()\" style=\"padding: 8px 16px; font-size: 13px; font-family: Tahoma; background: #16a34a; color: white; border: none; border-radius: 4px; cursor: pointer;\">🖨️ چاپ این گزارش / ذخیره به صورت PDF</button>");
            sb.AppendLine("  </div>");

            // سربرگ گزارش
            sb.AppendLine("  <div class=\"header\">");
            sb.AppendLine($"    <div class=\"title\">{reportTitle}</div>");
            sb.AppendLine($"    <div class=\"subtitle\">سامانه هوشمند بایگانی اسناد و مدارک | تاریخ صدور گزارش: {dateNow}</div>");
            sb.AppendLine("  </div>");

            // کادر شرایط و فیلترهای اعمال‌شده
            sb.AppendLine("  <div class=\"filter-info\">");
            sb.AppendLine($"    <b>شرایط و فیلترهای گزارش:</b> {filterDescription}");
            sb.AppendLine("  </div>");

            // کارت‌های KPI
            sb.AppendLine("  <div class=\"kpi-container\">");
            sb.AppendLine($"    <div class=\"kpi-box\"><div class=\"kpi-value\">{items.Count}</div><div class=\"kpi-label\">تعداد اسناد یافت‌شده</div></div>");
            sb.AppendLine($"    <div class=\"kpi-box\"><div class=\"kpi-value\">{totalPages}</div><div class=\"kpi-label\">مجموع برگه‌ها و صفحات</div></div>");
            sb.AppendLine($"    <div class=\"kpi-box\"><div class=\"kpi-value\">{(string.IsNullOrEmpty(minDate) ? "-" : minDate)}</div><div class=\"kpi-label\">تاریخ قدیمی‌ترین سند</div></div>");
            sb.AppendLine($"    <div class=\"kpi-box\"><div class=\"kpi-value\">{(string.IsNullOrEmpty(maxDate) ? "-" : maxDate)}</div><div class=\"kpi-label\">تاریخ جدیدترین سند</div></div>");
            sb.AppendLine("  </div>");

            // جدول اطلاعات
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead>");
            sb.AppendLine("      <tr>");
            sb.AppendLine("        <th style=\"width: 35px;\">ردیف</th>");
            sb.AppendLine("        <th style=\"width: 90px;\">کد بایگانی</th>");
            sb.AppendLine("        <th style=\"width: 80px;\">شماره مدرک</th>");
            sb.AppendLine("        <th style=\"width: 80px;\">تاریخ سند</th>");
            sb.AppendLine("        <th style=\"width: 75px;\">نوع مدرک</th>");
            sb.AppendLine("        <th>موضوعات (اصلی > فرعی ۱ > فرعی ۲)</th>");
            sb.AppendLine("        <th>اشخاص مرتبط (اصلی / فرعی)</th>");
            sb.AppendLine("        <th style=\"width: 45px;\">برگه‌ها</th>");
            sb.AppendLine("        <th>یادداشت‌ها و خلاصه</th>");
            sb.AppendLine("      </tr>");
            sb.AppendLine("    </thead>");
            sb.AppendLine("    <tbody>");

            for (int i = 0; i < items.Count; i++)
            {
                var doc = items[i];
                sb.AppendLine("      <tr>");
                sb.AppendLine($"        <td>{i + 1}</td>");
                sb.AppendLine($"        <td style=\"font-weight: bold; color: #1e3a8a;\">{doc.CodBayegany}</td>");
                sb.AppendLine($"        <td>{doc.DocNumber}</td>");
                sb.AppendLine($"        <td>{doc.DocDate}</td>");
                sb.AppendLine($"        <td>{doc.DocType}</td>");
                sb.AppendLine($"        <td style=\"text-align: right;\">{doc.FullSubjectPath}</td>");
                sb.AppendLine($"        <td style=\"text-align: right;\">{doc.FullPersonsList}</td>");
                sb.AppendLine($"        <td>{doc.PageCount}</td>");
                sb.AppendLine($"        <td style=\"text-align: right;\">{doc.Notes}</td>");
                sb.AppendLine("      </tr>");
            }

            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");

            // پاورقی
            sb.AppendLine("  <div class=\"footer\">");
            sb.AppendLine("    این گزارش توسط موتور گزارش‌گیری ماتریسی نرم‌افزار بایگانی تولید شده است.");
            sb.AppendLine("  </div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public static void OpenHtmlReportInBrowser(string htmlContent, string title = "Report")
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"Bayegany_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempFile, htmlContent, Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
    }
}
