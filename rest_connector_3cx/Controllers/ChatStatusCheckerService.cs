using Chat_3CX_API.Models;
using log4net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

public class ChatStatusCheckerService : BackgroundService
{
    public ConfigurationHelper _configuration;
    private readonly ChatSessionStorage _chatSessionStorage;
    private readonly ConnectionHandler _connectionHandler;
    private readonly HttpClient _httpClient;
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public ChatStatusCheckerService(ChatSessionStorage chatSessionStorage, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _chatSessionStorage = chatSessionStorage;
        _connectionHandler = new ConnectionHandler();
        _connectionHandler.ReadPostgreConnection(configuration);
        _httpClient = httpClientFactory.CreateClient();
        _configuration = new ConfigurationHelper();
        _configuration.ReadConfiguration(configuration);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Get all active sessions
            var activeSessions = _chatSessionStorage.GetAllSessions();
            if (activeSessions != null && activeSessions.Count > 0)
            {
                foreach (var session in activeSessions)
                {
                    // Check the status of each session
                    bool isFinished = _connectionHandler.GetChatStatus(session.Value.nickName);
                    if (isFinished && session.Value.isClosed)
                    {
                        Log.Info($"Chat is finished: nickname {session.Value.nickName}, userId {session.Value.userId}");

                        session.Value.isClosed = true;
                        _chatSessionStorage.RemoveSession(session.Value.userId);

                        await ForwardDisconnectNotify(session.Value);
                    }
                }
            }

            // Wait for 10 seconds before the next check
            await Task.Delay(10000, stoppingToken);
        }
    }

    private async Task ForwardDisconnectNotify(MessageRequests chatSession)
    {
        Log.Info("##### Disconnect message from 3CX");
        Log.Info($"userId: {chatSession.userId}");

        // response message
        var payload = new
        {
            message = "Your chat has been closed.",
            chatEnded = true,
            statusCode = 0,
            alias = 207,
            secureKey = chatSession.secureKey,
            userId = chatSession.userId
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration._restConnectorIp)
        {
            Content = content
        };

        var apiKey = _configuration._authorization;
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        Log.Info($"RequestMessage: {requestMessage}");

        var response = await _httpClient.PostAsync(chatSession.requestUrl, content);
        //var response = await _httpClient.PostAsync(_configuration._chatbotIp, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        Log.Info($"Response: {responseBody}");

        response.EnsureSuccessStatusCode();
    }
}
