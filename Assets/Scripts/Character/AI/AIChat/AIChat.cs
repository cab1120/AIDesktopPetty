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
    private readonly HashSet<UnityWebRequest> cancelledRequests = new HashSet<UnityWebRequest>();

    private int requestTimeoutSeconds = 30;
    private string model = "Pro/deepseek-ai/DeepSeek-V3";
    private string configError;
    private string searchConfigError;
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
        {
            cancelledRequests.Add(request);
            request.Abort();
        }
    }

    public void CancelBubbleRequests()
    {
        bubbleGeneration++;
        isProcessingBubble = false;
        foreach (UnityWebRequest request in new List<UnityWebRequest>(bubbleRequests))
        {
            cancelledRequests.Add(request);
            request.Abort();
        }
    }

    private void OnDisable()
    {
        GlobalSession.SessionVersionChanged -= OnSessionVersionChanged;
        CancelActiveRequests();
        isProcessingBubble = false;
    }


    private IEnumerator ExecuteRequest(UnityWebRequest request, Action<RequestExecutionResult> completed, bool bubbleRequest = false)
    {
        activeRequests.Add(request);

        if (bubbleRequest)
            bubbleRequests.Add(request);

        bool timedOut = false;

        try
        {
            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (Exception)
            {
                completed?.Invoke(new RequestExecutionResult(request,
                    RequestExecutionStatus.NetworkError, "请求启动失败"));
                yield break;
            }

            float startTime = Time.realtimeSinceStartup;

            while (!operation.isDone)
            {
                // 外部主动取消。
                if (cancelledRequests.Contains(request))
                    break;

                // 自己管理超时，这样能明确知道它是 Timeout，
                // 而不是依赖 UnityWebRequest.error 字符串判断。
                if (Time.realtimeSinceStartup - startTime
                    >= requestTimeoutSeconds)
                {
                    timedOut = true;
                    request.Abort();
                    break;
                }

                yield return null;
            }

            RequestExecutionResult result;

            if (cancelledRequests.Contains(request))
            {
                result = new RequestExecutionResult(
                    request,
                    RequestExecutionStatus.Cancelled,
                    "请求已取消");
            }
            else if (timedOut)
            {
                result = new RequestExecutionResult(
                    request,
                    RequestExecutionStatus.Timeout,
                    $"请求超过 {requestTimeoutSeconds} 秒");
            }
            else
            {
                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                        result = new RequestExecutionResult(
                            request,
                            RequestExecutionStatus.Success);
                        break;

                    case UnityWebRequest.Result.ConnectionError:
                        result = new RequestExecutionResult(
                            request,
                            RequestExecutionStatus.NetworkError,
                            request.error);
                        break;

                    case UnityWebRequest.Result.ProtocolError:
                        result = new RequestExecutionResult(
                            request,
                            RequestExecutionStatus.HttpError,
                            request.error);
                        break;

                    case UnityWebRequest.Result.DataProcessingError:
                        result = new RequestExecutionResult(
                            request,
                            RequestExecutionStatus.DataError,
                            request.error);
                        break;

                    default:
                        result = new RequestExecutionResult(
                            request,
                            RequestExecutionStatus.NetworkError,
                            request.error);
                        break;
                }
            }

            completed?.Invoke(result);
        }
        finally
        {
            activeRequests.Remove(request);
            bubbleRequests.Remove(request);
            cancelledRequests.Remove(request);

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
        public string siliconFlowUrl;
        public string bochaUrl;
        public string model;
        public int timeoutSeconds;
    }

    void Awake()
    {
        LoadConfig();
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
                    if (!string.IsNullOrWhiteSpace(config.siliconFlowUrl))
                        siliconFlowUrl = config.siliconFlowUrl;
                    if (!string.IsNullOrWhiteSpace(config.bochaUrl))
                        bochaUrl = config.bochaUrl;
                    if (!string.IsNullOrWhiteSpace(config.model))
                        model = config.model;
                    if (config.timeoutSeconds > 0)
                        requestTimeoutSeconds = Mathf.Clamp(config.timeoutSeconds, 1, 120);
                    if (string.IsNullOrWhiteSpace(siliconFlowKey))
                        configError = "配置缺少 siliconFlowKey: " + path;
                    if (string.IsNullOrWhiteSpace(bochaApiKey))
                        searchConfigError = "配置缺少 bochaApiKey: " + path;
                }
                else
                {
                    configError = "配置 JSON 为空: " + path;
                }
            }
            catch (System.Exception) { configError = "配置 JSON 无效: " + path; }
        }
        else { configError = "找不到配置文件: " + path; }
        if (configError != null)
            Debug.LogError(configError);

    }

    // 主调用接口
    // 新 Conversation 主链使用
    public IEnumerator GetAIReply(
        ChatTurn turn,
        System.Action<ChatReplyResult> callback)
    {
        if (turn == null)
        {
            callback?.Invoke(
                ChatReplyResult.Failure(
                    ChatReplyFailureReason.InvalidRequest,
                    "无效对话轮次"));

            yield break;
        }

        if (configError != null)
        {
            callback?.Invoke(ChatReplyResult.Failure(
                ChatReplyFailureReason.InvalidRequest, configError));
            yield break;
        }

        SessionSnapshot session =
            turn.Session;

        if (!GlobalSession.IsCurrent(session))
        {
            callback?.Invoke(
                ChatReplyResult.Cancelled(
                    "会话已发生变化"));

            yield break;
        }

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
            {
                callback?.Invoke(
                    ChatReplyResult.Cancelled(
                        "会话已发生变化"));

                yield break;
            }

            if (decision == null)
            {
                callback?.Invoke(
                    ChatReplyResult.Failure(
                        ChatReplyFailureReason.SearchDecisionFailed,
                        "联网搜索决策失败"));

                yield break;
            }

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
                {
                    callback?.Invoke(
                        ChatReplyResult.Cancelled(
                            "会话已发生变化"));

                    yield break;
                }

                if (searchResults == null)
                {
                    callback?.Invoke(
                        ChatReplyResult.Failure(
                            ChatReplyFailureReason.SearchFailed,
                            "联网搜索失败"));

                    yield break;
                }

                if (GlobalSession.IsCurrent(session)) SearchCacheService.Add(
                    session,
                    decision.Query,
                    searchResults,
                    decision.Reason
                );
            }
        }

        if (!GlobalSession.IsCurrent(session))
        {
            callback?.Invoke(
                ChatReplyResult.Cancelled(
                    "会话已发生变化"));

            yield break;
        }

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
        if (configError != null)
        {
            callback?.Invoke(null);
            yield break;
        }
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

                if (searchResults == null)
                {
                    callback?.Invoke(null);
                    yield break;
                }
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
        if (searchConfigError != null)
        {
            Debug.LogError(searchConfigError);
            searchCallback?.Invoke(null);
            yield break;
        }
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

        yield return ExecuteRequest(request, execution =>
        {
            if (!execution.IsSuccess)
            {
                Debug.LogWarning(
                    $"博查搜索失败：" +
                    $"{execution.Status} / {execution.Error}");

                searchCallback(null);
                return;
            }

            try
            {
                JObject res =
                    JObject.Parse(
                        execution.Request.downloadHandler.text);

                var pages =
                    res["data"]?["webPages"]?["value"];

                StringBuilder sb =
                    new StringBuilder();

                if (pages != null)
                {
                    foreach (var page in pages)
                    {
                        sb.AppendLine(
                            $"- {page["name"]}: {page["snippet"]}");
                    }
                }

                searchCallback(sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "解析博查结果出错: " + e.Message);

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
            model;

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

        yield return ExecuteRequest(request, execution =>
        {
            ChatReplyResult outcome;

            switch (execution.Status)
            {
                case RequestExecutionStatus.Cancelled:

                    outcome = ChatReplyResult.Cancelled(
                        execution.Error);
                    break;

                case RequestExecutionStatus.Timeout:

                    outcome = ChatReplyResult.Failure(
                        ChatReplyFailureReason.Timeout,
                        execution.Error);
                    break;

                case RequestExecutionStatus.NetworkError:

                    outcome = ChatReplyResult.Failure(
                        ChatReplyFailureReason.Network,
                        execution.Error);
                    break;

                case RequestExecutionStatus.HttpError:

                    outcome = ChatReplyResult.Failure(
                        ChatReplyFailureReason.HttpError,
                        execution.Error);
                    break;

                case RequestExecutionStatus.DataError:

                    outcome = ChatReplyResult.Failure(
                        ChatReplyFailureReason.InvalidResponse,
                        execution.Error);
                    break;

                case RequestExecutionStatus.Success:
                default:
                    outcome = ChatReplyParser.Parse(
                        execution.Request.downloadHandler.text);
                    break;
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
        model;

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
            root.ToString());

    UnityWebRequest request =
        new UnityWebRequest(
            siliconFlowUrl,
            "POST");

    request.uploadHandler =
        new UploadHandlerRaw(bodyRaw);

    request.downloadHandler =
        new DownloadHandlerBuffer();

    request.SetRequestHeader(
        "Content-Type",
        "application/json");

    request.SetRequestHeader(
        "Authorization",
        "Bearer " + siliconFlowKey);

    yield return ExecuteRequest(
        request,
        execution =>
        {
            // execution 已经不是 UnityWebRequest，
            // 而是我们自己的 RequestExecutionResult。
            if (!execution.IsSuccess)
            {
                callback?.Invoke(null);
                return;
            }

            try
            {
                JObject obj =
                    JObject.Parse(
                        execution.Request
                            .downloadHandler
                            .text);

                string result =
                    obj["choices"]?[0]?["message"]?["content"]
                        ?.ToString();

                callback?.Invoke(
                    string.IsNullOrWhiteSpace(result)
                        ? null
                        : result);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "主动气泡结果解析失败：" +
                    e.Message);

                callback?.Invoke(null);
            }
        },
        true);
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

        root["model"] = model;

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

        yield return ExecuteRequest(request, execution =>
        {
            if (!execution.IsSuccess)
            {
                Debug.LogWarning(
                    $"搜索决策调用失败：" +
                    $"{execution.Status} / {execution.Error}");

                callback(null);
                return;
            }

            try
            {
                JObject obj =
                    JObject.Parse(
                        execution.Request.downloadHandler.text);

                string result =
                    obj["choices"]?[0]?["message"]?["content"]
                        ?.ToString();

                callback(
                    string.IsNullOrWhiteSpace(result)
                        ? null
                        : result);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "搜索决策返回解析失败：" + e.Message);

                callback(null);
            }
        });
    }
    private enum RequestExecutionStatus
    {
        Success,
        Cancelled,
        Timeout,
        NetworkError,
        HttpError,
        DataError
    }

    private sealed class RequestExecutionResult
    {
        public UnityWebRequest Request { get; }
        public RequestExecutionStatus Status { get; }
        public string Error { get; }

        public bool IsSuccess =>
            Status == RequestExecutionStatus.Success;

        public RequestExecutionResult(
            UnityWebRequest request,
            RequestExecutionStatus status,
            string error = null)
        {
            Request = request;
            Status = status;
            Error = error;
        }
    }
}
