using System;
using System.Collections.Generic;
using QuickFix.Util;

namespace QuickFix.DataDictionary;

public class DefaultDataDictionaryProviderFactory : IDataDictionaryProviderFactory
{
    protected readonly Dictionary<string, DataDictionary> _dictionariesByPath = new();

    public DataDictionaryProvider CreateDataDictionaryProvider(SessionID sessionId, SettingsDictionary settings)
    {
        bool useDataDictionary = true;
        if (settings.Has(SessionSettings.USE_DATA_DICTIONARY))
            useDataDictionary = settings.GetBool(SessionSettings.USE_DATA_DICTIONARY);

        var provider = new DataDictionaryProvider();
        if (useDataDictionary)
        {
            if (sessionId.IsFIXT)
                ProcessFixTDataDictionaries(sessionId, settings, provider);
            else
                ProcessFixDataDictionary(sessionId, settings, provider);
        }

        return provider;
    }

    protected virtual void ProcessFixTDataDictionaries(SessionID sessionId, SettingsDictionary settings, DataDictionaryProvider provider)
    {
        provider.AddTransportDataDictionary(sessionId.BeginString, CreateDataDictionary(sessionId, settings, SessionSettings.TRANSPORT_DATA_DICTIONARY, sessionId.BeginString));

        foreach (KeyValuePair<string, string> setting in settings)
        {
            if (setting.Key.StartsWith(SessionSettings.APP_DATA_DICTIONARY, System.StringComparison.CurrentCultureIgnoreCase))
            {
                if (setting.Key.Equals(SessionSettings.APP_DATA_DICTIONARY, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    Fields.ApplVerID applVerId = Message.GetApplVerID(settings.GetString(SessionSettings.DEFAULT_APPLVERID));
                    DataDictionary dd = CreateDataDictionary(sessionId, settings, SessionSettings.APP_DATA_DICTIONARY, sessionId.BeginString);
                    provider.AddApplicationDataDictionary(applVerId.Value, dd);
                }
                else
                {
                    int offset = setting.Key.IndexOf('.');
                    if (offset == -1)
                        throw new ArgumentException(
                            $"Malformed {SessionSettings.APP_DATA_DICTIONARY} : {setting.Key}");

                    string beginStringQualifier = setting.Key.Substring(offset);
                    DataDictionary dd = CreateDataDictionary(sessionId, settings, setting.Key, beginStringQualifier);
                    provider.AddApplicationDataDictionary(Message.GetApplVerID(beginStringQualifier).Value, dd);
                }
            }
        }
    }

    protected virtual void ProcessFixDataDictionary(SessionID sessionId, SettingsDictionary settings, DataDictionaryProvider provider)
    {
        var dataDictionary = CreateDataDictionary(sessionId, settings, SessionSettings.DATA_DICTIONARY, sessionId.BeginString);
        provider.AddTransportDataDictionary(sessionId.BeginString, dataDictionary);
        provider.AddApplicationDataDictionary(FixValues.ApplVerID.FromBeginString(sessionId.BeginString), dataDictionary);
    }


    protected virtual DataDictionary CreateDataDictionary(SessionID sessionId, SettingsDictionary settings, string settingsKey, string beginString)
    {
        var dd = ReadDataDictionary(sessionId, settings, settingsKey, beginString);

        var ddCopy = new DataDictionary(dd);

        if (settings.Has(SessionSettings.VALIDATE_FIELDS_OUT_OF_ORDER))
            ddCopy.CheckFieldsOutOfOrder = settings.GetBool(SessionSettings.VALIDATE_FIELDS_OUT_OF_ORDER);
        if (settings.Has(SessionSettings.VALIDATE_FIELDS_HAVE_VALUES))
            ddCopy.CheckFieldsHaveValues = settings.GetBool(SessionSettings.VALIDATE_FIELDS_HAVE_VALUES);
        if (settings.Has(SessionSettings.VALIDATE_USER_DEFINED_FIELDS))
            ddCopy.CheckUserDefinedFields = settings.GetBool(SessionSettings.VALIDATE_USER_DEFINED_FIELDS);
        if (settings.Has(SessionSettings.ALLOW_UNKNOWN_FIELD_VALUES))
            ddCopy.AllowUnknownFieldValues = settings.GetBool(SessionSettings.ALLOW_UNKNOWN_FIELD_VALUES);
        if (settings.Has(SessionSettings.ALLOW_UNKNOWN_MSG_FIELDS))
            ddCopy.AllowUnknownMessageFields = settings.GetBool(SessionSettings.ALLOW_UNKNOWN_MSG_FIELDS);

        return ddCopy;
    }

    protected virtual DataDictionary ReadDataDictionary(SessionID sessionId, SettingsDictionary settings, string settingsKey, string beginString)
    {
        string path;
        if (settings.Has(settingsKey))
            path = settings.GetString(settingsKey);
        else
            path = beginString.Replace(".", "") + ".xml";

        path = StringUtil.FixSlashes(path);

        if (!_dictionariesByPath.TryGetValue(path, out var dd))
        {
            dd = new DataDictionary(path);
            _dictionariesByPath[path] = dd;
        }

        return dd;
    }
}