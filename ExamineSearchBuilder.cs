using Examine;
using Examine.LuceneEngine.SearchCriteria;
using Examine.SearchCriteria;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace TestingUmbraco.Helpers
{
    public class ExamineSearchBuilder
    {
        private ISearchCriteria _searchCriteria { get; set; }
        private IBooleanOperation _queryNodes { get; set; }
        private UmbracoHelper _uHelper { get; set; }
        private List<string> _NotProperties { get; set; }
        private List<string> _NotFields { get; set; }
        private List<string> _searchFields { get; set; }
        private List<string> _nodeTypeAlias { get; set; }
        private string _docTypeAliasFieldName { get { return "nodeTypeAlias"; } }
        private StringBuilder _wildCardQuery;
        private BooleanOperation _enumBooleanOperation;

        private string _searchTerm { get; set; }

        public ExamineSearchBuilder(UmbracoHelper uHelper, string[] nodeTypeAlias, string SearchProviderCollection, int enumBooleanOperation)
        {
            _searchCriteria = ExamineManager.Instance.SearchProviderCollection[SearchProviderCollection].CreateSearchCriteria((BooleanOperation)enumBooleanOperation);
            _queryNodes = null;
            _uHelper = uHelper;
            _NotProperties = new List<string>();
            _NotFields = new List<string>();
            _nodeTypeAlias = nodeTypeAlias.ToList();
            _queryNodes = _searchCriteria.GroupedOr(new string[] { _docTypeAliasFieldName }, _nodeTypeAlias.ToArray());
            _enumBooleanOperation = (BooleanOperation)enumBooleanOperation;
            _wildCardQuery = new StringBuilder();
            _searchTerm = "";
            _searchFields = new List<string>();
            _nodeTypeAlias = new List<string>();
        }
        public ExamineSearchBuilder(UmbracoHelper uHelper, string[] nodeTypeAlias, int enumBooleanOperation)
        {
            _searchCriteria = ExamineManager.Instance.CreateSearchCriteria((BooleanOperation)enumBooleanOperation);
            _queryNodes = null;
            _uHelper = uHelper;
            _NotProperties = new List<string>();
            _NotFields = new List<string>();
            _nodeTypeAlias = nodeTypeAlias.ToList();
            _queryNodes = _searchCriteria.GroupedOr(new string[] { _docTypeAliasFieldName }, _nodeTypeAlias.ToArray());
            _wildCardQuery = new StringBuilder();
            _enumBooleanOperation = (BooleanOperation)enumBooleanOperation;
            _searchTerm = "";
            _searchFields = new List<string>();
            _nodeTypeAlias = new List<string>();
        }
        public ExamineSearchBuilder(UmbracoHelper uHelper, string[] nodeTypeAlias)
        {
            _searchCriteria = ExamineManager.Instance.CreateSearchCriteria(BooleanOperation.And);
            _queryNodes = null;
            _uHelper = uHelper;
            _NotProperties = new List<string>();
            _NotFields = new List<string>();
            _nodeTypeAlias = nodeTypeAlias.ToList();
            _queryNodes = _searchCriteria.GroupedOr(new string[] { _docTypeAliasFieldName }, _nodeTypeAlias.ToArray());
            _wildCardQuery = new StringBuilder();
            _enumBooleanOperation = (BooleanOperation.And);
            _searchTerm = "";
            _searchFields = new List<string>();
            _nodeTypeAlias = new List<string>();
        }

        public ExamineSearchBuilder NOTGroup(IEnumerable<string> NotProperties, IEnumerable<string> Fields)
        {
            this._NotProperties.AddRange(NotProperties);
            this._NotFields.AddRange(Fields);
            this._queryNodes = _queryNodes.And().GroupedNot(_NotProperties, _NotFields.ToArray());
            return this;
        }

        public ExamineSearchBuilder SearchFields(List<string> Fields, string SearchTerm)
        {
            this._searchFields.AddRange(Fields);

            this._searchTerm += SearchTerm;
            foreach (var term in _searchTerm.Split(' '))
            {
                _queryNodes = _queryNodes.And().GroupedOr(_searchFields, term.Fuzzy(0.7f).Value.MultipleCharacterWildcard());
            }
            return this;
        }

        public ExamineSearchBuilder SearchContentPicker(string Category, List<string> CategoryValues, int enumBooleanOperation = 0)
        {
            BooleanOperation CategoryBooleanOperation = (BooleanOperation)enumBooleanOperation;

            if (!String.IsNullOrWhiteSpace(_wildCardQuery.ToString()))
            {
                _wildCardQuery.Append(CategoryBooleanOperation.ToString().ToUpper());
            }

            _wildCardQuery.Append(Category + ":(");
            foreach (var value in CategoryValues.Distinct())
            {
                _wildCardQuery.Append("*" + value.Replace("-", "") + "*" + (CategoryValues.Distinct().Count()>1&& CategoryValues.Distinct().Last()!=value? CategoryBooleanOperation.ToString().ToUpper() : ""));
            }
            _wildCardQuery.Append(")");
            _searchCriteria.RawQuery(_wildCardQuery.ToString());
            return this;
        }

        public ExamineSearchBuilder SetFildeWithRange(string Field, DateTime StartDate, DateTime EndDate, int enumBooleanOperation = 0)
        {
            var culture = new CultureInfo("en-US");
            var format = "yyyyMMdd";

            var Defualt_min = StartDate.ToString(format, culture);
            var Defualt_max = EndDate.ToString(format, culture);

            string _LuceneString = Field + ":[{{startTime}} TO {{endTime}}]";

            _LuceneString = _LuceneString.Replace("{{startTime}}", Defualt_min).Replace("{{endTime}}", Defualt_max);

            _searchCriteria.RawQuery(_LuceneString.ToString());

            return this;
        }
        public ExamineSearchBuilder Order(string[] Fields, string OrderType = "asc")
        {
            if (OrderType.ToLower() == "asc")
                _queryNodes = _queryNodes.And().OrderBy(Fields);
            else if (OrderType.ToLower() == "desc")
                _queryNodes = _queryNodes.And().OrderByDescending(Fields);
            return this;
        }

        public string TakeScreenShot()
        {
            return _queryNodes.Compile().ToString();
        }

        public IEnumerable<IPublishedContent> RunScreenShot(string LuceneString, int stratIndex, int PageSize)
        {

            var results = ExamineManager.Instance.Search(LuceneString, true);
            return _uHelper.TypedContent(results.Select(x => x.Id).Skip(stratIndex).Take(PageSize));
        }

        public IEnumerable<IPublishedContent> CompileAndExecute(int stratIndex, int PageSize)
        {
            var results = ExamineManager.Instance.Search(_queryNodes.Compile());
            return _uHelper.TypedContent(results.Select(x => x.Id).Skip(stratIndex).Take(PageSize));
        }

        public int Count()
        {
            var results = ExamineManager.Instance.Search(_queryNodes.Compile());
            return results.Count();
        }
    }
}
