namespace Chat_3CX_API.Models
{
    // internal chat session storage
    public class MessageRequests
    {
        public string chatId { get; set; }
        public string userId { get; set; }
        public string nickName { get; set; }
        public string secureKey { get; set; }
        public bool isClosed { get; set; } = false;
        public bool hasStarted { get; set; } = true;
        public Dictionary<string, string>? userData { get; set; }
        public string requestUrl { get; set; }
    }

    // Generali requests chat
    public class StartChatRequest
    {
        public string nickName { get; set; }
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? tenantName { get; set; }
        public string? emailAddress { get; set; }
        public string? subject { get; set; }
        public string? endpoint { get; set; }
        public Dictionary<string, string> userData { get; set; }
    }

    public class MessageRequestGenerali
    {
        public string operationName { get; set; }
        public string? text { get; set; }
        public string userId { get; set; }
        public string secureKey { get; set; }
        public string? alias { get; set; }
        public string? tenantName { get; set; }
        public string? messageType { get; set; }
        public string? pushUrl { get; set; }
        public Dictionary<string, string>? userData { get; set; }
    }

    public class MessageRequest3Cx
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Text { get; set; }
    }

    public class StartChatResponse
    {
        public string ChatId { get; set; }
        public string Path { get; set; }
        public string UserId { get; set; }
        public string SecureKey { get; set; }
        public string Alias { get; set; }
        public string TenantName { get; set; }
        public List<Message> Messages { get; set; }
        public int StatusCode { get; set; }
    }

    public class Message
    {
        public From From { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
        public long UtcTime { get; set; }
    }

    public class From
    {
        public string Nickname { get; set; }
        public int ParticipantId { get; set; }
        public string Type { get; set; }
    }

    // Gets statisctics about agents in specific queue
    public class AgentStatisticsResponse
    {
        public string OdataContext { get; set; }
        public List<AgentStatistics> Value { get; set; }
    }

    public class AgentStatistics
    {
        public string Dn { get; set; }
        public string DnDisplayName { get; set; }
        public string Queue { get; set; }
        public string QueueDisplayName { get; set; }
        public string LoggedInTime { get; set; }
        public int LostCount { get; set; }
        public int AnsweredCount { get; set; }
        public int AnsweredPercent { get; set; }
        public int AnsweredPerHourCount { get; set; }
        public string RingTime { get; set; }
        public string AvgRingTime { get; set; }
        public string TalkTime { get; set; }
        public string AvgTalkTime { get; set; }
    }

    // Gets statisctics about login of agents in specific queue
    public class AgentStatusResponse
    {
        public string OdataContext { get; set; }
        public List<AgentStatusStatistics> Value { get; set; }
    }

    public class AgentStatusStatistics
    {
        public string QueueNo { get; set; }
        public string AgentNo { get; set; }
        public string Agent { get; set; }
        public string Day { get; set; }
        public string loggedInDt { get; set; }
        public string LoggedOutDt { get; set; }
        public string LoggedInInterval { get; set; }
        public string LoggedInDayInterval { get; set; }
        public string LoggedInTotalInterval { get; set; }
        public string TalkingInterval { get; set; }
        public string TalkingDayInterval { get; set; }
        public string TalkingTotalInterval { get; set; }
    }
}
