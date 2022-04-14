using EtumrepMMO.Lib;

const string entityFolderName = "mons";
var inputs = GroupSeedFinder.GetInputs(entityFolderName);
if (inputs.Count < 2)
{
    Console.WriteLine("Insufficient inputs found in folder. Needs to have two (2) or more dumped files.");
}
else if (inputs.Count > 4)
{
    Console.WriteLine("Too many inputs found in folder. Needs to have only the first four (4) Pokémon.");
}
else
{
    var (seed, index) = GroupSeedFinder.FindSeed(inputs);
    if (index == -1)
    {
        Console.WriteLine($"No group seeds found with the input data. Double check your inputs (valid inputs: {inputs.Count}).");
    }
    else
    {
        Console.WriteLine($"Found seed from input {index + 1}/{inputs.Count}!");
        Console.WriteLine(seed);
    }
}

Console.WriteLine();
Console.WriteLine("Press [ENTER] to exit.");
Console.ReadLine();
