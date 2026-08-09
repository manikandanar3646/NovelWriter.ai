using LiteDB;
using System;
using System.IO;

namespace AI_Novel_writing_System.Data
{
    public class DatabaseService
    {
        private readonly string databasePath;

        public DatabaseService()
        {
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "AI_Novel_Writing_System"
            );

            Directory.CreateDirectory(appDataFolder);

            databasePath = Path.Combine(
                appDataFolder,
                "novel_database.db"
            );
        }

        public LiteDatabase GetDatabase()
        {
            return new LiteDatabase(databasePath);
        }
    }
}