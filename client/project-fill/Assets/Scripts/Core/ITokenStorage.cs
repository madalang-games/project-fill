namespace Game.Core
{
    public interface ITokenStorage
    {
        string Get(string key, string defaultValue = "");
        void Set(string key, string value);
        void Delete(string key);
        void Save();
    }
}
