using Microsoft.Extensions.Options;

namespace QuizMaster.Api.Services.Documents;

public class DocumentStorage
{
    private readonly string root;

    public DocumentStorage(IOptions<DocumentOptions> options, IWebHostEnvironment environment)
    {
        root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.StoragePath));
        Directory.CreateDirectory(root);
    }

    public string PathFor(string storedFileName) => Path.Combine(root, storedFileName);

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        var path = PathFor(storedFileName);
        try
        {
            await using var file = File.Create(path);
            await content.CopyToAsync(file, cancellationToken);
        }
        catch
        {
            // A cancelled or broken upload would otherwise leave a partial file that no database row points to.
            File.Delete(path);
            throw;
        }
    }

    public void Delete(string storedFileName) => File.Delete(PathFor(storedFileName));
}
