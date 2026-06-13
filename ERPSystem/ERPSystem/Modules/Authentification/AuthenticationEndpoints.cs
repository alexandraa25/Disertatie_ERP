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
              .RequireAuthorization(policy => policy.RequireRole("Admin"))
              .WithDefaultApiSettings( "RegisterUser", "Creează un utilizator nou", "REGISTER", false);

            group.MapPost(Route.CONFIRM_EMAIL_REGISTRATION,
              async (ConfirmEmail confirmEmail, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.ConfirmEmailAsync(confirmEmail, userManager))
              .WithDefaultApiSettings("ConfirmEmailAsync", "Confirmare email utilizator", "CONFIRM_EMAIL", false);

            group.MapPost(Route.LOGIN,
              async (LoginRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.LoginAsync(request, signInManager, userManager))
              .WithDefaultApiSettings("LoginUser", "Autentifică utilizatorul și returnează token-ul", "LOGIN", false);

            group.MapPost(Route.CONFIRM_LOGIN_CODE,
              async (ConfirmLoginRequest confirmLoginRequest, SignInManager<ApplicationUser> signInManager, HttpContext httpContext, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.ConfirmLoginRequest(confirmLoginRequest, httpContext, signInManager, userManager))
              .WithDefaultApiSettings("ConfirmLoginRequest", "Confirmare cod login", "CONFIRM_LOGIN_CODE", false);

            group.MapPost(Route.RESEND_LOGIN_CODE,
              async (ResendCodeRequest resendCodeRequest, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                  => await authService.ResendLoginCodeAsync(resendCodeRequest, signInManager, userManager))
              .WithDefaultApiSettings("ResendLoginCodeAsync", "Retrimitere cod login", "RESEND_LOGIN_CODE", false);

            group.MapPost(Route.FORGOT_PASSWORD,
               async (ForgotPasswordRequest forgotPasswordRequest, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                => await authService.ForgotPasswordAsync(forgotPasswordRequest, userManager))
               .WithDefaultApiSettings("ForgotPasswordAsync", "Inițiere resetare parolă", "FORGOT_PASSWORD", false);

            group.MapPost(Route.RESET_PASSWORD,
              async (ResetPasswordRequest resetPasswordRequest, UserManager<ApplicationUser> userManager, AuthentificationService authService)
                => await authService.ResetPasswordAsync(resetPasswordRequest, userManager))
              .WithDefaultApiSettings("ResetPasswordAsync", "Reset Password Async", "RESET_PASSWORD", false);

            group.MapGet(Route.GET_ROLES,
              async (AuthentificationService authService)
                  => await authService.GetRolesAsync())
              .RequireAuthorization(policy => policy.RequireRole("Admin"))
              .WithDefaultApiSettings( "GetRoles", "Returnează lista rolurilor disponibile", "GET_ROLES", false );

            group.MapPost(Route.CHANGE_PASSWORD,
              async (ChangePasswordRequest request, UserManager<ApplicationUser> userManager, AuthentificationService authService, HttpContext httpContext)
                   => await authService.ChangePasswordAsync(request, userManager, httpContext)) .RequireAuthorization()
              .WithDefaultApiSettings("ChangePasswordAsync", "Schimbare parolă utilizator", "CHANGE_PASSWORD", false);

            group.MapPost(Route.LOGOUT,
               (HttpContext context) =>
               {
                   context.Response.Cookies.Delete("Token");
              
                   return Results.Ok(new
                   {
                       isSuccess = true
                   });
               })
               .WithDefaultApiSettings("LogoutUser", "Logs out the current user", "LOGOUT", false);
        }
    }
}
