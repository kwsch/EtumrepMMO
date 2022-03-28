# EtumrepMMO
 
Reverses initial MMO data to find the origin (group) seed.

Requires [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0). The executable can be built with any compiler that supports C# 10.

Usage:
- Compile the ConsoleApp
- Put your 4 initial captures in a `mons` folder next to the executable.
- Run the executable, observe console output for matching seed.

Big thanks to Pokémon Automation (@Mysticial) for their [C++ implementation of Entity->Seed parallel brute-force](https://github.com/PokemonAutomation/Experimental/tree/4001b0402515ade042528d9bffb07ceab4476c96), which this repo uses (BSD license).
