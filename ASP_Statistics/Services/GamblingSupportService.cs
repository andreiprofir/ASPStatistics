using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ASP_Statistics.JsonModels;
using Newtonsoft.Json;
using RestSharp;

namespace ASP_Statistics.Services
{
    public class GamblingSupportService : IGamblingSupportService
    {
        private const string UserEmail = "bet.andrei.profir@gmail.com";
        private const string UserPassword = "betmoney131294";

        private static readonly Uri BaseUri = new Uri("https://gamblingsupport.ru");
        private static readonly Uri LoginUri = new Uri("https://gamblingsupport.ru/cabinet/login");
        private static readonly Uri ResultUri = new Uri("https://gamblingsupport.ru/result");
        private static readonly Uri ForecastsUri = new Uri("https://gamblingsupport.ru/cabinet?page=1");
        private static readonly Uri ResultsUri = new Uri("https://gamblingsupport.ru/forecast/result?url_middleware=parsed&page=1");
        
        private static SessionToken _sessionToken;

        public async Task<List<ForecastJson>> GetForecastsAsync(int? numberOfPages = null)
        {
            RestClient client = await GetAuthenticatedClientAsync();

            return await GetForecastsAsync(client, ForecastsUri, numberOfPages);
        }

        public async Task<List<ForecastJson>> GetStatisticsAsync(int? numberOfPages = null)
        {
            RestClient client = await GetClientAsync();

            return await GetForecastsAsync(client, ResultsUri, numberOfPages);
        }

        private async Task<RestClient> GetClientAsync()
        {
            var client = new RestClient
            {
                CookieContainer = new CookieContainer(), 
                BaseUrl = BaseUri
            };

            _sessionToken = await GetSessionTokenAsync(client, ResultUri);

            return client;
        }

        private async Task<List<ForecastJson>> GetForecastsAsync(RestClient client, Uri firstPage, int? numberOfPages)
        {
            var forecasts = new List<ForecastJson>();

            PageJson page = await GetPageAsync(client, firstPage);

            forecasts.AddRange(page.Forecasts);

            while ((numberOfPages == null || numberOfPages.Value != page.CurrentPage) && page.NextPageUri != null)
            {
                page = await GetPageAsync(client, page.NextPageUri);

                forecasts.AddRange(page.Forecasts);
            }

            return forecasts;
        }

        private async Task<PageJson> GetPageAsync(RestClient client, Uri pageUri)
        {
            var request = new RestRequest(BaseUri.MakeRelativeUri(pageUri), Method.POST);

            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
            request.AddParameter("undefined", $"_token={_sessionToken.Token}&undefined=", ParameterType.RequestBody);
                
            IRestResponse response = await client.ExecuteTaskAsync(request);

            PageJson page = GetPageFromContent(response.Content);

            return page;
        }

        private PageJson GetPageFromContent(string content)
        {
            return JsonConvert.DeserializeObject<PageJson>(content);
        }

        private async Task<RestClient> GetAuthenticatedClientAsync()
        {
            var client = new RestClient
            {
                CookieContainer = new CookieContainer(), 
                BaseUrl = BaseUri
            };

            _sessionToken = await GetSessionTokenAsync(client, LoginUri);

            await InitAuthCookiesAsync(client);

            return client;
        }

        private async Task InitAuthCookiesAsync(RestClient client)
        {
            var request = new RestRequest("cabinet/auth", Method.POST);
            
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("undefined", $"_token={_sessionToken.Token}&email={UserEmail}&password={UserPassword}&undefined=", ParameterType.RequestBody);
            
            await client.ExecuteTaskAsync(request);
        }

        private async Task<SessionToken> GetSessionTokenAsync(RestClient client, Uri uri)
        {
            var request = new RestRequest(BaseUri.MakeRelativeUri(uri), Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");

            IRestResponse response = await client.ExecuteTaskAsync(request);

            string result = Regex.Match(response.Content, "(\\{(.)*csrfToken(.)*\\})").Value;

            var sessionToken = JsonConvert.DeserializeObject<SessionToken>(result);

            return sessionToken;
        }
    }
}