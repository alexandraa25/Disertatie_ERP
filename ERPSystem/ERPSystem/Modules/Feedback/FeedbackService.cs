
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Feedback.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace ERPSystem.Modules.Feedback
{
    public class FeedbackService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly NotificationsService _notificationService;
        private readonly EmailBusinessLogic _emailBusinessLogic;



        public FeedbackService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, NotificationsService notificationService, EmailBusinessLogic emailBusinessLogic)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _emailBusinessLogic = emailBusinessLogic;
        }


        public async Task<PublicResponse> SendFeedbackFormsAsync(SendFeedbackFormsRequest request)
        {
            PublicResponse response = new(true);

            var session = await _context.CourseSessions
                .Include(x => x.Course)
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.Id == request.CourseSessionId);

            if (session == null)
                return response.SetError("SessionNotFound", "Sesiunea nu a fost găsită.");

            if (request.StudentIds == null || !request.StudentIds.Any())
                return response.SetError("NoStudentsSelected", "Nu ai selectat niciun cursant.");

            var students = await _context.Students
                .Where(x => request.StudentIds.Contains(x.Id) && !string.IsNullOrEmpty(x.Email))
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.Email
                })
                .ToListAsync();

            if (!students.Any())
                return response.SetError("NoRecipients", "Nu există destinatari cu email valid.");

            var emailLog = new EmailLog
            {
                Type = EmailLogTypes.FeedbackForm,
                ReferenceId = session.Id,
                Subject = "Te rugăm să ne lași feedback",
                HtmlContent = "Feedback form request",
                RecipientMode = "manual",
                TotalRecipients = students.Count,
                SentCount = 0,
                FailedCount = 0,
                SentAt = DateTime.UtcNow
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();

            int sentCount = 0;
            int failedCount = 0;

            foreach (var student in students)
            {
                var token = Guid.NewGuid().ToString("N");

                var feedbackForm = new FeedbackForm
                {
                    StudentId = student.Id,
                    CourseSessionId = session.Id,
                    Token = token,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(14)
                };

                _context.FeedbackForms.Add(feedbackForm);
                await _context.SaveChangesAsync();

                var feedbackUrl = $"http://localhost:4200/feedback/{token}";

                var emailModel = new FeedbackFormEmailModel
                {
                    CourseName = session.Course.Name,
                    SessionTitle = session.Title,
                    TeacherName = session.Teacher.FullName
                };

                var tableRow = JsonConvert.SerializeObject(emailModel);

                var emailResult = await _emailBusinessLogic.SendEmailTemplateAsync(
                    TemplateCode.FEEDBACK_FORM_REQUEST,
                    tableRow,
                    new List<string> { student.Email },
                    feedbackUrl
                );

                var recipientLog = new EmailRecipientLog
                {
                    EmailLogId = emailLog.Id,
                    StudentId = student.Id,
                    Email = student.Email,
                    Name = student.FullName,
                    IsSent = emailResult.IsSuccess,
                    SentAt = emailResult.IsSuccess ? DateTime.UtcNow : null,
                    ErrorMessage = emailResult.IsSuccess ? null : "Emailul nu a putut fi trimis."
                };

                _context.EmailRecipientLogs.Add(recipientLog);

                if (emailResult.IsSuccess)
                    sentCount++;
                else
                    failedCount++;
            }

            emailLog.SentCount = sentCount;
            emailLog.FailedCount = failedCount;

            await _context.SaveChangesAsync();

            return response.SetSuccess(new
            {
                emailLog.Id,
                emailLog.TotalRecipients,
                emailLog.SentCount,
                emailLog.FailedCount
            });
        }

        public async Task<PublicResponse> GetFeedbackFormAsync(string token)
        {
            PublicResponse response = new(true);

            var form = await _context.FeedbackForms
                .FirstOrDefaultAsync(x => x.Token == token);

            if (form == null)
                return response.SetError("FeedbackFormNotFound", "Formularul nu a fost găsit.");

            var session = await _context.CourseSessions
                .Include(x => x.Course)
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.Id == form.CourseSessionId);

            if (session == null)
                return response.SetError("SessionNotFound", "Sesiunea nu a fost găsită.");

            var isExpired = form.ExpiresAt.HasValue && form.ExpiresAt.Value < DateTime.UtcNow;

            return response.SetSuccess(new FeedbackFormDetailsDto
            {
                CourseName = session.Course.Name,
                SessionTitle = session.Title,
                TeacherName = session.Teacher.FullName,
                IsCompleted = form.IsCompleted,
                IsExpired = isExpired
            });
        }

        public async Task<PublicResponse> SubmitFeedbackAsync(SubmitFeedbackRequest request)
        {
            PublicResponse response = new(true);

            var form = await _context.FeedbackForms
                .FirstOrDefaultAsync(x => x.Token == request.Token);

            if (form == null)
                return response.SetError("FeedbackFormNotFound", "Formularul nu a fost găsit.");

            if (form.IsCompleted)
                return response.SetError("FeedbackAlreadyCompleted", "Feedbackul a fost deja completat.");

            if (form.ExpiresAt.HasValue && form.ExpiresAt.Value < DateTime.UtcNow)
                return response.SetError("FeedbackExpired", "Formularul de feedback a expirat.");

            if (request.Rating < 1 || request.Rating > 5)
                return response.SetError("InvalidRating", "Ratingul trebuie să fie între 1 și 5.");

            if (string.IsNullOrWhiteSpace(request.Comment))
                return response.SetError("CommentRequired", "Comentariul este obligatoriu.");

            var review = new CourseReview
            {
                CourseSessionId = form.CourseSessionId,
                FeedbackFormId = form.Id,

                Rating = request.Rating,

                CourseStructureRating = request.CourseStructureRating,
                CoursePaceRating = request.CoursePaceRating,
                MaterialsRating = request.MaterialsRating,

                TeacherClarityRating = request.TeacherClarityRating,
                TeacherEngagementRating = request.TeacherEngagementRating,
                TeacherSupportRating = request.TeacherSupportRating,

                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.CourseReviews.Add(review);

            form.IsCompleted = true;
            form.CompletedAt = DateTime.UtcNow;

            // pentru anonimitate
            form.StudentId = null;

            await _context.SaveChangesAsync();

            return response.SetSuccess(new
            {
                review.Id,
                Message = "Feedbackul a fost trimis cu succes."
            });
        }

        public async Task<PublicResponse> GetSessionReviewsAsync(int sessionId)
        {
            PublicResponse response = new(true);

            var sessionExists = await _context.CourseSessions
                .AnyAsync(x => x.Id == sessionId);

            if (!sessionExists)
                return response.SetError("SessionNotFound", "Sesiunea nu a fost găsită.");

            var items = await _context.CourseReviews
                .Where(x => x.CourseSessionId == sessionId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new CourseReviewDto
                {
                    Id = x.Id,
                    Rating = x.Rating,

                    CourseStructureRating = x.CourseStructureRating,
                    CoursePaceRating = x.CoursePaceRating,
                    MaterialsRating = x.MaterialsRating,

                    TeacherClarityRating = x.TeacherClarityRating,
                    TeacherEngagementRating = x.TeacherEngagementRating,
                    TeacherSupportRating = x.TeacherSupportRating,

                    Comment = x.Comment,
                    Sentiment = x.Sentiment,
                    SentimentScore = x.SentimentScore,
                    Keywords = x.Keywords,
                    CreatedAt = x.CreatedAt,
                    AnalyzedAt = x.AnalyzedAt
                })
                .ToListAsync();

            return response.SetSuccess(items);
        }


    }


}
