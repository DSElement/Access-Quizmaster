namespace QuizMaster.Api.Services.QuizGeneration;

// A generation that failed for a reason the user should see, with the HTTP status to answer with.
public class QuizGenerationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
