using System.Collections.Generic;
using System.Linq;
using Inventory;
using UnityEngine;

namespace Spawning
{
    /// <summary>
    /// Handles the distribution of elements (items, enemies or other distributables)
    /// and returns & stores the elements which should be spawned
    /// </summary>
    public class Distributor<T> where T : ItemInstance, new()
    {
        /// <summary>
        /// elements, which should be distributed
        /// </summary>
        private List<T> _randomElements;
        /// <summary>
        /// elements, which MUST be spawned. Will be reduced to 0 after all elements have been spawned
        /// </summary>
        private List<T> _mustElements;
        /// <summary>
        /// probabilities of elements normalized if the cumulated probability of _randomElements != 100
        /// is calculated when an object will be instantiated
        /// </summary>
        private List<float> _normalizedProbabilities;
    
        /// <summary>
        /// number of _mustElements which are necessary to be spawned
        /// </summary>
        public int MustCount => _mustElements?.Count ?? 0;

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
            var cumulativeProbability = 0.0f;
        
            foreach (T element in randomElements)
            {
                cumulativeProbability += element.itemData.rarity;
            }
        
            var elementProbability = 0.0f;
            foreach (var element in randomElements)
            {
                var normalizedProbability = element.itemData.rarity / cumulativeProbability;
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
    
        /// <summary>
        /// Initializes a new instance of the Distributor class, sets the _randomElements & _mustElements
        /// and calculates the normalized probabilities for each element in the same order as the randomElements appear in _randomElements
        /// </summary>
        /// <param name="randomElements">List of randomElements which should be distributed.</param>
        /// <param name="mustElements">list of elements that are necessary to be spawned</param>
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
            var randomValue = Random.Range(0.0f, 1.0f);
            var elementIndex = _normalizedProbabilities.FindIndex(x => x >= randomValue);
            return _randomElements[elementIndex];
        }
    
        /// <summary>
        /// function for retrieving an element out the elements that are available
        /// first it will be checked if there is an element in the _mustElements list remaining and if all are used then
        /// GetRandomElement is used to get a random element
        /// </summary>
        /// <returns>element out of _randomElements or _mustElements</returns>
        public T GetRandomElementIncludingMust()
        {
            if (_mustElements.Count <= 0) return GetRandomElement();
            var element = _mustElements.First();
            _mustElements.Remove(element);
            return element;
        }
    }
}

