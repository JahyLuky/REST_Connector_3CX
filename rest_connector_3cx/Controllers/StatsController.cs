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
    [Route("stats")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationHelper _configuration;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public StatsController(HttpClient httpClient, IConfiguration configuration)
        {
            Log.Info("##### Configuration for 3CX stats.");
            _httpClient = httpClient;
            _configuration = new ConfigurationHelper();
            _configuration.Read3CXConfiguration(configuration);
        }

        [HttpGet("isOnlineValue")]
        [SwaggerOperation(Summary = "Gets number of agents from queue", Description = "This endpoint shows current number of agents in specific queue.")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IsOnlineValue([FromQuery] string id)
        {
            Log.Info($"isOnlineValue id = {id}");

            string queue_DN = GetDN(id);

            if (queue_DN == "Queue not found")
            {
                Log.Error("Incorrect queue name.");
                return BadRequest("Incorrect queue name.");
            }

            string access_token = await InitConnection();
            if (string.IsNullOrEmpty(access_token))
            {
                Log.Error("Can't get token/empty token.");
                return BadRequest("Can't get token/empty token.");
            }

            int onlineAgents = await GetAgentsLoggedInQueue(queue_DN, access_token);

            return Ok(new { message = $"Agents logged in queue {id}: {onlineAgents}" });
        }

        string GetDN(string queue_name)
        {
            foreach (var queue in _configuration._queueList)
            {
                if (queue.Key == queue_name)
                {
                    return queue.Value;
                }
            }
            return "Queue not found";
        }

        private async Task<string> InitConnection()
        {
            string username = "customer-portal";
            string password = _configuration._apiToken3CX;
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

            var parameters = new Dictionary<string, string>
            {
                { "client_id", "customer-portal" },
                { "grant_type", "client_credentials" }
            };

            var encodedContent = new FormUrlEncodedContent(parameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_configuration._fqdn_3CX}/connect/token")
            {
                Content = encodedContent
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Error: {response.StatusCode}, {responseBody}");
                throw new Exception($"Request failed with status code: {response.StatusCode} and message: {responseBody}");
            }

            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                return tokenElement.GetString();
            }
            else
            {
                Log.Error("Access token not found in response");
                throw new Exception("Access token not found in response");
            }
        }

        // {{3CX_FQDN}}/xapi/v1/ReportAgentsInQueueStatistics/
        // Pbx.GetAgentsInQueueStatisticsData(
        //  queueDnStr='string',
        //  startDt=Edm.DateTimeOffset,
        //  endDt=Edm.DateTimeOffset,
        //  waitInterval='string')
        private async Task<int> GetAgentsLoggedInQueue(string queue_name, string access_token)
        {
            Log.Info($"Start looking for agents from queue: {queue_name}");
            List<string> agentsInQueue = await GetAgentsFromQueue(queue_name, access_token);

            var currentTime = DateTime.Now.TimeOfDay;
            var startTime = new TimeSpan(8, 0, 0);
            var endTime = new TimeSpan(18, 0, 0);
            Log.Info($"currentTime: {currentTime}, startTime {startTime}, endTime {endTime}");
            // TODO: hotfix -> remove later
            if (currentTime < startTime || currentTime > endTime)
            {
                Log.Info("Request for agents from queue statistics send outside 8AM - 6PM.");
                return 0;
            }
            // TODO: hotfix -> remove later
            Log.Info($"Total agents in queue: {agentsInQueue.Count()}");
            return agentsInQueue.Count();

            Log.Info($"Start looking for agents statuses");
            int agentLoggedIn = await GetAgentStatus(agentsInQueue, queue_name, access_token);

            return agentLoggedIn;
        }

        private async Task<List<string>> GetAgentsFromQueue(string queue_name, string access_token)
        {
            // We are 2 hours ahead, thus -2h on startDate
            var startTime = DateTime.UtcNow;
            var endTime = startTime;
            startTime = startTime.AddHours(-2);


            string formattedStartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string formattedEndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string requestUrl = $"{_configuration._fqdn_3CX}/xapi/v1/ReportAgentsInQueueStatistics/Pbx.GetAgentsInQueueStatisticsData(queueDnStr='{queue_name}',startDt={formattedStartTime},endDt={formattedEndTime},waitInterval='0')";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            Log.Info($"GetAgentsFromQueue: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                List<string> agentsInQueue = await GetAgents(responseBody);
                return agentsInQueue;
            }
            else
            {
                Log.Error($"Request failed with status code: {response.StatusCode} and message: {responseBody}");
                throw new Exception($"Request failed with status code: {response.StatusCode} and message: {responseBody}");
            }
        }

        private async Task<List<string>> GetAgents(string responseBody)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var agentStatisticsResponse = JsonSerializer.Deserialize<AgentStatisticsResponse>(responseBody, options);

            Log.Info($"Agent statistics response:  {agentStatisticsResponse}");

            if (agentStatisticsResponse == null)
            {
                Log.Error($"3CX response is empty. can't check available agents.");
                throw new Exception($"3CX response is empty. can't check available agents.");
            }

            List<string> agentsInQueue = new List<string>();

            foreach (var agent in agentStatisticsResponse.Value)
            {
                if (!string.IsNullOrEmpty(agent.Dn))
                {
                    agentsInQueue.Add(agent.Dn);
                    Log.Info($"Agent: {agent.DnDisplayName}, Queue: {agent.QueueDisplayName}, DN: {agent.Dn}");
                }
            }

            return agentsInQueue;
        }

        // {{3CX_FQDN}}/xapi/v1/ReportAgentLoginHistory/
        // Pbx.GetAgentLoginHistoryData(
        //  clientTimeZone='string',
        //  startDt=Edm.DateTimeOffset,
        //  endDt=Edm.DateTimeOffset,
        //  queueDnStr='string',
        //  agentDnStr='string')
        //  ?$top=1&$skip=0
        //  &$orderby=day DESC
        private async Task<int> GetAgentStatus(List<string> agentsInQueue, string queue_name, string access_token)
        {
            var startTime = "2022-10-21T00:30:00Z";
            var endTime = DateTime.UtcNow.AddHours(2);

            string formattedEndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            int agentLoggedIn = 0;

            foreach (var agentDn in agentsInQueue)
            {

                string requestUrl = $"{_configuration._fqdn_3CX}/xapi/v1/ReportAgentLoginHistory/Pbx.GetAgentLoginHistoryData(clientTimeZone='UTC',startDt={startTime},endDt={formattedEndTime},queueDnStr='{queue_name}',agentDnStr='{agentDn}')?$top=1&$skip=0&$orderby=day DESC";

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (await AgentStatus(responseBody, agentDn))
                        agentLoggedIn++;
                }
                else
                {
                    Log.Error($"Request failed with status code: {response.StatusCode} and message: {responseBody}");
                    throw new Exception($"Request failed with status code: {response.StatusCode} and message: {responseBody}");
                }
            }

            return agentLoggedIn;
        }

        private async Task<bool> AgentStatus(string responseBody, string agentDn)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var agentStatisticsResponse = JsonSerializer.Deserialize<AgentStatusResponse>(responseBody, options);

            if (agentStatisticsResponse == null)
            {
                Log.Error($"3CX response is empty. Can't check available agents.");
                throw new Exception($"3CX response is empty. Can't check available agents.");
            }

            List<string> agentsInQueue = new List<string>();

            var currentTime = DateTime.Now.TimeOfDay;
            var startTime = new TimeSpan(8, 0, 0);
            var endTime = new TimeSpan(18, 0, 0);

            if (currentTime < startTime || currentTime > endTime)
            {
                return false;
            }

            foreach (var agent in agentStatisticsResponse.Value)
            {
                if (agent.AgentNo == agentDn)
                {
                    Console.WriteLine($"Agent: {agent.Agent}, Queue: {agent.QueueNo}, DN: {agent.AgentNo}");
                    return true;
                }
            }

            return false;
        }
    }
}
