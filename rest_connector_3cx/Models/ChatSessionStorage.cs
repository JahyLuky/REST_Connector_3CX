using log4net;
using System.Reflection;

namespace Chat_3CX_API.Models
{
    // Store current sessions
    public class ChatSessionStorage
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        // <userId, Request>
        private readonly Dictionary<string, MessageRequests> _sessions = new();
        public ChatSessionStorage() { }

        public Dictionary<string, MessageRequests> GetAllSessions()
        {
            return _sessions;
        }

        public MessageRequests GetSession(string userID)
        {
            _sessions.TryGetValue(userID, out var session);
            if (session == null)
            {
                Log.Error("UserID was not found!");
                throw new Exception("UserID was not found!");
            }
            return session;
        }

        public MessageRequests GetSessionByChatName(string chatName)
        {
            var session = _sessions.Values.FirstOrDefault(session => session.nickName == chatName);
            if (session == null)
            {
                Log.Error("ChatName was not found!");
                throw new Exception("ChatName was not found!");
            }
            return session;
        }

        public void SetSession(string userId, MessageRequests session)
        {
            _sessions[userId] = session;
        }

        public void RemoveSession(string userID)
        {
            _sessions.Remove(userID);
        }

        public bool FindNickname(string nickname)
        {
            foreach (var name in _sessions)
            {
                if (name.Value.nickName == nickname)
                    return true;
            }
            return false;
        }
    }
}
