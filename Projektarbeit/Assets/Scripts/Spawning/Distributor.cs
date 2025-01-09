using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Distributor<T>
{
    //@TODO: change from spwanableData to item-id or -name
    private SpawnableData<T>[] _distributedElements;
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
        _elementCount = Mathf.Max(minElementCountForAppearance, elementCount, cumulativeProbability*_scalar);
        _elements = elements;
        _distributedElements = new SpawnableData<T>[_elementCount];
    }

    private void Distribute()
    {
        foreach (SpawnableData<T> elem in _elements)
        {
            for (int i = elem.spawnProbability * _scalar; i > 0; i--)
            {
                _distributedElements[i] = elem;
            }
        }
    }

    private void RemoveDistributedElement(int index)
    {
        SpawnableData<T>[] elements = new SpawnableData<T>[_distributedElements.Length - 1];
        
        for (int i = 0; i < index; i++)
        {
            elements[i] = _distributedElements[i];
        }
        for (int i = index; i < _distributedElements.Length - 1; i++)
        {
            elements[i] = _distributedElements[i + 1];
        }

        _distributedElements = elements;
    }

    public SpawnableData<T> GetRandomElement()
    {
        if (_distributedElements.Length == 0) return new SpawnableData<T>();
        int randomIndex = Random.Range(0, _distributedElements.Length);
        SpawnableData<T> element = _distributedElements[randomIndex];
        RemoveDistributedElement(randomIndex);
        return element;
    }
}

