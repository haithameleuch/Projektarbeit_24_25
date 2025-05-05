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
    private List<T> _elements;
    /// <summary>
    /// probabilities of elements normalized if the cumulated probablity of _elements != 100
    /// is calculated when object will be instantiated
    /// </summary>
    private List<float> _normalizedProbabilities;

    /// <summary>
    /// Initializes a new instance of the Distributor class, sets the _elements
    /// and calculates the normalized probabilities for each element in the same order as the elements appear in _elements
    /// </summary>
    /// <param name="elements">List of elements which should be distributed.</param>
    public Distributor(List<T> elements){
        if (!elements.Any())
        {
            return;
        }
        float cumulativeProbability = 0.0f;
        
        foreach (T element in elements)
        {
            cumulativeProbability += element.itemData.spawnProbability;
        }
        
        float elementProbability = 0.0f;
        foreach (T element in elements)
        {
            float normalizedProbability = element.itemData.spawnProbability / cumulativeProbability;
            if (elementProbability == 0.0f)
            {
                elementProbability += normalizedProbability;
                _normalizedProbabilities = new List<float>{elementProbability};
                continue;
            }
            elementProbability += normalizedProbability;
            _normalizedProbabilities.Add(elementProbability);
        }
        _elements = elements;
    }

    /// <summary>
    /// function for retrieving a random element out of the _elements
    /// first a random value is generated and then compared where this value is smaller or at least equal to a value
    /// and the index of this element where this suffices for the first time is the index of the element which is wanted
    /// bc the order of the normalized probabilities is the same as in _elements
    /// </summary>
    /// <returns>element out of _elements</returns>
    public T GetRandomElement()
    {
        if (_elements.Count == 0) return new T();
        float randomValue = Random.Range(0.0f, 1.0f);
        int elementIndex = _normalizedProbabilities.FindIndex(x => x >= randomValue);
        return _elements[elementIndex];
    }
}

