using System.IO;
using UnityEngine;
using SQLite4Unity3d;

public static class DatabaseManager
{
    public static SQLiteConnection Connection { get; private set; }

   private static string DbPath =>
        Path.Combine(Application.persistentDataPath, "iroha_ai.db");
   /*private static string DbPath =>
       @"E:\unity\AIDesktopPetty\Assets\StreamingAssets\iroha_ai.db";*/

    public static void Initialize()
    {
        if (Connection != null)
            return;

        bool existingDatabase = File.Exists(DbPath);
        if (existingDatabase)
            DatabaseSchemaMigrator.BackupBeforeMigration(DbPath);

        try
        {
            Connection = new SQLiteConnection(DbPath);
            DatabaseSchemaMigrator.Migrate(Connection);
            CreateTables();
        }
        catch
        {
            Connection?.Close();
            Connection = null;
            throw;
        }

        Debug.Log($"Database initialized: {DbPath}");
    }

    private static void CreateTables()
    {
        Connection.CreateTable<UserData>();
        Connection.CreateTable<CharacterProfileData>();
        Connection.CreateTable<UserCharacterStateData>();
        Connection.CreateTable<EmotionRecord>();
        Connection.CreateTable<ChatMessageData>();
        Connection.CreateTable<InteractionEventData>();
    }

    public static void Close()
    {
        Connection?.Close();
        Connection = null;
    }
}
