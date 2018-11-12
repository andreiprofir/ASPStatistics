using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Utils;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace ASP_Statistics.Services
{
    public class DataService : IDataService
    {
        private const string ForecastResultsFile = "forecast_results.json";
        private const string ForecastsFile = "forecasts.json";
        private const string StatesFile = "states.json";

        private readonly IHostingEnvironment _hostingEnvironment;

        public DataService(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<List<ForecastJson>> GetForecastResultsAsync()
        {
            return await GetForecastsFromFileAsync(ForecastResultsFile);
        }

        public async Task SaveForecastResultsAsync(List<ForecastJson> forecasts,
            SaveMethod saveMethod = SaveMethod.Prepend)
        {
            await SaveForecastsIntoFile(ForecastResultsFile, forecasts, saveMethod);
        }

        public async Task SaveForecastsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Prepend)
        {
            await SaveForecastsIntoFile(ForecastsFile, forecasts, saveMethod);
        }

        private async Task SaveForecastsIntoFile(string fileName, List<ForecastJson> forecasts, SaveMethod saveMethod)
        {
            List<ForecastJson> forecastsForSave = await PrepareForecastsForSaveAsync(fileName, forecasts, saveMethod);

            string content = JsonConvert.SerializeObject(forecastsForSave);

            await File.WriteAllTextAsync(GetFilePath(fileName), content);
        }

        private async Task<List<ForecastJson>> PrepareForecastsForSaveAsync(string fileName,
            List<ForecastJson> forecasts, SaveMethod saveMethod)
        {
            List<ForecastJson> existingData;

            if (saveMethod == SaveMethod.Rewrite)
                existingData = new List<ForecastJson>();
            else
                existingData = await GetForecastsFromFileAsync(fileName);

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

        public async Task<List<ForecastJson>> GetForecastsAsync()
        {
            return await GetForecastsFromFileAsync(ForecastsFile);
        }

        public async Task<List<StateJson>> GetAlgorithmStatesAsync()
        {
            string fileContent = await GetFileContentAsync(StatesFile);

            if (string.IsNullOrEmpty(fileContent)) 
                return new List<StateJson>();

            List<StateJson> states = JsonConvert.DeserializeObject<List<StateJson>>(fileContent)
                .OrderByDescending(x => x.Id)
                .ToList();

            return states;
        }

        private async Task<List<ForecastJson>> GetForecastsFromFileAsync(string fileName)
        {
            string fileContent = await GetFileContentAsync(fileName);

            if (string.IsNullOrEmpty(fileContent)) 
                return new List<ForecastJson>();

            List<ForecastJson> forecasts = JsonConvert.DeserializeObject<List<ForecastJson>>(fileContent)
                .Distinct(new ForecastJsonEqualityComparer()).ToList();

            return forecasts;
        }

        private async Task<string> GetFileContentAsync(string fileName)
        {
            string filePath = GetFilePath(fileName);

            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllTextAsync(filePath);
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(_hostingEnvironment.ContentRootPath, "files", fileName);
        }
    }
}