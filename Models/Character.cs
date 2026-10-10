
using LiteDB;

namespace AI_Novel_writing_System.Models
{
    public class Character
    {
        [BsonId]
        public int Id { get; set; }

        public int NovelId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Personality { get; set; } = string.Empty;

        public string Goals { get; set; } = string.Empty;

        public string Fears { get; set; } = string.Empty;

        public string Background { get; set; } = string.Empty;

        public string Secrets { get; set; } = string.Empty;

        public string DialogueStyle { get; set; } = string.Empty;

        public string Relationships { get; set; } = string.Empty;

        public string CharacterArc { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
