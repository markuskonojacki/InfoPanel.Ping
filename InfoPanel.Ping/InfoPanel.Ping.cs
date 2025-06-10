using System.Net.NetworkInformation;
using System.Reflection;
using InfoPanel.Plugins;
using IniParser;
using IniParser.Model;

namespace InfoPanel.Ping
{
    public class PingPlugin : BasePlugin
    {
        public override string? ConfigFilePath => _configFilePath;

        // UI display elements for InfoPanel
        private readonly PluginText _lastPingTime = new("ping-last", "Last ping time", "-");
        private readonly PluginSensor _pingSensor = new("ping", "Current ping", 0, "ms");

        // Configurable settings
        private string? _configFilePath;
        private double _pingRefreshTimer = 10;
        private string[] _pingServerAddresses;

        // Constants for timing and detection thresholds
        public override TimeSpan UpdateInterval => TimeSpan.FromSeconds(1);
        private DateTime _lastPingCallTime = DateTime.MinValue;

        // Constructor: Initializes the plugin with metadata
        public PingPlugin()
            : base("ping-plugin", "InfoPanel.Ping", "Displays your average ping to a list of servers in ms")
        { }

        public override void Initialize()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string basePath = assembly.ManifestModule.FullyQualifiedName;
            _configFilePath = $"{basePath}.ini";

            var parser = new FileIniDataParser();
            IniData config;
            if (!File.Exists(_configFilePath))
            {
                config = new IniData();

#if DEBUG
                config["Ping Plugin"]["Servers"] = "1.1.1.1,4.2.2.2,9.9.9.9";
#else
                config["Ping Plugin"]["Servers"] = "CommaSeparated,ListOf,ServersHere";
#endif
                config["Ping Plugin"]["RefreshTimer"] = "10";

                parser.WriteFile(_configFilePath, config);

                _pingServerAddresses = ParsePingAddresses(config["Ping Plugin"]["Servers"]);
            }
            else
            {
                try
                {
                    using (FileStream fileStream = new FileStream(_configFilePath!, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        string fileContent = reader.ReadToEnd();
                        config = parser.Parser.Parse(fileContent);
                    }

                    // parse server list
                    if (config["Ping Plugin"].ContainsKey("Servers"))
                    {
                        _pingServerAddresses = ParsePingAddresses(config["Ping Plugin"]["Servers"]);
                    }
                    else
                    {
                        config["Ping Plugin"]["Servers"] = "CommaSeparated,ListOf,ServersHere";
                        parser.WriteFile(_configFilePath, config);
                    }

                    // parse refresh timer
                    if (config["Ping Plugin"].ContainsKey("RefreshTimer") &&
                        double.TryParse(config["Ping Plugin"]["RefreshTimer"], out double pingRefreshTimer) &&
                        pingRefreshTimer > 0)
                    {
                        _pingRefreshTimer = pingRefreshTimer;
                    }
                    else
                    {
                        config["Ping Plugin"]["RefreshTimer"] = "10";
                        _pingRefreshTimer = 10;
                        parser.WriteFile(_configFilePath, config);
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private string[] ParsePingAddresses(string input)
        {
            // trim whitespace then split by comma
            return input.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Loads UI containers as required by BasePlugin
        public override void Load(List<IPluginContainer> containers)
        {
            var container = new PluginContainer("Ping");
            container.Entries.AddRange([_pingSensor, _lastPingTime]);
            containers.Add(container);
        }

        // Cleans up resources when the plugin is closed
        public override void Close()
        { }

        // Synchronous update method required by BasePlugin
        public override void Update()
        {
            UpdateAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task UpdateAsync(CancellationToken cancellationToken)
        {

            var now = DateTime.UtcNow;
            var timeSinceLastCall = (now - _lastPingCallTime).TotalSeconds;

            if (timeSinceLastCall > _pingRefreshTimer)
            {
                _pingSensor.Value = await PingAddressesAsync(_pingServerAddresses); ;
                _lastPingTime.Value = DateTime.UtcNow.ToString("o");
                _lastPingCallTime = now;
            }
        }

        static async Task<int> PingAddressesAsync(string[] addresses)
        {
            double totalPingTime = 0;
            int successfulPings = 0;

            foreach (var address in addresses)
            {
                try
                {
                    using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
                    {
                        PingReply reply = await ping.SendPingAsync(address.Trim(), 1000); // 1 second timeout
                        if (reply.Status == IPStatus.Success)
                        {
                            if (reply.RoundtripTime > 1000)
                            {
                                Console.WriteLine($"Ping to {address.Trim()} took longer than 1 second: {reply.RoundtripTime} ms");
                            }
                            else
                            {
                                totalPingTime += reply.RoundtripTime;
                                successfulPings++;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Ping to {address.Trim()} failed: {reply.Status}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pinging {address.Trim()}: {ex.Message}");
                }
            }

            return successfulPings > 0 ? (int)(totalPingTime / successfulPings) : 0;
        }

        // Logs errors and updates UI with error message
        private void HandleError(string errorMessage)
        { }
    }
}