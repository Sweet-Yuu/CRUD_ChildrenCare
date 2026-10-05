using System.Security.Claims;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Security;
using CRUD_ChildrenCare.Services.Accounts;
using CRUD_ChildrenCare.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRUD_ChildrenCare.Controllers;

public sealed class AccountController(IAccountService accountService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleHome(User.FindFirstValue(ClaimTypes.Role));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accountService.ValidateCredentialsAsync(
            new LoginCommand(model.Email, model.Password),
            cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ApplicationCookieEvents.CreatePrincipal(result.User),
            new AuthenticationProperties { IsPersistent = false });

        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl!)
            : RedirectToRoleHome(result.User.Role.Value);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var verificationUrl = Url.Action(
            nameof(VerifyEmail),
            "Account",
            values: null,
            protocol: Request.Scheme)!;
        var result = await accountService.RegisterAsync(
            new RegisterCommand(
                model.FullName,
                model.Gender,
                model.Email,
                model.Mobile,
                model.Address,
                model.Password,
                model.ConfirmPassword,
                verificationUrl),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(
        int userId,
        string token,
        CancellationToken cancellationToken)
    {
        var result = await accountService.VerifyEmailAsync(
            new VerifyEmailCommand(userId, token),
            cancellationToken);
        return View("Message", new AccountMessageViewModel("Email verification", result.Message, result.Succeeded));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetUrl = Url.Action(
            nameof(ResetPassword),
            "Account",
            values: null,
            protocol: Request.Scheme)!;
        await accountService.RequestPasswordResetAsync(
            new PasswordResetRequestCommand(model.Email, resetUrl),
            cancellationToken);
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(int userId, string token) =>
        View(new ResetPasswordViewModel { UserId = userId, Token = token });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accountService.ResetPasswordAsync(
            new ResetPasswordCommand(
                model.UserId,
                model.Token,
                model.NewPassword,
                model.ConfirmPassword),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!int.TryParse(User.FindFirstValue(AppClaimTypes.UserId), out var userId))
        {
            return Challenge();
        }

        var result = await accountService.ChangePasswordAsync(
            new ChangePasswordCommand(
                userId,
                model.CurrentPassword,
                model.NewPassword,
                model.ConfirmPassword),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(ChangePassword));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/AccessDenied.cshtml");
    }

    private IActionResult RedirectToRoleHome(string? roleValue) => roleValue switch
    {
        SystemData.RoleValues.Admin => Redirect("/Admin/Users"),
        SystemData.RoleValues.Manager => Redirect("/Admin"),
        SystemData.RoleValues.Doctor or SystemData.RoleValues.Nurse => Redirect("/Staff"),
        _ => Redirect("/")
    };
}
