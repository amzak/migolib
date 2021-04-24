using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MigoToolGui.Utils;

namespace MigoToolGui
{
    public class ConfigProvider
    {
        private const string FileName = "config.json";
        
        private Config _config;
        private volatile bool _isLoaded;
        
        private readonly string _filePath;
        private readonly JsonSerializerOptions _serializerOptions;

        public ConfigProvider()
        {
            var workingDir = Directory.GetCurrentDirectory();
            _filePath = Path.Combine(workingDir, FileName);
            
            _serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonIpAddressConverter()
                }
            };
        }

        public async Task<Config> GetConfig()
        {
            if (_isLoaded)
            {
                return _config;
            }
            
            if (!File.Exists(_filePath))
            {
                return Config.Default;
            }
            
            var fileInfo = new FileInfo(_filePath);
            var size = fileInfo.Length;

            if (size == 0)
            {
                return Config.Default;
            }

            await using var fileStream = new FileStream(_filePath, FileMode.Open);
            _config = await JsonSerializer.DeserializeAsync<Config>(fileStream, _serializerOptions)
                .ConfigureAwait(false);

            _isLoaded = true;

            return _config;
        }

        public async Task SaveConfig(Config config)
        {
            await using var fileStream = new FileStream(_filePath, FileMode.OpenOrCreate);
            await JsonSerializer.SerializeAsync(fileStream, config, _serializerOptions)
                .ConfigureAwait(false);
        }
    }
}