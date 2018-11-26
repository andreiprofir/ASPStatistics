using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Classes;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using Microsoft.AspNetCore.Mvc;
using ASP_Statistics.Models;
using ASP_Statistics.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ASP_Statistics.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDataOldService _dataOldService;
        private readonly ISynchronizationService _synchronizationService;
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;
        private readonly IAlgorithmService _algorithmService;
        private readonly IChartService _chartService;

        public HomeController(IDataOldService dataOldService,  
            ISynchronizationService synchronizationService, 
            IMapper mapper, 
            IDataService dataService,
            IAlgorithmService algorithmService, 
            IChartService chartService)
        {
            _dataOldService = dataOldService;
            _synchronizationService = synchronizationService;
            _mapper = mapper;
            _dataService = dataService;
            _algorithmService = algorithmService;
            _chartService = chartService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<ForecastJson> forecasts = _dataService.GetForecasts();

            List<ForecastViewModel> model = _mapper.Map<List<ForecastJson>, List<ForecastViewModel>>(forecasts);

            await InitializeBetValues(model);

            return View(model);
        }

        [HttpGet]
        public IActionResult GetSettingsAndInfoPartial()
        {
            var model = new SettingsAndInfoViewModel();

            StateJson lastState = _dataService.GetLastState();
            SettingsJson settings = _dataService.GetSettings();

            model.Settings = _mapper.Map<SettingsJson, SettingsViewModel>(settings);
            model.LastState = _mapper.Map<StateJson, StateViewModel>(lastState);
            model.LastState.ThreadNumbers = settings.ThreadNumbers;

            InitializeBetAndBankValueLimits(model);

            return PartialView("_SettingsAndInfoPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> CalculateChainBetsAsync(double coefficient, decimal bet)
        {
            List<decimal> model = await _algorithmService.GetDefeatChainBets(bet, coefficient);

            return PartialView("_ChainBetsPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBetsAsync(List<ForecastViewModel> model)
        {
            ResetBetValues(model);

            List<ForecastJson> forecasts = _mapper.Map<List<ForecastViewModel>, List<ForecastJson>>(model);

            await _dataService.UpdateForecastsAsync(forecasts);

            await SaveForecastBetsAsync(model);

            return Json("Ok");
        }

        [HttpPost]
        public async Task<IActionResult> SaveCurrentStateAsync(StateViewModel model)
        {
            StateJson state = _mapper.Map<StateViewModel, StateJson>(model);

            await _dataService.SaveStateAsync(state);
            
            return Json("Ok");
        }

        [HttpPost]
        public async Task<IActionResult> SyncResults(bool rewriteAllData = false)
        {
            await _synchronizationService.SynchronizeResultsAsync(rewriteAllData);

            return Json("Ok");
        }

        [HttpPost]
        public async Task<IActionResult> SyncForecasts()
        {
            await _synchronizationService.SynchronizeForecastsAsync();

            return Json("Ok");
        }

        [HttpGet]
        public IActionResult Statistics()
        {
            InitializeViewBags();

            SettingsJson settings = _dataService.GetSettings();

            var model = new StatisticsViewModel
            {
                ThreadNumbers = settings.ThreadNumbers,
                LowerBound = settings.LowerBound,
                UpperBound = settings.UpperBound
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> StatisticsAsync(StatisticsViewModel model)
        {
            FilterParameters filterParameters = _mapper.Map<StatisticsViewModel, FilterParameters>(model);
            
            List<ForecastJson> forecasts = _dataService.GetResults(filterParameters);
            
            var resultModel = new StatisticsResultViewModel
            {
                ForecastResults = forecasts,
                GeneralChartsData = await GetGeneralChartsDataAsync(filterParameters, model.ExcludeRefundResults, model.ThreadNumbers)
            };

            return PartialView("_StatisticsPartial", resultModel);
        }

        private async Task<Dictionary<ChartType, ChartViewModel>> GetGeneralChartsDataAsync(
            FilterParameters filterParameters, bool excludeRefundResults, int threadNumbers)
        {
            filterParameters.GameResultType = null;

            if (excludeRefundResults)
                filterParameters.ExcludedGameResultType = GameResultType.RefundOrCancellation;

            Dictionary<ChartType, ChartViewModel> generalChartsData =
                await _chartService.GetGeneralChartsAsync(_dataService.GetResults(filterParameters), threadNumbers);

            return generalChartsData;
        }

        [HttpPost]
        public IActionResult GetStrategyCharts(StatisticsViewModel model)
        {
            List<ForecastJson> data = _dataOldService.Filter(new StatisticsViewModel {ForecastType = ForecastType.Paid})
                .Where(x => x.Coefficient >= 3)
                //.OrderByDescending(x => x.Coefficient)
                .ToList();


            model.GameResultType = null;

            List<ForecastJson> filteredForecasts = _dataOldService.Filter(model).Where(x => x.GameAt.Month >= 4).ToList();
            //List<ForecastJson> filteredForecasts = _dataService.Filter(null).Where(x => x.GameAt.Year == 2018 && x.GameAt.Month + 5 >= DateTimeOffset.Now.Month).ToList();

            decimal bank = 200;

            decimal bet = _dataOldService.CalculateNextBetValue(325);
            decimal calculatedBank = _dataOldService.CalculateMaxBankValue(4);
            decimal calculatedBank2 = _dataOldService.CalculateMaxBankValuePursuit(1);

            var a = Helper.GetBetChain(4, 2.1M);

            if (bet <= 0)
                throw new Exception("Bankul este prea mic");

            var responseModel = _dataOldService.GetStrategyChartData(filteredForecasts, 1, 300);

            return PartialView("_StrategyChartsPartial", responseModel);
        }

        [HttpPost]
        public async Task<IActionResult> CalculateBankValueAsync(SettingsViewModel model)
        {
            CalculateBankValuesOptions options = _mapper.Map<SettingsViewModel, CalculateBankValuesOptions>(model);

            decimal bankValue = await _algorithmService.GetBankValueByBetAndMethodAsync(options,
                model.CalculationMethod, model.LowerBound, model.UpperBound);

            return Json(bankValue);
        }

        [HttpPost]
        public async Task<IActionResult> CalculateBetValueAsync(SettingsViewModel model)
        {
            CalculateBetValueOptions options = _mapper.Map<SettingsViewModel, CalculateBetValueOptions>(model);
            options.InitialBet = 0;

            decimal betValue = await _algorithmService.CalculateBetValueByBankAsync(options);

            return Json(betValue);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettingsAsync(SettingsViewModel model)
        {
            SettingsJson settings = _mapper.Map<SettingsViewModel, SettingsJson>(model);

            await _dataService.SaveSettingsAsync(settings);

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> GetBankValuesByYearsAsync(decimal bet, double coefficientBankReserve, 
            int threadNumbers, DateTimeOffset? lowerBound, DateTimeOffset? upperBound)
        {
            var options = new CalculateBankValuesOptions
            {
                ThreadNumbers = threadNumbers,
                CoefficientBankReserve = coefficientBankReserve,
                Bet = bet
            };

            Dictionary<int, Dictionary<Month, decimal>> model = 
                await _algorithmService.GetCalculatedBankValuesByBetAsync(options, lowerBound, upperBound);

            return PartialView("_BankValuesByYearsPartial", model);
        }

        private void ResetBetValues(List<ForecastViewModel> model)
        {
            foreach (ForecastViewModel forecast in model)
            {
                if (forecast.GameResultType == GameResultType.Expectation && !forecast.SaveBet)
                    forecast.BetValue = 0;
            }
        }

        private async Task InitializeBetValues(List<ForecastViewModel> model)
        {
            SettingsJson settings = _dataService.GetSettings();

            for (int i = 0; i < settings.ThreadNumbers; i++)
            {
                ForecastViewModel currentForecast = model.LastOrDefault(x =>
                    x.ThreadNumber == i && x.GameResultType == GameResultType.Expectation);

                if (currentForecast == null || currentForecast.BetValue > 0) continue;

                StateJson state = await _algorithmService.CalculateNextStateAsync(currentForecast.Id,
                    allowIncreaseBet: settings.AllowIncreaseBetValue);

                currentForecast.BetValue = state.Bets[i];
            }
        }

        private async Task SaveForecastBetsAsync(List<ForecastViewModel> forecasts)
        {
            IEnumerable<ForecastViewModel> query = forecasts
                .Where(x => x.SaveBet && x.GameResultType == GameResultType.Expectation && x.BetValue > 0)
                .Reverse();

            foreach (var forecast in query)
            {
                StateJson state = await _algorithmService.CalculateNextStateAsync(forecast.Id, forecast.BetValue);

                await _dataService.SaveStateAsync(state);
            }
        }

        private void InitializeViewBags()
        {
            ViewBag.Years = _dataOldService.Forecasts
                .Select(x => x.GameAt.Year)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new SelectListItem(x.ToString(), x.ToString()));
        }

        private void InitializeBetAndBankValueLimits(SettingsAndInfoViewModel model)
        {
            List<StateJson> stages = _dataService.GetStates();

            var betValueLimits = new Dictionary<RepresentsValueType, decimal>
            {
                [RepresentsValueType.Min] = stages.Any() ? stages.Min(x => x.InitialBet) : 0,
                [RepresentsValueType.Avg] = stages.Any() ? Math.Round(stages.Average(x => x.InitialBet), 2) : 0,
                [RepresentsValueType.Max] = stages.Any() ? stages.Max(x => x.InitialBet) : 0
            };

            var bankValueLimits = new Dictionary<RepresentsValueType, decimal>
            {
                [RepresentsValueType.Min] = stages.Any() ? stages.Min(x => x.Bank) : 0,
                [RepresentsValueType.Avg] = stages.Any() ? Math.Round(stages.Average(x => x.Bank), 2) : 0,
                [RepresentsValueType.Max] = stages.Any() ? stages.Max(x => x.Bank) : 0
            };

            model.LastState.BetValueLimits = betValueLimits;
            model.LastState.BankValueLimits = bankValueLimits;
        }
    }
}
