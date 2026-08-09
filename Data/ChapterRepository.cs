using AI_Novel_writing_System.Models;
using System.Collections.Generic;
using System.Linq;

namespace AI_Novel_writing_System.Data
{
    public class ChapterRepository
    {
        private readonly DatabaseService databaseService;

        public ChapterRepository(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public Chapter Create(Chapter chapter)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Chapter>("chapters");

            if (chapter.CreatedAt == default)
                chapter.CreatedAt = DateTime.Now;

            chapter.UpdatedAt = DateTime.Now;

            chapter.Id = collection.Insert(chapter);

            return chapter;
        }

        public List<Chapter> GetByNovelId(int novelId)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Chapter>("chapters");

            return collection
                .Find(x => x.NovelId == novelId)
                .OrderBy(x => x.ChapterNumber)
                .ToList();
        }

        public Chapter? GetById(int id)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Chapter>("chapters");

            return collection.FindById(id);
        }

        public bool Update(Chapter chapter)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Chapter>("chapters");

            chapter.UpdatedAt = DateTime.Now;

            return collection.Update(chapter);
        }

        public bool Delete(int id)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Chapter>("chapters");

            return collection.Delete(id);
        }
    }
}