using Examine;
using Umbraco.Core.Logging;
using Umbraco.Examine;

namespace Novicell.Examine.ElasticSearch.Indexers
{
    public class ContentElasticSearchIndex : ElasticSearchUmbracoIndex, IUmbracoIndex
    {
        public ContentElasticSearchIndex(string name, ElasticSearchConfig connectionConfiguration, IProfilingLogger profilingLogger,  
            FieldDefinitionCollection fieldDefinitions = null, string analyzer = null,
            IValueSetValidator validator = null) : base(name, connectionConfiguration,
            profilingLogger, fieldDefinitions, analyzer, validator)
        {
        }
    }
}