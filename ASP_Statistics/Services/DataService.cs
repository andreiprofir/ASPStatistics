using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;
using ASP_Statistics.Utils;
using Newtonsoft.Json;

namespace ASP_Statistics.Services
{
    public class DataService : IDataService
    {
        private static string _contentRootPath;
        
        private static List<ForecastJson> _results;
        private static List<ForecastJson> _forecasts;
        private static List<StateJson> _states;
        private static SettingsJson _settings;

        public static string ContentRootPath
        {
            get => _contentRootPath;
            set
            {
                _contentRootPath = value;
                _results = GetForecastsFromFileAsync(ResultsFile).Result;
                _forecasts = GetForecastsFromFileAsync(ForecastsFile).Result;
                _states = GetStatesAsync().Result;
                _settings = GetSettingsAsync().Result;
            }
        }

        private const string ResultsFile = "forecast_results.json";
        private const string ForecastsFile = "forecasts.json";
        private const string StatesFile = "states.json";
        private const string SettingsFile = "settings.json";

        public List<ForecastJson> GetResults(FilterParameters filterParameters = null, bool reverse = true)
        {
            IEnumerable<ForecastJson> query = _results.AsEnumerable();

            if (reverse)
                query = query.Reverse();

            if (filterParameters == null)
                return query.ToList();

            if (filterParameters.LowerBound != null)
                query = query.Where(x => x.GameAt >= filterParameters.LowerBound);
            else
                query = query.Where(x => x.GameAt.Year >= 2018 && x.GameAt.Month >= 4);

            if (filterParameters.UpperBound != null)
                query = query.Where(x => x.GameAt <= filterParameters.UpperBound);
            
            query = query.Where(x => x.ForecastType == filterParameters.ForecastType);

            if (filterParameters.GameResultType != null)
                query = query.Where(x => x.GameResultType == filterParameters.GameResultType);

            return query.ToList();
        }

        public List<ForecastJson> GetForecasts(bool reverse = true)
        {
            return reverse 
                ? _forecasts.AsEnumerable().Reverse().ToList() 
                : _forecasts;
        }

        public List<StateJson> GetStates()
        {
            return _states;
        }

        public StateJson GetLastState()
        {
            return _states.LastOrDefault();
        }

        public SettingsJson GetSettings()
        {
            return _settings.Copy();
        }

        public ForecastJson GetForecastBy(long forecastId)
        {
            return _forecasts.FirstOrDefault(x => x.Id == forecastId);
        }

        public ForecastJson GetLastCalculatedForecastByIndex(int index)
        {
            return _forecasts
                .GroupBy(x => x.ThreadNumber)
                .ElementAtOrDefault(index)
                ?.LastOrDefault(x => x.GameResultType != GameResultType.Expectation);
        }

        public async Task SaveResultsAsync(List<ForecastJson> forecasts,
            SaveMethod saveMethod = SaveMethod.Append)
        {
            await SaveForecastsIntoFileAsync(ResultsFile, forecasts, saveMethod);
        }

        public async Task SaveForecastsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Append)
        {
            await SaveForecastsIntoFileAsync(ForecastsFile, forecasts, saveMethod);
        }

        public async Task SaveSettingsAsync(SettingsJson settings)
        {
            _settings = settings.Copy();

            string content = JsonConvert.SerializeObject(settings);

            await File.WriteAllTextAsync(GetFilePath(SettingsFile), content);
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

            SetThreadNumbers(forecastsForSave);

            string content = JsonConvert.SerializeObject(forecastsForSave);

            await File.WriteAllTextAsync(GetFilePath(fileName), content);
        }

        private static void SetThreadNumbers(List<ForecastJson> forecasts)
        {
            var query = forecasts
                .GroupBy(x => new {x.GameAt.Year, x.GameAt.Month, x.GameAt.Day});

            var index = 0;

            foreach (var group in query)
            {
                foreach (ForecastJson forecast in group)
                {
                    forecast.ThreadNumber = index;

                    index += 1;

                    if (index > _settings.ThreadNumbers - 1)
                        index = 0;
                }
            }
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
                .OrderBy(x => x.Id)
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

        private static async Task<SettingsJson> GetSettingsAsync()
        {
            string fileContent = await GetFileContentAsync(SettingsFile);

            if (string.IsNullOrEmpty(fileContent))
                return new SettingsJson();

            return JsonConvert.DeserializeObject<SettingsJson>(fileContent);
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