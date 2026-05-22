/*using Unity.VisualScripting.Dependencies.Sqlite;

public class SQLiteEmotionStorage : IEmotionStorage
{
    private SQLiteConnection db = new SQLiteConnection("iroha.db");

    public void Save(EmotionData data) 
    {
        // 数据库操作：插入一条新心情，保留历史记录
        //db.Insert(data); 
    }

    public EmotionData Load() 
    {
        // 数据库操作：取出最近的一条情绪
        //return db.Table<EmotionData>().OrderByDescending(v => v.LastUpdateTicks).FirstOrDefault();
        return null;
    }
}*/