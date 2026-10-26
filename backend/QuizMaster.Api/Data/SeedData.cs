using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Data;

public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        if (db.Quizzes.Any(q => q.Visibility == QuizVisibility.Official)) return;
        db.Quizzes.AddRange(OfficialQuizzes());
        db.SaveChanges();
    }

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Quizzes.AnyAsync(q => q.Visibility == QuizVisibility.Official, cancellationToken)) return;
        db.Quizzes.AddRange(OfficialQuizzes());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<Quiz> OfficialQuizzes() =>
    [
        Official("Web Fundamentals", "HTTP, HTML and CSS basics every web developer needs.",
            Q("What does the HTTP status code 404 mean?", 2,
                "Internal Server Error", "Unauthorized", "Not Found", "Moved Permanently"),
            Q("Which HTTP method is normally used to create a new resource?", 0,
                "POST", "GET", "DELETE", "HEAD"),
            Q("What does CSS stand for?", 3,
                "Computer Style Syntax", "Creative Styling System", "Colorful Sheet Styles", "Cascading Style Sheets"),
            Q("Which HTML element creates a hyperlink?", 1,
                "<link>", "<a>", "<href>", "<url>")),

        Official("C# Basics", "Core C# language features.",
            Q("Which keyword declares a local variable whose type the compiler infers?", 1,
                "dynamic", "var", "let", "auto"),
            Q("With nullable reference types enabled, what does `string?` mean?", 3,
                "The parameter is optional", "The string is always empty",
                "The value is a compile-time constant", "The variable may hold null"),
            Q("Which collection stores key-value pairs?", 0,
                "Dictionary<TKey, TValue>", "List<T>", "HashSet<T>", "Queue<T>"),
            Q("What does `await` do?", 2,
                "Starts a new thread", "Makes the method run synchronously",
                "Waits for a Task to finish without blocking the thread", "Cancels the Task")),

        Official("SQL Essentials", "Querying and designing relational databases.",
            Q("Which clause filters rows before they are grouped?", 3,
                "HAVING", "ORDER BY", "GROUP BY", "WHERE"),
            Q("What does a foreign key do?", 1,
                "Encrypts a column", "Links a row to a row in another table",
                "Makes every query faster", "Makes a column unique"),
            Q("Which join returns only rows that have a match in both tables?", 2,
                "LEFT JOIN", "FULL OUTER JOIN", "INNER JOIN", "CROSS JOIN"),
            Q("Which statement removes rows from a table?", 0,
                "DELETE", "DROP", "ALTER", "REMOVE")),

        Official("React Basics", "Components, props and state.",
            Q("What does useState return?", 0,
                "The current value and a function to update it", "Only the current value",
                "A Promise", "A DOM element"),
            Q("Why does React need a `key` prop on list items?", 2,
                "To style the items", "To sort the list",
                "To identify items between renders", "It is required by CSS"),
            Q("What are props?", 1,
                "Internal state a component changes itself", "Inputs passed from a parent component to a child",
                "Global variables", "CSS properties"),
            Q("When does a useEffect with an empty dependency array run?", 3,
                "Before every render", "After every render",
                "Never", "Once, after the first render")),
    ];

    private static Quiz Official(string title, string description, params Question[] questions) => new()
    {
        Title = title,
        Description = description,
        Visibility = QuizVisibility.Official,
        Questions = questions.Select((q, i) => { q.Position = i; return q; }).ToList(),
    };

    private static Question Q(string text, int correctIndex, params string[] options) => new()
    {
        Text = text,
        Options = options.Select((option, i) => new AnswerOption
        {
            Text = option,
            Position = i,
            IsCorrect = i == correctIndex,
        }).ToList(),
    };
}
