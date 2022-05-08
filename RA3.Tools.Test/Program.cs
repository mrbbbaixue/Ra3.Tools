using RA3.Tools;

var ra3 = new RA3Instance();
foreach (var i in ra3.Profiles)
{
    Console.WriteLine(i);
}
Console.WriteLine(ra3.GetCurrentProfile());

Console.WriteLine("RA3 Path: " + RA3.Tools.Registry.GetRA3Path());
Console.WriteLine("Registry Status: " + RA3.Tools.Registry.Status);
Console.ReadKey();