namespace QuizMaster.Api.Models;

public enum QuizVisibility
{
    /// <summary>Seeded and maintained by admins. Visible to everyone, has no owner.</summary>
    Official,

    /// <summary>Visible only to its owner.</summary>
    Private,

    /// <summary>Published by its owner to the community feed.</summary>
    Public,
}
