using QuitQQ.Configuration;

var mre = new ManualResetEventSlim(false);

var config = ConfigManager.ReadConfig();
var bridge = new MessageBridge(config);
await bridge.StartAsync();

mre.Wait();