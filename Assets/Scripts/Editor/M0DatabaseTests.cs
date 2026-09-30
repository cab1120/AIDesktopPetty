using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SQLite4Unity3d;

public class M0DatabaseTests
{
    private string directory;
    private SQLiteConnection db;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "m0-db-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        db = new SQLiteConnection(Path.Combine(directory, "test.db"));
    }

    [TearDown]
    public void TearDown()
    {
        GlobalSession.Clear();
        DatabaseManager.Close();
        db.Close();
        Directory.Delete(directory, true);
    }

    [Test]
    public void LegacyMigrationBackfillsStableIdAndCanRunAgain()
    {
        db.Execute("CREATE TABLE User (UserId TEXT PRIMARY KEY, UserName TEXT NOT NULL)");
        db.Execute("CREATE TABLE CharacterProfile (CharacterId TEXT PRIMARY KEY, UserName TEXT NOT NULL)");
        db.Execute("INSERT INTO User VALUES ('u1', 'old-name')");
        db.Execute("INSERT INTO CharacterProfile VALUES ('c1', 'old-name')");

        DatabaseSchemaMigrator.Migrate(db);
        DatabaseSchemaMigrator.Migrate(db);

        Assert.AreEqual(1, db.ExecuteScalar<int>("PRAGMA user_version"));
        Assert.AreEqual("u1", db.ExecuteScalar<string>(
            "SELECT UserId FROM CharacterProfile WHERE CharacterId = 'c1'"));
    }

    [Test]
    public void OrphanLegacyProfileIsQuarantinedWithoutBlockingValidOwner()
    {
        db.Execute("CREATE TABLE User (UserId TEXT PRIMARY KEY, UserName TEXT NOT NULL)");
        db.Execute("CREATE TABLE CharacterProfile (CharacterId TEXT PRIMARY KEY, UserName TEXT NOT NULL)");
        db.Execute("INSERT INTO User VALUES ('u1', 'valid')");
        db.Execute("INSERT INTO CharacterProfile VALUES ('c2', 'valid')");
        db.Execute("INSERT INTO CharacterProfile VALUES ('c1', 'missing')");

        DatabaseSchemaMigrator.Migrate(db);
        DatabaseSchemaMigrator.Migrate(db);
        Assert.AreEqual(1, db.ExecuteScalar<int>("PRAGMA user_version"));
        Assert.AreEqual("u1", db.ExecuteScalar<string>(
            "SELECT UserId FROM CharacterProfile WHERE CharacterId = 'c2'"));
        Assert.AreEqual(1, db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM LegacyUnclaimedCharacter WHERE CharacterId = 'c1'"));
        Assert.AreEqual(1, db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM CharacterProfile WHERE CharacterId = 'c1'"));
        Assert.IsNull(db.ExecuteScalar<string>(
            "SELECT UserId FROM CharacterProfile WHERE CharacterId = 'c1'"));
    }

    [Test]
    public void QuarantinedProfileIsHiddenFromGlobalLookupAndCannotBeEdited()
    {
        CreateModernTables();
        SeedUserAndProfile();
        db.Insert(new CharacterProfileData { CharacterId = "orphan", UserId = null,
            UserName = "missing", CharacterName = "unclaimed", IsActive = true });

        Assert.AreEqual(1, CharacterRepository.GetAll().Count);
        Assert.IsNull(CharacterRepository.GetById("orphan"));
        Assert.IsNull(CharacterRepository.GetByName("unclaimed"));
        Assert.IsNull(CharacterRepository.GetByUserAndName("missing", "unclaimed"));
        Assert.AreEqual(0, CharacterRepository.GetByUserName("missing").Count);
        Assert.IsFalse(CharacterRepository.UpdateCharacter(
            "orphan", "changed", "", true, out string error));
        Assert.IsNotEmpty(error);
        Assert.AreEqual("unclaimed", db.Find<CharacterProfileData>("orphan").CharacterName);
    }

    [Test]
    public void RenameKeepsProfileLinkedByUserId()
    {
        CreateModernTables();
        SeedUserAndProfile();
        Assert.IsTrue(UserRepository.UpdateUser("u1", "new-name", "", "User", out string error), error);
        Assert.AreEqual("u1", CharacterRepository.GetByUserAndName("new-name", "pet").UserId);
        Assert.IsNull(CharacterRepository.GetByUserAndName("old-name", "pet"));
    }

    [Test]
    public void DeleteUserRollsBackEveryTableWhenChildDeleteFails()
    {
        CreateModernTables();
        SeedUserAndProfile();
        SeedChildRows();
        db.Execute("CREATE TRIGGER fail_emotion BEFORE DELETE ON EmotionState " +
            "BEGIN SELECT RAISE(ABORT, 'injected failure'); END");

        Assert.IsFalse(UserRepository.DeleteUser("u1", out string error));
        Assert.IsNotEmpty(error);
        Assert.AreEqual(1, Count("User"));
        Assert.AreEqual(1, Count("CharacterProfile"));
        Assert.AreEqual(1, Count("ChatMessage"));
        Assert.AreEqual(1, Count("UserCharacterState"));
        Assert.AreEqual(1, Count("EmotionState"));
        Assert.AreEqual(1, Count("InteractionEvent"));
    }

    [Test]
    public void DeleteUserRemovesAllAssociatedRows()
    {
        CreateModernTables();
        SeedUserAndProfile();
        SeedChildRows();
        Assert.IsTrue(UserRepository.DeleteUser("u1", out string error), error);
        Assert.AreEqual(0, Count("User"));
        Assert.AreEqual(0, Count("CharacterProfile"));
        Assert.AreEqual(0, Count("ChatMessage"));
        Assert.AreEqual(0, Count("UserCharacterState"));
        Assert.AreEqual(0, Count("EmotionState"));
        Assert.AreEqual(0, Count("InteractionEvent"));
    }

    [Test]
    public void DeleteCharacterRemovesItsRowsAndKeepsOtherCharacter()
    {
        CreateModernTables();
        SeedUserAndProfile();
        SeedChildRows();
        db.Insert(new CharacterProfileData { CharacterId = "c2", UserId = "u1",
            UserName = "old-name", CharacterName = "other", IsActive = false });

        Assert.IsTrue(CharacterRepository.DeleteCharacter("c1", out string error), error);
        Assert.AreEqual(1, Count("CharacterProfile"));
        Assert.AreEqual(0, Count("ChatMessage"));
        Assert.AreEqual(0, Count("UserCharacterState"));
        Assert.AreEqual(0, Count("EmotionState"));
        Assert.AreEqual(0, Count("InteractionEvent"));
        Assert.AreEqual(1, db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM CharacterProfile WHERE CharacterId='c2' AND IsActive=1"));
    }

    [Test]
    public void CurrentCharacterCannotBeDeleted()
    {
        CreateModernTables();
        SeedUserAndProfile();
        var user = db.Find<UserData>("u1");
        var character = db.Find<CharacterProfileData>("c1");
        db.Insert(new CharacterProfileData { CharacterId = "c2", UserId = "u1",
            UserName = "old-name", CharacterName = "other", IsActive = false });
        GlobalSession.SetSession(user, character);

        Assert.IsFalse(CharacterRepository.DeleteCharacter("c1", out string error));
        Assert.IsNotEmpty(error);
        Assert.AreEqual(2, Count("CharacterProfile"));
    }

    [Test]
    public void CurrentCharacterRenameInvalidatesCapturedSession()
    {
        CreateModernTables();
        SeedUserAndProfile();
        var user = db.Find<UserData>("u1");
        var character = db.Find<CharacterProfileData>("c1");
        GlobalSession.SetSession(user, character);
        SessionSnapshot beforeRename = GlobalSession.CaptureSnapshot();

        character.CharacterName = "renamed-pet";
        db.Update(character);
        GlobalSession.SetCurrentCharacter(character);

        Assert.IsFalse(GlobalSession.IsCurrent(beforeRename));
        Assert.AreEqual("renamed-pet", GlobalSession.CaptureSnapshot().CharacterName);
    }

    private int Count(string table) => db.ExecuteScalar<int>("SELECT COUNT(*) FROM " + table);

    private void CreateModernTables()
    {
        db.CreateTable<UserData>();
        db.CreateTable<CharacterProfileData>();
        db.CreateTable<ChatMessageData>();
        db.CreateTable<UserCharacterStateData>();
        db.CreateTable<EmotionRecord>();
        db.CreateTable<InteractionEventData>();
        db.Execute("PRAGMA user_version = 1");
        typeof(DatabaseManager)
            .GetField("<Connection>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, db);
    }

    private void SeedUserAndProfile()
    {
        db.Insert(new UserData { UserId = "u1", UserName = "old-name",
            PasswordHash = "hash", Role = "User" });
        db.Insert(new CharacterProfileData { CharacterId = "c1", UserId = "u1",
            UserName = "old-name", CharacterName = "pet", IsActive = true });
    }

    private void SeedChildRows()
    {
        db.Insert(new ChatMessageData { MessageId = "m1", UserId = "u1", CharacterId = "c1",
            Sender = "User", Content = "hello" });
        db.Insert(new UserCharacterStateData { StateId = "s1", UserId = "u1", CharacterId = "c1" });
        db.Insert(new EmotionRecord { EmotionId = "e1", UserId = "u1", CharacterId = "c1",
            EmotionType = "calm" });
        db.Insert(new InteractionEventData { EventId = "i1", UserId = "u1", CharacterId = "c1",
            EventType = "login" });
    }
}

public class M0ChatProtocolTests
{
    [Test]
    public void ValidModelBodyProducesSuccess()
    {
        var result = ChatReplyParser.Parse(
            "{\"choices\":[{\"message\":{\"content\":\"hello\"}}]}");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("hello", result.Text);
    }

    [Test]
    public void EmptyModelBodyCannotBecomeAssistantMessage()
    {
        var result = ChatReplyParser.Parse(
            "{\"choices\":[{\"message\":{\"content\":\"  \"}}]}");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ChatReplyFailureReason.EmptyResponse, result.FailureReason);
        Assert.IsNull(result.Text);
    }

    [Test]
    public void InvalidJsonCannotBecomeAssistantMessage()
    {
        var result = ChatReplyParser.Parse("not-json");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ChatReplyFailureReason.InvalidResponse, result.FailureReason);
        Assert.IsNull(result.Text);
    }
}
