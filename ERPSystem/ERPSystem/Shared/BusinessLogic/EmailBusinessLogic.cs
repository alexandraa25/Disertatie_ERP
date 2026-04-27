using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.AdditionalAct.Models;
using ERPSystem.Modules.Authentification.Models;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Modules.MarketingCampaign.Models;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using ERPSystem.Utils.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using SendGrid;
using SendGrid.Helpers.Mail;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;

namespace ERPSystem.Shared.BusinessLogic
{
    public class EmailBusinessLogic
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly ILogger<EmailBusinessLogic> _logger;
        private readonly IOptions<EmailConnectionSettings> _emailConnectionSettings;

        public EmailBusinessLogic(ILogger<EmailBusinessLogic> logger, IOptions<EmailConnectionSettings> emailConnectionSettings, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _emailConnectionSettings = emailConnectionSettings;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<PublicResponse> SendEmailTemplateAsync(string templateCode, string tableRow, List<string> to, string? url = null, string? code = null)
        {
            PublicResponse publicResponse = new PublicResponse(true);

            try
            {
                EmailTemplate emailTemplate = await _applicationDbContext.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateCode == templateCode && t.IsActive);

                if (emailTemplate == null)
                    return publicResponse.SetError(ErrorCodes.EmailTemplateNotFound, ErrorMessages.EmailTemplateNotFound);

                string subject = emailTemplate.Subject;
                string template = GetTemplateEmailByCode(templateCode: templateCode, tableRow, url, emailTemplate: emailTemplate);

                publicResponse =  await SendEmailAsync(emailTemplate.Subject, template, to);

                if(!publicResponse.IsSuccess)
                    return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);

                return publicResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error sending email template {TemplateCode} to {ToEmail}", templateCode, to);
                return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        public async Task<PublicResponse> SendEmailAsync(string subject, string htmlContent, List<string> toEmails)
        {
            PublicResponse publicResponse = new(true);

            try
            {
                var apiKey = _emailConnectionSettings.Value.Token;
                var client = new SendGridClient(apiKey);

                var from = new EmailAddress(_emailConnectionSettings.Value.DefaultFromEmail, "ERP System");

                var msg = new SendGridMessage()
                {
                    From = from,
                    Subject = subject,
                    HtmlContent = htmlContent
                };

                msg.AddTos(toEmails.Select(e => new EmailAddress(e)).ToList());
                msg.PlainTextContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<.*?>", string.Empty);

                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Email send failed: {Status} - {Body}", response.StatusCode, errorBody);

                    return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
                }

                return publicResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return publicResponse.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
            }
        }

        private  string GetTemplateEmailByCode( string templateCode, string tableRow, string url, EmailTemplate emailTemplate)
        {
            string template = null;

            if (templateCode == TemplateCode.EMAIL_REGISTRATION_CONFIRMATION)
            {
                var applicationUser = JsonConvert.DeserializeObject<ApplicationUser>(tableRow);

                template = emailTemplate.HtmlContent
                    .Replace(EmailConstants.FIRST_NAME, applicationUser.FirstName)
                    .Replace(EmailConstants.CONFIRMATION_EMAIL_URL, url)
                    .Replace(EmailConstants.YEAR, DateTime.UtcNow.Year.ToString());
            }
            else if (templateCode == TemplateCode.LOGIN_CONFIRMATION)
            {
                var emailConfirmation = JsonConvert.DeserializeObject<LoginCodeModel>(tableRow);

                template = emailTemplate.HtmlContent
                    .Replace(EmailConstants.CONFIRMATION_LOGIN_CODE, emailConfirmation.Code);
            }
            else if (templateCode == TemplateCode.FORGOT_PASSWORD)
            {
                var applicationUser = JsonConvert.DeserializeObject<ApplicationUser>(tableRow);

                template = emailTemplate.HtmlContent
                      .Replace(EmailConstants.FIRST_NAME, applicationUser.FirstName)
                      .Replace(EmailConstants.FORGOT_PASSWORD_URL, url)
                      .Replace(EmailConstants.YEAR, DateTime.UtcNow.Year.ToString());
            }
            else if (templateCode == TemplateCode.EMAIL_USER_CREDENTIALS)
            {
                var model = JsonConvert.DeserializeObject<UserCredentialsEmailModel>(tableRow);

                template = emailTemplate.HtmlContent
                      .Replace(EmailConstants.FIRST_NAME, model.FirstName)
                      .Replace(EmailConstants.EMAIL, model.Email)
                      .Replace(EmailConstants.PASSWORD, model.Password);
            }
            else if (templateCode == TemplateCode.CONTRACT_SIGN_REQUEST)
            {
                var model = JsonConvert.DeserializeObject<ContractSignEmailModel>(tableRow);

                template = emailTemplate.HtmlContent
                    .Replace(EmailConstants.CLIENT_NAME, model.ClientName)
                    .Replace(EmailConstants.CONTRACT_NUMBER, model.ContractNumber)
                    .Replace(EmailConstants.SIGN_URL, url)
                    .Replace(EmailConstants.YEAR, DateTime.UtcNow.Year.ToString());
            }

            else if (templateCode == TemplateCode.ADDITIONAL_ACT_SIGN_REQUEST)
            {
                var model = JsonConvert.DeserializeObject<AdditionalActSignEmailModel>(tableRow);

                template = emailTemplate.HtmlContent
                    .Replace(EmailConstants.CLIENT_NAME, model.ClientName)
                    .Replace(EmailConstants.ACT_NUMBER, model.ActNumber)
                    .Replace(EmailConstants.DESCRIPTION, model.Description ?? "")
                    .Replace(EmailConstants.SIGN_URL, url)
                    .Replace(EmailConstants.YEAR, DateTime.UtcNow.Year.ToString());
            }
            else if (templateCode == TemplateCode.CAMPAIGN_NEWSLETTER)
            {
                var model = JsonConvert.DeserializeObject<CampaignNewsletterEmailModel>(tableRow);

                template = emailTemplate.HtmlContent
                    .Replace(EmailConstants.CAMPAIGN_NAME, model.CampaignName)
                    .Replace(EmailConstants.CAMPAIGN_DESCRIPTION, model.CampaignDescription ?? "")
                    .Replace(EmailConstants.DISCOUNT, model.Discount)
                    .Replace(EmailConstants.START_DATE, model.StartDate)
                    .Replace(EmailConstants.END_DATE, model.EndDate)
                    .Replace(EmailConstants.YEAR, DateTime.UtcNow.Year.ToString());
            }


            return template;
        }
    }
}
