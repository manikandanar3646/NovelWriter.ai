using AI_Novel_writing_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI_Novel_writing_System.Data;

public class StoryNoteRepository
{
    private readonly DatabaseService _databaseService;

    public StoryNoteRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public StoryNote Create(StoryNote note)
    {
        using var db = _databaseService.GetDatabase();
        var collection = db.GetCollection<StoryNote>("story_notes");

        if (note.CreatedAt == default)
            note.CreatedAt = DateTime.Now;

        note.UpdatedAt = DateTime.Now;
        note.Id = collection.Insert(note);

        return note;
    }

    public List<StoryNote> GetByNovel(int novelId)
    {
        using var db = _databaseService.GetDatabase();

        return db.GetCollection<StoryNote>("story_notes")
            .Find(x => x.NovelId == novelId)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Title)
            .ToList();
    }

    public StoryNote? GetById(int id)
    {
        using var db = _databaseService.GetDatabase();

        return db.GetCollection<StoryNote>("story_notes")
            .FindById(id);
    }

    public bool Update(StoryNote note)
    {
        using var db = _databaseService.GetDatabase();

        note.UpdatedAt = DateTime.Now;

        return db.GetCollection<StoryNote>("story_notes")
            .Update(note);
    }

    public bool Delete(int id)
    {
        using var db = _databaseService.GetDatabase();

        return db.GetCollection<StoryNote>("story_notes")
            .Delete(id);
    }
}
