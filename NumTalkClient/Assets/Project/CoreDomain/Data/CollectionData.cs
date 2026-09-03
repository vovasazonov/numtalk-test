using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Project.CoreDomain.Data
{
    public class CollectionData<TDataInterface, TDataImplementation> : ICollectionData<TDataInterface> where TDataImplementation : TDataInterface, new()
    {
        [JsonProperty("elements")] private List<TDataImplementation> _collection = new List<TDataImplementation>();

        [JsonIgnore] public IEnumerable<TDataInterface> Collection => _collection as IEnumerable<TDataInterface>;
        
        public bool Remove(TDataInterface data)
        {
            return _collection.Remove(data is TDataImplementation implementation ? implementation : default);
        }

        public void Add(TDataInterface data)
        {
            if (data is TDataImplementation implementation)
            {
                _collection.Add(implementation);
            }
            else
            {
                throw new InvalidCastException("Implementation is not implemented from current interface.");
            }
        }

        public TDataInterface Create()
        {
            return new TDataImplementation();
        }
    }
}