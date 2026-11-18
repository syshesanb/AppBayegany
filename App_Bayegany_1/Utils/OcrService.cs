using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace BayeganyApp.Utils
{
    public static class OcrService
    {
        public static string ExtractTextFromImage(byte[]? imageBytes, string? fileName = null, string? existingOcr = null)
        {
            // ۱. بررسی تطبیق نام فایل با دیتابیس Tabdil_Aks.db جهت استخراج دقیق داده‌های چک/سند
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string tabdilDbPath = @"D:\Projects_AI\tahgigat\Khodam\Chek\Tabdil_Aks_to_Data\Tabdil_Aks.db";
                if (File.Exists(tabdilDbPath))
                {
                    try
                    {
                        using var conn = new SqliteConnection($"Data Source={tabdilDbPath};Mode=ReadOnly;");
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            SELECT c.ArchiveCode, c.SerialNumber, c.CheckNumber, c.CheckIndex, c.CheckDate, 
                                   c.Amount, b.BankName, c.BranchName, c.BranchCode, c.AccountNumber, 
                                   c.Iban, p.FullName, c.Payee, c.Reason
                            FROM Checks c
                            LEFT JOIN Banks b ON c.BankId = b.Id
                            LEFT JOIN Persons p ON c.AccountHolderPersonId = p.Id
                            WHERE c.ImagePath = $fn OR c.ImagePath LIKE $likeFn
                            LIMIT 1;";
                        cmd.Parameters.AddWithValue("$fn", fileName);
                        cmd.Parameters.AddWithValue("$likeFn", "%" + Path.GetFileName(fileName) + "%");

                        using var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("[اطلاعات و متن استخراج‌شده از سند / چک]");

                            string? code = reader.IsDBNull(0) ? null : reader.GetString(0);
                            string? ser = reader.IsDBNull(1) ? null : reader.GetString(1);
                            string? chkNo = reader.IsDBNull(2) ? null : reader.GetString(2);
                            string? idx = reader.IsDBNull(3) ? null : reader.GetString(3);
                            string? date = reader.IsDBNull(4) ? null : reader.GetString(4);
                            long? amt = reader.IsDBNull(5) ? null : reader.GetInt64(5);
                            string? bank = reader.IsDBNull(6) ? null : reader.GetString(6);
                            string? brName = reader.IsDBNull(7) ? null : reader.GetString(7);
                            string? brCode = reader.IsDBNull(8) ? null : reader.GetString(8);
                            string? acc = reader.IsDBNull(9) ? null : reader.GetString(9);
                            string? iban = reader.IsDBNull(10) ? null : reader.GetString(10);
                            string? person = reader.IsDBNull(11) ? null : reader.GetString(11);
                            string? payee = reader.IsDBNull(12) ? null : reader.GetString(12);
                            string? reason = reader.IsDBNull(13) ? null : reader.GetString(13);

                            if (!string.IsNullOrWhiteSpace(code)) sb.AppendLine($"• کد بایگانی: {code}");
                            if (!string.IsNullOrWhiteSpace(chkNo)) sb.AppendLine($"• شماره چک: {chkNo}");
                            if (!string.IsNullOrWhiteSpace(ser) || !string.IsNullOrWhiteSpace(idx)) sb.AppendLine($"• سری و اندیس: {ser} / {idx}");
                            if (!string.IsNullOrWhiteSpace(date)) sb.AppendLine($"• تاریخ سررسید: {date}");
                            if (amt.HasValue && amt.Value > 0)
                            {
                                long toman = amt.Value / 10;
                                sb.AppendLine($"• مبلغ: {amt.Value:N0} ریال (معادل {toman:N0} تومان)");
                            }
                            if (!string.IsNullOrWhiteSpace(bank)) sb.AppendLine($"• نام بانک: {bank}");
                            if (!string.IsNullOrWhiteSpace(brName) || !string.IsNullOrWhiteSpace(brCode)) sb.AppendLine($"• شعبه: {brName} (کد: {brCode ?? "-"})");
                            if (!string.IsNullOrWhiteSpace(person)) sb.AppendLine($"• صاحب حساب: {person}");
                            if (!string.IsNullOrWhiteSpace(acc)) sb.AppendLine($"• شماره حساب: {acc}");
                            if (!string.IsNullOrWhiteSpace(iban)) sb.AppendLine($"• شماره شبا: {iban}");
                            if (!string.IsNullOrWhiteSpace(payee)) sb.AppendLine($"• در وجه: {payee}");
                            if (!string.IsNullOrWhiteSpace(reason)) sb.AppendLine($"• بابت / توضیحات: {reason}");

                            return sb.ToString().Trim();
                        }
                    }
                    catch { }
                }
            }

            // ۲. اگر متن معتبر OCR در دیتابیس وجود داشت
            if (!string.IsNullOrWhiteSpace(existingOcr) && !existingOcr.Contains("بدین‌وسیله به اطلاع می‌رساند"))
            {
                return existingOcr.Trim();
            }

            // ۳. اجرای Tesseract OCR در صورتی که تصویر جدید باشد و در متادیتا نباشد
            if (imageBytes != null && imageBytes.Length > 0)
            {
                string tessExe = @"C:\Program Files\Asef\runtime\tesseract\tesseract.exe";
                string tessData = @"C:\Program Files\Asef\runtime\tesseract\tessdata";
                if (File.Exists(tessExe))
                {
                    string tempImg = Path.Combine(Path.GetTempPath(), $"bayegany_ocr_{Guid.NewGuid():N}.jpg");
                    try
                    {
                        File.WriteAllBytes(tempImg, imageBytes);
                        var psi = new ProcessStartInfo
                        {
                            FileName = tessExe,
                            Arguments = $"\"{tempImg}\" stdout -l fas+ara+eng --tessdata-dir \"{tessData}\" --psm 6",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8
                        };

                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            string ocrOut = proc.StandardOutput.ReadToEnd();
                            proc.WaitForExit(10000);

                            if (!string.IsNullOrWhiteSpace(ocrOut) && ocrOut.Trim().Length > 5)
                            {
                                return "[متن استخراج‌شده توسط موتور OCR Tesseract]:\n" + ocrOut.Trim();
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        try { if (File.Exists(tempImg)) File.Delete(tempImg); } catch { }
                    }
                }
            }

            return "[متن واضحی از این تصویر توسط OCR تشخیص داده نشد. شما می‌توانید متن را دستی در این کادر وارد نمایید.]";
        }
    }
}
