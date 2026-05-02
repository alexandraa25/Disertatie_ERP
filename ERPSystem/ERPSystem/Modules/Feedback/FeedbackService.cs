
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Feedback.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.Notifications;
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
        private readonly INlpAnalysisService _nlpAnalysisService;



        public FeedbackService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, NotificationsService notificationService, EmailBusinessLogic emailBusinessLogic, INlpAnalysisService nlpAnalysisService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _emailBusinessLogic = emailBusinessLogic;
            _nlpAnalysisService = nlpAnalysisService;
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

            await AddActivityLogAsync(
                "FeedbackForms",
                session.Id.ToString(),
                "FeedbackFormsSent",
                $"Au fost trimise formulare de feedback pentru sesiunea '{session.Title}' ({session.Course.Name}). Destinatari: {students.Count}, trimise: {sentCount}, eșuate: {failedCount}."
            );

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

            form.StudentId = null;

            await _context.SaveChangesAsync();

            var analysis = await _nlpAnalysisService.AnalyzeAsync(review.Comment, "course_review");

            if (analysis != null)
            {
                review.Sentiment = analysis.Sentiment;
                review.SentimentScore = analysis.SentimentScore;

                review.PositivePercent = analysis.PositivePercent;
                review.NegativePercent = analysis.NegativePercent;
                review.NeutralPercent = analysis.NeutralPercent;

                review.Emotion = analysis.Emotion;
                review.Keywords = analysis.Keywords;
                review.TopicsJson = JsonConvert.SerializeObject(analysis.Topics);

                review.TeacherScore = analysis.TeacherScore;
                review.CourseScore = analysis.CourseScore;
                review.BehaviorScore = analysis.BehaviorScore;

                review.AnalyzedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            await AddActivityLogAsync(
                "CourseReview",
                review.Id.ToString(),
                "FeedbackSubmitted",
                $"A fost înregistrat feedback anonim pentru sesiunea #{form.CourseSessionId} cu rating {request.Rating}/5."
            );

            if (request.Rating <= 2 || review.Sentiment == "negativ" || review.NegativePercent >= 40)
            {
                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "Admin", "Secretary" },
                    eventType: NotificationEvents.Feedback,
                    title: "Feedback negativ",
                    message: $"A fost primit un feedback negativ pentru sesiunea #{form.CourseSessionId}. Rating: {request.Rating}/5, sentiment: {review.Sentiment}.",
                    type: "Warning",
                    link: $"/courses/{form.CourseSessionId}",
                    entityType: "CourseReview",
                    entityId: review.Id.ToString()
                );
            }

            if (request.Rating == 5 || review.Sentiment == "pozitiv")
            {
                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "Admin" },
                    eventType: NotificationEvents.Feedback,
                    title: "Feedback pozitiv",
                    message: $"Sesiunea #{form.CourseSessionId} a primit feedback pozitiv. Rating: {request.Rating}/5.",
                    type: "Success",
                    link: $"/courses/{form.CourseSessionId}",
                    entityType: "CourseReview",
                    entityId: review.Id.ToString()
                );
            }

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

                   PositivePercent = x.PositivePercent,
                   NegativePercent = x.NegativePercent,
                   NeutralPercent = x.NeutralPercent,

                   Emotion = x.Emotion,
                   Keywords = x.Keywords,
                   TopicsJson = x.TopicsJson,

                   TeacherScore = x.TeacherScore,
                   CourseScore = x.CourseScore,
                   BehaviorScore = x.BehaviorScore,

                   CreatedAt = x.CreatedAt,
                   AnalyzedAt = x.AnalyzedAt
               })
                .ToListAsync();

            return response.SetSuccess(items);
        }

        public async Task<PublicResponse> CreateStudentEvaluationAsync(CreateStudentEvaluationRequest request)
        {
            PublicResponse response = new(true);

            var session = await _context.CourseSessions
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.Id == request.CourseSessionId);

            if (session == null)
                return response.SetError("SessionNotFound", "Sesiunea nu a fost găsită.");

            var studentExists = await _context.Students
                .AnyAsync(x => x.Id == request.StudentId);

            if (!studentExists)
                return response.SetError("StudentNotFound", "Cursantul nu a fost găsit.");

            if (request.Rating < 1 || request.Rating > 5)
                return response.SetError("InvalidRating", "Ratingul trebuie să fie între 1 și 5.");

            var evaluation = new StudentEvaluation
            {
                StudentId = request.StudentId,
                CourseSessionId = request.CourseSessionId,
                TeacherUserId = session.TeacherUserId,

                Rating = request.Rating,
                AttendanceScore = request.AttendanceScore,
                BehaviorScore = request.BehaviorScore,
                ProgressScore = request.ProgressScore,

                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentEvaluations.Add(evaluation);

            
            await _context.SaveChangesAsync();

            var analysis = await _nlpAnalysisService.AnalyzeAsync(
                evaluation.Comment ?? "",
                "student_evaluation"
            );

            if (analysis != null)
            {
                evaluation.Sentiment = analysis.Sentiment;
                evaluation.SentimentScore = analysis.SentimentScore;

                evaluation.PositivePercent = analysis.PositivePercent;
                evaluation.NegativePercent = analysis.NegativePercent;
                evaluation.NeutralPercent = analysis.NeutralPercent;

                evaluation.Emotion = analysis.Emotion;
                evaluation.Keywords = analysis.Keywords;
                evaluation.TopicsJson = JsonConvert.SerializeObject(analysis.Topics);

                evaluation.StudentRiskScore = analysis.StudentRiskScore;
                evaluation.BehaviorScoreNlp = analysis.BehaviorScoreNlp;
                evaluation.ProgressScoreNlp = analysis.ProgressScoreNlp;

                evaluation.AnalyzedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            await AddActivityLogAsync(
                 "StudentEvaluation",
                 evaluation.Id.ToString(),
                 "StudentEvaluated",
                 $"Profesorul a evaluat cursantul #{request.StudentId} la sesiunea #{request.CourseSessionId}. Rating: {request.Rating}/5."
             );

            if (request.Rating <= 2 ||
                (request.ProgressScore.HasValue && request.ProgressScore <= 2) ||
                (request.BehaviorScore.HasValue && request.BehaviorScore <= 2) ||
                evaluation.Sentiment == "negativ" ||
                evaluation.NegativePercent >= 40 ||
                evaluation.StudentRiskScore >= 60)
            {
                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "Admin", "Secretary" },
                    eventType: NotificationEvents.Feedback,
                    title: "Cursant în risc",
                    message: $"Cursantul #{request.StudentId} are risc crescut la sesiunea #{request.CourseSessionId}. Rating: {request.Rating}/5, risc AI: {evaluation.StudentRiskScore}%.",
                    type: "Warning",
                    link: $"/students/{request.StudentId}",
                    entityType: "StudentEvaluation",
                    entityId: evaluation.Id.ToString()
                );
            }

            return response.SetSuccess(new
            {
                evaluation.Id,
                Message = "Evaluarea cursantului a fost salvată."
            });
        }

        public async Task<PublicResponse> GetStudentEvaluationsAsync(int studentId, int? sessionId = null)
        {
            PublicResponse response = new(true);

            var query =
                from ev in _context.StudentEvaluations
                join st in _context.Students on ev.StudentId equals st.Id
                join cs in _context.CourseSessions on ev.CourseSessionId equals cs.Id
                join teacher in _context.Users on ev.TeacherUserId equals teacher.Id
                where ev.StudentId == studentId
                select new
                {
                    ev,
                    StudentName = st.FullName,
                    TeacherName = teacher.FirstName + " " + teacher.LastName
                };

            if (sessionId.HasValue)
            {
                query = query.Where(x => x.ev.CourseSessionId == sessionId.Value);
            }

            var items = await query
                .OrderByDescending(x => x.ev.CreatedAt)
                .Select(x => new StudentEvaluationDto
                {
                    Id = x.ev.Id,
                    StudentId = x.ev.StudentId,
                    StudentName = x.StudentName,
                    CourseSessionId = x.ev.CourseSessionId,
                    TeacherName = x.TeacherName,

                    Rating = x.ev.Rating,
                    AttendanceScore = x.ev.AttendanceScore,
                    BehaviorScore = x.ev.BehaviorScore,
                    ProgressScore = x.ev.ProgressScore,

                    Comment = x.ev.Comment,

                    Sentiment = x.ev.Sentiment,
                    SentimentScore = x.ev.SentimentScore,
                    Keywords = x.ev.Keywords,

                    PositivePercent = x.ev.PositivePercent,
                    NegativePercent = x.ev.NegativePercent,
                    NeutralPercent = x.ev.NeutralPercent,
                    Emotion = x.ev.Emotion,
                    TopicsJson = x.ev.TopicsJson,

                    StudentRiskScore = x.ev.StudentRiskScore,
                    BehaviorScoreNlp = x.ev.BehaviorScoreNlp,
                    ProgressScoreNlp = x.ev.ProgressScoreNlp,

                    CreatedAt = x.ev.CreatedAt
                })
                .ToListAsync();

            return response.SetSuccess(items);
        }

        public async Task<PublicResponse> CreateExternalReviewAsync(CreateExternalReviewRequest request)
        {
            PublicResponse response = new(true);

            if (string.IsNullOrWhiteSpace(request.Source))
                return response.SetError("SourceRequired", "Sursa feedback-ului este obligatorie.");

            if (string.IsNullOrWhiteSpace(request.Comment))
                return response.SetError("CommentRequired", "Comentariul este obligatoriu.");

            if (request.Rating.HasValue && (request.Rating < 1 || request.Rating > 5))
                return response.SetError("InvalidRating", "Ratingul trebuie să fie între 1 și 5.");

            var allowedTargets = new[] { "General", "Course", "CourseSession", "Teacher", "Student" };

            if (string.IsNullOrWhiteSpace(request.TargetType))
                request.TargetType = "General";

            if (!allowedTargets.Contains(request.TargetType))
                return response.SetError("InvalidTargetType", "Tipul țintei nu este valid.");

            if (request.TargetType != "General" && string.IsNullOrWhiteSpace(request.TargetId))
                return response.SetError("TargetIdRequired", "TargetId este obligatoriu pentru acest tip de feedback.");

            if (request.TargetType == "General")
                request.TargetId = null;

            var externalReview = new ExternalReview
            {
                Source = request.Source,
                TargetType = request.TargetType,
                TargetId = request.TargetId,

                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.ExternalReviews.Add(externalReview);
            await _context.SaveChangesAsync();

            try
            {
                var analysis = await _nlpAnalysisService.AnalyzeAsync(
                    externalReview.Comment,
                    "external_review"
                );

                if (analysis != null)
                {
                    externalReview.Sentiment = analysis.Sentiment;
                    externalReview.SentimentScore = analysis.SentimentScore;
                    externalReview.PositivePercent = analysis.PositivePercent;
                    externalReview.NegativePercent = analysis.NegativePercent;
                    externalReview.NeutralPercent = analysis.NeutralPercent;
                    externalReview.Emotion = analysis.Emotion;
                    externalReview.Keywords = analysis.Keywords;
                    externalReview.TopicsJson = JsonConvert.SerializeObject(analysis.Topics);
                    externalReview.PublicPerceptionScore = analysis.PublicPerceptionScore;
                    externalReview.AnalyzedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return response.SetError(
                    "NlpAnalysisFailed",
                    ex.ToString()
                );
            }

            await AddActivityLogAsync(
                "ExternalReview",
                externalReview.Id.ToString(),
                "ExternalReviewCreated",
                $"A fost adăugat feedback extern din sursa '{request.Source}' pentru ținta '{externalReview.TargetType}' #{externalReview.TargetId}."
            );

            if (externalReview.Rating <= 2 ||
                externalReview.Sentiment == "negativ" ||
                externalReview.NegativePercent >= 40)
            {
                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "Admin", "Secretary" },
                    eventType: NotificationEvents.Feedback,
                    title: "Feedback extern negativ",
                    message: $"A fost primit feedback extern negativ din {request.Source}. Țintă: {externalReview.TargetType}.",
                    type: "Warning",
                    link: "/feedback/external",
                    entityType: "ExternalReview",
                    entityId: externalReview.Id.ToString()
                );
            }

            return response.SetSuccess(new
            {
                externalReview.Id,
                Message = "Feedbackul extern a fost salvat."
            });
        }

        public async Task<PublicResponse> GetExternalReviewsAsync(string? targetType = null, string? targetId = null)
        {
            PublicResponse response = new(true);

            var query = _context.ExternalReviews.AsQueryable();

            if (!string.IsNullOrWhiteSpace(targetType))
            {
                query = query.Where(x => x.TargetType == targetType);
            }

            if (!string.IsNullOrWhiteSpace(targetId))
            {
                query = query.Where(x => x.TargetId == targetId);
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ExternalReviewDto
                {
                    Id = x.Id,
                    Source = x.Source,
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    Rating = x.Rating,
                    Comment = x.Comment,

                    Sentiment = x.Sentiment,
                    SentimentScore = x.SentimentScore,
                    PositivePercent = x.PositivePercent,
                    NegativePercent = x.NegativePercent,
                    NeutralPercent = x.NeutralPercent,
                    Emotion = x.Emotion,
                    Keywords = x.Keywords,
                    TopicsJson = x.TopicsJson,

                    PublicPerceptionScore = x.PublicPerceptionScore,

                    CreatedAt = x.CreatedAt,
                    AnalyzedAt = x.AnalyzedAt
                })
                .ToListAsync();

            return response.SetSuccess(items);
        }


        public async Task<PublicResponse> GetExternalReviewTargetsAsync(string targetType)
        {
            PublicResponse response = new(true);

            if (string.IsNullOrWhiteSpace(targetType) || targetType == "General")
                return response.SetSuccess(new List<object>());

            if (targetType == "Course")
            {
                var items = await _context.Courses
                    .OrderBy(x => x.Name)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name
                    })
                    .ToListAsync();

                return response.SetSuccess(items);
            }

            if (targetType == "CourseSession")
            {
                var items = await _context.CourseSessions
                    .Include(x => x.Course)
                    .Include(x => x.Teacher)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Course.Name + " - " + x.Title + " - " + x.Teacher.FullName
                    })
                    .ToListAsync();

                return response.SetSuccess(items);
            }

            if (targetType == "Teacher")
            {
                var items = await (
                    from user in _context.Users
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where role.Name == "Teacher"
                    orderby user.FirstName
                    select new
                    {
                        id = user.Id,
                        name = user.FirstName + " " + user.LastName
                    }
                ).ToListAsync();

                return response.SetSuccess(items);
            }

            if (targetType == "Student")
            {
                var items = await _context.Students
                    .OrderBy(x => x.FullName)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.FullName
                    })
                    .ToListAsync();

                return response.SetSuccess(items);
            }

            return response.SetError("InvalidTargetType", "Tipul țintei nu este valid.");
        }


        private async Task AddActivityLogAsync(string entityType, string entityId, string action, string description)
        {
            _context.ActivityLog.Add(new ActivityLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });

            await _context.SaveChangesAsync();
        }


    }


}
