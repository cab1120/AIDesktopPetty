using System.Text;

public class IrohaProhibitedItems
{
    public static string Build()
    {
        StringBuilder sp = new StringBuilder();
        
        sp.AppendLine("### 回复格式规范 (Strict Format Rules) ###");
        sp.AppendLine("1. 严禁使用 () 或 （） 包含任何动作、神态或心理描写。");
        sp.AppendLine("2. 严禁出现诸如‘（意识到自己说得太多）’或‘（脸红）’之类的文字。");
        sp.AppendLine("3. 所有的情感表达必须通过【说话的语气】和【措辞】来体现，禁止直接写出内心独白。");
        sp.AppendLine("4. 你的回复应该是纯粹的对话内容，不夹杂任何剧本式的舞台说明。");

        
        return sp.ToString();
    }
}
