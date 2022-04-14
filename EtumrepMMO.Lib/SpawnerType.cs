namespace EtumrepMMO.Lib;

/// <summary>
/// Options for validating a group seed with a set of entities.
/// </summary>
[Flags]
public enum SpawnerType
{
    /// <summary>
    /// Please never use this. The code will not return anything, but will still crunch data.
    /// </summary>
    None = 0,

    /// <summary>
    /// The set of data contains entities that all spawned at the same time.
    /// </summary>
    MultiSpawn = 1 << 0,

    /// <summary>
    /// The set of data contains single entities that spawned separately, one after another.
    /// </summary>
    SingleSpawn = 1 << 1,

    /// <summary>
    /// Validate with all modes (unspecific). First match will return, collisions impossible with sufficient data.
    /// </summary>
    All = MultiSpawn | SingleSpawn,
}
