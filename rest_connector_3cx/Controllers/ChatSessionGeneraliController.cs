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
    // Route from Generali
    [ApiController]
    [Route("webapi/api/v2/chats")]
    public class ChatSessionGeneraliController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ChatSessionStorage _chatSessions;
        public ConfigurationHelper _configuration;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ChatSessionGeneraliController(HttpClient httpClient, IConfiguration configuration, ChatSessionStorage chatSessionService)
        {
            Log.Info("##### Configuration for Generali Loaded");

            _httpClient = httpClient;
            _chatSessions = chatSessionService;
            _configuration = new ConfigurationHelper();
            _configuration.ReadConfiguration(configuration);
        }

        [HttpPost("")]
        [SwaggerOperation(Summary = "Request chat", Description = "This endpoint requests chat and generates IDs (chatId, userId, secureKey) that are used for further communication. For userData key = \"chatbot_service\" must be specified.")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartChat([FromBody] StartChatRequest? request)
        {
            string nickname, tenantName, alias;
            Dictionary<string, string>? userData;

            if (request != null)
            {
                nickname = request.nickName;

                if (string.IsNullOrEmpty(nickname))
                {
                    Log.Error("JSON request is missing nickname.");
                    return BadRequest(new { status = "error", message = "Missing nickname." });
                }

                if (request.userData == null || !request.userData.ContainsKey("chatbot_service") || string.IsNullOrEmpty(request.userData["chatbot_service"]))
                {
                    Log.Error("JSON request is missing userData[chatbot_service].");
                    return BadRequest(new { status = "error", message = "Missing userData[chatbot_service]." });
                }

                userData = request.userData;
            }
            else
            {
                Log.Error("Unsupported content type.");
                return BadRequest(new { status = "error", message = "Unsupported content type." });
            }

            if (_chatSessions.FindNickname(nickname))
            {
                nickname = "Chatbot_" + new Random().Next(0, 10000);
            }

            var senderIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var senderPort = Request.HttpContext.Connection.RemotePort;
            var senderUrl = $"{Request.Scheme}://{senderIp}:{senderPort}{Request.Path}{Request.QueryString}";

            var chatSession = new MessageRequests
            {
                chatId = GenerateId.GenerateUppercaseGuid().Substring(0, 16),
                userId = GenerateId.GenerateUppercaseGuid().Substring(0, 16),
                secureKey = GenerateId.GenerateCustomId(17),
                nickName = nickname,
                userData = userData,
                requestUrl = senderUrl
            };

            chatSession.requestUrl = senderUrl + $"/{chatSession.chatId}";
            Log.Info($"Sender URL: {chatSession.requestUrl}");

            _chatSessions.SetSession(chatSession.userId, chatSession);

            Log.Info("##### Chat Started");
            Log.Info($"Generated IDs:\nUserId: {chatSession.userId}\nChatName: {chatSession.nickName}\nChatId: {chatSession.chatId}\nSecureKey: {chatSession.secureKey}");
            Log.Info($"Userdata:");
            foreach (var kvp in chatSession.userData)
            {
                Log.Info($"Key: {kvp.Key}, Value: {kvp.Value}");
            }

            var response = new
            {
                chatId = chatSession.chatId.ToString(),
                path = $"/api/v2/chats/{chatSession.chatId}",
                userId = chatSession.userId,
                secureKey = chatSession.secureKey,
                alias = "207",
                tenantName = "Resources",
                messages = new[]
                {
                    new
                    {
                        from = new
                        {
                            nickname = nickname,
                            participantId = 1,
                            type = "Client"
                        },
                        index = 1,
                        type = "ParticipantJoined",
                        utcTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                },
                statusCode = 0
            };

            return Ok(response);
        }

        [HttpPost("{chatId}")]
        [SwaggerOperation(Summary = "Handle a chat", Description = "This endpoint allows you to handle specified chat - use operationName: \"SendMessage\", \"SendUrl\", \"UpdateUserData\" or \"Complete\". If operationName = \"UpdateUserData\" is used, key = \"chatbot_service\" must be specified.")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveMessage(string chatId, [FromBody] MessageRequestGenerali? request)
        {
            Log.Info("##### Message From Generali");

            string userId, operationName, tenantName, alias;

            if (request != null)
            {
                userId = request.userId;

                operationName = request.operationName;
                tenantName = request.tenantName;
                alias = request.alias;

                if (string.IsNullOrEmpty(userId))
                {
                    Log.Error("JSON request is missing userId");
                    return BadRequest(new { status = "error", message = "Missing userId." });
                }
            }
            else
            {
                Log.Error("Unsupported content type.");
                return BadRequest(new { status = "error", message = "Unsupported content type." });
            }

            var chatSession = _chatSessions.GetSession(userId);
            if (chatSession != null)
            {
                switch (operationName)
                {
                    case "SendMessage":
                        {
                            try
                            {
                                string text = request.text;
                                if (string.IsNullOrEmpty(text))
                                {
                                    Log.Error("JSON request is missing text");
                                    return BadRequest(new { status = "error", message = "Missing text." });
                                }

                                await ForwardMessageTo3CX(chatSession, text);

                                var response = new
                                {
                                    messages = new object[] { },
                                    chatEnded = false,
                                    statusCode = 0,
                                    alias = alias,
                                    secureKey = chatSession.secureKey,
                                    userId = chatSession.userId,
                                    tenantName = tenantName
                                };
                                chatSession.isClosed = false;
                                chatSession.hasStarted = true;
                                return Ok(response);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error forwarding message to 3CX. {ex}");
                                return BadRequest(new { status = "error", message = ex.Message });
                            }
                        }
                    case "Complete":
                        {
                            try
                            {
                                string text = "***Chat bol uzavretý zákazníkom. Zatvorte tento chat.***";

                                await ForwardMessageTo3CX(chatSession, text);

                                var response = new
                                {
                                    statusCode = 0
                                };
                                chatSession.hasStarted = true;
                                chatSession.isClosed = true;
                                return Ok(response);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error forwarding message to 3CX. {ex}");
                                return BadRequest(new { status = "error", message = ex.Message });
                            }
                        }
                    case "SendUrl":
                        {
                            try
                            {
                                string text = request.pushUrl;
                                if (string.IsNullOrEmpty(text))
                                {
                                    Log.Error("JSON request is missing pushUrl.");
                                    return BadRequest(new { status = "error", message = "Missing pushUrl." });
                                }

                                await ForwardMessageTo3CX(chatSession, text);

                                var response = new
                                {
                                    statusCode = 0,
                                    alias = alias,
                                    secureKey = chatSession.secureKey,
                                    userId = chatSession.userId,
                                    tenantName = tenantName
                                };
                                chatSession.isClosed = false;
                                chatSession.hasStarted = true;
                                return Ok(response);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error forwarding message to 3CX. {ex}");
                                return BadRequest(new { status = "error", message = ex.Message });
                            }
                        }
                    case "UpdateUserData":
                        {
                            if (request.userData == null)
                            {
                                Log.Error("JSON request is missing userData[chatbot_service] to update.");
                                return BadRequest(new { status = "error", message = "Missing userData[chatbot_service] to update." });
                            }
                            if (chatSession.userData == null)
                            {
                                chatSession.userData = new Dictionary<string, string>();
                            }

                            chatSession.userData = request.userData;

                            var response = new
                            {
                                statusCode = 0,
                                alias = alias,
                                secureKey = chatSession.secureKey,
                                userId = chatSession.userId,
                                tenantName = tenantName
                            };
                            chatSession.isClosed = false;
                            chatSession.hasStarted = true;
                            return Ok(response);
                        }
                    default:
                        {
                            Log.Error("Unknown operationName.");
                            return BadRequest(new { status = "error", message = "Unknown operationName." });
                        }
                }
            }
            return NotFound(new { status = "error", message = "No or wrong answer from client." });
        }

        private async Task ForwardMessageTo3CX(MessageRequests chatSession, string message)
        {
            Log.Info("##### Forward Message To 3CX");
            Log.Info($"Message: {message}, userId: {chatSession.userId}, chatName: {chatSession.nickName}, userData: {chatSession.userData["chatbot_service"]}");
            var now = DateTime.Now;
            string queue;

            // TODO: Update the logic for UserData
            switch (chatSession.userData["chatbot_service"])
            {
                case "Servis":
                    {
                        queue = _configuration._trunkNumber_3CX_Queue2;
                        break;
                    }
                default:
                    {
                        queue = _configuration._trunkNumber_3CX_Queue1;
                        break;
                    }
            }

            var payload = new
            {
                data = new
                {
                    event_type = "message.received",
                    id = Guid.NewGuid().ToString(),
                    occurred_at = now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    payload = new
                    {
                        from = new
                        {
                            phone_number = chatSession.nickName,
                        },
                        id = Guid.NewGuid().ToString(),
                        received_at = now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                        text = message,
                        to = new[]
                        {
                            new
                            {
                                phone_number = queue,
                            }
                        },
                    },
                },
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration._apiUrl_3CX)
            {
                Content = content
            };

            var apiKey = _configuration._authorization;
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            Log.Info($"RequestMessage: {requestMessage}");

            chatSession.hasStarted = true;

            var response = await _httpClient.PostAsync(_configuration._apiUrl_3CX, content);
            Log.Info($"Response: {response}");
            var responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
        }
    }
}
