using RA3.Tools;
using Con = System.Console;


var ra3 = new RA3Instance();

Con.WriteLine("[Profiles]");
foreach (var i in ra3.Profiles)
{
    Con.WriteLine(i);
}
Con.WriteLine();

Con.WriteLine("[Current Profile]");
Con.WriteLine(ra3.GetCurrentProfile());
Con.WriteLine();

Con.WriteLine("[RA3 Path] " + RA3.Tools.Registry.GetRA3Path());
Con.WriteLine("[Registry Status] " + RA3.Tools.Registry.Status);


Con.ReadKey();