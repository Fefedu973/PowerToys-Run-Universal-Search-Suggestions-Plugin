using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Controls;
using System.Xml.Linq;
using Wox.Plugin;
using Wox.Plugin.Logger;
using System.Reflection;
using static Microsoft.PowerToys.Settings.UI.Library.PluginAdditionalOption;


namespace Community.PowerToys.Run.Plugin.GoogleSearchSuggestions
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable, IDelayedExecutionPlugin
    {
        private const string SettingOldApi = nameof(SettingOldApi);

        private const string SettingSelectedSearchEngine = nameof(SettingSelectedSearchEngine);

        //define the search engines list

        private static readonly string[] searchEngines = new string[] { "Google", "Bing", "DuckDuckGo", "Yahoo", "Baidu", "Yandex", "Startpage", "Qwant", "Ecosia", "Brave", "Ask", "Naver", "Seznam" };

        //define the search engines URLs

        private static readonly string[] searchEnginesUrls = new string[] { "https://www.google.com/search?q=", "https://www.bing.com/search?q=", "https://duckduckgo.com/?q=", "https://search.yahoo.com/search?p=", "https://www.baidu.com/s?wd=", "https://yandex.com/search/?text=", "https://www.startpage.com/do/dsearch?query=", "https://www.qwant.com/?q=", "https://www.ecosia.org/search?q=", "https://search.brave.com/search?q=", "https://www.ask.com/web?q=", "https://search.naver.com/search.naver?query=", "https://search.seznam.cz/?q=" };

        private int _selectedSearchEngine;

        // current value of the setting
        private bool _useOldApi;

        private PluginInitContext _context;

        private string _iconPath;

        private string _errorIconPath;

        private bool _disposed;

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        // TODO: remove dash from ID below and inside plugin.json
        public static string PluginID => "64861420-a0ca-442d-ae1c-35054e15a4b7";

        // TODO: add additional options (optional)
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = SettingOldApi,
                DisplayLabel = "Use the old Google API (no images)",
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = SettingSelectedSearchEngine,
                DisplayDescription = "Change the search engine you will be redirected to when you select a suggestion",
                DisplayLabel = "Selected Search Engine",
                PluginOptionType = AdditionalOptionType.Combobox,
                ComboBoxOptions = searchEngines.ToList(),
                ComboBoxValue = 0,
                ComboBoxItems = searchEngines.Select((val, idx) =>
                {
                    return new KeyValuePair<string, string>(val, idx.ToString());
                }).ToList()
            }
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _useOldApi = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == SettingOldApi)?.Value ?? false;

            _selectedSearchEngine = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == SettingSelectedSearchEngine)?.ComboBoxValue ?? 0;           

        }

        // TODO: return context menus for each Result (optional)
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return new List<ContextMenuResult>(0);
        }

        // TODO: return query results
        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                results.Add(new Result
                {
                    Title = "Start typing to search...",
                    SubTitle = "Powered by " + Description + " made by Fefe_du_973",
                    QueryTextDisplay = string.Empty,
                    IcoPath = _iconPath,
                    Action = action =>
                    {
                        return true;
                    },
                });
                return results;
            }

            return results;
        }

        // TODO: return delayed query results (optional)
        public List<Result> Query(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                results.Add(new Result
                {
                    Title = "Start typing to search...",
                    SubTitle = "Powered by " + Description + " made by Fefe_du_973",
                    QueryTextDisplay = string.Empty,
                    IcoPath = _iconPath,
                    Action = action =>
                    {
                        return true;
                    },
                });
                return results;
            }

            var task = QueryAsync(query, delayedExecution);
            task.Wait();
            results.AddRange(task.Result);

            return results;
        }

        public async Task<List<Result>> QueryAsync(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                return results;
            }

            var searchTerm = Uri.EscapeDataString(query.Search);
            var requestUri = _useOldApi
                ? $"https://www.google.com/complete/search?output=toolbar&q={searchTerm}"
                : $"https://www.google.com/complete/search?q={searchTerm}&client=gws-wiz";

            try
            {
                PurgeImagesFolder();

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(requestUri);

                    if (_useOldApi)
                    {
                        results.AddRange(ParseOldApiResponse(response));
                    }
                    else
                    {
                        results.AddRange(await ParseNewApiResponse(response));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception($"Failed to query Google Search Suggestions API: {ex.Message}", ex, GetType());
                results.Add(new Result
                {
                    Title = "ERROR: Failed to query Google Search Suggestions API",
                    SubTitle = ex.Message,
                    IcoPath = _errorIconPath,
                    //report issue to GitHub
                    Action = action =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/Fefedu973/PowerToys-Run-Google-Search-Suggestions-Plugin/issues/new",
                            UseShellExecute = true,
                        });
                        return true;
                    }
                });
            }
            return results;
        }

        private IEnumerable<Result> ParseOldApiResponse(string response)
        {
            var results = new List<Result>();

            var xml = XDocument.Parse(response);
            var suggestions = xml.Descendants("suggestion")
                                 .Select(element => element.Attribute("data").Value)
                                 .Where(suggestion => !string.IsNullOrEmpty(suggestion))
                                 .ToList();

            foreach (var suggestion in suggestions)
            {
                results.Add(new Result
                {
                    Title = suggestion,
                    SubTitle = "Search for " + suggestion,
                    IcoPath = _iconPath,
                    Action = action =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(suggestion)}",
                            UseShellExecute = true,
                        });
                        return true;
                    },
                });
            }

            return results;
        }

        private async Task<IEnumerable<Result>> ParseNewApiResponse(string response)
        {
            var results = new List<Result>();

            const string jsonPrefix = "window.google.ac.h(";
            const string jsonSuffix = ")";

            var jsonStart = response.IndexOf(jsonPrefix);
            var jsonEnd = response.LastIndexOf(jsonSuffix);

            if (jsonStart == -1 || jsonEnd == -1)
            {
                Log.Warn("Failed to parse Google Search Suggestions API response: JSON data not found", GetType());
                results.Add(new Result
                {
                    Title = "ERROR: Failed to parse Google Search Suggestions API response",
                    SubTitle = "JSON data not found",
                    IcoPath = _errorIconPath,
                    //report issue to GitHub
                    Action = action =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/Fefedu973/PowerToys-Run-Google-Search-Suggestions-Plugin/issues/new",
                            UseShellExecute = true,
                        });
                        return true;
                    }
                });

            }

            string jsonResponse = response.Substring(jsonStart + jsonPrefix.Length, jsonEnd - (jsonStart + jsonPrefix.Length));

            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
            };

            try
            {
                using (var document = JsonDocument.Parse(jsonResponse, options))
                {
                    var suggestions = document.RootElement[0].EnumerateArray();

                    foreach (var suggestion in suggestions)
                    {
                        if (suggestion.ValueKind != JsonValueKind.Array || suggestion.GetArrayLength() < 4)
                        {
                            var simpleTitle = suggestion[0].GetString();
                            simpleTitle = RemoveHtmlTags(simpleTitle);
                            simpleTitle = DecodeHtmlEntities(simpleTitle);

                            results.Add(new Result
                            {
                                Title = simpleTitle,
                                SubTitle = "",
                                QueryTextDisplay = simpleTitle,
                                IcoPath = _iconPath,
                                Action = action =>
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(simpleTitle)}",
                                        UseShellExecute = true,
                                    });
                                    return true;
                                },
                            });
                            continue;
                        }

                        var title = suggestion[0].GetString();
                        var subtitle = suggestion[3].TryGetProperty("zi", out var ziElement)
                            ? ziElement.GetString()
                            : string.Empty;
                        var imageUrl = suggestion[3].TryGetProperty("zs", out var zsElement)
                            ? zsElement.GetString()
                            : null;

                        title = RemoveHtmlTags(title);
                        title = DecodeHtmlEntities(title);

                        subtitle = RemoveHtmlTags(subtitle);
                        subtitle = DecodeHtmlEntities(subtitle);

                        string imagePath = await DownloadImageAsync(imageUrl);

                        results.Add(new Result
                        {
                            Title = title,
                            SubTitle = subtitle,
                            QueryTextDisplay = title,
                            IcoPath = imagePath ?? _iconPath,
                            Action = action =>
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(title)}",
                                    UseShellExecute = true,
                                });
                                return true;
                            },
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to parse Google Search Suggestions API response: {ex.Message}", GetType());
                results.Add(new Result
                {
                    Title = "ERROR: Failed to parse Google Search Suggestions API response",
                    SubTitle = ex.Message,
                    IcoPath = _errorIconPath,
                    //report issue to GitHub
                    Action = action =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/Fefedu973/PowerToys-Run-Google-Search-Suggestions-Plugin/issues/new",
                            UseShellExecute = true,
                        });
                        return true;
                    }
                });
            }

            return results;
        }

        private string RemoveHtmlTags(string input)
        {
            return input.Replace("<b>", string.Empty)
                        .Replace("</b>", string.Empty);
        }

        private string DecodeHtmlEntities(string input)
        {
            return System.Net.WebUtility.HtmlDecode(input);
        }

        private async Task<string> DownloadImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return null;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(imageUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images", "Previews");

                        Directory.CreateDirectory(path);

                        string randomFileName = Path.Combine(path, Guid.NewGuid().ToString() + ".png");

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(randomFileName))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        return randomFileName;
                    }
                    else
                    {
                        Log.Warn($"Failed to download image from URL: {imageUrl}", GetType());
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to download image from URL: {imageUrl} - {ex.Message}", GetType());
                return null;
            }
        }

        private void PurgeImagesFolder()
        {
            try
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images", "Previews");

                if (Directory.Exists(path))
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    foreach (FileInfo file in directory.GetFiles())
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to purge images folder: {ex.Message}", GetType());
            }
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images/Search.light.png";
                _errorIconPath = "Images/warn-light.png";
            }
            else
            {
                _iconPath = "Images/Search.dark.png";
                _errorIconPath = "Images/warn-dark.png";
            }
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }
    }
}
