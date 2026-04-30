
using ERPSystem.Extensions;
using ERPSystem.Modules.Feedback.Models;
using ERPSystem.Modules.MarketingCampaign.Models;
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

        }
    }
}