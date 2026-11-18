# -*- coding: utf-8 -*-
import sqlite3
import os
import glob
import io
import sys
from PIL import Image

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

db_dir = r"D:\Projects_AI\tahgigat\Khodam\Bayegany\App_Bayegany\Data"
os.makedirs(db_dir, exist_ok=True)
db_path = os.path.join(db_dir, "bayegany.db")

tasvir_dir = r"D:\Projects_AI\tahgigat\Khodam\Chek\Tabdil_Aks_to_Data\Tasvir"
images = sorted(glob.glob(os.path.join(tasvir_dir, "*.jpg")))

print(f"Connecting to database: {db_path}")
conn = sqlite3.connect(db_path)
cur = conn.cursor()

# اطمینان از ساخت جداول
cur.executescript("""
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

    CREATE TABLE IF NOT EXISTS Persons (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        FullName TEXT NOT NULL,
        PersonType TEXT,
        NationalCode TEXT,
        ContactInfo TEXT,
        Notes TEXT
    );

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
        ColorTag TEXT
    );

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

    CREATE INDEX IF NOT EXISTS idx_doc_subjects ON Documents(MainSubjectId, SubSubject1Id, SubSubject2Id);
    CREATE INDEX IF NOT EXISTS idx_doc_persons ON Documents(MainPersonId, SubPerson1Id, SubPerson2Id);
    CREATE INDEX IF NOT EXISTS idx_doc_matrix ON Documents(MainSubjectId, MainPersonId, DocDate);
    CREATE INDEX IF NOT EXISTS idx_doc_codbayegany ON Documents(CodBayegany);

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
""")

def get_or_create_main_subject(title, code, desc):
    cur.execute("SELECT Id FROM MainSubjects WHERE Title = ?", (title,))
    r = cur.fetchone()
    if r: return r[0]
    cur.execute("INSERT INTO MainSubjects (Title, Code, Description) VALUES (?, ?, ?)", (title, code, desc))
    return cur.lastrowid

def get_or_create_sub1(main_id, title, code):
    cur.execute("SELECT Id FROM SubSubjects1 WHERE MainSubjectId = ? AND Title = ?", (main_id, title))
    r = cur.fetchone()
    if r: return r[0]
    cur.execute("INSERT INTO SubSubjects1 (MainSubjectId, Title, Code) VALUES (?, ?, ?)", (main_id, title, code))
    return cur.lastrowid

def get_or_create_sub2(sub1_id, title, code):
    cur.execute("SELECT Id FROM SubSubjects2 WHERE SubSubject1Id = ? AND Title = ?", (sub1_id, title))
    r = cur.fetchone()
    if r: return r[0]
    cur.execute("INSERT INTO SubSubjects2 (SubSubject1Id, Title, Code) VALUES (?, ?, ?)", (sub1_id, title, code))
    return cur.lastrowid

def get_or_create_person(name, ptype, nat_code, contact, notes):
    cur.execute("SELECT Id FROM Persons WHERE FullName = ?", (name,))
    r = cur.fetchone()
    if r: return r[0]
    cur.execute("INSERT INTO Persons (FullName, PersonType, NationalCode, ContactInfo, Notes) VALUES (?, ?, ?, ?, ?)",
                (name, ptype, nat_code, contact, notes))
    return cur.lastrowid

# ۱. درج موضوعات
ms_omran = get_or_create_main_subject("امور پروژه‌های عمرانی", "PRJ-01", "پروژه‌های ساخت و ساز و ابنیه")
ss1_shariati = get_or_create_sub1(ms_omran, "پروژه ساختمانی شریعتی", "PRJ-01-SHR")
ss2_eskelet = get_or_create_sub2(ss1_shariati, "خرید اسکلت فلزی و سازه", "PRJ-01-SHR-SKL")
ss2_beton = get_or_create_sub2(ss1_shariati, "بتن‌ریزی و فونداسیون", "PRJ-01-SHR-BTN")
ss2_tasisat = get_or_create_sub2(ss1_shariati, "نازک‌کاری و تاسیسات مکانیکی", "PRJ-01-SHR-TAS")

ss1_saadi = get_or_create_sub1(ms_omran, "پروژه مجتمع تجاری سعدی", "PRJ-01-SAD")
ss2_shahrdari = get_or_create_sub2(ss1_saadi, "مکاتبات شهرداری و پروانه", "PRJ-01-SAD-SHH")
ss2_tavaghof = get_or_create_sub2(ss1_saadi, "توافق‌نامه‌ها و تقسیم‌نامه", "PRJ-01-SAD-TVG")

ms_mali = get_or_create_main_subject("امور مالی و قراردادها", "FIN-02", "اسناد مالی، فاکتورها و پیمان‌ها")
ss1_kharid = get_or_create_sub1(ms_mali, "خرید مواد اولیه و تجهیزات", "FIN-02-KHD")
ss2_motori = get_or_create_sub2(ss1_kharid, "تجهیزات صنعتی و الکتروموتور", "FIN-02-KHD-TGH")
ss2_ahan = get_or_create_sub2(ss1_kharid, "آهن‌آلات و ورق‌های فولادی", "FIN-02-KHD-AHN")

ms_hoghogh = get_or_create_main_subject("امور حقوقی و اسناد ثبتی", "LEG-03", "اسناد مالکیت و دعاوی")
ss1_melki = get_or_create_sub1(ms_hoghogh, "اسناد ملکی و ثبتی", "LEG-03-MLK")
ss2_takbarg = get_or_create_sub2(ss1_melki, "سند رسمی تک‌برگ", "LEG-03-MLK-SND")
ss2_enhsar = get_or_create_sub2(ss1_melki, "انحصار وراثت و گواهی مالیاتی", "LEG-03-MLK-ENH")

ms_edari = get_or_create_main_subject("مکاتبات اداری و سازمانی", "ADM-04", "مکاتبات دولتی و ارگان‌ها")
ss1_ostandari = get_or_create_sub1(ms_edari, "مکاتبات استانداری و فرمانداری", "ADM-04-OST")
ss2_tashilat = get_or_create_sub2(ss1_ostandari, "نامه‌های پیگیری تسهیلات", "ADM-04-OST-TSH")

# ۲. درج اشخاص
p_foolad = get_or_create_person("شرکت فولاد صنعت آذرین", "حقوقی خصوصی", "10103456789", "021-88776655", "تامین‌کننده اسکلت و مقاطع فولادی")
p_rezaei = get_or_create_person("مهندس مسعود رضایی", "حقیقی", "0078945612", "09121112233", "مهندس ناظر ارشد و مشاور سازه")
p_shahrdari = get_or_create_person("شهرداری منطقه ۳", "حقوقی دولتی", "14002345678", "021-22334455", "مرجع صدور پروانه ساختمانی")
p_beton = get_or_create_person("شرکت پیشگامان بتن آریا", "حقوقی خصوصی", "10109876543", "021-44556677", "تولیدکننده بتن آماده استاندارد")
p_akbari = get_or_create_person("دکتر حمید اکبری", "حقیقی", "0065432198", "09123334455", "وکیل پایه یک دادگستری")
p_ostandari = get_or_create_person("استانداری استان البرز", "حقوقی دولتی", "14008765432", "026-32112233", "معاونت هماهنگی امور عمرانی")
p_bank = get_or_create_person("بانک ملی ایران شعبه مرکزی", "حقوقی دولتی", "10100000001", "021-66554433", "تسهیلات و ضمانت‌نامه‌ها")
p_asadi = get_or_create_person("علی اسدی", "حقیقی", "0054321678", "09129998877", "کارشناس رسمی دادگستری")

def compress_img(path, max_dim=1500, quality=78):
    with Image.open(path) as img:
        img = img.convert("RGB")
        w, h = img.size
        if w > max_dim or h > max_dim:
            scale = min(max_dim / w, max_dim / h)
            img = img.resize((int(w * scale), int(h * scale)), Image.Resampling.LANCZOS)
        buf = io.BytesIO()
        img.save(buf, format="JPEG", quality=quality, optimize=True)
        return buf.getvalue()

def make_thumb(path, max_dim=220, quality=65):
    with Image.open(path) as img:
        img = img.convert("RGB")
        w, h = img.size
        scale = min(max_dim / w, max_dim / h)
        img = img.resize((int(w * scale), int(h * scale)), Image.Resampling.LANCZOS)
        buf = io.BytesIO()
        img.save(buf, format="JPEG", quality=quality)
        return buf.getvalue()

# پاک‌سازی اسناد قبلی برای تزریق داده‌های تمیز و کامل
cur.execute("DELETE FROM DocumentPages;")
cur.execute("DELETE FROM Documents;")

sample_documents = [
    {
        "code": "B-1405-0101",
        "num": "GH-1405/882",
        "date": "1405/08/15",
        "type": "قرارداد",
        "ms": ms_omran, "ss1": ss1_shariati, "ss2": ss2_eskelet,
        "mp": p_foolad, "sp1": p_rezaei, "sp2": None,
        "tag": "فوری",
        "notes": "قرارداد ساخت، حمل و نصب سازه اسکلت فلزی پروژه شریعتی با تایید مهندس ناظر رضایی",
        "ocr": "قرارداد پیمانکاری ساخت اسکلت فلزی. کارفرما: پروژه ساختمانی شریعتی. پیمانکار: شرکت فولاد صنعت آذرین. ناظر فنی: مهندس مسعود رضایی. مبلغ کل قرارداد: چهل و هشت میلیارد ریال معادل چهار میلیارد و هشتصد میلیون تومان.",
        "img_count": 2
    },
    {
        "code": "B-1405-0102",
        "num": "SH-1405/110",
        "date": "1405/08/18",
        "type": "نامه اداری",
        "ms": ms_omran, "ss1": ss1_saadi, "ss2": ss2_shahrdari,
        "mp": p_shahrdari, "sp1": p_akbari, "sp2": p_asadi,
        "tag": "عادی",
        "notes": "پاسخ شهرداری منطقه ۳ در خصوص صدور عدم خلاف و عوارض نوسازی ملک سعدی",
        "ocr": "جمهوری اسلامی ایران - شهرداری منطقه ۳. به: مالکین پلاک ثبتی ملک سعدی. موضوع: پاسخ استعلام پروانه ساختمانی و اعلام عوارض قانونی. پیرو درخواست شماره ۱۱۰ به استحضار می‌رساند با توجه به بازدید کارشناس رسمی، ادامه عملیات ساختمانی بلامانع است.",
        "img_count": 1
    },
    {
        "code": "B-1405-0103",
        "num": "BT-9920",
        "date": "1405/08/20",
        "type": "قرارداد",
        "ms": ms_omran, "ss1": ss1_shariati, "ss2": ss2_beton,
        "mp": p_beton, "sp1": p_rezaei, "sp2": None,
        "tag": "اقدام شده",
        "notes": "قرارداد تامین ۱۲۰۰ مترمکعب بتن آماده با عیار ۴۰۰ برای فونداسیون پروژه شریعتی",
        "ocr": "قرارداد تامین بتن آماده عیار ۴۰۰ استاندارد. تامین‌کننده: شرکت پیشگامان بتن آریا. محل تحویل: کارگاه پروژه شریعتی. نظارت و آزمایشگاه مقاومت فشاری ۲۸ روزه تحت نظارت مهندس مسعود رضایی صورت می‌پذیرد.",
        "img_count": 2
    },
    {
        "code": "B-1405-0104",
        "num": "LEG-405/33",
        "date": "1405/08/22",
        "type": "سند ملکی و ثبتی",
        "ms": ms_hoghogh, "ss1": ss1_melki, "ss2": ss2_takbarg,
        "mp": p_akbari, "sp1": p_asadi, "sp2": None,
        "tag": "محرمانه",
        "notes": "سند رسمی تک‌برگ ملک تجاری سعدی به همراه کروکی و تاییدیه کارشناس رسمی دادگستری",
        "ocr": "جمهوری اسلامی ایران - سازمان ثبت اسناد و املاک کشور. سند مالکیت تک‌برگ کاداستری. شماره دفتر الکترونیک ثبتی: ۹۸۷۶۵۴۳۲۱. مساحت عرصه: ۵۴۰ مترمربع. کاربری: تجاری و اداری واقع در پلاک ثبتی سعدی.",
        "img_count": 1
    },
    {
        "code": "B-1405-0105",
        "num": "OST-1405/741",
        "date": "1405/08/24",
        "type": "نامه اداری",
        "ms": ms_edari, "ss1": ss1_ostandari, "ss2": ss2_tashilat,
        "mp": p_ostandari, "sp1": p_bank, "sp2": None,
        "tag": "در انتظار پیگیری",
        "notes": "نامه استانداری به بانک ملی جهت تخصیص خط اعتباری و تسهیلات رونق تولید",
        "ocr": "استانداری استان البرز - معاونت هماهنگی امور اقتصادی. به: مدیریت محترم بانک ملی ایران شعبه مرکزی. موضوع: معرفی طرح تولیدی جهت اعطای تسهیلات سرمایه در گردش. احتراما مقتضی است بر اساس مصوبه کارگروه تسهیل و رفع موانع تولید اقدام لازم به عمل آید.",
        "img_count": 1
    },
    {
        "code": "B-1405-0106",
        "num": "FAK-405/901",
        "date": "1405/08/25",
        "type": "سند مالی و فاکتور",
        "ms": ms_mali, "ss1": ss1_kharid, "ss2": ss2_ahan,
        "mp": p_foolad, "sp1": p_rezaei, "sp2": None,
        "tag": "اقدام شده",
        "notes": "صورتحساب رسمی و تاییدیه باسکول محموله میلگرد آجدار و تیرآهن اصفهان",
        "ocr": "صورتحساب الکترونیکی فروش کالا و خدمات. فروشنده: شرکت فولاد صنعت آذرین (شناسه ملی: ۱۰۱۰۳۴۵۶۷۸۹). خریدار: کارگاه پروژه شریعتی. شرح کالا: ۳۵ تن میلگرد سایز ۲۰ و ۱۸ استاندارد A3. مبلغ کل با ارزش افزوده: پانزده میلیارد و هفتصد میلیون ریال.",
        "img_count": 2
    },
    {
        "code": "B-1405-0107",
        "num": "SOR-1405/04",
        "date": "1405/08/26",
        "type": "صورتجلسه",
        "ms": ms_omran, "ss1": ss1_saadi, "ss2": ss2_tavaghof,
        "mp": p_akbari, "sp1": p_asadi, "sp2": p_shahrdari,
        "tag": "محرمانه",
        "notes": "صورتجلسه توافق نهایی مالکین مشاعی ملک سعدی با حضور وکیل و کارشناس رسمی",
        "ocr": "صورتجلسه توافق و تقسیم‌نامه داخلی. حاضرین جلسه: وکیل مالکین دکتر حمید اکبری، کارشناس منتخب دادگستری جناب آقای علی اسدی و نماینده حقوقی. مقرر گردید قدرالسهم هر یک از واحدها مطابق نقشه پیوست تفکیک و تثبیت گردد.",
        "img_count": 1
    },
    {
        "code": "B-1405-0108",
        "num": "MOT-1405/12",
        "date": "1405/08/27",
        "type": "سند مالی و فاکتور",
        "ms": ms_mali, "ss1": ss1_kharid, "ss2": ss2_motori,
        "mp": p_foolad, "sp1": p_rezaei, "sp2": None,
        "tag": "عادی",
        "notes": "فاکتور خرید دو دستگاه الکتروموتور ۵۵ کیلووات زیمنس برای تاسیسات شریعتی",
        "ocr": "پیش‌فاکتور و ضمانت‌نامه خرید تجهیزات صنعتی. اقلام: دو دستگاه الکتروموتور صنعتی سه فاز ۵۵ کیلووات ۳۰۰۰ دور. ساخت کارخانه زیمنس به همراه برگه سبز گمرکی و گارانتی ۱۸ ماهه شرکتی.",
        "img_count": 1
    }
]

img_idx = 0
total_inserted_docs = 0
total_inserted_pages = 0

for doc in sample_documents:
    cur.execute("""
        INSERT INTO Documents (
            CodBayegany, DocNumber, DocDate, RegDate, DocType,
            MainSubjectId, SubSubject1Id, SubSubject2Id,
            MainPersonId, SubPerson1Id, SubPerson2Id,
            Notes, ColorTag
        ) VALUES (?, ?, ?, '1405/08/28 14:00', ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        doc["code"], doc["num"], doc["date"], doc["type"],
        doc["ms"], doc["ss1"], doc["ss2"],
        doc["mp"], doc["sp1"], doc["sp2"],
        doc["notes"], doc["tag"]
    ))
    doc_id = cur.lastrowid
    total_inserted_docs += 1

    img_count = doc["img_count"]
    for p_num in range(1, img_count + 1):
        if images:
            img_path = images[img_idx % len(images)]
            img_idx += 1
            full_blob = compress_img(img_path, 1500, 78)
            thumb_blob = make_thumb(img_path, 220, 65)
            fname = os.path.basename(img_path)
        else:
            full_blob = b"DUMMY_BLOB"
            thumb_blob = b"DUMMY_THUMB"
            fname = f"page_{p_num}.jpg"

        p_ocr = f"[صفحه {p_num} از {doc['code']}]\n" + doc["ocr"]

        cur.execute("""
            INSERT INTO DocumentPages (DocumentId, PageNumber, ImageBlob, ThumbnailBlob, PageOcrText, FileName)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (doc_id, p_num, full_blob, thumb_blob, p_ocr, fname))
        total_inserted_pages += 1

conn.commit()
conn.close()

print(f"Database successfully populated!")
print(f"Total documents inserted: {total_inserted_docs}")
print(f"Total document pages & images inserted: {total_inserted_pages}")
