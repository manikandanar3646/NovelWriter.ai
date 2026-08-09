using LiteDB;

namespace AI_Novel_writing_System.Models
{
    public class Chapter
    {
        [BsonId]
        public int Id { get; set; }

        public int NovelId { get; set; }

        public int ChapterNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}