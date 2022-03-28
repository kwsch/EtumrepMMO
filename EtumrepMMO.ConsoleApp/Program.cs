using EtumrepMMO.Lib;

const string entityFolderName = "mons";
var result = GroupSeedFinder.FindSeeds(entityFolderName).FirstOrDefault();
if (result is default(ulong))
{
    Console.WriteLine("No group seeds found with the input data. Double check your inputs.");
}
else
{
    Console.WriteLine("Found seed!");
    Console.WriteLine(result);
}

Console.WriteLine();
Console.WriteLine("Press [ENTER] to exit.");
Console.ReadLine();
