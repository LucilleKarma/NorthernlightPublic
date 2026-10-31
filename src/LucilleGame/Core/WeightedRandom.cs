using Northernlight.Core.Utilities;

namespace Northernlight.Core;

/// <summary>
///     A simpler provider for choose elements at random in a weighted manner.
/// </summary>
public sealed class WeightedRandom<T>
{
    private bool needsRefresh = true;

    private float totalWeight;

    private readonly Random rng;

    private readonly List<Tuple<T, float>> elements = new List<Tuple<T, float>>();

    /// <summary>
    ///     Whether this random generator has no
    ///     entries or not.
    /// </summary>
    public bool Empty => elements.Count <= 0;

    public WeightedRandom() => rng = new Random();

    public WeightedRandom(int seed) => rng = new Random(seed);

    public WeightedRandom(Random random) => rng = random;

    /// <summary>
    ///     Adds a new item to this mechanism to choose from.
    /// </summary>
    public void Add(T element, float weight = 1f)
    {
        elements.Add(new Tuple<T, float>(element, weight));
        needsRefresh = true;
    }

    /// <summary>
    ///     Samples a random item from this mechanism.
    /// </summary>
    public T Get()
    {
        if (needsRefresh)
            RecalculateTotalWeight();

        float x = rng.NextFloat(totalWeight);
        foreach (Tuple<T, float> elementWeightPairing in elements)
        {
            if (x > elementWeightPairing.Item2)
            {
                x -= elementWeightPairing.Item2;
                continue;
            }
            return elementWeightPairing.Item1;
        }

        return default!;
    }

    private void RecalculateTotalWeight()
    {
        totalWeight = 0f;
        foreach (Tuple<T, float> element in elements)
            totalWeight += element.Item2;

        needsRefresh = false;
    }

    public T this[int index] => elements[index].Item1;
}
