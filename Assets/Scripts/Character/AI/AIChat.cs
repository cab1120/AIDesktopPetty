using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;

public class AIChat : MonoBehaviour
{
    // --- 请求锁 ---
    private bool isProcessingBubble = false;
    // --- 配置信息 ---
    private string siliconFlowKey;
    private string bochaApiKey;
    
    private string siliconFlowUrl = "https://api.siliconflow.cn/v1/chat/completions";
    private string bochaUrl = "https://api.bochaai.com/v1/web-search"; 

    [System.Serializable]
    public class ApiConfig {
        public string siliconFlowKey;
        public string bochaApiKey;
    }
    void Awake()
    {
        RunLog();
        
        LoadConfig();
    }

    private void RunLog()
    {
        // 指定日志文件保存到程序根目录下的 log.txt
        string logPath = Path.Combine(Application.dataPath, "../run_log.txt");
        Application.logMessageReceived += (condition, stackTrace, type) => {
            File.AppendAllText(logPath, $"[{System.DateTime.Now}] [{type}] {condition}\n");
            if (type == LogType.Exception || type == LogType.Error) {
                File.AppendAllText(logPath, stackTrace + "\n");
            }
        };
    }
    private void LoadConfig()
    {
        // 路径：Assets/StreamingAssets/config.json
        string path = Path.Combine(Application.streamingAssetsPath, "config.json");
    
        if (File.Exists(path))
        {
            try 
            {
                string json = File.ReadAllText(path);
                // 使用 Newtonsoft.Json 解析
                ApiConfig config = JsonConvert.DeserializeObject<ApiConfig>(json);
            
                if (config != null)
                {
                    siliconFlowKey = config.siliconFlowKey;
                    bochaApiKey = config.bochaApiKey;
                    Debug.Log("API 密钥通过 Newtonsoft 加载成功");
                }
            }
            catch (System.Exception e) { Debug.LogError("解析 JSON 失败: " + e.Message); }
        }
        else { Debug.LogError("找不到配置文件: " + path); }
        
    }
    
    // 主调用接口
    public IEnumerator GetAIReply(string userMessage, System.Action<string> callback)
    {
        string searchResults = "";

        // 1. 先去博查进行联网搜索
        yield return StartCoroutine(SearchWeb(userMessage, (results) => {
            searchResults = results;
        }));

        // 2. 获取当前时间
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss dddd");

        // 3. 构建增强型 System Prompt
        string systemPrompt =AIPrompt(searchResults, currentTime,null);
        
        
        //callback(systemPrompt);

        // 4. 调用 DeepSeek 获取回答
        yield return StartCoroutine(CallDeepSeek(systemPrompt, userMessage, callback));
    }
    
    public IEnumerator GetAIBubbleReply(string context, Action<string> callback)
    {
        if (isProcessingBubble) yield break; // 防止并发请求
        isProcessingBubble = true;

        string searchResults = "";
    
        // 策略优化：只有包含特定关键词才搜索，否则留空
        if (NeedSearch(context)) {
            yield return StartCoroutine(SearchWeb(context, (results) => {
                searchResults = results;
            }));
        }

        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss dddd");
        string systemPrompt = AIBubblePrompt(searchResults, currentTime, null);

        yield return StartCoroutine(CallDeepSeek(systemPrompt, context, (reply) => {
            isProcessingBubble = false;
            callback?.Invoke(reply);
        }));
    }

    private bool NeedSearch(string title) {
        // 仅针对视频、特定网页进行搜索，减少开销
        return title.Contains("Bilibili") || title.Contains("YouTube") || title.Contains("新闻") || title.Contains("-");
    }
    //AI人格设定

    private string AIPrompt(string searchResults, string currentTime, string userMemory)
    {
        return IrohaPromptBuilder.Build(currentTime, searchResults, userMemory);
    }
    
    private string AIBubblePrompt(string searchResults, string currentTime, string userMemory)
    {
        return IrohaPromptBuilder.BubbleBuild(currentTime, searchResults, userMemory);
    }
    
    
    // --- 第一步：博查搜索逻辑 ---
    private IEnumerator SearchWeb(string query, System.Action<string> searchCallback)
    {
        JObject requestBody = new JObject();
        requestBody["query"] = query;
        requestBody["freshness"] = "noLimit"; // 搜索时间范围：noLimit, oneDay, oneWeek等
        requestBody["summary"] = true;       // 是否需要总结

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody.ToString());
        UnityWebRequest request = new UnityWebRequest(bochaUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + bochaApiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("博查搜索失败: " + request.error);
            searchCallback("未搜索到相关实时信息。");
        }
        else
        {
            try
            {
                JObject res = JObject.Parse(request.downloadHandler.text);
                // 提取博查返回的网页摘要片段 (具体解析字段需对照博查最新文档)
                // 假设返回格式为 data: { webPages: { value: [...] } }
                var pages = res["data"]?["webPages"]?["value"];
                StringBuilder sb = new StringBuilder();
                if (pages != null)
                {
                    foreach (var page in pages)
                    {
                        sb.AppendLine($"- {page["name"]}: {page["snippet"]}");
                    }
                }
                searchCallback(sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError("解析博查结果出错: " + e.Message);
                searchCallback("搜索内容解析失败。");
            }
        }
    }

    // --- 第二 step：调用 DeepSeek ---
    private IEnumerator CallDeepSeek(string systemPrompt, string userMessage, System.Action<string> callback)
    {
        JObject root = new JObject();
        root["model"] = "Pro/deepseek-ai/DeepSeek-V3";
        root["messages"] = new JArray(
            new JObject { { "role", "system" }, { "content", systemPrompt } },
            new JObject { { "role", "user" }, { "content", userMessage } }
        );
        root["stream"] = false;
        root["temperature"] = 0.8; // 增加随机性和灵性，建议 0.7 - 0.9 之间
        root["presence_penalty"] = 0.6; // 减少重复话术的概率
        root["max_tokens"] = 1024;

        byte[] bodyRaw = Encoding.UTF8.GetBytes(root.ToString());
        UnityWebRequest request = new UnityWebRequest(siliconFlowUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + siliconFlowKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            callback("出错了: " + request.error);
        }
        else
        {
            JObject obj = JObject.Parse(request.downloadHandler.text);
            string reply = obj["choices"][0]["message"]["content"].ToString();
            callback(reply);
        }
    }
}