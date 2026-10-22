using System.ComponentModel;
using System.Text.Json.Serialization;
using QuizMaster.Contracts;

namespace QuizMaster.Services;

public sealed record QuizQuestionDraft(
    [property: JsonRequired] string Text,
    [property: JsonRequired] string OptionA,
    [property: JsonRequired] string OptionB,
    [property: JsonRequired] string OptionC,
    [property: JsonRequired] string OptionD,
    [property: JsonRequired, Description("The label of the single correct option: A, B, C, or D.")] QuizAnswerLabel CorrectOption)
{
    public CreateQuestionRequest ToQuestion() => new()
    {
        Text = Text,
        Options = new[] { OptionA, OptionB, OptionC, OptionD }.Select((text, index) => new CreateOptionRequest
        {
            Text = text, IsCorrect = index == (int)CorrectOption
        }).ToList()
    };
}

[JsonConverter(typeof(JsonStringEnumConverter<QuizAnswerLabel>))]
public enum QuizAnswerLabel { A, B, C, D }
