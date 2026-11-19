using System;

namespace BayeganyApp.Models
{
    public class UserItem
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "کاربر بایگانی";
        
        public bool CanAddDoc { get; set; } = true;
        public bool CanEditDoc { get; set; } = true;
        public bool CanDeleteDoc { get; set; } = false;
        public bool CanManagePersons { get; set; } = true;
        public bool CanManageSubjects { get; set; } = true;
        public bool CanViewReports { get; set; } = true;
        public bool CanBackupRestore { get; set; } = false;
        public bool CanManageUsers { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
        public string CreatedAt { get; set; } = string.Empty;

        // ویژگی‌های کمکی برای نمایش در جدول
        public string StatusTitle => IsActive ? "فعال" : "غیرفعال";
        public string RoleTitle => Role switch
        {
            "Admin" => "مدیر سیستم (دسترسی کامل)",
            "Operator" => "اپراتور بایگانی",
            "Viewer" => "مشاهده‌گر / کارشناس",
            _ => Role
        };
    }

    public static class UserSession
    {
        public static UserItem? CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser != null && (CurrentUser.Role == "Admin" || CurrentUser.Role == "مدیر سیستم (دسترسی کامل)" || CurrentUser.CanManageUsers);
        
        public static bool CanAddDoc => CurrentUser != null && (IsAdmin || CurrentUser.CanAddDoc);
        public static bool CanEditDoc => CurrentUser != null && (IsAdmin || CurrentUser.CanEditDoc);
        public static bool CanDeleteDoc => CurrentUser != null && (IsAdmin || CurrentUser.CanDeleteDoc);
        public static bool CanManagePersons => CurrentUser != null && (IsAdmin || CurrentUser.CanManagePersons);
        public static bool CanManageSubjects => CurrentUser != null && (IsAdmin || CurrentUser.CanManageSubjects);
        public static bool CanViewReports => CurrentUser != null && (IsAdmin || CurrentUser.CanViewReports);
        public static bool CanBackupRestore => CurrentUser != null && (IsAdmin || CurrentUser.CanBackupRestore);
        public static bool CanManageUsers => CurrentUser != null && (IsAdmin || CurrentUser.CanManageUsers);
    }
}
