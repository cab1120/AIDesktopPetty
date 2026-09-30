using System;

public class SearchCacheEntry
{
    public string UserId;
    public string CharacterId;
    public long SessionVersion;
    public string Query;
    public string Results;
    public string Reason;
    public long CreatedAtTicks;

    public DateTime CreatedAt => new DateTime(CreatedAtTicks);
}
