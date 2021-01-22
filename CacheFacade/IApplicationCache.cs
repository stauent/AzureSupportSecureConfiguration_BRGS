namespace CacheFacade
{
    public interface IApplicationCache
    {
        bool KeyExists(string Key);
        void KeyDelete(string key);
        void StringSet(string Value, string Key);
        string StringGet(string Key);
        void CacheObject<T>(T cachedObject, string Key) where T: class;
        T GetCachedObject<T>(string Key) where T : class;
    }
}