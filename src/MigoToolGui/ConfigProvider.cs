namespace MigoToolGui.Bootstrap
{
    public class ConfigProvider
    {
        public Config GetConfig() =>
            new()
            {
                Ip = "192.168.2.57",
                //Ip = "127.0.0.1",
                Port = 10086
            };
    }
}