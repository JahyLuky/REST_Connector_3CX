using log4net;
using System.Reflection;

namespace Chat_3CX_API.Models
{
    public class ConfigurationHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string _fqdn_3CX { get; private set; }
        public string _apiUrl_3CX { get; private set; }
        public string _trunkNumber_3CX_Queue1 { get; private set; }
        public string _trunkNumber_3CX_Queue2 { get; private set; }
        public string _restConnectorIp { get; private set; }
        public int _restConnectorPort { get; private set; }
        public string _chatbotIp { get; private set; }
        public string _authorization { get; private set; }
        public string _apiToken3CX { get; private set; }
        public Dictionary<string, string> _queueList { get; private set; }

        public void ReadConfiguration(IConfiguration configuration)
        {
            _apiUrl_3CX = configuration["RestConnectorApiSettings:ApiUrl_3CX"];
            if (string.IsNullOrEmpty(_apiUrl_3CX))
            {
                Log.Error("API url for 3CX is empty!");
                throw new Exception("API url for 3CX is empty!");
            }
            //Log.Info($"ApiUrl_3CX {_apiUrl_3CX}");

            _trunkNumber_3CX_Queue1 = configuration["RestConnectorApiSettings:TrunkNumber_3CX_TestQ1"];
            if (string.IsNullOrEmpty(_trunkNumber_3CX_Queue1))
            {
                Log.Error("Trunk number for 3CX Queue1 is empty!");
                throw new Exception("Trunk number for 3CX Queue1 is empty!");
            }
            //Log.Info($"TrunkNumber_3CX {_trunkNumber_3CX_Queue1}");

            _trunkNumber_3CX_Queue2 = configuration["RestConnectorApiSettings:TrunkNumber_3CX_TestQ2"];
            if (string.IsNullOrEmpty(_trunkNumber_3CX_Queue2))
            {
                Log.Error("Trunk number for 3CX Queue2 is empty!");
                throw new Exception("Trunk number for 3CX Queue2 is empty!");
            }
            //Log.Info($"TrunkNumber_3CX {_trunkNumber_3CX_Queue2}");

            _restConnectorIp = configuration["RestConnectorApiSettings:RestConnectorIp"];
            if (string.IsNullOrEmpty(_restConnectorIp))
            {
                Log.Error("Rest connector IP is empty!");
                throw new Exception("Rest connector IP is empty!");
            }
            //Log.Info($"RestConnectorIp {_restConnectorIp}");

            _restConnectorPort = int.Parse(configuration["RestConnectorApiSettings:RestConnectorPort"]);
            if (_restConnectorPort <= 0)
            {
                Log.Error("Rest connector port is invalid!");
                throw new Exception("Rest connector API port is invalid!");
            }
            //Log.Info($"RestConnectorPort {_restConnectorPort}");

            _authorization = configuration["RestConnectorApiSettings:Authorization"];
            if (string.IsNullOrEmpty(_authorization))
            {
                Log.Error("Authorization token is empty!");
                throw new Exception("Authorization token is empty!");
            }
            //Log.Info($"Authorization {_authorization}");
        }

        public void Read3CXConfiguration(IConfiguration configuration)
        {
            _fqdn_3CX = configuration["Stats3CXSettings:FQDN_3CX"];
            if (string.IsNullOrEmpty(_fqdn_3CX))
            {
                Log.Error("3CX FQDN is empty!");
                throw new Exception("3CX FQDN is empty!");
            }
            //Log.Info($"ApiUrl_3CX {_fqdn_3CX}");

            _apiToken3CX = configuration["Stats3CXSettings:ApiToken_3CX"];
            if (string.IsNullOrEmpty(_apiToken3CX))
            {
                Log.Error("3CX API token is empty!");
                throw new Exception("3CX API token is empty!");
            }
            //Log.Info($"ApiToken_3CX {_apiToken3CX}");

            _queueList = new Dictionary<string, string>();

            var queuesConfig = configuration.GetSection("Stats3CXSettings:Queues_3CX").GetChildren();

            if (queuesConfig == null || !queuesConfig.Any())
            {
                Log.Error("Queue configuration is empty or incorrect!");
                throw new Exception("Queue configuration is empty or incorrect!");
            }

            foreach (var queue in queuesConfig)
            {
                var queueKeyValue = queue.GetChildren().FirstOrDefault();

                if (queueKeyValue == null || string.IsNullOrEmpty(queueKeyValue.Key) || string.IsNullOrEmpty(queueKeyValue.Value))
                {
                    Log.Error("Queue name or value is empty or incorrect!");
                    throw new Exception("Queue name or value is empty or incorrect!");
                }

                _queueList.Add(queueKeyValue.Key, queueKeyValue.Value);
                //Log.Info($"Queue name: {queueKeyValue.Key}, DN: {queueKeyValue.Value}");
            }
        }
    }
}
