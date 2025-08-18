using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the distribution of elements (items, enemies or other distributables)
/// 
/// </summary>
public class Distributor<T> where T : ItemInstance, new()
{
    /// <summary>
    /// elements which should be distributed
    /// </summary>
    private List<T> _randomElements;
    private List<T> _mustElements;
    /// <summary>
    /// probabilities of elements normalized if the cumulated probablity of _randomElements != 100
    /// is calculated when object will be instantiated
    /// </summary>
    private List<float> _normalizedProbabilities;

    /// <summary>
    /// Initializes a new instance of the Distributor class, sets the _randomElements
    /// and calculates the normalized probabilities for each element in the same order as the randomElements appear in _randomElements
    /// </summary>
    /// <param name="randomElements">List of randomElements which should be distributed.</param>
    public Distributor(List<T> randomElements){
        if (!randomElements.Any())
        {
            return;
        }
        float cumulativeProbability = 0.0f;
        
        foreach (T element in randomElements)
        {
            cumulativeProbability += element.itemData.rarity;
        }
        
        float elementProbability = 0.0f;
        foreach (T element in randomElements)
        {
            float normalizedProbability = element.itemData.rarity / cumulativeProbability;
            if (elementProbability == 0.0f)
            {
                elementProbability += normalizedProbability;
                _normalizedProbabilities = new List<float>{elementProbability};
                continue;
            }
            elementProbability += normalizedProbability;
            _normalizedProbabilities.Add(elementProbability);
        }
        _randomElements = randomElements;
    }

    public Distributor(List<T> randomElements, List<T> mustElements) : this(randomElements)
    {
        _mustElements = mustElements;
    }

    /// <summary>
    /// function for retrieving a random element out of the _randomElements
    /// first a random value is generated and then compared where this value is smaller or at least equal to a value
    /// and the index of this element where this suffices for the first time is the index of the element which is wanted
    /// bc the order of the normalized probabilities is the same as in _randomElements
    /// </summary>
    /// <returns>element out of _randomElements</returns>
    public T GetRandomElement()
    {
        if (_randomElements.Count == 0) return new T();
        float randomValue = Random.Range(0.0f, 1.0f);
        int elementIndex = _normalizedProbabilities.FindIndex(x => x >= randomValue);
        return _randomElements[elementIndex];
    }

    public T GetRandomElementIncludingMust()
    {
        if (_mustElements.Count <= 0) return GetRandomElement();
        var element = _mustElements.First();
        _mustElements.Remove(element);
        return element;
    }
}

