using ERPSystem.Data.Entities;
using ERPSystem.Modules.Authentification.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using ERPSystem.Utils.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoginRequest = ERPSystem.Modules.Authentification.Models.LoginRequest;
using RegisterRequest = ERPSystem.Modules.Authentification.Models.RegisterRequest;

namespace ERPSystem.Modules.Authentificate
{
    public class AuthentificationService
    {
        #region Properties

        private ILogger<AuthentificationService> _logger;
        private EmailBusinessLogic _emailBusinessLogic;
        private IOptions<ERPSystemSettings> _ERPSystemSettings;
        private IOptions<JwtSettings> _jwtSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        #endregion

        #region Constructors
        public AuthentificationService(ILogger<AuthentificationService> logger, EmailBusinessLogic emailBusinessLogic,
            IOptions<ERPSystemSettings> ERPSystemSettings, IOptions<JwtSettings> jwtSettings, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _emailBusinessLogic = emailBusinessLogic;
            _ERPSystemSettings = ERPSystemSettings;
            _jwtSettings = jwtSettings;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        #endregion

        #region Methods

        public async Task<PublicResponse> RegisterUserAsync(RegisterRequest request, UserManager<ApplicationUser> userManager)
        {
            PublicResponse publicResponse = new PublicResponse(true);

            try
            {
                var userByEmail = await userManager.FindByEmailAsync(request.Email);
                if (userByEmail != null)
                    return publicResponse.SetError(ErrorCodes.EmailAlreadyExist, ErrorMessages.EmailAlreadyExist);

                var userByUsername = await userManager.FindByNameAsync(request.Username);
                if (userByUsername != null)
                    return publicResponse.SetError(ErrorCodes.EmailAlreadyExist, ErrorMessages.EmailAlreadyExist);

                ApplicationUser newUser = new ApplicationUser(userName: request.Username, email: request.Email, firstName: request.FirstName, lastName: request.LastName)
                {
                    PhoneNumber = request.PhoneNumber,
                    IsActive = true,
                    MustChangePassword = true,
                    EmailConfirmed = false, // foarte important
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newUser, request.Password);

                if (!result.Succeeded)
                    return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);

                if (!string.IsNullOrEmpty(request.Role))
                {
                    await userManager.AddToRoleAsync(newUser, request.Role);

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(newUser);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var confirmationUrl = $"{_ERPSystemSettings.Value.BaseUrl}/confirm-email-registration" + $"?userId={newUser.Id}&token={encodedToken}";
                    List<string> to = new List<string>() { newUser.Email };

                    await _emailBusinessLogic.SendEmailTemplateAsync(TemplateCode.EMAIL_REGISTRATION_CONFIRMATION, JsonConvert.SerializeObject(newUser), to, confirmationUrl);

                    return publicResponse.SetCreated();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error occurred while registering user.");
                return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
            return publicResponse;
        }


        public async Task<PublicResponse> ConfirmEmailAsync(ConfirmEmail confirmEmail, UserManager<ApplicationUser> userManager)
        {
            PublicResponse response = new PublicResponse(true);

            try
            {
                if (string.IsNullOrWhiteSpace(confirmEmail.UserId) || string.IsNullOrWhiteSpace(confirmEmail.Token))
                    return response.SetError(ErrorCodes.InvalidParameters, ErrorMessages.InvalidParameters);

                var user = await userManager.FindByIdAsync(confirmEmail.UserId);
                if (user == null)
                    return response.SetError(ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);

                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(confirmEmail.Token));

                var result = await userManager.ConfirmEmailAsync(user, decodedToken);

                if (!result.Succeeded)
                    return response.SetError(ErrorCodes.InvalidToken, ErrorMessages.InvalidToken);

                return response.SetCreated();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming email.");
                return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        public async Task<PublicResponse> LoginAsync(LoginRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)

        {
            PublicResponse response = new PublicResponse(true);

            try
            {
                var user = await userManager.FindByEmailAsync(request.Email)
                           ?? await userManager.FindByNameAsync(request.UserName);
                if (user == null)
                    return response.SetError(ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);

                if (!user.IsActive)
                    return response.SetError(ErrorCodes.InvalidParameters, "Account is inactive.");

                if (!user.EmailConfirmed)
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

                    var encodedToken = WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(token)
                    );

                    var link = $"http://localhost:4200/confirm-email?userId={user.Id}&token={encodedToken}";
                    Console.WriteLine("LINK: " + link);

                    await _emailBusinessLogic.SendEmailTemplateAsync(templateCode: TemplateCode.EMAIL_REGISTRATION_CONFIRMATION, tableRow: JsonConvert.SerializeObject(user),
                        url: link,
                        to: new List<string> { user.Email }
                    );

                    return response.SetSuccess(new
                    {
                        emailNotConfirmed = true,
                        userId = user.Id
                    });
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);

                if (!result.Succeeded)
                    return response.SetError(ErrorCodes.InvalidParameters, ErrorMessages.InvalidCredentials);
                //if (user.MustChangePassword)
                //{
                //    return response.SetSuccess(new
                //    {
                //        requiresPasswordChange = true,
                //        userId = user.Id
                //    });
                //}

                var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                List<string> to = new List<string>() { user.Email };
                await _emailBusinessLogic.SendEmailTemplateAsync(TemplateCode.LOGIN_CONFIRMATION,
                    JsonConvert.SerializeObject(new { Code = code }), to);

                var tempToken = GenerateTempToken(user);

                return response.SetSuccess(new
                {
                    requiresCode = true,
                    tempToken = tempToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login.");
                return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        public async Task<PublicResponse> ConfirmLoginRequest(ConfirmLoginRequest request, HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            PublicResponse response = new PublicResponse(true);

            try
            {

                var handler = new JwtSecurityTokenHandler();
                var jwtTemp = handler.ReadJwtToken(request.TempToken);

                var userId = jwtTemp.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return response.SetError(ErrorCodes.InvalidToken, ErrorMessages.InvalidToken);

                var user = await userManager.FindByIdAsync(userId);


                if (user == null)
                    return response.SetError(ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);

                var isValid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, request.Code);

                if (!isValid)
                    return response.SetError(ErrorCodes.InvalidToken, ErrorMessages.InvalidCode);

                var roles = await userManager.GetRolesAsync(user);

                var jwt = await GenerateJwtToken(user);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1)
                };

                httpContext.Response.Cookies.Append("Token", jwt, cookieOptions);

                return response.SetSuccess(new
                {
                    AccessToken = jwt,
                    User = new { user.Id, user.Email, user.UserName, user.FirstName, user.LastName, Roles = roles, user.MustChangePassword }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming login 2FA.");
                return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        public async Task<PublicResponse> ResendLoginCodeAsync(ResendCodeRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            PublicResponse publicResponse = new PublicResponse(true);

            try
            {
                if (string.IsNullOrEmpty(request.TempToken))
                    return publicResponse.SetError(ErrorCodes.InvalidToken, ErrorMessages.InvalidToken);

                var handler = new JwtSecurityTokenHandler();
                var jwtTemp = handler.ReadJwtToken(request.TempToken);

                var userId = jwtTemp.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return publicResponse.SetError(ErrorCodes.InvalidToken, ErrorMessages.InvalidToken);

                var user = await userManager.FindByIdAsync(userId);

                if (user == null)
                    return publicResponse.SetError(ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);

                var newCode = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                List<string> to = new List<string>() { user.Email };
                await _emailBusinessLogic.SendEmailTemplateAsync(
                    TemplateCode.LOGIN_CONFIRMATION,
                    JsonConvert.SerializeObject(new { Code = newCode }),
                    to
                );

                return publicResponse.SetSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error while resending login 2FA code");
                return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        public async Task<PublicResponse> ForgotPasswordAsync(ForgotPasswordRequest request, UserManager<ApplicationUser> userManager)
        {
            PublicResponse publicResponse = new PublicResponse(true);

            try
            {
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user == null)
                    return publicResponse.SetError(ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);

                var token = await userManager.GeneratePasswordResetTokenAsync(user);

                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var resetUrl = $"{_ERPSystemSettings.Value.BaseUrl}/reset-password?userId={user.Id}&token={encodedToken}";

                List<string> to = new List<string>() { user.Email };

                await _emailBusinessLogic.SendEmailTemplateAsync(TemplateCode.FORGOT_PASSWORD,
                    JsonConvert.SerializeObject(new
                    {
                        ResetUrl = resetUrl,
                        FirstName = user.FirstName
                    }),
                    to,
                    url: resetUrl
                );

                return publicResponse.SetSuccess(true);
            }
            catch (Exception ex)
            {
                return publicResponse.SetError(ErrorCodes.InternalServerError, ex.Message);
            }
        }

        public async Task<PublicResponse> ResetPasswordAsync(Authentification.Models.ResetPasswordRequest request, UserManager<ApplicationUser> userManager)
        {
            PublicResponse response = new PublicResponse(true);

            try
            {
                var user = await userManager.FindByIdAsync(request.UserId);
                if (user == null)
                    return response.SetError(ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);

                // Decode + fix for Identity tokens
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
                var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

                if (!result.Succeeded)
                    return response.SetError(ErrorCodes.InvalidToken, ErrorMessages.InvalidToken);

                return response.SetSuccess(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reset password error");
                return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        #endregion

        #region  Private Methods

        private string GenerateTempToken(ApplicationUser user)
        {
            try
            {
                var secret = _jwtSettings.Value.SecretKey;

                if (string.IsNullOrEmpty(secret))
                    throw new Exception("JWT SecretKey is missing!");

                if (secret.Length < 32)
                    throw new Exception("JWT SecretKey must be at least 32 characters long!");

                var key = Encoding.UTF8.GetBytes(secret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                      new Claim("userId", user.Id),
                      new Claim("purpose", "login_2fa")
                     }),
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var handler = new JwtSecurityTokenHandler();
                var token = handler.CreateToken(tokenDescriptor);

                return handler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "An error occured in GenerateTempToken");
                throw;
            }
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var jwtKey = _jwtSettings.Value.SecretKey;
            var jwtIssuer = _jwtSettings.Value.Issuer;
            var jwtAudience = _jwtSettings.Value.Audience;

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("username", user.UserName ?? ""),
        new Claim("firstname", user.FirstName ?? ""),
        new Claim("lastname", user.LastName ?? "")
    };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        #endregion


        public async Task<IResult> GetRolesAsync()
        {
            var roles =  _roleManager.Roles
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name
                })
                .ToList();

            return Results.Ok(roles);
        }
    }
}
