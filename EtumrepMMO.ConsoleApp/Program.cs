using EtumrepMMO.Lib;

const string entityFolderName = "mons";
var inputs = GroupSeedFinder.GetInputs(entityFolderName);
if (inputs.Count < 2)
{
    Console.WriteLine("Insufficient inputs found in folder. Needs to have two (2) or more dumped files.");

    Console.WriteLine();
    Console.WriteLine("Press [ENTER] to exit.");
    Console.ReadLine();
}

var result = GroupSeedFinder.FindSeed(inputs);
if (result is default(ulong))
{
    Console.WriteLine($"No group seeds found with the input data. Double check your inputs (valid inputs: {inputs.Count}).");
}
else
{
    Console.WriteLine("Found seed!");
    Console.WriteLine(result);
}

Console.WriteLine();
Console.WriteLine("Press [ENTER] to exit.");
Console.ReadLine();
