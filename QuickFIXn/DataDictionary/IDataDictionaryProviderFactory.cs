namespace QuickFix.DataDictionary;

public interface IDataDictionaryProviderFactory
{
    DataDictionaryProvider CreateDataDictionaryProvider(SessionID sessionId, SettingsDictionary settings);
}