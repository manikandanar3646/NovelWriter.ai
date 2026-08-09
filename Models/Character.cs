using LiteDB;

namespace AI_Novel_writing_System.Models
{
    public class Character
    {
        [BsonId]
        public int Id { get; set; }

        public int NovelId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Personality { get; set; } = string.Empty;

        public string Background { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}