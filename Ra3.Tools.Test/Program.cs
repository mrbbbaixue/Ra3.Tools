using Console = System.Console;


var ra3 = new Ra3.Tools.Instance();

Console.WriteLine("[Profiles]");
foreach (var i in ra3.Profiles)
{
    Console.WriteLine(i);
}
Console.WriteLine();

Console.WriteLine("[Current Profile]");
Console.WriteLine(ra3.GetCurrentProfile());
Console.WriteLine();

Console.WriteLine("[RA3 Path] " + Ra3.Tools.Registry.GetRA3Path());
Console.WriteLine("[Registry Status] " + Ra3.Tools.Registry.Status);

Console.ReadKey();