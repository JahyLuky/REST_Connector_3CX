using log4net;
using Npgsql;
using System.Reflection;

namespace Chat_3CX_API.Models
{
    public class ConnectionHandler
    {
        public string _connectionString { get; private set; }
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void ReadPostgreConnection(IConfiguration configuration)
        {
            _connectionString = configuration["RestConnectorApiSettings:ConnectionToDatabase"];
            if (string.IsNullOrEmpty(_connectionString))
            {
                Log.Error("ConnectionToDatabase is empty!");
                return;
            }
            Log.Info($"ConnectionToDatabase {_connectionString}");
        }

        public bool GetChatStatus(string chatName)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                Log.Error($"Database is not connected!");
                throw new Exception("Database is not connected!");
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT chat_conversation_member.fkid_conversation, chat_results.is_finished, chat_conversation_member.participant_no " +
                                   "FROM chat_conversation_member " +
                                   "JOIN chat_results ON chat_conversation_member.fkid_conversation = chat_results.fkid_conversation " +
                                   "WHERE chat_conversation_member.participant_no = @chatName " +
                                   "ORDER BY chat_conversation_member.fkid_conversation DESC " +
                                   "LIMIT 1;";

                    try
                    {
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@chatName", chatName);

                            using (var reader = cmd.ExecuteReader())
                            {
                                bool? isFinished = null;
                                while (reader.Read())
                                {
                                    int fkid_conversation = reader.GetInt32(0);
                                    isFinished = reader.IsDBNull(1) ? null : reader.GetBoolean(1);
                                    if (isFinished.HasValue && isFinished.Value)
                                        return true;
                                }
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error running query. {ex}");
                        throw new Exception($"Error running query. {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error openning connection. {ex}");
                throw new Exception($"Error openning connection. {ex}");
            }
        }
    }
}
