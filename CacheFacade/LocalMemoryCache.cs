using System;
using System.Collections.Generic;
using System.Text;
using ConfigurationAssistant;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CacheFacade
{
    public class LocalMemoryCache : CacheBase, IApplicationCache
    {
        protected readonly IMemoryCache Cache;

        public LocalMemoryCache(IApplicationSecrets applicationSecrets, IMemoryCache memoryCache) : base(applicationSecrets)
        {
            Cache = memoryCache;
        }

        #region public methods

        public bool KeyExists(string Key)
        {
            // In memory cache does not have a nice way to check if a key exists
            object value;
            return Cache.TryGetValue(Key, out value);
        }

        public void KeyDelete(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            Cache.Remove(key);
        }

        public void StringSet(string Value, string Key)
        {
            Cache.Set(Key, Value);
        }

        public string StringGet(string Key)
        {
            return Cache.Get<string>(Key);
        }

        public void CacheObject<T>(T cachedObject, string Key) where T : class
        {
            StringSet(JsonConvert.SerializeObject(cachedObject), Key);
        }
        public T GetCachedObject<T>(string Key) where T : class
        {
            string value = StringGet(Key);
            if(!string.IsNullOrEmpty(value))
                return JsonConvert.DeserializeObject<T>(value);
            return null;
        }
        #endregion
    }
}


