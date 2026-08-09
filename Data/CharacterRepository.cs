using AI_Novel_writing_System.Models;
using System.Collections.Generic;
using System.Linq;

namespace AI_Novel_writing_System.Data
{
    public class CharacterRepository
    {
        private readonly DatabaseService databaseService;

        public CharacterRepository(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public Character Create(Character character)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Character>("characters");

            if (character.CreatedAt == default)
                character.CreatedAt = DateTime.Now;

            character.Id = collection.Insert(character);

            return character;
        }

        public List<Character> GetByNovelId(int novelId)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Character>("characters");

            return collection
                .Find(x => x.NovelId == novelId)
                .OrderBy(x => x.Name)
                .ToList();
        }

        public Character? GetById(int id)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Character>("characters");

            return collection.FindById(id);
        }

        public bool Update(Character character)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Character>("characters");

            return collection.Update(character);
        }

        public bool Delete(int id)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Character>("characters");

            return collection.Delete(id);
        }
    }
}