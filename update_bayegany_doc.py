# -*- coding: utf-8 -*-
"""
اسکریپت به‌روزرسانی سند طرح جامع نرم‌افزار هوشمند بایگانی اسناد
اضافه کردن ماژول و معماری کامل سیستم گزارش‌گیری جامع، سلسله‌مراتبی و ماتریسی
(موضوعات ۳ سطحی × اشخاص ۳ گانه)
"""
import os
import sys
import shutil
import docx

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import qn, nsdecls

def create_document():
    doc = docx.Document()

    # تنظیم حاشیه‌های صفحه (معادل ۱.۶۵ سانتی‌متر یا ۰.۶۵ اینچ مشابه سند اصلی)
    for section in doc.sections:
        section.top_margin = Inches(0.65)
        section.bottom_margin = Inches(0.65)
        section.left_margin = Inches(0.65)
        section.right_margin = Inches(0.65)

    # پالت رنگی حرفه‌ای سند
    COLOR_PRIMARY = RGBColor(0x1E, 0x3A, 0x8A)     # سرمه‌ای تیره اداری
    COLOR_SECONDARY = RGBColor(0x64, 0x74, 0x8B)   # خاکستری-آبی ملایم
    COLOR_HEADING = RGBColor(0x1E, 0x3A, 0x8A)     # رنگ عناوین اصلی
    COLOR_SUBHEADING = RGBColor(0x1E, 0x29, 0x3B)  # رنگ عناوین فرعی و آیتم‌ها
    COLOR_TEXT = RGBColor(0x33, 0x41, 0x55)        # رنگ متن اصلی (خوانا و استاندارد)
    COLOR_MUTED = RGBColor(0x64, 0x74, 0x8B)       # رنگ توضیحات کمرنگ
    HEX_PRIMARY = "1E3A8A"
    HEX_ROW_EVEN = "F8FAFC"
    HEX_ROW_ODD = "FFFFFF"

    # توابع کمکی قالب‌بندی پاراگراف و متن
    def set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.RIGHT, space_before=2, space_after=4, line_spacing=1.15):
        pPr = p._p.get_or_add_pPr()
        bidi = OxmlElement('w:bidi')
        bidi.set(qn('w:val'), '1')
        pPr.append(bidi)
        p.alignment = align
        p.paragraph_format.space_before = Pt(space_before)
        p.paragraph_format.space_after = Pt(space_after)
        p.paragraph_format.line_spacing = line_spacing

    def add_run(p, text, font_name="Tahoma", size_pt=9.5, bold=False, italic=False, color_rgb=COLOR_TEXT):
        run = p.add_run(text)
        run.font.name = font_name
        rPr = run._r.get_or_add_rPr()
        
        # تنظیم فونت فارسی و لاتین
        rFonts = OxmlElement('w:rFonts')
        rFonts.set(qn('w:ascii'), font_name)
        rFonts.set(qn('w:hAnsi'), font_name)
        rFonts.set(qn('w:cs'), font_name)
        rPr.append(rFonts)

        # جهت راست‌به‌چپ برای کاراکترها
        rtl = OxmlElement('w:rtl')
        rtl.set(qn('w:val'), '1')
        rPr.append(rtl)

        run.font.size = Pt(size_pt)
        run.bold = bold
        run.italic = italic
        if color_rgb:
            run.font.color.rgb = color_rgb
        return run

    def add_title(text):
        p = doc.add_paragraph()
        set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.CENTER, space_before=6, space_after=4, line_spacing=1.2)
        add_run(p, text, font_name="Tahoma", size_pt=16.0, bold=True, color_rgb=COLOR_PRIMARY)

    def add_subtitle(text):
        p = doc.add_paragraph()
        set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.CENTER, space_before=2, space_after=14, line_spacing=1.15)
        add_run(p, text, font_name="Tahoma", size_pt=9.5, bold=False, color_rgb=COLOR_SECONDARY)

    def add_heading_1(text):
        p = doc.add_paragraph()
        set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.RIGHT, space_before=16, space_after=6, line_spacing=1.2)
        add_run(p, text, font_name="Tahoma", size_pt=13.0, bold=True, color_rgb=COLOR_HEADING)

    def add_heading_2(text):
        p = doc.add_paragraph()
        set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.RIGHT, space_before=10, space_after=4, line_spacing=1.15)
        add_run(p, text, font_name="Tahoma", size_pt=11.0, bold=True, color_rgb=COLOR_SUBHEADING)

    def add_bullet_item(label, desc):
        p = doc.add_paragraph()
        set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.JUSTIFY, space_before=2, space_after=4, line_spacing=1.15)
        add_run(p, label + " ", font_name="Tahoma", size_pt=9.5, bold=True, color_rgb=COLOR_SUBHEADING)
        add_run(p, desc, font_name="Tahoma", size_pt=9.5, bold=False, color_rgb=COLOR_TEXT)

    def add_body_p(text):
        p = doc.add_paragraph()
        set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.JUSTIFY, space_before=2, space_after=4, line_spacing=1.15)
        add_run(p, text, font_name="Tahoma", size_pt=9.5, bold=False, color_rgb=COLOR_TEXT)

    def style_table(table, col_widths, headers, data_rows):
        table.alignment = WD_TABLE_ALIGNMENT.CENTER
        # راست‌به‌چپ کردن کل جدول
        tblPr = table._tbl.tblPr
        tblBidi = OxmlElement('w:bidiVisual')
        tblPr.append(tblBidi)

        # ردیف هدر
        hdr_row = table.rows[0]
        hdr_trPr = hdr_row._tr.get_or_add_trPr()
        tblHeader = OxmlElement('w:tblHeader')
        hdr_trPr.append(tblHeader)

        for col_idx, cell in enumerate(hdr_row.cells):
            tcPr = cell._tc.get_or_add_tcPr()
            # رنگ پس‌زمینه هدر
            shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{HEX_PRIMARY}"/>')
            tcPr.append(shd)
            # پدینگ سلول
            tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="80" w:type="dxa"/><w:bottom w:w="80" w:type="dxa"/><w:left w:w="90" w:type="dxa"/><w:right w:w="90" w:type="dxa"/></w:tcMar>')
            tcPr.append(tcMar)
            # متن هدر
            p = cell.paragraphs[0]
            set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.RIGHT, space_before=1, space_after=1)
            add_run(p, headers[col_idx], font_name="Tahoma", size_pt=9.0, bold=True, color_rgb=RGBColor(0xFF, 0xFF, 0xFF))

        # ردیف‌های داده
        for r_idx, row_data in enumerate(data_rows):
            row = table.rows[r_idx + 1]
            fill_color = HEX_ROW_EVEN if r_idx % 2 == 0 else HEX_ROW_ODD
            for c_idx, cell_value in enumerate(row_data):
                cell = row.cells[c_idx]
                tcPr = cell._tc.get_or_add_tcPr()
                shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_color}"/>')
                tcPr.append(shd)
                tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="65" w:type="dxa"/><w:bottom w:w="65" w:type="dxa"/><w:left w:w="75" w:type="dxa"/><w:right w:w="75" w:type="dxa"/></w:tcMar>')
                tcPr.append(tcMar)
                p = cell.paragraphs[0]
                set_p_rtl(p, align=WD_ALIGN_PARAGRAPH.RIGHT, space_before=1, space_after=1)
                is_bold = (c_idx == 0)
                add_run(p, cell_value, font_name="Tahoma", size_pt=8.5, bold=is_bold, color_rgb=COLOR_TEXT)

        # تنظیم عرض ستون‌ها
        for row in table.rows:
            for idx, width in enumerate(col_widths):
                row.cells[idx].width = width

    def add_callout_box(title, items, bg_hex="F0FDF4", border_hex="16A34A"):
        table = doc.add_table(rows=1, cols=1)
        table.alignment = WD_TABLE_ALIGNMENT.CENTER
        tblPr = table._tbl.tblPr
        tblBidi = OxmlElement('w:bidiVisual')
        tblPr.append(tblBidi)

        cell = table.cell(0, 0)
        tcPr = cell._tc.get_or_add_tcPr()
        shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{bg_hex}"/>')
        tcPr.append(shd)
        tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="90" w:type="dxa"/><w:bottom w:w="90" w:type="dxa"/><w:left w:w="140" w:type="dxa"/><w:right w:w="140" w:type="dxa"/></w:tcMar>')
        tcPr.append(tcMar)
        borders = parse_xml(f'<w:tcBorders {nsdecls("w")}><w:top w:val="none"/><w:left w:val="single" w:sz="28" w:space="0" w:color="{border_hex}"/><w:bottom w:val="none"/><w:right w:val="none"/></w:tcBorders>')
        tcPr.append(borders)
        cell.width = Inches(7.2)

        # عنوان کادر
        p_title = cell.paragraphs[0]
        set_p_rtl(p_title, align=WD_ALIGN_PARAGRAPH.RIGHT, space_before=2, space_after=4)
        add_run(p_title, title, font_name="Tahoma", size_pt=9.5, bold=True, color_rgb=RGBColor(0x15, 0x80, 0x3D) if border_hex=="16A34A" else RGBColor(0xB4, 0x53, 0x09))

        # آیتم‌های کادر
        for item in items:
            p_item = cell.add_paragraph()
            set_p_rtl(p_item, align=WD_ALIGN_PARAGRAPH.JUSTIFY, space_before=1, space_after=3)
            add_run(p_item, item, font_name="Tahoma", size_pt=9.0, bold=False, color_rgb=COLOR_TEXT)

    # -------------------------------------------------------------
    # شروع بدنه سند
    # -------------------------------------------------------------

    # ۱. عنوان و زیرعنوان سند
    add_title("طرح جامع و پیش‌نویس معماری نرم‌افزار هوشمند بایگانی اسناد و مدارک")
    add_subtitle("طراحی رابط کاربری WPF، ساختار رابطه‌ای نرمال‌شده (Relational DB)، سیستم جامع گزارش‌گیری ماتریسی (موضوعات ۳ سطحی × اشخاص ۳ گانه)، فیلد کد بایگانی (CodBayegany)، ذخیره مستقیم تصویر (BLOB) و OCR فارسی")

    # ۲. بخش اول: مقدمه، اهداف پروژه و نیازمندی‌های کلیدی
    add_heading_1("۱. مقدمه، اهداف پروژه و نیازمندی‌های کلیدی")
    add_bullet_item("• هدف کلان سیستم:", "این پروژه با هدف طراحی و تولید یک نرم‌افزار دسکتاپ بومی، سریع، امن و مستقل برای بایگانی اسناد، قراردادها، نامه‌ها و مدارک هویتی همراه با تصاویر و متن استخراج‌شده آن‌ها طراحی شده است. نرم‌افزار بر پایه معماری آزموده و سریع پروژه چک‌ها (ChekApp) و با ارتقای تخصصی ساختار پایگاه‌داده به مدل رابطه‌ای نرمال (Relational Normal Form) پیاده‌سازی می‌گردد.")
    add_bullet_item("۱. فیلد کد بایگانی (CodBayegany):", "تعریف فیلد متنی اختصاصی CodBayegany برای ثبت کد یکتای پرونده، شماره زونکن یا شناسه فیزیکی سند در بایگانی راکد/جاری جهت دسترسی سریع به اصل فیزیکی مدرک.")
    add_bullet_item("۲. ساختار پایگاه‌داده رابطه‌ای (Relational):", "جلوگیری از ورود اطلاعات تکراری با تفکیک موضوعات سه‌گانه و اشخاص سه‌گانه در جداول پایه، و ثبت صرفاً شناسه (ID) آن‌ها در جدول اصلی اسناد برای بهینه‌سازی سرعت و دقت.")
    add_bullet_item("۳. ذخیره مستقیم تصویر در دیتابیس:", "ذخیره مستقیم بایت‌های تصویر اسناد درون دیتابیس محلی SQLite به صورت باینری (BLOB) تا هیچ عکسی به مرور زمان جابه‌جا، پاک یا گم نشود.")
    add_bullet_item("۴. موتور OCR هوشمند:", "مجهز به موتور استخراج متن OCR فارسی و انگلیسی برای خواندن خودکار متن نامه‌ها و قراردادها و ذخیره متن درون دیتابیس جهت جستجوهای آینده.")
    add_bullet_item("۵. موتور جستجو و فیلترینگ چندمعیاره:", "جستجوی تمام‌متن (Full-Text Search) در محتوای کلمات اسناد و فیلترهای چندمعیاره بر اساس موضوعات، اشخاص و تاریخ شمسی.")
    add_bullet_item("۶. سیستم گزارش‌گیری جامع، سلسله‌مراتبی و ماتریسی (نیازمندی ویژه و جدید):", "امکان استخراج گزارش‌های آماری و تحلیلی از تمام اسناد ذیل یک «موضوع اصلی»، با امکان پالایش اختیاری بر اساس «موضوع فرعی ۱» و «موضوع فرعی ۲»؛ استخراج گزارش اسناد مرتبط با «شخص اصلی» با امکان پالایش بر اساس «شخص فرعی ۱» و «شخص فرعی ۲»؛ و قابلیت تلفیق ماتریسی آزاد بین موضوعات و اشخاص جهت تولید گزارش‌های مدیریتی ترکیبی بسیار دقیق با خروجی‌های رسمی PDF و اکسل.")
    add_bullet_item("۷. پرتابل و بدون وابستگی:", "عملکرد ۱۰۰٪ مستقل و آفلاین بدون نیاز به هیچ‌گونه سرور خارجی یا نرم‌افزارهای پیچیده واسط.")

    # ۳. بخش دوم: پشته فناوری
    add_heading_1("۲. پشته فناوری (Technology Stack) منطبق با الگوی ChekApp")
    t0_headers = ["لایه و بخش فنی", "فناوری و نسخه انتخابی", "مزیت و علت انتخاب در سیستم بایگانی"]
    t0_data = [
        ["زبان برنامه‌نویسی", "C# 13 (.NET 9.0)", "سرعت پردازش حداکثری، معماری مدرن و بهینه‌سازی مصرف حافظه RAM"],
        ["چارچوب رابط کاربری", "WPF با XAML مدرن", "طراحی راست‌به‌چپ روان، قابلیت Zoom/Pan اسناد و نمایشگر دوپنجره‌ای"],
        ["پایگاه‌داده (Database)", "SQLite (Microsoft.Data.Sqlite 9.0.0)", "تک‌فایلی، پرتابل، پشتیبانی از کلید خارجی (Foreign Keys) و ذخیره BLOB تا ۲ گیگابایت"],
        ["موتور ذخیره تصویر", "باینری BLOB در SQLite با فشرده‌سازی WebP", "نگهداری تصاویر اصل در کنار متادیتا، بدون پراکندگی فایل‌ها روی هارد"],
        ["موتور استخراج متن (OCR)", "Tesseract 5 فارسی + اسکریپت‌های پایتون", "استخراج متن دست‌نویس و تایپی با پیش‌پردازش تنظیم زاویه و کنتراست"],
        ["موتور گزارش‌گیری و خروجی‌ها", "سرویس اختصاصی بر پایه FlowDocument و ClosedXML", "تولید پیش‌نمایش چاپی با سربرگ اداری، خروجی‌های PDF تجمیعی و گزارش‌های اکسل غنی"],
        ["اسکریپت‌های مکمل پایتون", "پوشه Tabdil_Aks_to_Data", "پردازش دسته‌ای صدها تصویر، اصلاح چرخش (Rotation) و استخراج داده اولیه"]
    ]
    t0 = doc.add_table(rows=len(t0_data) + 1, cols=3)
    style_table(t0, [Inches(1.8), Inches(2.2), Inches(3.2)], t0_headers, t0_data)

    # ۴. بخش سوم: معماری پایگاه‌داده رابطه‌ای
    add_heading_1("۳. معماری پایگاه‌داده رابطه‌ای (Relational Database Design)")
    add_bullet_item("• مزیت ساختار نرمال‌شده و کلیدهای خارجی:", "طبق درخواست دقیق جناب‌عالی، برای جلوگیری از ثبت تکراری متون و اسامی در هر بار ورود سند، معماری پایگاه‌داده به صورت رابطه‌ای کامل طراحی می‌شود. در این الگو، موضوعات و اشخاص یک‌بار در جداول پایه تعریف می‌شوند و در جدول اسناد، صرفاً شناسه عددی (ID) آن‌ها ذخیره می‌گردد. این ساختار حجم دیتابیس را به حداقل رسانده، سرعت جستجو و گزارش‌گیری ترکیبی را تا ۱۰ برابر افزایش داده و از هرگونه غلط املایی در ثبت‌های تکراری جلوگیری می‌کند.")

    add_heading_2("جدول ۱: مشخصات و متادیتای اسناد (Documents)")
    add_body_p("جدول اصلی که شناسنامه هر سند را به همراه فیلد جدید CodBayegany و کلیدهای ارجاعی (Foreign Keys) نگهداری می‌کند:")
    t1_headers = ["نام فیلد", "نوع داده", "نوع کلید / ارجاع", "شرح و کاربرد فیلد در سیستم بایگانی"]
    t1_data = [
        ["Id", "INTEGER", "Primary Key", "شناسه عددی یکتا و اتوماتیک هر سند در بایگانی"],
        ["CodBayegany", "TEXT", "Index (یکتا/جستجو)", "کد بایگانی / شماره پرونده / شناسه زونکن فیزیکی (فیلد درخواستی)"],
        ["DocNumber", "TEXT", "Index", "شماره نامه / شماره قرارداد / شماره پیگیری مدرک"],
        ["DocDate", "TEXT", "Standard Date", "تاریخ صدور مدرک به صورت تقویم شمسی (مثلاً ۱۴۰۵/۰۸/۲۴)"],
        ["RegDate", "TEXT", "Auto Timestamp", "تاریخ و ساعت ثبت سند در سیستم نرم‌افزاری"],
        ["DocType", "TEXT", "Lookup", "نوع مدرک (نامه اداری، قرارداد، سند مالی، مدرک هویتی، صورتجلسه، ...)"],
        ["MainSubjectId", "INTEGER", "FK -> MainSubjects.Id", "آی‌دی موضوع اصلی مدرک (ارجاع به جدول موضوعات اصلی)"],
        ["SubSubject1Id", "INTEGER", "FK -> SubSubjects1.Id", "آی‌دی موضوع فرعی اول (ارجاع به جدول موضوعات فرعی سطح ۱)"],
        ["SubSubject2Id", "INTEGER", "FK -> SubSubjects2.Id", "آی‌دی موضوع فرعی دوم (ارجاع به جدول موضوعات فرعی سطح ۲)"],
        ["MainPersonId", "INTEGER", "FK -> Persons.Id", "آی‌دی شخص/شرکت اصلی مرتبط (ارجاع به جدول مرجع اشخاص)"],
        ["SubPerson1Id", "INTEGER", "FK -> Persons.Id", "آی‌دی شخص فرعی اول (ناظر، ارجاع‌شونده، وکیل، طرف دوم)"],
        ["SubPerson2Id", "INTEGER", "FK -> Persons.Id", "آی‌دی شخص فرعی دوم (شاهد قرارداد، رونوشت‌گیرنده، کارشناس)"],
        ["Notes", "TEXT", "Optional", "یادداشت‌ها، توضیحات تکمیلی یا دستورات اقدام روی سند"]
    ]
    t1 = doc.add_table(rows=len(t1_data) + 1, cols=4)
    style_table(t1, [Inches(1.2), Inches(1.1), Inches(1.9), Inches(3.0)], t1_headers, t1_data)

    add_heading_2("جدول ۲: جدول مرجع اشخاص حقیقی و حقوقی (Persons)")
    add_bullet_item("• ساختار جدول واحد اشخاص:", "یک شخص یا شرکت (مانند «شرکت ملی نفت» یا «علی محمدی») ممکن است در یک قرارداد «شخص اصلی»، در نامه‌ای دیگر «شخص فرعی اول» (ناظر)، و در سندی دیگر «شخص فرعی دوم» (شاهد یا رونوشت) باشد. برای جلوگیری از تکرار، یک جدول مرجع واحد برای اشخاص تعریف شده و هر سه فیلد اشخاص در جدول اسناد به این جدول متصل می‌شوند.")
    t2_headers = ["نام فیلد در Persons", "نوع داده", "توضیح کاربرد"]
    t2_data = [
        ["Id", "INTEGER PRIMARY KEY", "شناسه یکتای شخص (که در اسناد به عنوان PersonId ذخیره می‌شود)"],
        ["FullName", "TEXT NOT NULL", "نام و نام‌خانوادگی شخص حقیقی یا عنوان کامل شرکت/سازمان حقوقی"],
        ["PersonType", "TEXT", "نوع شخص (حقیقی، حقوقی دولتی، حقوقی خصوصی، همکار)"],
        ["NationalCode", "TEXT", "کد ملی / شناسه ملی شرکت / شماره ثبت (اختیاری جهت بررسی دقیق)"],
        ["ContactInfo", "TEXT", "شماره تماس، ایمیل یا آدرس مربوط به شخص/شرکت"]
    ]
    t2 = doc.add_table(rows=len(t2_data) + 1, cols=3)
    style_table(t2, [Inches(1.8), Inches(1.8), Inches(3.6)], t2_headers, t2_data)

    add_heading_2("جداول ۳، ۴ و ۵: جداول سلسله‌مراتبی موضوعات (سه‌سطحی)")
    add_bullet_item("• ساختار درخت موضوعی (Hierarchical Subjects):", "برای موضوعات، دقیق‌ترین شیوه ایجاد ۳ جدول مرتبط سلسله‌مراتبی است تا قابلیت انتخاب آبشاری (Cascading) و گزارش‌گیری چندسطحی فراهم شود؛ به طوری که با انتخاب موضوع اصلی، گزینه‌های فرعی ۱ و ۲ به صورت خودکار فیلتر شوند:")
    t3_headers = ["جدول پایه", "فیلدهای جدول", "ارتباط و والد (Relationship)", "مثال ملموس کاربردی"]
    t3_data = [
        ["MainSubjects (موضوع اصلی)", "Id, Title, Code, Description", "سطح اول درخت موضوعات", "امور پروژه‌های عمرانی"],
        ["SubSubjects1 (موضوع فرعی ۱)", "Id, MainSubjectId, Title, Code", "FK -> MainSubjects.Id (وابسته به سطح ۱)", "پروژه ساختمانی شریعتی"],
        ["SubSubjects2 (موضوع فرعی ۲)", "Id, SubSubject1Id, Title, Code", "FK -> SubSubjects1.Id (وابسته به سطح ۲)", "خرید اسکلت فلزی و بتن‌ریزی"]
    ]
    t3 = doc.add_table(rows=len(t3_data) + 1, cols=4)
    style_table(t3, [Inches(1.8), Inches(2.0), Inches(1.8), Inches(1.6)], t3_headers, t3_data)

    add_heading_2("نمای ترکیبی جامع پایگاه‌داده (Database View: vw_DocumentsDetails)")
    add_bullet_item("• تکنیک واکشی خودکار نام‌ها با SQL JOIN:", "برای اینکه در گرید اصلی نرم‌افزار و گزارش‌ها کدهای عددی نمایش داده نشوند، یک View استاندارد تعریف می‌شود که با اتصال جدول اسناد به جداول اشخاص و موضوعات، عناوین کامل موضوعات اصلی و فرعی و اسامی اشخاص سه‌گانه را فوراً بازیابی کرده و گزارش‌گیری ماتریسی را بدون نیاز به کوئری‌های تکراری و پیچیده انجام می‌دهد.")

    add_heading_2("ایندکس‌های ترکیبی پیشرفته جهت سرعت گزارش‌گیری (Composite Indexes)")
    add_bullet_item("• تضمین سرعت زیر ۱۰ میلی‌ثانیه در گزارش‌های ماتریسی:", "جهت پاسخ‌گویی آنی حتی با بیش از ۱۰۰,۰۰۰ سند، ۳ ایندکس چندستونی در SQLite تعریف می‌گردد:")
    add_body_p("۱. ایندکس موضوعات: CREATE INDEX idx_doc_subjects ON Documents(MainSubjectId, SubSubject1Id, SubSubject2Id);")
    add_body_p("۲. ایندکس اشخاص: CREATE INDEX idx_doc_persons ON Documents(MainPersonId, SubPerson1Id, SubPerson2Id);")
    add_body_p("۳. ایندکس ماتریسی: CREATE INDEX idx_doc_matrix ON Documents(MainSubjectId, MainPersonId, DocDate);")

    add_heading_2("جدول ۶: تصاویر و صفحات اسناد (DocumentPages - BLOB)")
    add_bullet_item("• فیلد ImageBlob (BLOB):", "تصویر باکیفیت و رزولوشن اصلی سند با فرمت بهینه WebP یا JPEG برای زوم و پرینت دقیق.")
    add_bullet_item("• فیلد ThumbnailBlob (BLOB):", "تصویر بندانگشتی بسیار سبک و کوچک جهت نمایش سریع در گالری و ردیف‌های جدول بدون کندی سرعت.")
    add_bullet_item("• فیلد PageNumber (INTEGER):", "شماره ترتیب برگه برای پرونده‌ها و قراردادهای چندصفحه‌ای.")
    add_bullet_item("• فیلد PageOcrText (TEXT):", "متن اختصاصی استخراج‌شده از همان صفحه برای دسترسی و جستجوی صفحه‌به‌صفحه.")

    add_heading_2("جدول ۷: موتور جستجوی تمام‌متن (Documents_FTS5)")
    add_bullet_item("• سرعت جستجوی کلمات در متن:", "یک جدول ایندکس مجازی با موتور داخلی FTS5 اس‌کیولایت ایجاد می‌شود که متون تمام نامه‌ها، کدهای بایگانی (CodBayegany) و متادیتای آن‌ها را ایندکس می‌کند و جستجوی کلمات درون متون را با سرعت زیر ۱۰ میلی‌ثانیه فراهم می‌سازد.")

    # ۵. بخش چهارم: طراحی صفحات و رابط کاربری (UI/UX)
    add_heading_1("۴. طراحی صفحات و رابط کاربری (UI/UX) با WPF")
    add_heading_2("الف) پنجره اصلی (MainWindow): طراحی دوپنجره‌ای هوشمند (Split-View)")
    add_bullet_item("۱. نوار ابزار فوقانی:", "شامل دکمه‌های «ثبت مدرک جدید»، «ورود دسته‌ای تصاویر»، «مدیریت اشخاص و موضوعات»، «گزارش‌گیری جامع و ماتریسی»، «پشتیبان‌گیری سریع»، «خروجی اکسل/PDF» و «تنظیمات».")
    add_bullet_item("۲. پنل فیلتر پیشرفته در سمت راست:", "فیلتر کشویی بر اساس کد بایگانی (CodBayegany)، موضوع اصلی، موضوع فرعی ۱ و ۲، اشخاص مرتبط ۱ و ۲ و ۳، بازه تاریخ صدور شمسی و نوع مدرک.")
    add_bullet_item("۳. جدول اسناد در مرکز (DataGrid):", "نمایش فهرست اسناد با قابلیت مرتب‌سازی، ستون‌های سفارشی، نشانگر تعداد صفحات و پیش‌نمایش بندانگشتی.")
    add_bullet_item("۴. نمایشگر تصویر مدرک:", "نمایش تصویر اصل سند با امکانات پیشرفته نظیر بزرگنمایی نامحدود (Zoom In/Out)، چرخش ۹۰ درجه (Rotate)، جابه‌جایی با ماوس (Pan) و کادر نمایش متن OCR با قابلیت کپی متن.")

    add_heading_2("ب) پنجره ثبت و ویرایش مدرک (DocumentEditWindow)")
    add_bullet_item("۱. کادر کد بایگانی:", "شامل فیلد متنی CodBayegany برای ثبت دستی یا تولید خودکار کد بایگانی (مثلاً: B-1405-0012 یا شماره زونکن).")
    add_bullet_item("۲. منوهای آبشاری هوشمند:", "فرم ورود اطلاعات با فیلدهای تاریخ شمسی، نوع مدرک، منوهای کشویی متصل به جداول اشخاص و موضوعات با امکان تعریف شخص یا موضوع جدید در همان لحظه با دکمه (+) کوچک کنار هر فیلد.")
    add_bullet_item("۳. بخش بارگذاری تصاویر:", "امکان درگ و دراپ (Drag & Drop) تصاویر، اتصال مستقیم به اسکنر (WIA/TWAIN) یا انتخاب فایل‌های تصویری (JPG, PNG, TIFF, PDF).")
    add_bullet_item("۴. دکمه استخراج متن با OCR:", "به محض کلیک، تصویر آنالیز شده و متن استخراج‌شده در کادر متنی نمایش می‌یابد تا کاربر بتواند در صورت نیاز آن را ویرایش یا تصحیح نماید.")

    add_heading_2("ج) پنجره ورود دسته‌ای تصاویر (Batch Import Window)")
    add_bullet_item("• مرحله اول (انتخاب پوشه):", "کاربر پوشه حاوی صدها تصویر نامه یا مدارک اسکن‌شده را انتخاب می‌کند.")
    add_bullet_item("• مرحله دوم (اصلاح چرخش):", "نرم‌افزار در پس‌زمینه (Background Worker) تمام تصاویر را لود کرده و با اسکریپت پایتون چرخش نامتعارف آن‌ها را اصلاح می‌نماید.")
    add_bullet_item("• مرحله سوم (بایگانی اتوماتیک):", "موتور OCR متن هر تصویر را استخراج کرده و تصویر و متن را به صورت دسته‌ای و با نمایش نوار پیشرفت در دیتابیس ثبت می‌کند.")

    add_heading_2("د) پنجره گزارش‌گیری جامع و ماتریسی (AdvancedReportWindow - بخش درخواستی جدید)")
    add_bullet_item("• ماژول تخصصی گزارش‌گیری چندبعدی:", "پنجره‌ای مستقل، شکیل و قدرتمند که به کاربر امکان می‌دهد با انتخاب هر یک از حالت‌های تک‌معیاره یا ترکیبی، گزارش‌های آماری و جدولی را مشاهده کرده و مستقیماً چاپ، فایل PDF یا فایل اکسل خروجی دریافت نماید.")

    # ۶. بخش پنجم: معماری سیستم گزارش‌گیری جامع و ماتریسی (فصل کاملاً جدید)
    add_heading_1("۵. معماری جامع سیستم گزارش‌گیری سلسله‌مراتبی و ماتریسی (موضوعات × اشخاص)")
    add_body_p("در پاسخ به نیاز بسیار دقیق و هوشمندانه مطرح‌شده، یک موتور گزارش‌گیری اختصاصی و ماتریسی در سیستم تعبیه شده است که تمام سناریوهای فیلترینگ مستقل و تلفیقی را با نهایت سرعت پوشش می‌دهد:")

    add_heading_2("۱-۵. سناریوهای گزارش‌گیری موضوعی سلسله‌مراتبی (Hierarchical Subject Reports)")
    add_bullet_item("• گزارش سطح ۱ (فقط موضوع اصلی):", "با انتخاب موضوع اصلی (مثلاً «امور قراردادها»)، تمام اسناد مرتبط با این موضوع اصلی — صرف‌نظر از اینکه موضوع فرعی دارند یا نه — استخراج می‌شوند. این حالت برای گزارش‌های کلان مدیریتی کاربرد دارد.")
    add_bullet_item("• گزارش سطح ۲ (موضوع اصلی + موضوع فرعی ۱):", "اگر کاربر علاوه بر موضوع اصلی، موضوع فرعی اول را نیز انتخاب کند (مثلاً «امور قراردادها» > «پروژه شریعتی»)، گزارش محدود به اسناد همین پروژه خاص می‌شود.")
    add_bullet_item("• گزارش سطح ۳ (موضوع اصلی + فرعی ۱ + فرعی ۲):", "اگر کاربر موضوع فرعی دوم را نیز تعیین کند (مثلاً «امور قراردادها» > «پروژه شریعتی» > «خرید آهن‌آلات»)، ریزترین و دقیق‌ترین گزارش از اسناد مربوط به آن سرفصل مشخص استخراج می‌گردد.")
    add_bullet_item("• انعطاف‌پذیری پویا (Optional Drill-Down):", "انتخاب سطوح فرعی ۱ و ۲ کاملاً اختیاری است. کاربر می‌تواند در هر مرحله متوقف شود یا با انتخاب زیرشاخه‌ها، گزارش را محدودتر نماید.")

    add_heading_2("۲-۵. سناریوهای گزارش‌گیری اشخاص حقیقی و حقوقی (Multi-Party Person Reports)")
    add_bullet_item("• گزارش شخص اصلی مرتبط:", "استخراج تمام قراردادها، نامه‌ها و مدارکی که شخص اصلی آن یک فرد یا شرکت خاص (مثلاً «شرکت ملی نفت» یا «علی محمدی») است.")
    add_bullet_item("• گزارش هم‌زمان شخص اصلی + شخص فرعی اول:", "استخراج اسنادی که شخص اصلی آن شرکت الف و شخص فرعی اول آن (ناظر، نماینده، وکیل یا طرف دوم) شخص ب است.")
    add_bullet_item("• گزارش هم‌زمان شخص اصلی + شخص فرعی ۱ + شخص فرعی ۲:", "استخراج اسناد حساسی که هر سه شخص در آن حضور دارند (مثلاً شرکت الف به عنوان کارفرما، شرکت ب به عنوان پیمانکار و مهندس ج به عنوان ناظر یا شاهد).")
    add_bullet_item("• قابلیت کارنامه ۳۶۰ درجه شخص:", "امکان استخراج تمام اسنادی که یک شخص خاص در هر یک از جایگاه‌های سه‌گانه (اصلی، فرعی ۱ یا فرعی ۲) حضور داشته است؛ این ویژگی برای یافتن تمام سوابق یک فرد در سازمان حیاتی است.")

    add_heading_2("۳-۵. ماتریس تقاطعی و گزارش‌های ترکیبی چندبعدی (موضوعات × اشخاص)")
    add_bullet_item("• قدرت ترکیب نامحدود:", "کاربر می‌تواند به صورت کاملاً آزاد، هر ترکیبی از موضوعات (اصلی، فرعی ۱، فرعی ۲) را با هر ترکیبی از اشخاص (اصلی، فرعی ۱، فرعی ۲) پیوند دهد:")

    # جدول سناریوهای ترکیبی
    t_rep_headers = ["عنوان سناریو گزارش", "ترکیب فیلتر موضوعات", "ترکیب فیلتر اشخاص", "خروجی کاربردی در سازمان"]
    t_rep_data = [
        ["گزارش کلان موضوعی", "موضوع اصلی: امور پروژه‌ها", "(بدون فیلتر شخص)", "فهرست جامع کلیه مدارک پروژه‌های سازمان"],
        ["گزارش پروژه با شخص اصلی", "موضوع اصلی + فرعی ۱: پروژه شریعتی", "شخص اصلی: شرکت سازنده الف", "کلیه اسناد و توافقات شرکت سازنده در پروژه شریعتی"],
        ["گزارش فاز اجرایی با ناظر", "موضوع فرعی ۱ و ۲: خرید اسکلت فلزی", "شخص فرعی ۱: مهندس ناظر رضایی", "اسناد تاییدشده خرید اسکلت تحت نظارت مهندس رضایی"],
        ["گزارش ماتریسی کامل سه‌به‌سه", "موضوع اصلی + فرعی ۱ + فرعی ۲", "شخص اصلی + فرعی ۱ + فرعی ۲", "قراردادهای پیمانکاری معین با حضور کارفرما، پیمانکار و شاهد"],
        ["گزارش بازه زمانی با موضوع و شخص", "موضوع اصلی معین + بازه تاریخی شمسی", "شخص اصلی معین", "گزارش مالی/حقوقی یک‌ساله یک شرکت در یک موضوع مشخص"]
    ]
    t_rep = doc.add_table(rows=len(t_rep_data) + 1, cols=4)
    style_table(t_rep, [Inches(1.8), Inches(1.8), Inches(1.8), Inches(1.8)], t_rep_headers, t_rep_data)

    add_heading_2("۴-۵. امکانات و ابزارهای پنجره گزارش‌گیری (AdvancedReportWindow)")
    add_bullet_item("۱. پنل بالایی فیلترهای آبشاری موضوعات:", "سه منوی کشویی هوشمند که با تغییر موضوع اصلی، زیرشاخه‌های فرعی ۱ و ۲ فوراً بروزرسانی می‌شوند، به همراه دکمه پاک‌کردن فیلتر.")
    add_bullet_item("۲. پنل فیلترهای اشخاص با جستجوی سریع:", "سه کادر هوشمند برای انتخاب شخص اصلی، فرعی ۱ و فرعی ۲ با امکان جستجوی نام، کدملی یا تلفن.")
    add_bullet_item("۳. فیلتر بازه تاریخ صدور مدرک:", "دو تقویم بازشوی شمسی (از تاریخ / تا تاریخ) برای محدودکردن گزارش به یک دوره زمانی مشخص.")
    add_bullet_item("۴. کارت‌های خلاصه آماری (KPI Cards):", "نمایش شاخص‌های مهم در بالای گزارش شامل: تعداد کل اسناد یافت‌شده، تعداد کل برگه‌های اسکن‌شده، تاریخ اولین سند و تاریخ آخرین سند.")
    add_bullet_item("۵. جدول نتایج تعاملی (Interactive DataGrid):", "نمایش ردیف به ردیف اسناد با کد بایگانی (CodBayegany)، تاریخ شمسی، عناوین موضوعات، اسامی اشخاص و پیش‌نمایش بندانگشتی تصویر. دابل‌کلیک روی هر ردیف، اصل سند و تصویر را باز می‌کند.")
    add_bullet_item("۶. دکمه‌های صدور و چاپ آنی:", "شامل «چاپ مستقیم با سربرگ اداری»، «خروجی فایل اکسل (Excel) با فرمت‌بندی کامل» و «تولید فایل PDF تجمیعی با تصاویر اسناد».")

    # کادر طلایی خلاصه کوئری دیتابیس
    add_callout_box("💡 معماری کوئری پویا در لایه پایگاه‌داده (Dynamic Parameterized SQL)", [
        "کوئری‌های گزارش‌گیری در لایه دیتابیس با تکنیک Dynamic WHERE Clause پیاده‌سازی می‌شوند؛ یعنی سیستم به ازای هر فیلتری که کاربر انتخاب کرده است، شرط آن را با پارامتر امن SQL اضافه می‌کند: WHERE (@MainSubjectId IS NULL OR MainSubjectId = @MainSubjectId) AND (@SubSubject1Id IS NULL OR SubSubject1Id = @SubSubject1Id) ... این ساختار علاوه بر امنیت کامل در برابر SQL Injection، به لطف ایندکس‌های ترکیبی، گزارش را ظرف کسری از ثانیه آماده می‌سازد."
    ], bg_hex="FEF9C3", border_hex="CA8A04")

    # ۷. بخش ششم: موتور استخراج متن (OCR)
    add_heading_1("۶. موتور استخراج متن (OCR فارسی و انگلیسی)")
    add_callout_box("📌 ویژگی‌های فنی موتور OCR و پیش‌پردازش تصاویر", [
        "۱. موتور پیش‌فرض آفلاین (Tesseract 5): مجهز به دیتاست اختصاصی زبان فارسی (fas.traineddata) به همراه زبان انگلیسی برای اسناد دو زبانه.",
        "۲. خط لوله پیش‌پردازش تصویر (Pre-processing Pipeline): قبل از ارسال تصویر به OCR، عملیات اصلاح زاویه کجی (Deskewing)، حذف نویز اسکن و افزایش کنتراست (Binarization) انجام می‌شود تا دقت خوانایی متون دست‌نویس و تایپی تا ۳ برابر افزایش یابد.",
        "۳. امکان ادغام با اسکریپت‌های پایتون در پوشه Tabdil_Aks_to_Data: استفاده از کتابخانه‌های پردازش عمیق پایتون در مواقعی که کیفیت سند بسیار پایین یا نیازمند تصحیح زوایای نامتعارف است."
    ], bg_hex="F0FDF4", border_hex="16A34A")

    # ۸. بخش هفتم: ساختار معماری و سازماندهی سورس‌کد پروژه
    add_heading_1("۷. ساختار معماری و سازماندهی سورس‌کد پروژه")
    add_bullet_item("• ساختار فایل‌ها و پوشه‌ها:", "پروژه با ساختار ماژولار، استاندارد و بسیار تمیز مشابه ChekApp در آدرس D:\\Projects_AI\\tahgigat\\Khodam\\Bayegany به شرح جدول زیر ایجاد خواهد شد:")
    t5_headers = ["پوشه / فایل پروژه", "نقش و وظیفه فنی در سیستم بایگانی"]
    t5_data = [
        ["BayeganyApp.csproj", "فایل اصلی پروژه دات‌نت ۹ با فعال‌بودن WPF و رفرنس‌های رسمی SQLite"],
        ["App.xaml / App.xaml.cs", "نقطه ورود برنامه، تنظیم استایل‌های فونت و استارتاپ فرم"],
        ["MainWindow.xaml / .cs", "پنجره اصلی بایگانی با چیدمان دوپنجره‌ای، نوار ابزار و مدیریت اسناد"],
        ["Windows/", "پنجره‌های DocumentEditWindow، AdvancedReportWindow (گزارش ماتریسی)، ManagePersonsWindow، ManageSubjectsWindow و BatchImportWindow"],
        ["Controls/", "کنترل تقویم شمسی (PersianDatePicker)، کنترل فیلترهای آبشاری موضوعات، و کنترل حرفه‌ای نمایش تصویر با زوم و چرخش"],
        ["Database/", "کلاس DatabaseHelper برای مدیریت SQLite، کلیدهای خارجی (FK)، کوئری‌های فیلتر ماتریسی و جداول BLOB"],
        ["Models/", "کلاس‌های داده DocumentItem، DocumentReportModel (مدل گزارش‌های ترکیبی)، DocumentPage، PersonItem و SubjectItem"],
        ["Services/", "سرویس استخراج متن OCR، سرویس فشرده‌سازی عکس، سرویس خروجی اکسل و سرویس تولید PDF و چاپ رسمی (ReportService)"],
        ["Data/", "پوشه محل استقرار فایل پایگاه‌داده محلی bayegany.db"],
        ["Tabdil_Aks_to_Data/", "اسکریپت‌های پایتون برای پردازش دسته‌ای تصاویر، چرخش خودکار و استخراج اولیه"],
        ["bayegany.bat", "فایل اجرایی بچ برای اجرای مستقیم خروجی نهایی و بهینه‌شده Release"]
    ]
    t5 = doc.add_table(rows=len(t5_data) + 1, cols=2)
    style_table(t5, [Inches(2.4), Inches(4.8)], t5_headers, t5_data)

    # ۹. بخش هشتم: امکانات ویژه و ارزش‌افزوده پیشنهادی
    add_heading_1("۸. امکانات ویژه و ارزش‌افزوده پیشنهادی")
    add_bullet_item("۱. سیستم پشتیبان‌گیری خودکار تک‌فایلی:", "پشتیبان‌گیری آنی با ۱ کلیک؛ با توجه به اینکه عکس‌ها درون دیتابیس هستند، یک کپی از فایل دیتابیس به همراه تاریخ روی فلش یا هارد ذخیره می‌شود.")
    add_bullet_item("۲. امنیت و رمزنگاری اسناد محرمانه:", "امکان تعریف کلمه عبور برای نرم‌افزار یا فعال‌سازی قابلیت رمزگذاری داخلی SQLite (SQLCipher) برای اسناد محرمانه و قراردادهای حساس.")
    add_bullet_item("۳. خروجی اکسل و تولید PDF تجمیعی و رسمی:", "تهیه گزارش اکسل از لیست نامه‌ها یا قراردادها بر اساس فیلترهای ماتریسی اعمال‌شده، به همراه امکان ادغام صفحات یک پرونده و خروجی یک فایل PDF واحد رسمی با سربرگ.")
    add_bullet_item("۴. برچسب‌گذاری بصری (Color Tags):", "قابلیت تخصیص برچسب‌های رنگی (مثل: قرارداد جاری، اقدام فوری، پرداخت‌شده) برای تشخیص بصری سریع در جدول و گزارش‌ها.")

    # ۱۰. بخش نهم: نقشه راه و مراحل اجرایی
    add_heading_1("۹. نقشه راه و مراحل اجرایی (Roadmap)")
    add_callout_box("📌 مراحل توسعه گام‌به‌گام پس از تایید نهایی طرح", [
        "گام اول: بررسی و اعلام‌نظر نهایی جناب‌عالی بر روی این پیش‌نویس اصلاح‌شده و تایید آغاز فاز کدنویسی.",
        "گام دوم: ایجاد پروژه C# .NET 9.0 WPF در پوشه Bayegany و اتصال پکیج‌های Microsoft.Data.Sqlite و موتور OCR و کامپوننت‌های گزارش‌گیری.",
        "گام سوم: ساخت دیتابیس bayegany.db شامل جدول Documents (با فیلد CodBayegany)، جداول پایه Persons و موضوعات سه‌گانه، ایندکس‌های ترکیبی گزارش‌گیری، جدول تصاویر BLOB و ایندکس FTS5.",
        "گام چهارم: طراحی رابط کاربری پنجره اصلی (نمای دوپنجره‌ای با تقویم شمسی، منوهای آبشاری و فیلترهای چندمعیاره).",
        "گام پنجم: پیاده‌سازی پنجره اختصاصی گزارش‌گیری ماتریسی (AdvancedReportWindow) با قابلیت فیلتر سلسله‌مراتبی موضوعات (۱، ۲، ۳) و اشخاص (۱، ۲، ۳) و ترکیب آزاد آن‌ها با صدور اکسل و PDF.",
        "گام ششم: پیاده‌سازی ماژول OCR فارسی، دکمه‌های تعریف سریع شخص/موضوع و پنجره ثبت/ورود دسته‌ای مدارک.",
        "گام هفتم: تست عملیاتی با مجموعه‌ای از تصاویر واقعی نامه‌ها و قراردادها و بیلد نهایی در فایل bayegany.bat."
    ], bg_hex="FEF9C3", border_hex="CA8A04")

    # مسیرهای ذخیره‌سازی
    out_dir = r"D:\Projects_AI\tahgigat\Khodam\Bayegany"
    file_new = os.path.join(out_dir, "پیش نویس طرح نرم افزار بایگانی_جدید.docx")
    file_noyesh = os.path.join(out_dir, "پیش نویش طرح نرم افزار بایگانی.docx")
    file_orig = os.path.join(out_dir, "پیش نویس طرح نرم افزار بایگانی.docx")

    # ذخیره در فایل جدید با ویرایش جامع
    doc.save(file_new)
    print(f"Saved successfully: {file_new}")

    # ذخیره در فایل پیش نویش (که قفل نیست)
    try:
        shutil.copyfile(file_new, file_noyesh)
        print(f"Copied successfully to: {file_noyesh}")
    except Exception as e:
        print(f"Could not copy to {file_noyesh}: {e}")

    # تلاش برای ذخیره در فایل اصلی در صورت بسته بودن در ورد
    try:
        shutil.copyfile(file_new, file_orig)
        print(f"Overwrote original file successfully: {file_orig}")
    except Exception as e:
        print(f"Original file is currently open in Word (locked): {e}")

if __name__ == "__main__":
    create_document()
