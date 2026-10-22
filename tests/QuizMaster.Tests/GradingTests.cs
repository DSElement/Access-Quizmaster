using QuizMaster.Contracts;
using QuizMaster.Services;

namespace QuizMaster.Tests;

public sealed class GradingTests
{
    [Fact]
    public void CorrectWrongAndUnansweredQuestionsAreGradedOnTheServer()
    {
        var quiz = TestData.Quiz();
        var result = QuizGrader.Grade(quiz,
        [
            new(quiz.Questions[0].Id, quiz.Questions[0].Options[0].Id),
            new(quiz.Questions[1].Id, quiz.Questions[1].Options[1].Id)
        ]);
        Assert.Equal(3, result.TotalQuestions);
        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(33.3, result.ScorePercentage);
        Assert.True(result.Results[0].WasCorrect);
        Assert.False(result.Results[1].WasCorrect);
        Assert.Null(result.Results[2].SelectedOptionId);
        Assert.All(result.Results, item => Assert.Single(item.Options, option => option.IsCorrect));
    }

    [Fact]
    public void AllCorrectAnswersProduceOneHundredPercent()
    {
        var quiz = TestData.Quiz();
        var result = QuizGrader.Grade(quiz, quiz.Questions.Select(q => new SubmitAnswerRequest(q.Id, q.Options[0].Id)).ToList());
        Assert.Equal(100, result.ScorePercentage);
    }

    [Fact]
    public void EmptyQuizHasZeroScore()
    {
        Assert.Equal(0, QuizGrader.Grade(TestData.Quiz(0), []).ScorePercentage);
    }

    [Fact]
    public void AnswerFromAnotherQuestionIsRejected()
    {
        var quiz = TestData.Quiz();
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(quiz, [new(quiz.Questions[0].Id, quiz.Questions[1].Options[0].Id)]));
    }

    [Fact]
    public void UnknownQuestionIsRejected()
    {
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(TestData.Quiz(), [new(Guid.NewGuid(), null)]));
    }

    [Fact]
    public void DuplicateAnswersAreRejected()
    {
        var quiz = TestData.Quiz();
        var answer = new SubmitAnswerRequest(quiz.Questions[0].Id, quiz.Questions[0].Options[0].Id);
        Assert.Throws<ArgumentException>(() => QuizGrader.Grade(quiz, [answer, answer]));
    }
}
