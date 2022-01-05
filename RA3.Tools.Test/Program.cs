using RA3.Tools;

var ra3 = new RA3Instance();
foreach (var i in ra3.Profiles)
{
    Console.WriteLine(i);
}
Console.WriteLine(ra3.GetCurrentProfile());
