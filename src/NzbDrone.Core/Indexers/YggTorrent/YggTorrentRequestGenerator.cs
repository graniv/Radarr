using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.YggTorrent
{
    public class YggTorrentRequestGenerator : IIndexerRequestGenerator
    {
        public YggTorrentSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetSearchRequests());

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            IndexerPageableRequestChain pageableRequests = new IndexerPageableRequestChain();

            string searchTitle = $"{searchCriteria.Movie.Title} {searchCriteria.Movie.Year}".Replace(" ", "+");
            pageableRequests.Add(GetSearchRequests(searchTitle));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetSearchRequests(string searchParameters = null)
        {
            string url = $"{Settings.BaseUrl}/{Settings.SearchUrlFormat}{searchParameters}";

            if (string.IsNullOrWhiteSpace(searchParameters))
            {
                LoginRequest();
            }

            HttpRequest searchRequest = new HttpRequest(url, HttpAccept.Json);
            yield return new IndexerRequest(searchRequest);
        }

        private HttpRequest LoginRequest()
        {
            HttpRequest request = new HttpRequest(String.Format("{0}/user/login", Settings.BaseUrl), HttpAccept.Wilcards);
            request.Method = HttpMethod.POST;
            request.StoreResponseCookie = true;

            SetMultiFrom(request, new Dictionary<string, string> { { "id", Settings.User }, { "pass", Settings.Password } });

            HttpResponse testResponse = HttpClient.Execute(request);

            if (testResponse.StatusCode == HttpStatusCode.Unauthorized
                || testResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpException(testResponse);
            }

            return request;
        }

        private void SetMultiFrom(HttpRequest request, Dictionary<string, string> keys)
        {
            string boundaryString = string.Format("----WebKitFormBoundary{0}", Guid.NewGuid().ToString().Split('-')[0]);
            string contentType = "multipart/form-data; boundary=" + boundaryString;

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> fd in keys)
            {
                sb.Append($"--{boundaryString}\r\n");
                sb.Append($"Content-Disposition: form-data; name=\"{fd.Key}\"\r\n\r\n{fd.Value}\r\n");

            }
            sb.Append($"--{boundaryString}--");

            request.Headers.ContentType = contentType;
            request.SetContent(sb.ToString());
        }
    }
}
