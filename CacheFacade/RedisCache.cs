using System;
using System.Collections.Generic;
using System.Text;
using ConfigurationAssistant;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CacheFacade
{

    public class RedisCache: CacheBase, IApplicationCache
    {
        public RedisCache(IApplicationSecrets applicationSecrets): base(applicationSecrets)
        {
        }

#region private methods
        private static IDatabase Cache
        {
            get
            {
                return Connection.GetDatabase();
            }
        }

        private static ConnectionMultiplexer Connection
        {
            get
            {
                return LazyConnection.Value;
            }
        }
        private static readonly Lazy<ConnectionMultiplexer> LazyConnection
            = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(CacheConnectionString));
#endregion

#region public methods

        public bool KeyExists(string Key)
        {
            return Cache.KeyExists(Key);
        }

        public void KeyDelete(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            Cache.KeyDelete(key);
        }

        public void StringSet(string Value, string Key)
        {
            Cache.StringSet(Key, Value);
        }

        public string StringGet(string Key)
        {
            return Cache.StringGet(Key);
        }

        public void CacheObject<T>(T cachedObject, string Key) where T: class
        {
            StringSet(JsonConvert.SerializeObject(cachedObject), Key);
        }
        public T GetCachedObject<T>(string Key) where T : class
        {
            string value = StringGet(Key);
            if (!string.IsNullOrEmpty(value))
                return JsonConvert.DeserializeObject<T>(value);
            return null;
        }
        #endregion
    }
}
