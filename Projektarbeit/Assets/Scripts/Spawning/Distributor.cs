using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Distributor<T>
{
    private string[] _distributedElementNames;
    private List<SpawnableData<T>> _elements;
    private int _elementCount;
    private int _scalar = 1;

    Distributor(int elementCount, List<SpawnableData<T>> elements){
        if (!elements.Any())
        {
            return;
        }
        int lowestProbability = elements.First().spawnProbability;
        int cumulativeProbability = 0;
        
        foreach (SpawnableData<T> element in elements)
        {
            cumulativeProbability += element.spawnProbability;
            if (element.spawnProbability < lowestProbability)
                lowestProbability = element.spawnProbability;
        }
        
        if((elementCount > cumulativeProbability) && (cumulativeProbability != 0))
            _scalar = Mathf.Max(elementCount / cumulativeProbability, 1);

        int minElementCountForAppearance = 100 / lowestProbability;
        _elementCount = Mathf.Max(minElementCountForAppearance, Mathf.Min(elementCount, cumulativeProbability*_scalar));
        _elements = elements;
        _distributedElementNames = new string[elementCount];
    }

    private void Distribute()
    {
        foreach (SpawnableData<T> elem in _elements)
        {
            for (int i = elem.spawnProbability * _scalar; i > 0; i--)
            {
                _distributedElementNames[i] = elem.spawnableName;
            }
        }
    }

    private void RemoveDistributedElement(int index)
    {
        string[] elements = new string[_distributedElementNames.Length - 1];
        
        for (int i = 0; i < index; i++)
        {
            elements[i] = _distributedElementNames[i];
        }
        for (int i = index; i < _distributedElementNames.Length - 1; i++)
        {
            elements[i] = _distributedElementNames[i + 1];
        }

        _distributedElementNames = elements;
    }

    public SpawnableData<T> GetRandomElement()
    {
        if (_distributedElementNames.Length == 0) return new SpawnableData<T>();
        int randomIndex = Random.Range(0, _distributedElementNames.Length);
        SpawnableData<T> element = _elements.Find(x => x.spawnableName == _distributedElementNames[randomIndex]);
        RemoveDistributedElement(randomIndex);
        return element;
    }
}

