using AI_Novel_writing_System.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace AI_Novel_writing_System.Data
{
    public class NovelRepository
    {
        private readonly DatabaseService databaseService;

        public NovelRepository(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public Novel Create(Novel novel)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Novel>("novels");

            if (novel.CreatedAt == default)
                novel.CreatedAt = DateTime.Now;

            novel.UpdatedAt = DateTime.Now;

            novel.Id = collection.Insert(novel);

            return novel;
        }

        public List<Novel> GetAll()
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Novel>("novels");

            return collection
                .FindAll()
                .OrderByDescending(x => x.UpdatedAt)
                .ToList();
        }

        public Novel? GetById(int id)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Novel>("novels");

            return collection.FindById(id);
        }

        public bool Update(Novel novel)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Novel>("novels");

            novel.UpdatedAt = DateTime.Now;

            return collection.Update(novel);
        }

        public bool Delete(int id)
        {
            using var db = databaseService.GetDatabase();

            var collection =
                db.GetCollection<Novel>("novels");

            return collection.Delete(id);
        }
    }
}