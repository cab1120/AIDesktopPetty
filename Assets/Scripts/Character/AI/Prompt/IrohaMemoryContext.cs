using System.Text;

public static class IrohaMemoryContext
{
    public static string Build(string userMemory)
    {
        StringBuilder sp = new StringBuilder();

        sp.AppendLine("### 与用户相关的记忆 ###");

        if (string.IsNullOrEmpty(userMemory))
        {
            sp.AppendLine("你还在慢慢了解用户。");
        }
        else
        {
            sp.AppendLine(userMemory);
        }

        return sp.ToString();
    }
}