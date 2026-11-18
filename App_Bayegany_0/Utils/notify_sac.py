import os
import tkinter as tk
from tkinter import messagebox

# باز کردن صفحه تنظیمات امنیت ویندوز
os.system("start windowsdefender://smartappcontrol")

root = tk.Tk()
root.withdraw()
root.attributes("-topmost", True)

title = "سیستم مدیریت بایگانی اسناد"
msg = (
    "قابلیت امنیتی Smart App Control در ویندوز ۱۱ فعال است و مانع اجرای نرم‌افزارهای محلی می‌شود.\n\n"
    "صفحه تنظیمات امنیتی هم‌اکنون در پس‌زمینه برای شما باز شد.\n"
    "لطفاً در آن پنجره، گزینه Smart App Control را روی Off (خاموش) قرار دهید تا برنامه اجرا گردد."
)

messagebox.showwarning(title, msg)
