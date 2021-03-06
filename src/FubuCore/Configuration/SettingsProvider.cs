using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore.Binding;

namespace FubuCore.Configuration
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly IObjectResolver _resolver;
        private readonly IEnumerable<ISettingsSource> _sources;

        private SettingsProvider(IObjectResolver resolver, IEnumerable<SettingsData> settings)
            : this(resolver, new ISettingsSource[]{new SettingsSource(settings)})
        {
            
        }

        public SettingsProvider(IObjectResolver resolver, IEnumerable<ISettingsSource> sources)
        {
            _resolver = resolver;
            _sources = sources;
        }

        public T SettingsFor<T>() where T : class, new()
        {
            return (T) SettingsFor(typeof (T));
        }

        public object SettingFor(string key)
        {
            //TODO: REVIEW
            var sub = new SubstitutedRequestData(getSettingsData(), getSettingsData());
            return sub.Value(key);
        }

        public object SettingsFor(Type settingsType)
        {
            var prefixedData = createRequestData(settingsType);

            var result = _resolver.BindModel(settingsType, prefixedData);
            result.AssertNoProblems(settingsType);

            return result.Value;
        }

        protected virtual IRequestData createRequestData(Type settingsType)
        {
            var settingsData = getSettingsData();
            var prefixedData = new PrefixedRequestData(settingsData, settingsType.Name + ".");
            return new SubstitutedRequestData(prefixedData, settingsData);
        }

        protected SettingsRequestData getSettingsData()
        {
            return new SettingsRequestData(_sources.SelectMany(x => x.FindSettingData()));
        }

        public IEnumerable<SettingDataSource> CreateDiagnosticReport()
        {
            return getSettingsData().CreateDiagnosticReport();
        }

        public IEnumerable<SettingDataSource> CreateResolvedDiagnosticReport()
        {
            var settingsData = getSettingsData();
            
            return settingsData.CreateDiagnosticReport().Select(s => new SettingDataSource()
                                                                     {
                                                                         Key = s.Key,
                                                                         Value = TemplateParser.Parse(s.Value, settingsData),
                                                                         Provenance =  s.Provenance
                                                                     });
        }

        public static SettingsProvider For(params SettingsData[] data)
        {
            return new SettingsProvider(ObjectResolver.Basic(), data);
        }
    }

}