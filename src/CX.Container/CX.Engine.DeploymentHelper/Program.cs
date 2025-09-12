// See https://aka.ms/new-console-template for more information

using CX.Engine.DeploymentHelper;

var curDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
Directory.SetCurrentDirectory(curDir!);
Directory.CreateDirectory("Config");
var ab = new AppSettingsBuilder();
File.WriteAllText("Config\\Generated appsettings.json.tpl", ab.Write());
File.WriteAllText("Config\\Generated values.yaml", Values.Map.Write());

JsonTplMerger.Merge("Config\\appsettings.json.txt", "Config\\appsettings.json.tpl", "Config\\Generated appsettings.json.tpl");
YamlMerger.Merge("Config\\values.yaml.txt", "Config\\values.yaml", "Config\\Generated values.yaml");

Console.WriteLine("Configuration merged.");
//Process.Start("explorer.exe", Path.Combine(curDir, "Config"));

