﻿using System;
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

        public HomeController(IDataOldService dataOldService,  
            ISynchronizationService synchronizationService, 
            IMapper mapper, 
            IDataService dataService,
            IAlgorithmService algorithmService)
        {
            _dataOldService = dataOldService;
            _synchronizationService = synchronizationService;
            _mapper = mapper;
            _dataService = dataService;
            _algorithmService = algorithmService;
        }

        public async Task<IActionResult> Index()
        {
            List<ForecastJson> forecasts = _dataService.GetForecasts();

            List<ForecastViewModel> model = _mapper.Map<List<ForecastJson>, List<ForecastViewModel>>(forecasts);

            //decimal bet = _dataOldService.CalculateNextBetValue(325);
            //decimal bet2 = await _algorithmService.CalculateBetValueByBankAsync(new CalculateBetValueOptions
            //{
            //    Bank = 300
            //});
            //var banks = await _algorithmService.GetBankValuesByBetAsync(new CalculateBankValuesOptions
            //{
            //    Bet = 2M
            //});

            //var bets = await _algorithmService.GetDefeatChainBets(5, 2.15);
            //decimal calculatedBank = _dataOldService.CalculateMaxBankValue(4);

            //var a = await _algorithmService.GetWinLoseCountByThreadNumber();
            //var b = a.Select(x => new { Key = x.Key, CountMax = x.Value.Max(y => y.Count), Res = x.Value.First(z => z.Count == x.Value.Max(f => f.Count)) }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetSettingsAndInfoPartial()
        {
            var model = new SettingsAndInfoViewModel();

            StateJson lastState = _dataService.GetLastState() ?? new StateJson();
            SettingsJson settings = _dataService.GetSettings();

            model.BankValue = lastState.Bank;
            model.BetValue = lastState.InitialBet;
            model.Bets = lastState.Bets;
            model.LoseValues = lastState.LoseValues;
            model.ThreadNumbers = settings.ThreadNumbers;
            model.Settings = _mapper.Map<SettingsJson, SettingsViewModel>(settings);
            
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
        public async Task<IActionResult> Index(List<ForecastViewModel> model)
        {
            //List<ForecastJson> forecasts = await _dataService.GetForecastsAsync();

            //List<ForecastViewModel> model = _mapper.Map<List<ForecastJson>, List<ForecastViewModel>>(forecasts);

            //return View(model);

            return null;
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

        public IActionResult Statistics()
        {
            InitializeViewBags();

            return View();
        }

        private void InitializeViewBags()
        {
            ViewBag.Years = _dataOldService.Forecasts
                .Select(x => x.GameAt.Year)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new SelectListItem(x.ToString(), x.ToString()));
        }

        [HttpPost]
        public IActionResult FilterForecasts(RequestViewModel model)
        {
            List<ForecastJson> filteredForecasts = _dataOldService.Filter(model);
            
            return PartialView("_ForecastsTablePartial", 
                filteredForecasts
                    //.OrderByDescending(x => x.ShowAt)
                    //.ThenBy(x => x.Id)
                    .ToList());
        }

        [HttpPost]
        public IActionResult GetCharts(RequestViewModel model)
        {
            GameResultType? gameResultType = model.GameResultType;
            model.GameResultType = null;
            List<ForecastJson> filteredForecasts = _dataOldService.Filter(model);
            
            var responseModel = _dataOldService.GetChartData(filteredForecasts, gameResultType, true);

            return PartialView("_ChartsPartial", responseModel);
        }

        [HttpPost]
        public IActionResult GetStrategyCharts(RequestViewModel model)
        {
            List<ForecastJson> data = _dataOldService.Filter(new RequestViewModel {ForecastType = ForecastType.Paid})
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

        private void InitializeBetAndBankValueLimits(SettingsAndInfoViewModel model)
        {
            List<StateJson> stages = _dataService.GetStates();

            var betValueLimits = new Dictionary<RepresentsValueType, decimal>
            {
                [RepresentsValueType.Min] = stages.Any() ? stages.Min(x => x.InitialBet) : 0,
                [RepresentsValueType.Avg] = stages.Any() ? stages.Average(x => x.InitialBet) : 0,
                [RepresentsValueType.Max] = stages.Any() ? stages.Max(x => x.InitialBet) : 0
            };

            var bankValueLimits = new Dictionary<RepresentsValueType, decimal>
            {
                [RepresentsValueType.Min] = stages.Any() ? stages.Min(x => x.Bank) : 0,
                [RepresentsValueType.Avg] = stages.Any() ? stages.Average(x => x.Bank) : 0,
                [RepresentsValueType.Max] = stages.Any() ? stages.Max(x => x.Bank) : 0
            };

            model.BetValueLimits = betValueLimits;
            model.BankValueLimits = bankValueLimits;
        }
    }
}
