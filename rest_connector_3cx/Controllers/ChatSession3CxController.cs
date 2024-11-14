using Chat_3CX_API.Models;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Chat_3CX_API.Controllers
{
    // Route from 3CX
    [ApiController]
    [Route("/")]
    public class ChatSession3CxController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ChatSessionStorage _chatSessions;
        public ConfigurationHelper _configuration;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ChatSession3CxController(HttpClient httpClient, IConfiguration configuration, ChatSessionStorage chatSessionService)
        {
            Log.Info("##### Configuration for 3CX Loaded");

            _httpClient = httpClient;
            _chatSessions = chatSessionService;
            _configuration = new ConfigurationHelper();
            _configuration.ReadConfiguration(configuration);
        }

        [HttpPost("/")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [SwaggerOperation(Summary = "Send a message", Description = "This endpoint allows sending a message to 3CX.")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveMessage3CX([FromBody] MessageRequest3Cx request)
        {
            Log.Info("##### Message From 3CX");
            Log.Info($"From: {request.From}, To: {request.To}, Text: {request.Text}");

            var chatSession = _chatSessions.GetSessionByChatName(request.To);
            string chatName = chatSession.nickName;
            if (string.IsNullOrEmpty(chatName))
            {
                Log.Error($"ChatName was not found in active sessions!");
                throw new Exception("ChatName was not found in active sessions!");
            }

            if (chatSession.isClosed)
            {
                return Ok("Chat is already closed.");
            }

            var text = request.Text ?? string.Empty;

            try
            {
                await ForwardMessageToGenerali(chatName, text);
                return Ok(new { status = "success", message = "Message is being forwarded." });
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending message to Generali.{ex}");
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        private async Task ForwardMessageToGenerali(string chatName, string message)
        {
            Log.Info("##### Forward Message To Generali");
            Log.Info($"chatName = {chatName}");

            if (string.IsNullOrEmpty(chatName))
            {
                Log.Error($"Chat session with name '{chatName}' was not found.\n#######\n");
                throw new KeyNotFoundException($"Chat session with name '{chatName}' was not found.");
            }

            var chatSession = _chatSessions.GetSessionByChatName(chatName);

            var payload = new
            {
                statusCode = 0,
                message = message,
                userId = chatSession.userId,
                secureKey = chatSession.secureKey,
                alias = "207",
                messageType = "text"
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            Log.Info($"Sending 3CX message to {chatSession.requestUrl}");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration._restConnectorIp)
            {
                Content = content
            };

            var apiKey = _configuration._authorization;
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            Log.Info($"RequestMessage: {requestMessage}\nContent: {content}");

            var response = await _httpClient.PostAsync(chatSession.requestUrl, content);

            //var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            Log.Info($"Response: {responseBody}");

            response.EnsureSuccessStatusCode();
        }
    }
}
