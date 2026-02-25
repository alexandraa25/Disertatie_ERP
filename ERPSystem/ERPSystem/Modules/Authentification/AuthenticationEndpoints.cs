using ERPSystem.Data.Entities;
using ERPSystem.Extensions;
using ERPSystem.Modules.Authentification.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using LoginRequest = ERPSystem.Modules.Authentification.Models.LoginRequest;
using RegisterRequest = ERPSystem.Modules.Authentification.Models.RegisterRequest;
using ResetPasswordRequest = ERPSystem.Modules.Authentification.Models.ResetPasswordRequest;
using Route = ERPSystem.Utils.Constants.General.Route;

namespace ERPSystem.Modules.Authentificate
{
    public static class AuthenticationEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            group.MapPost(Route.REGISTER,
                async (RegisterRequest request, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                    => await authService.RegisterUserAsync(request, userManager))
                .WithDefaultApiSettings("RegisterUser", "Creates a new user with extended fields", "REGISTER", false);

            group.MapPost(Route.CONFIRM_EMAIL_REGISTRATION,
                async (ConfirmEmail confirmEmail, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                    => await authService.ConfirmEmailAsync(confirmEmail, userManager))
                .WithDefaultApiSettings("ConfirmEmailAsync", "Confirm Email Async", "CONFIRM_EMAIL", false);

            group.MapPost(Route.LOGIN,
              async (LoginRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.LoginAsync(request, signInManager, userManager))
              .WithDefaultApiSettings("LoginUser", "Authenticates a user and returns token", "LOGIN", false);

            group.MapPost(Route.CONFIRM_LOGIN_CODE,
              async (ConfirmLoginRequest confirmLoginRequest, SignInManager<ApplicationUser> signInManager, HttpContext httpContext, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.ConfirmLoginRequest(confirmLoginRequest, httpContext, signInManager, userManager))
              .WithDefaultApiSettings("ConfirmLoginRequest", "Confirm Login Request", "CONFIRM_LOGIN_CODE", false);

            group.MapPost(Route.RESEND_LOGIN_CODE,
              async (ResendCodeRequest resendCodeRequest, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.ResendLoginCodeAsync(resendCodeRequest, signInManager, userManager))
              .WithDefaultApiSettings("ResendLoginCodeAsync", "Resend Login Code Async", "RESEND_LOGIN_CODE", false);

            group.MapPost(Route.FORGOT_PASSWORD,
            async (ForgotPasswordRequest forgotPasswordRequest, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                => await authService.ForgotPasswordAsync(forgotPasswordRequest, userManager))
               .WithDefaultApiSettings("ForgotPasswordAsync", "Forgot Password Async", "FORGOT_PASSWORD", false);

            group.MapPost(Route.RESET_PASSWORD,
            async (ResetPasswordRequest resetPasswordRequest, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                => await authService.ResetPasswordAsync(resetPasswordRequest, userManager))
              .WithDefaultApiSettings("ResetPasswordAsync", "Reset Password Async", "RESET_PASSWORD", false);
        }
    }
}
