using System;

public class SearchCacheEntry
{
    public string Query;
    public string Results;
    public string Reason;
    public long CreatedAtTicks;

    public DateTime CreatedAt => new DateTime(CreatedAtTicks);
}