using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Utils;
using Newtonsoft.Json;

namespace ASP_Statistics.Services
{
    public class DataService : IDataService
    {
        private static List<ForecastJson> _results = GetForecastsFromFileAsync(ResultsFile).Result;

        private static List<ForecastJson> _forecasts = GetForecastsFromFileAsync(ForecastsFile).Result;

        private static List<StateJson> _states = GetStatesAsync().Result;

        private const string ResultsFile = "forecast_results.json";
        private const string ForecastsFile = "forecasts.json";
        private const string StatesFile = "states.json";

        public static string ContentRootPath { get; set; }

        public List<ForecastJson> GetResults()
        {
            return _results;
        }

        public List<ForecastJson> GetForecasts()
        {
            return _forecasts;
        }

        public List<StateJson> GetStates()
        {
            return _states;
        }

        public StateJson GetStateByForecastId(long forecastId)
        {
            return _states.FirstOrDefault(x => x.ForecastId == forecastId);
        }

        public StateJson GetLastState()
        {
            return _states.FirstOrDefault();
        }

        public ForecastJson GetLastCalculatedForecastByIndex(int index)
        {
            return _forecasts
                .GroupBy(x => x.ThreadNumber)
                .ElementAtOrDefault(index)
                ?.FirstOrDefault(x => x.GameResultType != GameResultType.Expectation);
        }

        public async Task SaveResultsAsync(List<ForecastJson> forecasts,
            SaveMethod saveMethod = SaveMethod.Prepend)
        {
            await SaveForecastsIntoFileAsync(ResultsFile, forecasts, saveMethod);
        }

        public async Task SaveForecastsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Prepend)
        {
            await SaveForecastsIntoFileAsync(ForecastsFile, forecasts, saveMethod);
        }

        private static async Task SaveForecastsIntoFileAsync(string fileName, List<ForecastJson> forecasts,
            SaveMethod saveMethod)
        {
            List<ForecastJson> forecastsForSave = new List<ForecastJson>();

            bool needRewrite = saveMethod == SaveMethod.Rewrite;

            if (fileName == ResultsFile)
            {
                _results = GetForecastsForSave(needRewrite ? null : _results, forecasts, saveMethod);
                forecastsForSave = _results;
            }
            else if (fileName == ForecastsFile)
            {
                _forecasts = GetForecastsForSave(needRewrite ? null : _results, forecasts, saveMethod);
                forecastsForSave = _forecasts;
            }

            string content = JsonConvert.SerializeObject(forecastsForSave);

            await File.WriteAllTextAsync(GetFilePath(fileName), content);
        }

        private static List<ForecastJson> GetForecastsForSave(List<ForecastJson> existingData, List<ForecastJson> forecasts, SaveMethod saveMethod)
        {
            if (existingData == null)
                existingData = new List<ForecastJson>();

            IEnumerable<ForecastJson> query;

            if (saveMethod == SaveMethod.Append)
                query = existingData.Concat(forecasts);
            else
                query = forecasts.Concat(existingData);

            forecasts = query
                .Distinct(new ForecastJsonEqualityComparer())
                .ToList();

            return forecasts;
        }

        private static async Task<List<StateJson>> GetStatesAsync()
        {
            string fileContent = await GetFileContentAsync(StatesFile);

            if (string.IsNullOrEmpty(fileContent)) 
                return new List<StateJson>();

            List<StateJson> states = JsonConvert.DeserializeObject<List<StateJson>>(fileContent)
                .OrderByDescending(x => x.Id)
                .ToList();

            return states;
        }

        private static async Task<List<ForecastJson>> GetForecastsFromFileAsync(string fileName)
        {
            string fileContent = await GetFileContentAsync(fileName);

            if (string.IsNullOrEmpty(fileContent)) 
                return new List<ForecastJson>();

            List<ForecastJson> forecasts = JsonConvert.DeserializeObject<List<ForecastJson>>(fileContent)
                .Distinct(new ForecastJsonEqualityComparer())
                .OrderByDescending(x => x.Id)
                .ToList();

            return forecasts;
        }

        private static async Task<string> GetFileContentAsync(string fileName)
        {
            string filePath = GetFilePath(fileName);

            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllTextAsync(filePath);
        }

        private static string GetFilePath(string fileName)
        {
            return Path.Combine(ContentRootPath, "files", fileName);
        }
    }
}