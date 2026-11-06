using System.ComponentModel.DataAnnotations;

namespace HR.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "يرجى إدخال اسم المستخدم أو البريد الإلكتروني")]
    [Display(Name = "اسم المستخدم أو البريد الإلكتروني")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "يرجى إدخال كلمة المرور")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "تذكرني على هذا الجهاز")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الحالية")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "يجب ألا تقل كلمة المرور عن 6 أحرف")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    [Compare("NewPassword", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserProfileViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
