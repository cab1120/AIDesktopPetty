using System;
using System.IO;
using SQLite4Unity3d;

/// <summary>迁移旧库并隔离无法证明归属的角色；原始记录及迁移前备份均保留。</summary>
public static class DatabaseSchemaMigrator
{
    public const int CurrentVersion = 1;

    public static void BackupBeforeMigration(string path)
    {
        using (var probe = new SQLiteConnection(path))
        {
            int version = probe.ExecuteScalar<int>("PRAGMA user_version");
            if (version >= CurrentVersion)
                return;
            if (version < 0 || version > CurrentVersion)
                throw new InvalidOperationException("数据库版本不受支持: " + version);
        }

        // 旧版本使用回滚日志；如果存在 WAL，简单复制主文件不能构成完整备份。
        if (File.Exists(path + "-wal"))
            throw new IOException("数据库存在 WAL 文件，请先安全关闭其他数据库连接后再迁移: " + path);

        string backup = path + ".before-m0-v1-" +
            DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".bak";
        File.Copy(path, backup, false);
    }

    public static void Migrate(SQLiteConnection db)
    {
        int version = db.ExecuteScalar<int>("PRAGMA user_version");
        if (version > CurrentVersion || version < 0)
            throw new InvalidOperationException("数据库版本不受支持: " + version);
        if (version == CurrentVersion)
        {
            Validate(db);
            return;
        }

        db.RunInTransaction(() =>
        {
            int profiles = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CharacterProfile'");
            if (profiles != 0)
            {
                bool hasUserId = db.GetTableInfo("CharacterProfile")
                    .Exists(column => column.Name == "UserId");
                if (!hasUserId)
                    db.Execute("ALTER TABLE CharacterProfile ADD COLUMN UserId TEXT");

                db.Execute("UPDATE CharacterProfile SET UserId = " +
                    "(SELECT UserId FROM User WHERE User.UserName = CharacterProfile.UserName) " +
                    "WHERE UserId IS NULL OR UserId = ''");
                db.Execute("CREATE TABLE IF NOT EXISTS LegacyUnclaimedCharacter (" +
                    "CharacterId TEXT PRIMARY KEY NOT NULL)");
                db.Execute("INSERT OR IGNORE INTO LegacyUnclaimedCharacter (CharacterId) " +
                    "SELECT CharacterId FROM CharacterProfile WHERE UserId IS NULL OR UserId = '' " +
                    "OR NOT EXISTS (SELECT 1 FROM User WHERE User.UserId = CharacterProfile.UserId)");
                Validate(db);
                db.Execute("CREATE INDEX IF NOT EXISTS IDX_CharacterProfile_UserId " +
                    "ON CharacterProfile(UserId)");
            }
            db.Execute("PRAGMA user_version = 1");
        });
    }

    private static void Validate(SQLiteConnection db)
    {
        int profiles = db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CharacterProfile'");
        if (profiles == 0)
            return;
        bool hasUserId = db.GetTableInfo("CharacterProfile")
            .Exists(column => column.Name == "UserId");
        if (!hasUserId)
            throw new InvalidOperationException("CharacterProfile 缺少 UserId，迁移未完成");
        int markers = db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' " +
            "AND name='LegacyUnclaimedCharacter'");
        if (markers == 0)
        {
            int orphanCount = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM CharacterProfile c WHERE c.UserId IS NULL OR c.UserId = '' " +
                "OR NOT EXISTS (SELECT 1 FROM User u WHERE u.UserId = c.UserId)");
            if (orphanCount != 0)
                throw new InvalidOperationException("存在没有隔离标记的角色资料");
            return;
        }
        int unmarkedOrphans = db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM CharacterProfile c WHERE " +
            "(c.UserId IS NULL OR c.UserId = '' OR NOT EXISTS " +
            "(SELECT 1 FROM User u WHERE u.UserId = c.UserId)) " +
            "AND NOT EXISTS (SELECT 1 FROM LegacyUnclaimedCharacter q " +
            "WHERE q.CharacterId = c.CharacterId)");
        int invalidMarkers = db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM LegacyUnclaimedCharacter q WHERE " +
            "NOT EXISTS (SELECT 1 FROM CharacterProfile c WHERE c.CharacterId = q.CharacterId " +
            "AND (c.UserId IS NULL OR c.UserId = '' OR NOT EXISTS " +
            "(SELECT 1 FROM User u WHERE u.UserId = c.UserId)))");
        if (unmarkedOrphans != 0 || invalidMarkers != 0)
            throw new InvalidOperationException("角色归属与旧角色隔离标记不一致");
    }
}
