
using ERPSystem.Extensions;
using ERPSystem.Modules.Feedback.Analytics;
using ERPSystem.Modules.Feedback.Models;
using Microsoft.AspNetCore.Mvc;
using Route = ERPSystem.Utils.Constants.General.Route.Feedback;

namespace ERPSystem.Modules.Feedback
{
    public class FeedbackEndpoint
    {
        public static void Map(RouteGroupBuilder group)
        {
            group.MapPost(Route.SEND_FEEDBACK_FORMS,
               async ([FromBody] SendFeedbackFormsRequest request, FeedbackService service)
                   => await service.SendFeedbackFormsAsync(request))
               .WithDefaultApiSettings("SendFeedbackForms", "Trimite formulare feedback","SEND_FEEDBACK_FORMS", true );

            group.MapGet(Route.GET_FEEDBACK_FORM,
                async (string token, FeedbackService service)
                    => await service.GetFeedbackFormAsync(token))
                .WithDefaultApiSettings( "GetFeedbackForm","Obține formular feedback", "GET_FEEDBACK_FORM",    false);

            group.MapPost(Route.SUBMIT_FEEDBACK_FORM,
                async ([FromBody] SubmitFeedbackRequest request, FeedbackService service)
                    => await service.SubmitFeedbackAsync(request))
                .WithDefaultApiSettings(  "SubmitFeedbackForm", "Trimite feedback", "SUBMIT_FEEDBACK_FORM",false );

            group.MapGet(Route.SESSION_REVIEWS,
                async (int sessionId, FeedbackService service)
                    => await service.GetSessionReviewsAsync(sessionId))
                .WithDefaultApiSettings( "GetSessionReviews", "Listare feedbackuri sesiune", "GET_SESSION_REVIEWS",  true );



            group.MapPost(Route.CREATE_STUDENT_EVALUATION,
               async ([FromBody] CreateStudentEvaluationRequest request, FeedbackService service)
                   => await service.CreateStudentEvaluationAsync(request))
               .WithDefaultApiSettings( "CreateStudentEvaluation","Adaugă evaluare profesor pentru cursant","CREATE_STUDENT_EVALUATION",true );
           
            group.MapGet(Route.GET_STUDENT_EVALUATIONS,
                async (int studentId, int? sessionId, FeedbackService service)
                    => await service.GetStudentEvaluationsAsync(studentId, sessionId))
                .WithDefaultApiSettings( "GetStudentEvaluations", "Listare evaluări cursant", "GET_STUDENT_EVALUATIONS", true);

            group.MapPost(Route.CREATE_EXTERNAL_REVIEW,
                async ([FromBody] CreateExternalReviewRequest request, FeedbackService service)
                    => await service.CreateExternalReviewAsync(request))
                .WithDefaultApiSettings( "CreateExternalReview", "Adaugă feedback extern pentru curs","CREATE_EXTERNAL_REVIEW", true );

            group.MapGet(Route.GET_EXTERNAL_REVIEWS,
                async ([AsParameters] ExternalReviewFilterRequest request, FeedbackService service)
                    => await service.GetExternalReviewsAsync(request.TargetType, request.TargetId))
                .WithDefaultApiSettings( "GetExternalReviews","Returnează feedbackurile externe filtrate", "GET_EXTERNAL_REVIEWS", true );


            group.MapGet(Route.GET_COURSE_ANALYTICS,
                async (int courseSessionId, CourseAnalyticsService service)
                    => await service.GetCourseAnalyticsAsync(courseSessionId))
                .WithDefaultApiSettings( "GetCourseAnalytics","Returnează analiza AI agregată pentru o sesiune de curs","GET_COURSE_ANALYTICS",  true  );


            group.MapGet(Route.GET_STUDENT_ANALYTICS,
               async (int studentId, StudentAnalyticsService service)
                   => await service.GetStudentAnalyticsAsync(studentId))
               .WithDefaultApiSettings( "GetStudentAnalytics",  "Returnează analiza AI agregată pentru un cursant", "GET_STUDENT_ANALYTICS", true );

            group.MapGet(Route.GET_EXTERNAL_ANALYTICS,
               async ([AsParameters] ExternalReviewFilterRequest request, ExternalAnalyticsService service)
                   => await service.GetExternalAnalyticsAsync(request.TargetType, request.TargetId, request.Source))
               .WithDefaultApiSettings( "GetExternalAnalytics","Returnează analiza AI agregată pentru feedback extern", "GET_EXTERNAL_ANALYTICS",  true );

            group.MapGet(Route.GET_EXTERNAL_REVIEW_TARGETS,
               async (string targetType, FeedbackService service)
                   => await service.GetExternalReviewTargetsAsync(targetType))
               .WithDefaultApiSettings( "GetExternalReviewTargets","Returnează lista de ținte pentru feedback extern","GET_EXTERNAL_REVIEW_TARGETS", true );

            group.MapGet(Route.GET_FEEDBACK_GLOBAL_ANALYTICS,
                async (FeedbackGlobalAnalyticsService service)
                    => await service.GetGlobalAnalyticsAsync())
                .WithDefaultApiSettings("GetFeedbackGlobalAnalytics", "Returnează analiza globală a feedbackului (dashboard AI)","GET_FEEDBACK_GLOBAL_ANALYTICS",  true  );

        }
    }
}