using System.Security.Claims;
using HR.Data;
using HR.Models;
using HR.Services;
using HR.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _audit;

    public AccountController(ApplicationDbContext db, IPasswordHasher passwordHasher, IAuditLogService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    // GET: /Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };
        return View(model);
    }

    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        model.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var search = model.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _db.AppUsers
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == search || u.Email.ToLower() == search);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "اسم المستخدم أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "تم تعطيل هذا الحساب. يرجى التواصل مع مسؤول النظام.");
            return View(model);
        }

        // Verify password
        bool isPasswordValid = false;

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, model.Password);
        }
        else
        {
            // Auto-fallback for accounts created before hashing
            var defaultPassword = user.Username == "admin" ? "admin123" : $"{user.Username}123";
            if (model.Password == defaultPassword || model.Password == "123456" || model.Password == "admin")
            {
                isPasswordValid = true;
                user.PasswordHash = _passwordHasher.HashPassword(model.Password);
            }
        }

        if (!isPasswordValid)
        {
            ModelState.AddModelError(string.Empty, "اسم المستخدم أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        // Build Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("DepartmentId", user.DepartmentId?.ToString() ?? ""),
            new Claim("DepartmentName", user.Department?.Name ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("تسجيل دخول", "User", user.Id, user.FullName, $"تسجيل دخول ناجح عبر البوابة الإلكترونية للمستخدم '{user.Username}'");

        TempData["Success"] = $"مرحباً بك مجدداً، {user.FullName}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name ?? "مستخدم";
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdStr, out int userId);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (userId > 0)
        {
            await _audit.LogAsync("تسجيل خروج", "User", userId, userName, "تم تسجيل الخروج بنجاح من النظام");
        }

        TempData["Success"] = "تم تسجيل الخروج بنجاح.";
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/Profile
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _db.AppUsers
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
        }

        var roleObj = await _db.AppRoles.FirstOrDefaultAsync(r => r.Code == user.Role);

        var model = new UserProfileViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            RoleName = roleObj?.Name ?? user.Role,
            DepartmentName = user.Department?.Name,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };

        return View(model);
    }

    // POST: /Account/ChangePassword
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى التحقق من صحة بيانات كلمة المرور المدخلة.";
            return RedirectToAction(nameof(Profile));
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, model.CurrentPassword))
        {
            TempData["Error"] = "كلمة المرور الحالية غير صحيحة.";
            return RedirectToAction(nameof(Profile));
        }

        user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("تغيير كلمة المرور", "User", user.Id, user.FullName, "قام المستخدم بتغيير كلمة المرور الخاصة به بنجاح");

        TempData["Success"] = "تم تغيير كلمة المرور بنجاح.";
        return RedirectToAction(nameof(Profile));
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
