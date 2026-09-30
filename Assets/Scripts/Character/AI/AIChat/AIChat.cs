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
    private readonly HashSet<UnityWebRequest> activeRequests = new HashSet<UnityWebRequest>();
    private readonly HashSet<UnityWebRequest> bubbleRequests = new HashSet<UnityWebRequest>();
    private const int RequestTimeoutSeconds = 30;
    private Application.LogCallback logCallback;
    private int bubbleGeneration;

    private void OnEnable()
    {
        GlobalSession.SessionVersionChanged += OnSessionVersionChanged;
    }

    private void OnSessionVersionChanged(long version)
    {
        CancelActiveRequests();
        SearchCacheService.Clear();
    }

    public void CancelActiveRequests()
    {
        bubbleGeneration++;
        isProcessingBubble = false;
        foreach (UnityWebRequest request in new List<UnityWebRequest>(activeRequests))
            request.Abort();
    }

    public void CancelBubbleRequests()
    {
        bubbleGeneration++;
        isProcessingBubble = false;
        foreach (UnityWebRequest request in new List<UnityWebRequest>(bubbleRequests))
            request.Abort();
    }

    private void OnDisable()
    {
        GlobalSession.SessionVersionChanged -= OnSessionVersionChanged;
        CancelActiveRequests();
        isProcessingBubble = false;
    }

    private void OnDestroy()
    {
        if (logCallback != null)
            Application.logMessageReceived -= logCallback;
    }

    private IEnumerator ExecuteRequest(UnityWebRequest request, Action<UnityWebRequest> completed, bool bubbleRequest = false)
    {
        request.timeout = RequestTimeoutSeconds;
        activeRequests.Add(request);
        if (bubbleRequest) bubbleRequests.Add(request);
        try
        {
            yield return request.SendWebRequest();
            completed?.Invoke(request);
        }
        finally
        {
            activeRequests.Remove(request);
            bubbleRequests.Remove(request);
            request.Dispose();
        }
    }
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
        logCallback = (condition, stackTrace, type) => {
            File.AppendAllText(logPath, $"[{System.DateTime.Now}] [{type}] {condition}\n");
            if (type == LogType.Exception || type == LogType.Error) {
                File.AppendAllText(logPath, stackTrace + "\n");
            }
        };
        Application.logMessageReceived += logCallback;
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
    // 新 Conversation 主链使用
    public IEnumerator GetAIReply(
        ChatTurn turn,
        System.Action<ChatReplyResult> callback)
    {
        if (turn == null)
        {
            callback?.Invoke(ChatReplyResult.Failure("无效对话轮次"));
            yield break;
        }

        SessionSnapshot session =
            turn.Session;

        if (!GlobalSession.IsCurrent(session))
            yield break;

        string currentTime =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss dddd"
            );

        string recentContext =
            ChatContextTextBuilder
                .BuildRecentContextText(
                    session.UserId,
                    session.CharacterId,
                    8,
                    turn.UserMessageId
                );

        SearchDecision decision = null;

        string searchResults = "";

        if (SearchCacheService.TryGetRecent(
                session,
                turn.Input,
                out SearchCacheEntry cached))
        {
            decision =
                new SearchDecision
                {
                    NeedSearch = true,
                    Query = cached.Query,
                    Reason =
                        "用户当前话题可能延续之前的月读空间链路。"
                };

            Debug.Log(
                "从缓存读取搜索结果"
            );

            searchResults =
                cached.Results;
        }
        else
        {
            yield return StartCoroutine(
                SearchDecisionService.Decide(
                    turn.Input,
                    recentContext,
                    CallDeepSeekRaw,
                    result =>
                        decision = result
                )
            );

            if (!GlobalSession.IsCurrent(session))
                yield break;

            if (decision != null &&
                decision.NeedSearch)
            {
                yield return StartCoroutine(
                    SearchWeb(
                        decision.Query,
                        results =>
                        {
                            searchResults =
                                results;
                        }
                    )
                );

                if (!GlobalSession.IsCurrent(session))
                    yield break;

                if (GlobalSession.IsCurrent(session)) SearchCacheService.Add(
                    session,
                    decision.Query,
                    searchResults,
                    decision.Reason
                );
            }
        }

        if (!GlobalSession.IsCurrent(session))
            yield break;

        string searchBlock =
            SearchResultFormatter
                .FormatForPrompt(
                    searchResults,
                    decision
                );

        string systemPrompt =
            AIPrompt(
                searchBlock,
                currentTime,
                recentContext,
                session
            );

        yield return StartCoroutine(
            CallDeepSeek(
                systemPrompt,
                turn,
                callback
            )
        );
    }
    
    
    public IEnumerator GetAIBubbleReply(
        SessionSnapshot session,
        string context,
        Action<string> callback)
    {
        if (isProcessingBubble)
            yield break;

        if (!GlobalSession.IsCurrent(session))
            yield break;

        int generation = ++bubbleGeneration;
        isProcessingBubble = true;
        try
        {

        string searchResults = "";

        if (NeedSearch(context))
        {
            yield return StartCoroutine(
                SearchWeb(
                    context,
                    results =>
                    {
                        searchResults = results;
                    },
                    true
                )
            );
            if (!GlobalSession.IsCurrent(session))
                yield break;
        }

        string currentTime =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss dddd"
            );

        string systemPrompt =
            AIBubblePrompt(
                searchResults,
                currentTime,
                null,
                session
            );

        string reply = null;
        bool callbackInvoked = false;

        yield return StartCoroutine(
            CallDeepSeekBubble(
                systemPrompt,
                context,
                result =>
                {
                    reply = result;
                    callbackInvoked = true;
                }
            )
        );

        if (!callbackInvoked)
            yield break;

        // 请求期间如果切换了角色，
        // 这个旧角色产生的主动气泡不能再显示。
        if (!GlobalSession.IsCurrent(session))
            yield break;

        callback?.Invoke(reply);
        }
        finally
        {
            if (bubbleGeneration == generation)
                isProcessingBubble = false;
        }
    }

    private bool NeedSearch(string title) {
        // 仅针对视频、特定网页进行搜索，减少开销
        return title.Contains("Bilibili") || title.Contains("YouTube") || title.Contains("新闻") || title.Contains("-");
    }
    
    /// <summary>
    /// 构建ai提示词
    /// </summary>
    /// <param name="联网搜索结果"></param>
    /// <param name="当下时间"></param>
    /// <param name="与用户相关记忆"></param>
    /// <returns></returns>
    private string AIPrompt(
        string searchResults,
        string currentTime,
        string userMemory,
        SessionSnapshot session)
    {
        PromptContext context =
            new PromptContext
            {
                CurrentTime = currentTime,

                SearchResults =
                    searchResults,

                UserMemory =
                    userMemory,

                Emotion =
                    EmotionMemory.GetCurrentEmotion(
                        session.UserName,
                        session.CharacterName
                    ),

                RelationshipText =
                    RelationshipService
                        .BuildRelationshipPromptText(
                            session
                        )
            };

        return CharacterPromptBuilder
            .BuildChatPrompt(
                context,
                session
            );
    }
    
    private string AIBubblePrompt(
        string searchResults,
        string currentTime,
        string userMemory,
        SessionSnapshot session)
    {
        PromptContext context =
            new PromptContext
            {
                CurrentTime = currentTime,

                SearchResults = searchResults,

                UserMemory = userMemory,

                Emotion =
                    EmotionMemory.GetCurrentEmotion(
                        session.UserName,
                        session.CharacterName
                    ),

                RelationshipText =
                    RelationshipService
                        .BuildRelationshipPromptText(
                            session
                        )
            };

        return CharacterPromptBuilder
            .BuildBubblePrompt(
                context,
                session
            );
    }
    
    
    // --- 第一步：博查搜索逻辑 ---
    private IEnumerator SearchWeb(string query, System.Action<string> searchCallback, bool bubbleRequest = false)
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

        yield return ExecuteRequest(request, completed =>
        {
            if (completed.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("博查搜索失败: " + completed.error);
                searchCallback(null);
                return;
            }
            try
            {
                JObject res = JObject.Parse(completed.downloadHandler.text);
                var pages = res["data"]?["webPages"]?["value"];
                StringBuilder sb = new StringBuilder();
                if (pages != null)
                    foreach (var page in pages)
                        sb.AppendLine($"- {page["name"]}: {page["snippet"]}");
                searchCallback(sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError("解析博查结果出错: " + e.Message);
                searchCallback(null);
            }
        }, bubbleRequest);
    }

    /// <summary>
    /// 获取回答内容
    /// </summary>
    /// <param name="systemPrompt"></param>
    /// <param name="turn"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator CallDeepSeek(
        string systemPrompt,
        ChatTurn turn,
        System.Action<ChatReplyResult> callback)
    {
        JObject root =
            new JObject();

        root["model"] =
            "Pro/deepseek-ai/DeepSeek-V3";

        root["messages"] =
            ChatContextBuilder.BuildMessages(
                systemPrompt,
                turn
            );

        root["stream"] = false;
        root["temperature"] = 0.8;
        root["presence_penalty"] = 0.6;
        root["max_tokens"] = 1024;

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(
                root.ToString()
            );

        UnityWebRequest request =
            new UnityWebRequest(
                siliconFlowUrl,
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                bodyRaw
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + siliconFlowKey
        );

        yield return ExecuteRequest(request, completed =>
        {
            ChatReplyResult outcome;
            if (completed.result != UnityWebRequest.Result.Success)
            {
                outcome = ChatReplyResult.Failure(completed.error);
            }
            else
            {
                try
                {
                    JObject obj = JObject.Parse(completed.downloadHandler.text);
                    string result = obj["choices"]?[0]?["message"]?["content"]?.ToString();
                    outcome = string.IsNullOrWhiteSpace(result)
                        ? ChatReplyResult.Failure("回复为空")
                        : ChatReplyResult.Success(result);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("聊天结果解析失败：" + e.Message);
                    outcome = ChatReplyResult.Failure("回复解析失败");
                }
            }
            callback?.Invoke(outcome);
        });
    }
    
    private IEnumerator CallDeepSeekBubble(
        string systemPrompt,
        string context,
        Action<string> callback)
    {
        JObject root =
            new JObject();

        root["model"] =
            "Pro/deepseek-ai/DeepSeek-V3";

        root["messages"] =
            new JArray(
                new JObject
                {
                    { "role", "system" },
                    { "content", systemPrompt }
                },
                new JObject
                {
                    { "role", "user" },
                    { "content", context }
                }
            );

        root["stream"] = false;
        root["temperature"] = 0.8;
        root["presence_penalty"] = 0.6;
        root["max_tokens"] = 1024;

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(
                root.ToString()
            );

        UnityWebRequest request =
            new UnityWebRequest(
                siliconFlowUrl,
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + siliconFlowKey
        );

        yield return ExecuteRequest(request, completed =>
        {
            if (completed.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(null);
                return;
            }
            try
            {
                JObject obj = JObject.Parse(completed.downloadHandler.text);
                string result = obj["choices"]?[0]?["message"]?["content"]?.ToString();
                callback?.Invoke(string.IsNullOrWhiteSpace(result) ? null : result);
            }
            catch (Exception e)
            {
                Debug.LogWarning("主动气泡结果解析失败：" + e.Message);
                callback?.Invoke(null);
            }
        }, true);
    }
    
    /// <summary>
    /// 判断是否需要联网搜索
    /// </summary>
    /// <param name="systemPrompt"></param>
    /// <param name="userMessage"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator CallDeepSeekRaw(
        string systemPrompt,
        string userMessage,
        System.Action<string> callback)
    {
        JObject root = new JObject();

        root["model"] = "Pro/deepseek-ai/DeepSeek-V3";

        root["messages"] = new JArray(
            new JObject
            {
                { "role", "system" },
                { "content", systemPrompt }
            },
            new JObject
            {
                { "role", "user" },
                { "content", userMessage }
            }
        );

        root["stream"] = false;
        root["temperature"] = 0.1;
        root["presence_penalty"] = 0.0;
        root["max_tokens"] = 256;

        byte[] bodyRaw = Encoding.UTF8.GetBytes(root.ToString());

        UnityWebRequest request =
            new UnityWebRequest(siliconFlowUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + siliconFlowKey);

        yield return ExecuteRequest(request, completed =>
        {
            if (completed.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("搜索决策调用失败：" + completed.error);
                callback("");
                return;
            }
            try
            {
                JObject obj = JObject.Parse(completed.downloadHandler.text);
                string result = obj["choices"]?[0]?["message"]?["content"]?.ToString();
                callback(result ?? "");
            }
            catch (Exception e)
            {
                Debug.LogWarning("搜索决策返回解析失败：" + e.Message);
                callback("");
            }
        });
    }
}

public sealed class ChatReplyResult
{
    public bool IsSuccess { get; }
    public string Text { get; }
    public string Error { get; }

    private ChatReplyResult(bool isSuccess, string text, string error)
    {
        IsSuccess = isSuccess;
        Text = text;
        Error = error;
    }

    public static ChatReplyResult Success(string text) => new ChatReplyResult(true, text, null);
    public static ChatReplyResult Failure(string error) => new ChatReplyResult(false, null, error);
}
