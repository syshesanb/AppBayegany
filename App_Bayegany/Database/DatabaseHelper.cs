using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using BayeganyApp.Models;
using BayeganyApp.Utils;

namespace BayeganyApp.Database
{
    public static class DatabaseHelper
    {
        private static string _dbFilePath = string.Empty;

        public static string DbFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_dbFilePath))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string? dir = baseDir;
                    string? projectRoot = null;
                    for (int i = 0; i < 5 && dir != null; i++)
                    {
                        if (File.Exists(Path.Combine(dir, "BayeganyApp.csproj")))
                        {
                            projectRoot = dir;
                            break;
                        }
                        dir = Path.GetDirectoryName(dir);
                    }

                    string dataDir;
                    if (projectRoot != null)
                    {
                        dataDir = Path.Combine(projectRoot, "Data");
                    }
                    else
                    {
                        dataDir = Path.Combine(baseDir, "Data");
                    }

                    if (!Directory.Exists(dataDir))
                    {
                        Directory.CreateDirectory(dataDir);
                    }

                    _dbFilePath = Path.Combine(dataDir, "bayegany.db");

                    // اگر دیتابیس اصلی نبود، از روی دیتابیس خالی (که از گیت پول شده) نسخه اولیه ساخته شود
                    string emptyDb = Path.Combine(dataDir, "bayegany_empty.db");
                    if (!File.Exists(_dbFilePath) && File.Exists(emptyDb))
                    {
                        try { File.Copy(emptyDb, _dbFilePath, true); } catch { }
                    }

                    string altDb = Path.Combine(baseDir, "Data", "bayegany.db");
                    if (!File.Exists(_dbFilePath) && File.Exists(altDb))
                    {
                        try { File.Copy(altDb, _dbFilePath, true); } catch { }
                    }
                }
                return _dbFilePath;
            }
        }

        private static string ConnectionString => $"Data Source={DbFilePath};Cache=Shared;";

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
            cmd.ExecuteNonQuery();
            return conn;
        }

        public static void InitializeDatabase()
        {
            using var conn = GetConnection();
            using var trans = conn.BeginTransaction();

            // ۱. جداول سلسله‌مراتبی موضوعات
            ExecuteNonQuery(conn, @"
                CREATE TABLE IF NOT EXISTS MainSubjects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL UNIQUE,
                    Code TEXT,
                    Description TEXT
                );

                CREATE TABLE IF NOT EXISTS SubSubjects1 (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MainSubjectId INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Code TEXT,
                    FOREIGN KEY(MainSubjectId) REFERENCES MainSubjects(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS SubSubjects2 (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SubSubject1Id INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Code TEXT,
                    FOREIGN KEY(SubSubject1Id) REFERENCES SubSubjects1(Id) ON DELETE CASCADE
                );
            ");

            // ۲. جدول مرجع اشخاص حقیقی و حقوقی
            ExecuteNonQuery(conn, @"
                CREATE TABLE IF NOT EXISTS Persons (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    PersonType TEXT,
                    NationalCode TEXT,
                    ContactInfo TEXT,
                    Notes TEXT
                );
            ");

            // ۳. جدول اسناد (شامل فیلد کلیدی CodBayegany)
            ExecuteNonQuery(conn, @"
                CREATE TABLE IF NOT EXISTS Documents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CodBayegany TEXT NOT NULL,
                    DocNumber TEXT,
                    DocDate TEXT,
                    RegDate TEXT,
                    DocType TEXT,
                    MainSubjectId INTEGER,
                    SubSubject1Id INTEGER,
                    SubSubject2Id INTEGER,
                    MainPersonId INTEGER,
                    SubPerson1Id INTEGER,
                    SubPerson2Id INTEGER,
                    Notes TEXT,
                    ColorTag TEXT,
                    FOREIGN KEY(MainSubjectId) REFERENCES MainSubjects(Id) ON DELETE SET NULL,
                    FOREIGN KEY(SubSubject1Id) REFERENCES SubSubjects1(Id) ON DELETE SET NULL,
                    FOREIGN KEY(SubSubject2Id) REFERENCES SubSubjects2(Id) ON DELETE SET NULL,
                    FOREIGN KEY(MainPersonId) REFERENCES Persons(Id) ON DELETE SET NULL,
                    FOREIGN KEY(SubPerson1Id) REFERENCES Persons(Id) ON DELETE SET NULL,
                    FOREIGN KEY(SubPerson2Id) REFERENCES Persons(Id) ON DELETE SET NULL
                );
            ");

            // ۴. ایندکس‌های ترکیبی گزارش‌گیری ماتریسی
            ExecuteNonQuery(conn, @"
                CREATE INDEX IF NOT EXISTS idx_doc_subjects ON Documents(MainSubjectId, SubSubject1Id, SubSubject2Id);
                CREATE INDEX IF NOT EXISTS idx_doc_persons ON Documents(MainPersonId, SubPerson1Id, SubPerson2Id);
                CREATE INDEX IF NOT EXISTS idx_doc_matrix ON Documents(MainSubjectId, MainPersonId, DocDate);
                CREATE INDEX IF NOT EXISTS idx_doc_codbayegany ON Documents(CodBayegany);
            ");

            // ۵. جدول ذخیره مستقیم بایت‌های تصویر (BLOB) و متن OCR
            ExecuteNonQuery(conn, @"
                CREATE TABLE IF NOT EXISTS DocumentPages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DocumentId INTEGER NOT NULL,
                    PageNumber INTEGER NOT NULL DEFAULT 1,
                    ImageBlob BLOB NOT NULL,
                    ThumbnailBlob BLOB NOT NULL,
                    PageOcrText TEXT,
                    FileName TEXT,
                    FOREIGN KEY(DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE
                );
            ");

            // ۶. ایجاد View جامع برای واکشی اسامی و اتصال به لایه نمایش
            ExecuteNonQuery(conn, @"
                DROP VIEW IF EXISTS vw_DocumentsDetails;
                CREATE VIEW vw_DocumentsDetails AS
                SELECT 
                    d.Id,
                    d.CodBayegany,
                    d.DocNumber,
                    d.DocDate,
                    d.RegDate,
                    d.DocType,
                    d.MainSubjectId,
                    d.SubSubject1Id,
                    d.SubSubject2Id,
                    d.MainPersonId,
                    d.SubPerson1Id,
                    d.SubPerson2Id,
                    d.Notes,
                    d.ColorTag,
                    COALESCE(ms.Title, '-') AS MainSubjectTitle,
                    COALESCE(ss1.Title, '-') AS SubSubject1Title,
                    COALESCE(ss2.Title, '-') AS SubSubject2Title,
                    COALESCE(mp.FullName, '-') AS MainPersonName,
                    COALESCE(sp1.FullName, '-') AS SubPerson1Name,
                    COALESCE(sp2.FullName, '-') AS SubPerson2Name,
                    (SELECT COUNT(*) FROM DocumentPages p WHERE p.DocumentId = d.Id) AS PageCount,
                    (SELECT p.ThumbnailBlob FROM DocumentPages p WHERE p.DocumentId = d.Id ORDER BY p.PageNumber ASC LIMIT 1) AS ThumbnailBlob,
                    (SELECT GROUP_CONCAT(p.PageOcrText, ' ') FROM DocumentPages p WHERE p.DocumentId = d.Id) AS FullOcrText
                FROM Documents d
                LEFT JOIN MainSubjects ms ON d.MainSubjectId = ms.Id
                LEFT JOIN SubSubjects1 ss1 ON d.SubSubject1Id = ss1.Id
                LEFT JOIN SubSubjects2 ss2 ON d.SubSubject2Id = ss2.Id
                LEFT JOIN Persons mp ON d.MainPersonId = mp.Id
                LEFT JOIN Persons sp1 ON d.SubPerson1Id = sp1.Id
                LEFT JOIN Persons sp2 ON d.SubPerson2Id = sp2.Id;
            ");

            // ۷. جدول کاربران و سطوح دسترسی
            ExecuteNonQuery(conn, @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    CanAddDoc INTEGER NOT NULL DEFAULT 1,
                    CanEditDoc INTEGER NOT NULL DEFAULT 1,
                    CanDeleteDoc INTEGER NOT NULL DEFAULT 0,
                    CanManagePersons INTEGER NOT NULL DEFAULT 1,
                    CanManageSubjects INTEGER NOT NULL DEFAULT 1,
                    CanViewReports INTEGER NOT NULL DEFAULT 1,
                    CanBackupRestore INTEGER NOT NULL DEFAULT 0,
                    CanManageUsers INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT
                );
            ");

            trans.Commit();

            // اطمینان از وجود حداقل یک کاربر مدیر در اولین راه‌اندازی
            EnsureDefaultAdmin();

            // توجه: درج خودکار داده‌های پیش‌فرض غیرفعال شد تا دیتابیس همیشه کاملاً خام و بدون دیتای اضافی باقی بماند
            // SeedSampleDataIfEmpty(conn);
        }

        private static void ExecuteNonQuery(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        // --- مدیریت موضوعات ---

        public static List<MainSubjectItem> GetAllMainSubjects()
        {
            var list = new List<MainSubjectItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Code, Description FROM MainSubjects ORDER BY Title ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MainSubjectItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Code = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return list;
        }

        public static List<SubSubject1Item> GetSubSubjects1(int mainSubjectId)
        {
            var list = new List<SubSubject1Item>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, MainSubjectId, Title, Code FROM SubSubjects1 WHERE MainSubjectId = @mId ORDER BY Title ASC;";
            cmd.Parameters.AddWithValue("@mId", mainSubjectId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SubSubject1Item
                {
                    Id = reader.GetInt32(0),
                    MainSubjectId = reader.GetInt32(1),
                    Title = reader.GetString(2),
                    Code = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return list;
        }

        public static List<SubSubject2Item> GetSubSubjects2(int subSubject1Id)
        {
            var list = new List<SubSubject2Item>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, SubSubject1Id, Title, Code FROM SubSubjects2 WHERE SubSubject1Id = @sId ORDER BY Title ASC;";
            cmd.Parameters.AddWithValue("@sId", subSubject1Id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SubSubject2Item
                {
                    Id = reader.GetInt32(0),
                    SubSubject1Id = reader.GetInt32(1),
                    Title = reader.GetString(2),
                    Code = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return list;
        }

        public static int AddMainSubject(string title, string? code, string? desc)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO MainSubjects (Title, Code, Description) VALUES (@t, @c, @d); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@t", title.Trim());
            cmd.Parameters.AddWithValue("@c", (object?)code?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@d", (object?)desc?.Trim() ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static int AddSubSubject1(int mainSubjectId, string title, string? code)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO SubSubjects1 (MainSubjectId, Title, Code) VALUES (@mId, @t, @c); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@mId", mainSubjectId);
            cmd.Parameters.AddWithValue("@t", title.Trim());
            cmd.Parameters.AddWithValue("@c", (object?)code?.Trim() ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static int AddSubSubject2(int subSubject1Id, string title, string? code)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO SubSubjects2 (SubSubject1Id, Title, Code) VALUES (@sId, @t, @c); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@sId", subSubject1Id);
            cmd.Parameters.AddWithValue("@t", title.Trim());
            cmd.Parameters.AddWithValue("@c", (object?)code?.Trim() ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateMainSubject(int id, string title)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE MainSubjects SET Title = @t WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@t", title.Trim());
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMainSubject(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM SubSubjects2 WHERE SubSubject1Id IN (SELECT Id FROM SubSubjects1 WHERE MainSubjectId = @id);
                DELETE FROM SubSubjects1 WHERE MainSubjectId = @id;
                DELETE FROM MainSubjects WHERE Id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateSubSubject1(int id, string title)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE SubSubjects1 SET Title = @t WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@t", title.Trim());
            cmd.ExecuteNonQuery();
        }

        public static void DeleteSubSubject1(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM SubSubjects2 WHERE SubSubject1Id = @id;
                DELETE FROM SubSubjects1 WHERE Id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateSubSubject2(int id, string title)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE SubSubjects2 SET Title = @t WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@t", title.Trim());
            cmd.ExecuteNonQuery();
        }

        public static void DeleteSubSubject2(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SubSubjects2 WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // --- مدیریت اشخاص ---

        public static List<PersonItem> GetAllPersons()
        {
            var list = new List<PersonItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FullName, PersonType, NationalCode, ContactInfo, Notes FROM Persons ORDER BY FullName ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PersonItem
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    PersonType = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NationalCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ContactInfo = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Notes = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public static int AddPerson(string fullName, string? personType, string? nationalCode, string? contactInfo, string? notes)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Persons (FullName, PersonType, NationalCode, ContactInfo, Notes) 
                                VALUES (@fn, @pt, @nc, @ci, @nt); 
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@fn", fullName.Trim());
            cmd.Parameters.AddWithValue("@pt", (object?)personType?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nc", (object?)nationalCode?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ci", (object?)contactInfo?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nt", (object?)notes?.Trim() ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdatePerson(int id, string fullName, string? personType, string? nationalCode, string? contactInfo, string? notes)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Persons 
                                SET FullName = @fn, 
                                    PersonType = @pt, 
                                    NationalCode = @nc, 
                                    ContactInfo = @ci, 
                                    Notes = @nt 
                                WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@fn", fullName.Trim());
            cmd.Parameters.AddWithValue("@pt", (object?)personType?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nc", (object?)nationalCode?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ci", (object?)contactInfo?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nt", (object?)notes?.Trim() ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public static void DeletePerson(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Persons WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // --- مدیریت اسناد و برگه‌ها ---

        public static List<DocumentItem> GetDocuments(
            string? search = null,
            int? mainSubjectId = null,
            int? subSubject1Id = null,
            int? subSubject2Id = null,
            int? mainPersonId = null,
            int? subPerson1Id = null,
            int? subPerson2Id = null,
            string? fromDate = null,
            string? toDate = null,
            string? docType = null,
            string? ocrSearch = null)
        {
            var list = new List<DocumentItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            var whereClauses = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string sEng = PersianTextHelper.ToEnglishDigits(search.Trim());
                string sFa = PersianTextHelper.ToPersianDigits(search.Trim());

                whereClauses.Add(@"(
                    CodBayegany LIKE @qEng OR CodBayegany LIKE @qFa OR
                    DocNumber LIKE @qEng OR DocNumber LIKE @qFa OR
                    Notes LIKE @qEng OR Notes LIKE @qFa OR
                    FullOcrText LIKE @qEng OR FullOcrText LIKE @qFa OR
                    MainPersonName LIKE @qEng OR MainPersonName LIKE @qFa OR
                    MainSubjectTitle LIKE @qEng OR MainSubjectTitle LIKE @qFa
                )");
                cmd.Parameters.AddWithValue("@qEng", $"%{sEng}%");
                cmd.Parameters.AddWithValue("@qFa", $"%{sFa}%");
            }

            if (!string.IsNullOrWhiteSpace(ocrSearch))
            {
                string ocrEng = PersianTextHelper.ToEnglishDigits(ocrSearch.Trim());
                string ocrFa = PersianTextHelper.ToPersianDigits(ocrSearch.Trim());

                whereClauses.Add("(FullOcrText LIKE @ocrEng OR FullOcrText LIKE @ocrFa)");
                cmd.Parameters.AddWithValue("@ocrEng", $"%{ocrEng}%");
                cmd.Parameters.AddWithValue("@ocrFa", $"%{ocrFa}%");
            }

            if (mainSubjectId.HasValue && mainSubjectId.Value > 0)
            {
                whereClauses.Add("MainSubjectId = @msId");
                cmd.Parameters.AddWithValue("@msId", mainSubjectId.Value);
            }

            if (subSubject1Id.HasValue && subSubject1Id.Value > 0)
            {
                whereClauses.Add("SubSubject1Id = @ss1Id");
                cmd.Parameters.AddWithValue("@ss1Id", subSubject1Id.Value);
            }

            if (subSubject2Id.HasValue && subSubject2Id.Value > 0)
            {
                whereClauses.Add("SubSubject2Id = @ss2Id");
                cmd.Parameters.AddWithValue("@ss2Id", subSubject2Id.Value);
            }

            if (mainPersonId.HasValue && mainPersonId.Value > 0)
            {
                whereClauses.Add("MainPersonId = @mpId");
                cmd.Parameters.AddWithValue("@mpId", mainPersonId.Value);
            }

            if (subPerson1Id.HasValue && subPerson1Id.Value > 0)
            {
                whereClauses.Add("SubPerson1Id = @sp1Id");
                cmd.Parameters.AddWithValue("@sp1Id", subPerson1Id.Value);
            }

            if (subPerson2Id.HasValue && subPerson2Id.Value > 0)
            {
                whereClauses.Add("SubPerson2Id = @sp2Id");
                cmd.Parameters.AddWithValue("@sp2Id", subPerson2Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                whereClauses.Add("DocDate >= @fromD");
                cmd.Parameters.AddWithValue("@fromD", fromDate);
            }

            if (!string.IsNullOrWhiteSpace(toDate))
            {
                whereClauses.Add("DocDate <= @toD");
                cmd.Parameters.AddWithValue("@toD", toDate);
            }

            if (!string.IsNullOrWhiteSpace(docType) && docType != "همه")
            {
                whereClauses.Add("DocType = @dt");
                cmd.Parameters.AddWithValue("@dt", docType);
            }

            string sql = "SELECT * FROM vw_DocumentsDetails";
            if (whereClauses.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", whereClauses);
            }
            sql += " ORDER BY DocDate DESC, Id DESC;";

            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                byte[]? thumbBytes = reader.IsDBNull(reader.GetOrdinal("ThumbnailBlob")) ? null : (byte[])reader["ThumbnailBlob"];

                var item = new DocumentItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CodBayegany = reader.GetString(reader.GetOrdinal("CodBayegany")),
                    DocNumber = reader.IsDBNull(reader.GetOrdinal("DocNumber")) ? "" : reader.GetString(reader.GetOrdinal("DocNumber")),
                    DocDate = reader.IsDBNull(reader.GetOrdinal("DocDate")) ? "" : reader.GetString(reader.GetOrdinal("DocDate")),
                    RegDate = reader.IsDBNull(reader.GetOrdinal("RegDate")) ? "" : reader.GetString(reader.GetOrdinal("RegDate")),
                    DocType = reader.IsDBNull(reader.GetOrdinal("DocType")) ? "" : reader.GetString(reader.GetOrdinal("DocType")),
                    MainSubjectId = reader.IsDBNull(reader.GetOrdinal("MainSubjectId")) ? null : reader.GetInt32(reader.GetOrdinal("MainSubjectId")),
                    SubSubject1Id = reader.IsDBNull(reader.GetOrdinal("SubSubject1Id")) ? null : reader.GetInt32(reader.GetOrdinal("SubSubject1Id")),
                    SubSubject2Id = reader.IsDBNull(reader.GetOrdinal("SubSubject2Id")) ? null : reader.GetInt32(reader.GetOrdinal("SubSubject2Id")),
                    MainPersonId = reader.IsDBNull(reader.GetOrdinal("MainPersonId")) ? null : reader.GetInt32(reader.GetOrdinal("MainPersonId")),
                    SubPerson1Id = reader.IsDBNull(reader.GetOrdinal("SubPerson1Id")) ? null : reader.GetInt32(reader.GetOrdinal("SubPerson1Id")),
                    SubPerson2Id = reader.IsDBNull(reader.GetOrdinal("SubPerson2Id")) ? null : reader.GetInt32(reader.GetOrdinal("SubPerson2Id")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString(reader.GetOrdinal("Notes")),
                    ColorTag = reader.IsDBNull(reader.GetOrdinal("ColorTag")) ? "" : reader.GetString(reader.GetOrdinal("ColorTag")),
                    MainSubjectTitle = reader.GetString(reader.GetOrdinal("MainSubjectTitle")),
                    SubSubject1Title = reader.GetString(reader.GetOrdinal("SubSubject1Title")),
                    SubSubject2Title = reader.GetString(reader.GetOrdinal("SubSubject2Title")),
                    MainPersonName = reader.GetString(reader.GetOrdinal("MainPersonName")),
                    SubPerson1Name = reader.GetString(reader.GetOrdinal("SubPerson1Name")),
                    SubPerson2Name = reader.GetString(reader.GetOrdinal("SubPerson2Name")),
                    PageCount = reader.GetInt32(reader.GetOrdinal("PageCount")),
                    ThumbnailBlob = thumbBytes,
                    ThumbnailImage = ImageHelper.BytesToBitmapSource(thumbBytes),
                    FullOcrText = reader.IsDBNull(reader.GetOrdinal("FullOcrText")) ? "" : reader.GetString(reader.GetOrdinal("FullOcrText"))
                };
                list.Add(item);
            }
            return list;
        }

        public static List<DocumentPageItem> GetDocumentPages(int documentId)
        {
            var list = new List<DocumentPageItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, PageNumber, ImageBlob, ThumbnailBlob, PageOcrText, FileName FROM DocumentPages WHERE DocumentId = @docId ORDER BY PageNumber ASC;";
            cmd.Parameters.AddWithValue("@docId", documentId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                byte[] imgBytes = (byte[])reader["ImageBlob"];
                byte[] thumbBytes = (byte[])reader["ThumbnailBlob"];

                list.Add(new DocumentPageItem
                {
                    Id = reader.GetInt32(0),
                    DocumentId = reader.GetInt32(1),
                    PageNumber = reader.GetInt32(2),
                    ImageBlob = imgBytes,
                    ThumbnailBlob = thumbBytes,
                    PageOcrText = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    FileName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    ThumbnailImage = ImageHelper.BytesToBitmapSource(thumbBytes),
                    FullImage = ImageHelper.BytesToBitmapSource(imgBytes)
                });
            }
            return list;
        }

        public static int SaveDocument(DocumentItem doc)
        {
            using var conn = GetConnection();
            using var trans = conn.BeginTransaction();

            int docId = doc.Id;
            if (docId == 0)
            {
                // درج سند جدید
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Documents (
                        CodBayegany, DocNumber, DocDate, RegDate, DocType,
                        MainSubjectId, SubSubject1Id, SubSubject2Id,
                        MainPersonId, SubPerson1Id, SubPerson2Id,
                        Notes, ColorTag
                    ) VALUES (
                        @cb, @dn, @dd, @rd, @dt,
                        @msId, @ss1Id, @ss2Id,
                        @mpId, @sp1Id, @sp2Id,
                        @nt, @ct
                    );
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@cb", doc.CodBayegany.Trim());
                cmd.Parameters.AddWithValue("@dn", (object?)doc.DocNumber?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dd", (object?)doc.DocDate?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rd", (object?)doc.RegDate?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dt", (object?)doc.DocType?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@msId", (object?)doc.MainSubjectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ss1Id", (object?)doc.SubSubject1Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ss2Id", (object?)doc.SubSubject2Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mpId", (object?)doc.MainPersonId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sp1Id", (object?)doc.SubPerson1Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sp2Id", (object?)doc.SubPerson2Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nt", (object?)doc.Notes?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ct", (object?)doc.ColorTag?.Trim() ?? DBNull.Value);

                docId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                // ویرایش سند موجود
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Documents SET
                        CodBayegany = @cb,
                        DocNumber = @dn,
                        DocDate = @dd,
                        DocType = @dt,
                        MainSubjectId = @msId,
                        SubSubject1Id = @ss1Id,
                        SubSubject2Id = @ss2Id,
                        MainPersonId = @mpId,
                        SubPerson1Id = @sp1Id,
                        SubPerson2Id = @sp2Id,
                        Notes = @nt,
                        ColorTag = @ct
                    WHERE Id = @id;";

                cmd.Parameters.AddWithValue("@id", docId);
                cmd.Parameters.AddWithValue("@cb", doc.CodBayegany.Trim());
                cmd.Parameters.AddWithValue("@dn", (object?)doc.DocNumber?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dd", (object?)doc.DocDate?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dt", (object?)doc.DocType?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@msId", (object?)doc.MainSubjectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ss1Id", (object?)doc.SubSubject1Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ss2Id", (object?)doc.SubSubject2Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mpId", (object?)doc.MainPersonId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sp1Id", (object?)doc.SubPerson1Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sp2Id", (object?)doc.SubPerson2Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nt", (object?)doc.Notes?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ct", (object?)doc.ColorTag?.Trim() ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            // بروزرسانی، درج و حذف برگه‌ها
            if (doc.Pages != null)
            {
                // ۱. حذف برگه‌هایی که توسط کاربر حذف شده‌اند
                var currentIds = doc.Pages.Where(p => p.Id > 0).Select(p => p.Id).ToList();
                using (var cmdDel = conn.CreateCommand())
                {
                    if (currentIds.Count > 0)
                    {
                        string inClause = string.Join(",", currentIds);
                        cmdDel.CommandText = $"DELETE FROM DocumentPages WHERE DocumentId = @dId AND Id NOT IN ({inClause});";
                    }
                    else
                    {
                        cmdDel.CommandText = "DELETE FROM DocumentPages WHERE DocumentId = @dId;";
                    }
                    cmdDel.Parameters.AddWithValue("@dId", docId);
                    cmdDel.ExecuteNonQuery();
                }

                // ۲. ذخیره یا بروزرسانی تک تک برگه‌ها
                foreach (var p in doc.Pages)
                {
                    if (p.ImageBlob != null && p.ImageBlob.Length > 0)
                    {
                        byte[] thumb = p.ThumbnailBlob ?? ImageHelper.GenerateThumbnail(p.ImageBlob);

                        if (p.Id > 0)
                        {
                            // بروزرسانی برگه موجود (تصویر کراپ‌شده، متن OCR، شماره برگه و بندانگشتی)
                            using var cmdUpd = conn.CreateCommand();
                            cmdUpd.CommandText = @"
                                UPDATE DocumentPages SET 
                                    PageNumber = @pn, 
                                    ImageBlob = @ib, 
                                    ThumbnailBlob = @tb, 
                                    PageOcrText = @ocr, 
                                    FileName = @fn 
                                WHERE Id = @pId AND DocumentId = @dId;";
                            cmdUpd.Parameters.AddWithValue("@pId", p.Id);
                            cmdUpd.Parameters.AddWithValue("@dId", docId);
                            cmdUpd.Parameters.AddWithValue("@pn", p.PageNumber);
                            cmdUpd.Parameters.AddWithValue("@ib", p.ImageBlob);
                            cmdUpd.Parameters.AddWithValue("@tb", thumb);
                            cmdUpd.Parameters.AddWithValue("@ocr", (object?)p.PageOcrText ?? DBNull.Value);
                            cmdUpd.Parameters.AddWithValue("@fn", (object?)p.FileName ?? DBNull.Value);
                            cmdUpd.ExecuteNonQuery();
                        }
                        else
                        {
                            // درج برگه جدید
                            using var cmdPage = conn.CreateCommand();
                            cmdPage.CommandText = @"
                                INSERT INTO DocumentPages (DocumentId, PageNumber, ImageBlob, ThumbnailBlob, PageOcrText, FileName)
                                VALUES (@dId, @pn, @ib, @tb, @ocr, @fn);";
                            cmdPage.Parameters.AddWithValue("@dId", docId);
                            cmdPage.Parameters.AddWithValue("@pn", p.PageNumber);
                            cmdPage.Parameters.AddWithValue("@ib", p.ImageBlob);
                            cmdPage.Parameters.AddWithValue("@tb", thumb);
                            cmdPage.Parameters.AddWithValue("@ocr", (object?)p.PageOcrText ?? DBNull.Value);
                            cmdPage.Parameters.AddWithValue("@fn", (object?)p.FileName ?? DBNull.Value);
                            cmdPage.ExecuteNonQuery();
                        }
                    }
                }
            }

            trans.Commit();
            return docId;
        }

        public static void DeleteDocument(int documentId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Documents WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", documentId);
            cmd.ExecuteNonQuery();
        }

        // --- گزارش‌گیری ماتریسی چندبعدی و جامع ---

        public static (List<DocumentItem> Results, int TotalPages, string MinDate, string MaxDate) GetMatrixReport(
            int? mainSubjectId,
            int? subSubject1Id,
            int? subSubject2Id,
            int? mainPersonId,
            int? subPerson1Id,
            int? subPerson2Id,
            int? person360Id, // شخص در هر نقشی
            string? fromDate,
            string? toDate,
            string? docType,
            string? ocrKeyword)
        {
            var list = new List<DocumentItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            var whereClauses = new List<string>();

            // ۱. فیلتر سلسله‌مراتبی موضوعات (۱، ۲، ۳)
            if (mainSubjectId.HasValue && mainSubjectId.Value > 0)
            {
                whereClauses.Add("MainSubjectId = @msId");
                cmd.Parameters.AddWithValue("@msId", mainSubjectId.Value);
            }
            if (subSubject1Id.HasValue && subSubject1Id.Value > 0)
            {
                whereClauses.Add("SubSubject1Id = @ss1Id");
                cmd.Parameters.AddWithValue("@ss1Id", subSubject1Id.Value);
            }
            if (subSubject2Id.HasValue && subSubject2Id.Value > 0)
            {
                whereClauses.Add("SubSubject2Id = @ss2Id");
                cmd.Parameters.AddWithValue("@ss2Id", subSubject2Id.Value);
            }

            // ۲. فیلتر اشخاص سه‌گانه
            if (person360Id.HasValue && person360Id.Value > 0)
            {
                whereClauses.Add("(MainPersonId = @p360 OR SubPerson1Id = @p360 OR SubPerson2Id = @p360)");
                cmd.Parameters.AddWithValue("@p360", person360Id.Value);
            }
            else
            {
                if (mainPersonId.HasValue && mainPersonId.Value > 0)
                {
                    whereClauses.Add("MainPersonId = @mpId");
                    cmd.Parameters.AddWithValue("@mpId", mainPersonId.Value);
                }
                if (subPerson1Id.HasValue && subPerson1Id.Value > 0)
                {
                    whereClauses.Add("SubPerson1Id = @sp1Id");
                    cmd.Parameters.AddWithValue("@sp1Id", subPerson1Id.Value);
                }
                if (subPerson2Id.HasValue && subPerson2Id.Value > 0)
                {
                    whereClauses.Add("SubPerson2Id = @sp2Id");
                    cmd.Parameters.AddWithValue("@sp2Id", subPerson2Id.Value);
                }
            }

            // ۳. فیلتر بازه تاریخ شمسی
            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                whereClauses.Add("DocDate >= @fromD");
                cmd.Parameters.AddWithValue("@fromD", fromDate);
            }
            if (!string.IsNullOrWhiteSpace(toDate))
            {
                whereClauses.Add("DocDate <= @toD");
                cmd.Parameters.AddWithValue("@toD", toDate);
            }

            // ۴. فیلتر نوع مدرک
            if (!string.IsNullOrWhiteSpace(docType) && docType != "همه")
            {
                whereClauses.Add("DocType = @dt");
                cmd.Parameters.AddWithValue("@dt", docType);
            }

            // ۵. فیلتر متن داخل سند (OCR)
            if (!string.IsNullOrWhiteSpace(ocrKeyword))
            {
                whereClauses.Add("(FullOcrText LIKE @ocrKey OR Notes LIKE @ocrKey)");
                cmd.Parameters.AddWithValue("@ocrKey", $"%{ocrKeyword.Trim()}%");
            }

            string sql = "SELECT * FROM vw_DocumentsDetails";
            if (whereClauses.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", whereClauses);
            }
            sql += " ORDER BY DocDate DESC, Id DESC;";

            cmd.CommandText = sql;
            int totalPages = 0;
            string minDate = string.Empty;
            string maxDate = string.Empty;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                byte[]? thumbBytes = reader.IsDBNull(reader.GetOrdinal("ThumbnailBlob")) ? null : (byte[])reader["ThumbnailBlob"];
                int pages = reader.GetInt32(reader.GetOrdinal("PageCount"));
                string docDate = reader.IsDBNull(reader.GetOrdinal("DocDate")) ? "" : reader.GetString(reader.GetOrdinal("DocDate"));

                totalPages += pages;
                if (!string.IsNullOrWhiteSpace(docDate))
                {
                    if (string.IsNullOrEmpty(minDate) || string.Compare(docDate, minDate) < 0) minDate = docDate;
                    if (string.IsNullOrEmpty(maxDate) || string.Compare(docDate, maxDate) > 0) maxDate = docDate;
                }

                list.Add(new DocumentItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CodBayegany = reader.GetString(reader.GetOrdinal("CodBayegany")),
                    DocNumber = reader.IsDBNull(reader.GetOrdinal("DocNumber")) ? "" : reader.GetString(reader.GetOrdinal("DocNumber")),
                    DocDate = docDate,
                    DocType = reader.IsDBNull(reader.GetOrdinal("DocType")) ? "" : reader.GetString(reader.GetOrdinal("DocType")),
                    MainSubjectTitle = reader.GetString(reader.GetOrdinal("MainSubjectTitle")),
                    SubSubject1Title = reader.GetString(reader.GetOrdinal("SubSubject1Title")),
                    SubSubject2Title = reader.GetString(reader.GetOrdinal("SubSubject2Title")),
                    MainPersonName = reader.GetString(reader.GetOrdinal("MainPersonName")),
                    SubPerson1Name = reader.GetString(reader.GetOrdinal("SubPerson1Name")),
                    SubPerson2Name = reader.GetString(reader.GetOrdinal("SubPerson2Name")),
                    PageCount = pages,
                    ThumbnailBlob = thumbBytes,
                    ThumbnailImage = ImageHelper.BytesToBitmapSource(thumbBytes),
                    FullOcrText = reader.IsDBNull(reader.GetOrdinal("FullOcrText")) ? "" : reader.GetString(reader.GetOrdinal("FullOcrText")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString(reader.GetOrdinal("Notes")),
                    ColorTag = reader.IsDBNull(reader.GetOrdinal("ColorTag")) ? "" : reader.GetString(reader.GetOrdinal("ColorTag"))
                });
            }

            return (list, totalPages, minDate, maxDate);
        }

        // --- درج خودکار داده‌های نمونه ---

        private static void SeedSampleDataIfEmpty(SqliteConnection conn)
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Documents;";
            long count = (long)checkCmd.ExecuteScalar()!;
            if (count > 0)
                return; // دیتابیس قبلاً داده دارد

            using var trans = conn.BeginTransaction();

            // ۱. موضوعات سه‌گانه
            int sProj = InsertMainSubject(conn, "امور پروژه‌های عمرانی", "PRJ-01", "قراردادها و صورتجلسات پروژه‌های ساختمانی");
            int sProj1 = InsertSubSubject1(conn, sProj, "پروژه ساختمانی شریعتی", "PRJ-01-SHR");
            InsertSubSubject2(conn, sProj1, "خرید اسکلت فلزی و سازه", "PRJ-01-SHR-SKL");
            InsertSubSubject2(conn, sProj1, "بتن‌ریزی و فونداسیون", "PRJ-01-SHR-BTN");
            InsertSubSubject2(conn, sProj1, "نازک‌کاری و تاسیسات", "PRJ-01-SHR-TAS");

            int sProj2 = InsertSubSubject1(conn, sProj, "پروژه مجتمع سعدی", "PRJ-01-SAD");
            InsertSubSubject2(conn, sProj2, "مکاتبات شهرداری و پروانه", "PRJ-01-SAD-SHH");
            InsertSubSubject2(conn, sProj2, "توافق‌نامه‌ها و تقسیم‌نامه", "PRJ-01-SAD-TVG");

            int sFin = InsertMainSubject(conn, "امور مالی و قراردادها", "FIN-02", "اسناد مالی، قراردادهای خرید و فاکتورها");
            int sFin1 = InsertSubSubject1(conn, sFin, "خرید مواد اولیه و تجهیزات", "FIN-02-KHD");
            InsertSubSubject2(conn, sFin1, "تجهیزات صنعتی و الکتروموتور", "FIN-02-KHD-TGH");
            InsertSubSubject2(conn, sFin1, "آهن‌آلات و ورق‌های فولادی", "FIN-02-KHD-AHN");

            int sLeg = InsertMainSubject(conn, "امور حقوقی و اسناد ثبتی", "LEG-03", "اسناد مالکیت، وکالت‌نامه‌ها و دعاوی");
            int sLeg1 = InsertSubSubject1(conn, sLeg, "اسناد ملکی و ثبتی", "LEG-03-MLK");
            InsertSubSubject2(conn, sLeg1, "سند رسمی تک‌برگ", "LEG-03-MLK-SND");
            InsertSubSubject2(conn, sLeg1, "انحصار وراثت و گواهی مالیاتی", "LEG-03-MLK-ENH");

            int sAdm = InsertMainSubject(conn, "مکاتبات اداری و سازمانی", "ADM-04", "نامه‌های اداری، استانداری و ارگان‌ها");
            int sAdm1 = InsertSubSubject1(conn, sAdm, "مکاتبات استانداری و فرمانداری", "ADM-04-OST");
            InsertSubSubject2(conn, sAdm1, "نامه‌های پیگیری تسهیلات", "ADM-04-OST-TSH");
            InsertSubSubject2(conn, sAdm1, "مجوزهای بهره‌برداری", "ADM-04-OST-MJZ");

            // ۲. اشخاص حقیقی و حقوقی
            int p1 = InsertPerson(conn, "شرکت فولاد صنعت آذرین", "حقوقی خصوصی", "10103456789", "021-88776655", "تامین‌کننده اسکلت و آهن‌آلات");
            int p2 = InsertPerson(conn, "مهندس مسعود رضایی", "حقیقی", "0078945612", "09121112233", "مهندس ناظر و مشاور فنی");
            int p3 = InsertPerson(conn, "شهرداری منطقه ۳", "حقوقی دولتی", "14002345678", "021-22334455", "صدور پروانه و مجوزهای ساخت");
            int p4 = InsertPerson(conn, "شرکت پیشگامان بتن آریا", "حقوقی خصوصی", "10109876543", "021-44556677", "پیمانکار بتن‌ریزی آماده");
            int p5 = InsertPerson(conn, "دکتر حمید اکبری", "حقیقی", "0065432198", "09123334455", "وکیل پایه یک دادگستری و مشاور حقوقی");
            int p6 = InsertPerson(conn, "استانداری استان البرز", "حقوقی دولتی", "14008765432", "026-32112233", "معاونت امور عمرانی و اقتصادی");
            int p7 = InsertPerson(conn, "بانک ملی شعبه مرکزی", "حقوقی دولتی", "10100000001", "021-66554433", "شعبه پیگیری ضمانت‌نامه‌ها");
            int p8 = InsertPerson(conn, "علی اسدی", "حقیقی", "0054321678", "09129998877", "کارشناس رسمی دادگستری");

            trans.Commit();
        }

        private static int InsertMainSubject(SqliteConnection conn, string title, string code, string desc)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO MainSubjects (Title, Code, Description) VALUES (@t, @c, @d); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@c", code);
            cmd.Parameters.AddWithValue("@d", desc);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static int InsertSubSubject1(SqliteConnection conn, int mainId, string title, string code)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO SubSubjects1 (MainSubjectId, Title, Code) VALUES (@mId, @t, @c); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@mId", mainId);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@c", code);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static int InsertSubSubject2(SqliteConnection conn, int sub1Id, string title, string code)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO SubSubjects2 (SubSubject1Id, Title, Code) VALUES (@sId, @t, @c); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@sId", sub1Id);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@c", code);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static int InsertPerson(SqliteConnection conn, string fullName, string type, string natCode, string phone, string notes)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Persons (FullName, PersonType, NationalCode, ContactInfo, Notes) VALUES (@fn, @pt, @nc, @ci, @nt); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@fn", fullName);
            cmd.Parameters.AddWithValue("@pt", type);
            cmd.Parameters.AddWithValue("@nc", natCode);
            cmd.Parameters.AddWithValue("@ci", phone);
            cmd.Parameters.AddWithValue("@nt", notes);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ==================== مدیریت کاربران و سطوح دسترسی ====================

        public static void EnsureDefaultAdmin()
        {
            using var conn = GetConnection();
            using var cmdCount = conn.CreateCommand();
            cmdCount.CommandText = "SELECT COUNT(*) FROM Users;";
            var count = Convert.ToInt32(cmdCount.ExecuteScalar());
            if (count == 0)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Users (Username, PasswordHash, FullName, Role, 
                        CanAddDoc, CanEditDoc, CanDeleteDoc, CanManagePersons, CanManageSubjects, 
                        CanViewReports, CanBackupRestore, CanManageUsers, IsActive, CreatedAt)
                    VALUES (@un, @ph, @fn, @ro, 1, 1, 1, 1, 1, 1, 1, 1, 1, @ca);";
                cmd.Parameters.AddWithValue("@un", "admin");
                cmd.Parameters.AddWithValue("@ph", SecurityHelper.HashPassword("admin"));
                cmd.Parameters.AddWithValue("@fn", "مدیر سیستم");
                cmd.Parameters.AddWithValue("@ro", "مدیر سیستم (دسترسی کامل)");
                cmd.Parameters.AddWithValue("@ca", DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                cmd.ExecuteNonQuery();
            }
        }

        public static UserItem? AuthenticateUser(string username, string password)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE LOWER(Username) = LOWER(@un);";
            cmd.Parameters.AddWithValue("@un", username.Trim());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                if (SecurityHelper.VerifyPassword(password, storedHash))
                {
                    bool isActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1;
                    if (!isActive)
                    {
                        throw new InvalidOperationException("حساب کاربری شما غیرفعال شده است. لطفاً با مدیر سیستم تماس بگیرید.");
                    }

                    return ReadUserFromReader(reader);
                }
            }
            return null;
        }

        public static List<UserItem> GetAllUsers()
        {
            var list = new List<UserItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users ORDER BY Id ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadUserFromReader(reader));
            }
            return list;
        }

        public static UserItem? GetUserById(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadUserFromReader(reader);
            }
            return null;
        }

        public static bool IsUsernameTaken(string username, int excludeUserId = 0)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE LOWER(Username) = LOWER(@un) AND Id != @id;";
            cmd.Parameters.AddWithValue("@un", username.Trim());
            cmd.Parameters.AddWithValue("@id", excludeUserId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static int AddUser(UserItem user, string plainPassword)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Role, 
                    CanAddDoc, CanEditDoc, CanDeleteDoc, CanManagePersons, CanManageSubjects, 
                    CanViewReports, CanBackupRestore, CanManageUsers, IsActive, CreatedAt)
                VALUES (@un, @ph, @fn, @ro, @cad, @ced, @cdd, @cmp, @cms, @cvr, @cbr, @cmu, @ia, @ca);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@un", user.Username.Trim());
            cmd.Parameters.AddWithValue("@ph", SecurityHelper.HashPassword(plainPassword));
            cmd.Parameters.AddWithValue("@fn", user.FullName.Trim());
            cmd.Parameters.AddWithValue("@ro", user.Role);
            cmd.Parameters.AddWithValue("@cad", user.CanAddDoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@ced", user.CanEditDoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@cdd", user.CanDeleteDoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@cmp", user.CanManagePersons ? 1 : 0);
            cmd.Parameters.AddWithValue("@cms", user.CanManageSubjects ? 1 : 0);
            cmd.Parameters.AddWithValue("@cvr", user.CanViewReports ? 1 : 0);
            cmd.Parameters.AddWithValue("@cbr", user.CanBackupRestore ? 1 : 0);
            cmd.Parameters.AddWithValue("@cmu", user.CanManageUsers ? 1 : 0);
            cmd.Parameters.AddWithValue("@ia", user.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@ca", DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static bool UpdateUser(UserItem user, string? newPlainPassword = null)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (!string.IsNullOrEmpty(newPlainPassword))
            {
                cmd.CommandText = @"
                    UPDATE Users SET 
                        Username = @un, 
                        PasswordHash = @ph,
                        FullName = @fn, 
                        Role = @ro, 
                        CanAddDoc = @cad, 
                        CanEditDoc = @ced, 
                        CanDeleteDoc = @cdd, 
                        CanManagePersons = @cmp, 
                        CanManageSubjects = @cms, 
                        CanViewReports = @cvr, 
                        CanBackupRestore = @cbr, 
                        CanManageUsers = @cmu, 
                        IsActive = @ia
                    WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@ph", SecurityHelper.HashPassword(newPlainPassword));
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE Users SET 
                        Username = @un, 
                        FullName = @fn, 
                        Role = @ro, 
                        CanAddDoc = @cad, 
                        CanEditDoc = @ced, 
                        CanDeleteDoc = @cdd, 
                        CanManagePersons = @cmp, 
                        CanManageSubjects = @cms, 
                        CanViewReports = @cvr, 
                        CanBackupRestore = @cbr, 
                        CanManageUsers = @cmu, 
                        IsActive = @ia
                    WHERE Id = @id;";
            }

            cmd.Parameters.AddWithValue("@un", user.Username.Trim());
            cmd.Parameters.AddWithValue("@fn", user.FullName.Trim());
            cmd.Parameters.AddWithValue("@ro", user.Role);
            cmd.Parameters.AddWithValue("@cad", user.CanAddDoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@ced", user.CanEditDoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@cdd", user.CanDeleteDoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@cmp", user.CanManagePersons ? 1 : 0);
            cmd.Parameters.AddWithValue("@cms", user.CanManageSubjects ? 1 : 0);
            cmd.Parameters.AddWithValue("@cvr", user.CanViewReports ? 1 : 0);
            cmd.Parameters.AddWithValue("@cbr", user.CanBackupRestore ? 1 : 0);
            cmd.Parameters.AddWithValue("@cmu", user.CanManageUsers ? 1 : 0);
            cmd.Parameters.AddWithValue("@ia", user.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", user.Id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool UpdateUserProfile(int userId, string newUsername, string newFullName, string? newPlainPassword = null)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (!string.IsNullOrEmpty(newPlainPassword))
            {
                cmd.CommandText = "UPDATE Users SET Username = @un, FullName = @fn, PasswordHash = @ph WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@ph", SecurityHelper.HashPassword(newPlainPassword));
            }
            else
            {
                cmd.CommandText = "UPDATE Users SET Username = @un, FullName = @fn WHERE Id = @id;";
            }
            cmd.Parameters.AddWithValue("@un", newUsername.Trim());
            cmd.Parameters.AddWithValue("@fn", newFullName.Trim());
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool DeleteUser(int userId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static int GetActiveAdminCount()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE IsActive = 1 AND (Role LIKE '%مدیر%' OR CanManageUsers = 1);";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static UserItem ReadUserFromReader(SqliteDataReader reader)
        {
            return new UserItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CanAddDoc = reader.GetInt32(reader.GetOrdinal("CanAddDoc")) == 1,
                CanEditDoc = reader.GetInt32(reader.GetOrdinal("CanEditDoc")) == 1,
                CanDeleteDoc = reader.GetInt32(reader.GetOrdinal("CanDeleteDoc")) == 1,
                CanManagePersons = reader.GetInt32(reader.GetOrdinal("CanManagePersons")) == 1,
                CanManageSubjects = reader.GetInt32(reader.GetOrdinal("CanManageSubjects")) == 1,
                CanViewReports = reader.GetInt32(reader.GetOrdinal("CanViewReports")) == 1,
                CanBackupRestore = reader.GetInt32(reader.GetOrdinal("CanBackupRestore")) == 1,
                CanManageUsers = reader.GetInt32(reader.GetOrdinal("CanManageUsers")) == 1,
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? "" : reader.GetString(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
