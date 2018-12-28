using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.FFXIVHelper;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class ParseTotalModel :
        BindableBase
    {
        private bool isInitializing = false;

        public ParseTotalModel()
        {
            this.ParseList.CollectionChanged += (x, y) =>
            {
                if (!this.isInitializing)
                {
                    this.RaisePropertyChanged(nameof(this.BestPerfAvg));
                    this.RaisePropertyChanged(nameof(this.DPSAvg));
                    this.RaisePropertyChanged(nameof(this.Category));
                    this.RaisePropertyChanged(nameof(this.CategoryColor));
                    this.RaisePropertyChanged(nameof(this.CategoryBrush));
                    this.RaisePropertyChanged(nameof(this.ExistsParses));
                }
            };
        }

        private string characterName;

        public string CharacterName
        {
            get => this.characterName;
            set => this.SetProperty(ref this.characterName, value);
        }

        private string server;

        public string Server
        {
            get => this.server;
            set => this.SetProperty(ref this.server, value);
        }

        private FFLogsRegions region = FFLogsRegions.JP;

        public FFLogsRegions Region
        {
            get => this.region;
            set => this.SetProperty(ref this.region, value);
        }

        private Job job = Jobs.Find(JobIDs.Unknown);

        public Job Job
        {
            get => this.job;
            set => this.SetProperty(ref this.job, value);
        }

        private DateTime timestamp = DateTime.MinValue;

        public DateTime Timestamp
        {
            get => this.timestamp;
            set => this.SetProperty(ref this.timestamp, value);
        }

        public string DataID => $"{this.CharacterName}-{this.Server}-{this.Region}-{this.Job}";

        public ObservableCollection<ParseModel> ParseList { get; } = new ObservableCollection<ParseModel>();

        public bool ExistsParses => this.ParseList.Any();

        public void AddRangeParse(
            IEnumerable<ParseModel> parses)
        {
            try
            {
                this.isInitializing = true;

                lock (this)
                {
                    this.ParseList.Clear();
                    this.ParseList.AddRange(parses);
                }
            }
            finally
            {
                this.isInitializing = false;
            }

            this.RaisePropertyChanged(nameof(this.BestPerfAvg));
            this.RaisePropertyChanged(nameof(this.DPSAvg));
            this.RaisePropertyChanged(nameof(this.Category));
            this.RaisePropertyChanged(nameof(this.CategoryColor));
            this.RaisePropertyChanged(nameof(this.CategoryBrush));
            this.RaisePropertyChanged(nameof(this.ExistsParses));
        }

        public float BestPerfAvg =>
            this.ParseList.Any() ?
            this.ParseList.Average(x => x.Percentile) :
            0f;

        public float DPSAvg =>
            this.ParseList.Any() ?
            this.ParseList.Average(x => x.Total) :
            0f;

        public string Category => GetCategory(this.BestPerfAvg);

        public Color CategoryColor => GetCategoryColor(this.BestPerfAvg);

        public SolidColorBrush CategoryBrush => GetCategoryBrush(this.BestPerfAvg);

        public static string GetCategory(
            float perf)
        {
            if (perf >= 100)
            {
                return "A";
            }
            else if (perf < 100 && perf >= 95)
            {
                return "B";
            }
            else if (perf < 95 && perf >= 75)
            {
                return "C";
            }
            else if (perf < 75 && perf >= 50)
            {
                return "D";
            }
            else if (perf < 50 && perf >= 25)
            {
                return "E";
            }
            else
            {
                return "F";
            }
        }

        public static Color GetCategoryColor(
            float perf)
            => CategoryBrushesDictionary[GetCategory(perf)].Color;

        public static SolidColorBrush GetCategoryBrush(
            float perf)
        {
            var brush = CategoryBrushesDictionary[GetCategory(perf)].Brush;
            if (!brush.IsFrozen)
            {
                brush.Freeze();
            }

            return brush;
        }

        private static readonly Color Category1Color = (Color)ColorConverter.ConvertFromString("#E9D086");
        private static readonly Color Category2Color = (Color)ColorConverter.ConvertFromString("#FC9E3E");
        private static readonly Color Category3Color = (Color)ColorConverter.ConvertFromString("#A83DB8");
        private static readonly Color Category4Color = (Color)ColorConverter.ConvertFromString("#064ECC");
        private static readonly Color Category5Color = (Color)ColorConverter.ConvertFromString("#219D1E");
        private static readonly Color Category6Color = (Color)ColorConverter.ConvertFromString("#C8C2B7");

        private static readonly Dictionary<string, (Color Color, SolidColorBrush Brush)> CategoryBrushesDictionary = new Dictionary<string, (Color, SolidColorBrush)>()
        {
            { "A", (Category1Color, new SolidColorBrush(Category1Color)) },
            { "B", (Category2Color, new SolidColorBrush(Category2Color)) },
            { "C", (Category3Color, new SolidColorBrush(Category3Color)) },
            { "D", (Category4Color, new SolidColorBrush(Category4Color)) },
            { "E", (Category5Color, new SolidColorBrush(Category5Color)) },
            { "F", (Category6Color, new SolidColorBrush(Category6Color)) },
        };

        private static HttpClient httpClient;

        public HttpClient HttpClient
        {
            get
            {
                if (httpClient != null)
                {
                    return httpClient;
                }

                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://www.fflogs.com/v1");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                return httpClient;
            }
        }

        public async Task GetParseAsync(
            string characterName,
            string server,
            FFLogsRegions region,
            Job job)
        {
            if (string.IsNullOrEmpty(Settings.Instance.FFLogs.ApiKey))
            {
                await WPFHelper.InvokeAsync(this.ParseList.Clear);
                return;
            }

            // 同じ条件で6分以内ならば再取得しない
            if (this.ExistsParses &&
                characterName == this.CharacterName &&
                server == this.Server &&
                region == this.Region)
            {
                if ((DateTime.Now - this.Timestamp).TotalMinutes <= 6.0)
                {
                    return;
                }
            }

            var uri = string.Format(
                "parses/character/{0}/{1}/{2}",
                HttpUtility.HtmlEncode(characterName),
                server,
                region.ToString());

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["api_key"] = Settings.Instance.FFLogs.ApiKey;

            uri += $"{uri}?{query.ToString()}";

            var res = await this.HttpClient.GetAsync(uri);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                await WPFHelper.InvokeAsync(this.ParseList.Clear);
                return;
            }

            var s = await res.Content.ReadAsStringAsync();
            var parses = JsonConvert.DeserializeObject<ParseModel[]>(s);

            if (parses == null ||
                parses.Length < 1)
            {
                await WPFHelper.InvokeAsync(this.ParseList.Clear);
                return;
            }

            var bests =
                from x in parses
                where
                job == null ||
                string.Equals(x.Spec, job.NameEN, StringComparison.OrdinalIgnoreCase)
                group x by x.EncounterName into g
                select
                g.OrderByDescending(y => y.Percentile).First();

            await WPFHelper.InvokeAsync(() =>
            {
                this.CharacterName = characterName;
                this.Server = server;
                this.Region = region;
                this.Job = job;
                this.AddRangeParse(bests);
                this.Timestamp = DateTime.Now;
            });
        }
    }
}
