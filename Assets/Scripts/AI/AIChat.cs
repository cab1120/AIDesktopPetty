using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Text;

public class AIChat : MonoBehaviour
{
    private static readonly string apiKey = "sk-vuauywtlrpdgiekcmxlypyozmucyqrurdjnefouewksedbhs";
    private static readonly string url = "https://api.siliconflow.cn/v1/chat/completions";

    public IEnumerator SendMessage(string userMessage, System.Action<string> callback)
    {
        string json = $@"
        {{
            ""model"": ""Pro/deepseek-ai/DeepSeek-V3"",
            ""messages"": [
                {{""role"": ""system"", ""content"": ""你是一个温柔的AI女友，说话自然一点""}},
                {{""role"": ""user"", ""content"": ""{userMessage}""}}
            ]
        }}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("请求失败: " + request.error);
            callback("出错了: " + request.error);
        }
        else
        {
            string result = request.downloadHandler.text;
            Debug.Log(result);

            JObject obj = JObject.Parse(result);
            string reply = obj["choices"][0]["message"]["content"].ToString();

            callback(reply);
        }
    }
}