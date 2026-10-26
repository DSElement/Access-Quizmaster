using System.ComponentModel;
using System.Text.Json.Serialization;

namespace QuizMaster.Api.Services.QuizGeneration;

// What the model sends to create_quiz for each question. Four named options and a letter are easier
// for a model to fill in correctly than a list with an isCorrect flag on each item.
public record GeneratedQuestion(
    [property: JsonRequired] string Text,
    [property: JsonRequired] string OptionA,
    [property: JsonRequired] string OptionB,
    [property: JsonRequired] string OptionC,
    [property: JsonRequired] string OptionD,
    [property: JsonRequired, Description("The letter of the single correct option.")] AnswerLetter CorrectOption,
    [property: JsonRequired, Description("The number of the chunk (from search_document) the question is based on.")] int SourceChunk);

[JsonConverter(typeof(JsonStringEnumConverter<AnswerLetter>))]
public enum AnswerLetter { A, B, C, D }
