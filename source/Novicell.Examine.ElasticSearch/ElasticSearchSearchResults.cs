using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Examine;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Helpers;
using Novicell.Examine.ElasticSearch.Model;
using SortField = Lucene.Net.Search.SortField;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchSearchResults : ISearchResults
    {
        private readonly int? _maxResults;
        private readonly ElasticClient _client;
        private readonly BooleanQuery _luceneQuery;
        private QueryContainer _queryContainer;
        private ISearchRequest _searchRequest;
        private Func<SearchDescriptor<Document>, ISearchRequest> _searchSelector;
        private SortDescriptor<Document> _sortDescriptor;
        public AggregateDictionary Aggregation;
        private readonly string _indexName;
        private IEnumerable<ISearchResult> results;
        private int lastskip = 0;
        public long TotalItemCount { get; private set; }


        public ElasticSearchSearchResults(ElasticClient client, BooleanQuery luceneQuery, string indexName, List<SortField> sortFields,
            int? maxResults = null, int? skip = null)
        {
            _luceneQuery = luceneQuery ?? throw new ArgumentNullException(nameof(luceneQuery));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
            _sortDescriptor = GetSortDescriptor(sortFields);

            results = DoSearch(skip).ConvertResult();
        }
        public ElasticSearchSearchResults(ElasticClient client, ISearchRequest searchRequest, string indexName, List<SortField> sortFields, int? maxResults = null, int? skip = null)
        {
            _searchRequest = searchRequest ?? throw new ArgumentNullException(nameof(searchRequest));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
            _sortDescriptor = GetSortDescriptor(sortFields);
            
            lastskip = skip ?? 0;
            results = DoSearch(skip).ConvertResult();
        }
        public ElasticSearchSearchResults(ElasticClient client, Func<SearchDescriptor<Document>, ISearchRequest> searchSelector, string indexName, List<SortField> sortFields, int? maxResults = null, int? skip = null)
        {
            _searchSelector = searchSelector ?? throw new ArgumentNullException(nameof(searchSelector));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
            _sortDescriptor = GetSortDescriptor(sortFields);
            
            lastskip = skip ?? 0;
            results = DoSearch(skip).ConvertResult();
        }
        public ElasticSearchSearchResults(ElasticClient client, QueryContainer queryContainer, string indexName, List<SortField> sortFields, int? maxResults = null, int? skip = null)
        {
            _queryContainer = queryContainer ?? throw new ArgumentNullException(nameof(queryContainer));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
            _sortDescriptor = GetSortDescriptor(sortFields);
            
            lastskip = skip ?? 0;
            results = DoSearch(skip).ConvertResult();
        }
     
        public IEnumerator<ISearchResult> GetEnumerator()
        {
       
            return results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

       

        private ISearchResponse<Document> DoSearch(int? skip)
        {
            lastskip = skip ?? 0;
            ISearchResponse<Document> searchResult;
          
            if (_luceneQuery != null)
            {
                _queryContainer = new QueryContainer(new QueryStringQuery()
                {
                    Query = _luceneQuery.ToString(),
                    AnalyzeWildcard = true
                    
                });
            }
            if(_queryContainer != null){
            SearchDescriptor<Document> searchDescriptor = new SearchDescriptor<Document>();
            searchDescriptor.Index(_indexName)
                .Skip(skip)
                .Size(_maxResults)
                .Query(q => _queryContainer)
                
                .Sort(s => _sortDescriptor);

            var json = _client.RequestResponseSerializer.SerializeToString(searchDescriptor);
            searchResult = _client.Search<Document>(searchDescriptor);
            }else if (_searchRequest != null)
            {
                searchResult = _client.Search<Document>(_searchRequest);
            }
            else
            {
                searchResult = _client.Search<Document>(_searchSelector);
            }

            TotalItemCount = searchResult.Total;
            Aggregation = searchResult.Aggregations;
            return searchResult;
        }



        [Obsolete("Method is redundant as now ElasticSearchSearchResults could get skip as additional paramater")]
        public IEnumerable<ISearchResult> Skip(int skip)
        {
            if (lastskip == skip)
                return results;
            results = DoSearch(skip).ConvertResult();
            return results;
        }

        public SortDescriptor<Document> GetSortDescriptor(List<SortField> fields)
        {
            SortDescriptor<Document> sortDescriptor = new SortDescriptor<Document>();

            foreach (var field in fields)
            {

                sortDescriptor.Field(e =>
                    e.Field(field.Field).UnmappedType(FieldType.Long).MissingLast()
                        .Order(field.Reverse ? SortOrder.Descending : SortOrder.Ascending));
            }
      
            return sortDescriptor;
        }
    }
}