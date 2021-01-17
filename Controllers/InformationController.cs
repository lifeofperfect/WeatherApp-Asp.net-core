﻿using DarkSky.Models;
using DarkSky.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SystemInfo.Controllers
{
    public class InformationController : Controller
    {
        public string PublicIP { get; set; } = "IP Lookup Failed";
        public double Long { get; set; }
        public double Latt { get; set; }
        public string City { get; set; }
        public string CurrentWeatherIcon { get; set; }
        public string WeatherAttribution { get; set; }
        public string CurrentTemp { get; set; } = "undetermined";
        public string DayWeatherSummary { get; set; }
        public string TempUnitOfMeasure { get; set; }
        private readonly IHostingEnvironment _hostEnv;

        public InformationController(IHostingEnvironment hostingEnvironment)
        {
            _hostEnv = hostingEnvironment;
        }

        public IActionResult GetInfo()
        {
            Models.InformationModel model = new Models.InformationModel();
            model.OperatingSystem = RuntimeInformation.OSDescription;
            model.FrameworkDescription = RuntimeInformation.FrameworkDescription;
            model.OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
            model.ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
            
            string title = string.Empty;
            string OSArchitecture = string.Empty;


            if (model.OSArchitecture.ToUpper().Equals("X64"))
            {
                OSArchitecture = "64-bit";
            }
            else
            {
                OSArchitecture = "32-bit";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                title = $"Windows {OSArchitecture}";
            }else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                title = $"OSX {OSArchitecture}";
            }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                title = $"Linux {OSArchitecture}";
            }

            GetLocationInfo().Wait();
            model.IPAdressString = PublicIP;

            GetWeatherInfo().Wait();
            model.CurrentIcon = CurrentWeatherIcon;
            model.WeatherBy = WeatherAttribution;
            model.CurrentTemperature = CurrentTemp;
            model.DailySummary = DayWeatherSummary;
            model.CurrentCity = City;
            model.UnitOfMeasure = TempUnitOfMeasure;

            model.InfoTitle = title;

            

            return View(model);
        }

        
        private async Task GetLocationInfo()
        {
            var httpClient = new HttpClient();
            string json = await httpClient.GetStringAsync("https://ipapi.co/json");
            LocationInfo info = JsonConvert.DeserializeObject<LocationInfo>(json);

            PublicIP = info.ip;
            Long = info.longitude;
            Latt = info.latitude;
            City = info.city;
        }

        private DarkSkyService.OptionalParameters GetUnitOfMeasure()
        {
            bool blnMetric = RegionInfo.CurrentRegion.IsMetric;
            DarkSkyService.OptionalParameters optParms = new DarkSkyService.OptionalParameters();

            if (blnMetric)
            {
                optParms.MeasurementUnits = "si";
                TempUnitOfMeasure = "C";
            }
            else
            {
                optParms.MeasurementUnits = "us";
                TempUnitOfMeasure = "F";
            }
            return optParms;
        }

        private async Task GetWeatherInfo()
        {
            string apiKey = "7b2386b7eb27edfe56672ce1520b8893";
            DarkSkyService weather = new DarkSkyService(apiKey);
            DarkSkyService.OptionalParameters optParams = GetUnitOfMeasure();
            var foreCast = await weather.GetForecast(Latt, Long, optParams);
            string iconFileName = GetCurrentWeatherIcon(foreCast.Response.Currently.Icon);
            string svgFile = Path.Combine(_hostEnv.ContentRootPath, "climacons", iconFileName);
            CurrentWeatherIcon = System.IO.File.ReadAllText($"{svgFile}");
            WeatherAttribution = foreCast.AttributionLine;
            DayWeatherSummary = foreCast.Response.Daily.Summary;

            if (foreCast.Response.Currently.Temperature.HasValue)
                CurrentTemp = Math.Round(foreCast.Response.Currently.Temperature.Value, 0).ToString();
        }

        private string GetCurrentWeatherIcon(Icon ic)
        {
            string iconFilename = string.Empty;

            switch (ic)
            {
                case Icon.ClearDay:
                    iconFilename = "Sun.svg";
                    break;

                case Icon.ClearNight:
                    iconFilename = "Moon.svg";
                    break;

                case Icon.Cloudy:
                    iconFilename = "Cloud.svg";
                    break;

                case Icon.Fog:
                    iconFilename = "Cloud-Fog.svg";
                    break;

                case Icon.PartlyCloudyDay:
                    iconFilename = "Cloud-Sun.svg";
                    break;

                case Icon.PartlyCloudyNight:
                    iconFilename = "Cloud-Moon.svg";
                    break;

                case Icon.Rain:
                    iconFilename = "Cloud-Rain.svg";
                    break;

                case Icon.Snow:
                    iconFilename = "Snowflake.svg";
                    break;

                case Icon.Wind:
                    iconFilename = "Wind.svg";
                    break;
                default:
                    iconFilename = "Thermometer.svg";
                    break;
            }
            return iconFilename;
        }
        
        
        
        public class LocationInfo
        {
            public string ip { get; set; }
            public string city { get; set; }
            public string region { get; set; }
            public string region_code { get; set; }
            public string country { get; set; }
            public string country_name { get; set; }
            public string postal { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            public string timezone { get; set; }
            public string asn { get; set; }
            public string org { get; set; }
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
