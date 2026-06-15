using UnityEngine;

namespace Game.Core
{
    public sealed class PlayerPrefsTokenStorage : ITokenStorage
    {
        public string Get(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
        public void Set(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}
