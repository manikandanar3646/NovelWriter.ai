using LiteDB;

namespace AI_Novel_writing_System.Models;

public class StoryNote
{
    [BsonId]
    public int Id { get; set; }

    public int NovelId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsCanon { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

