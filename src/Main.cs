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
using Svg;
using System.Text;
using System.Drawing.Imaging;
using static Microsoft.PowerToys.Settings.UI.Library.PluginAdditionalOption;
using System.Net;

namespace Community.PowerToys.Run.Plugin.UniversalSearchSuggestions
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable, IDelayedExecutionPlugin
    {
        private const string SettingSelectedSearchEngine = nameof(SettingSelectedSearchEngine);

        private const string CustomSearchEngine = nameof(CustomSearchEngine);

        private const string SettingSuggestionProvider = nameof(SettingSuggestionProvider);

        private const string AlwaysShowAResult = nameof(AlwaysShowAResult);

        private const string UpdatePluginSetting = nameof(UpdatePluginSetting);

        private string _customSearchEngineUrl;

        private static readonly string[] searchEngines = new string[] {
            "Google",
            "Bing",
            "Yahoo",
            "Baidu",
            "Yandex",
            "DuckDuckGo",
            "Naver",
            "Ask",
            "Ecosia",
            "Brave",
            "Qwant",
            "Startpage",
            "SwissCows",
            "Dogpile",
            "Gibiru",
            "Mojeek",
            "MetaGer",
            "ZapMeta",
            "Search Encrypt",
            "OneSearch",
            "Ekoru",
            "Custom",
        };

        private static string[] searchEnginesUrls = new string[] {
            "https://www.google.com/search?q=",
            "https://www.bing.com/search?q=",
            "https://search.yahoo.com/search?p=",
            "https://www.baidu.com/s?wd=",
            "https://yandex.com/search/?text=",
            "https://duckduckgo.com/?q=",
            "https://search.naver.com/search.naver?query=",
            "https://www.ask.com/web?q=",
            "https://www.ecosia.org/search?q=",
            "https://search.brave.com/search?q=",
            "https://www.qwant.com/?q=",
            "https://www.startpage.com/do/dsearch?query=",
            "https://swisscows.com/web?query=",
            "https://www.dogpile.com/serp?q=",
            "https://gibiru.com/results.html?q=",
            "https://www.mojeek.com/search?q=",
            "https://metager.org/meta/meta.ger3?eingabe=",
            "https://www.zapmeta.com/search?q=",
            "https://www.searchencrypt.com/search?q=",
            "https://www.onesearch.com/yhs/search?q=",
            "https://ekoru.org/search?q=",
            string.Empty
        };

        private static readonly string[] searchEnginesSuggestions = new string[] {
            "https://www.google.com/complete/search?client=gws-wiz&q=",
            "https://www.google.com/complete/search?output=toolbar&q=",
            "https://www.bingapis.com/api/v7/suggestions?appid=6D0A9B8C5100E9ECC7E11A104ADD76C10219804B&q=",
            "https://sugg.search.yahoo.net/sg/?output=json&nresults=10&command=",
            "https://duckduckgo.com/ac/?type=json&q=",
            "https://ac.ecosia.org/?q=",
            "https://search.brave.com/api/suggest?rich=true&q=",
            "https://api.qwant.com/v3/suggest?q=",
            "https://api.swisscows.com/suggest?query=",
        };

        private static readonly string[] searchEnginesSuggestionsName = new string[] {
            "Google",
            "Old Google api",
            "Bing",
            "Yahoo",
            "DuckDuckGo",
            "Ecosia",
            "Brave",
            "Qwant",
            "SwissCows",
        };

        private int _selectedSearchEngine;

        private int _selectedApi;

        private PluginInitContext _context;

        private string _iconPath;

        private string _errorIconPath;

        private bool _disposed;

        private bool _updatePlugin;

        private bool _alwaysShowAResult;

        private bool _preventMultipleUpdates = false;

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        public static string PluginID => "64861420-a0ca-442d-ae1c-35054e15a4b7";

        // Create an update method to update this plugin if there is a new release on the GitHub repository
        public async void UpdatePlugin()
        {
            // Get the latest release from the GitHub making a webrequest api call to https://api.github.com/repos/Fefedu973/PowerToys-Run-Search-Suggestions-Plugin/releases/latest


            using var proxyClientHandler = new HttpClientHandler
            {
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                Proxy = WebRequest.GetSystemWebProxy(),
                PreAuthenticate = true,
            };

            using var getReleaseInfoClient = new HttpClient(proxyClientHandler);

            // GitHub APIs require sending an user agent
            // https://docs.github.com/rest/overview/resources-in-the-rest-api#user-agent-required
            getReleaseInfoClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PowerToys");
            string releaseInfo = await getReleaseInfoClient.GetStringAsync("https://api.github.com/repos/Fefedu973/PowerToys-Run-Search-Suggestions-Plugin/releases/latest");

            var tag = JsonDocument.Parse(releaseInfo).RootElement.GetProperty("tag_name").GetString();
            var latestVersion = tag.Substring(1);
            // Get the current version of the plugin by getting the version key from the plugin.json file in the plugin directory of the PowerToys installation directory
            var jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "PowerToys", "PowerToys Run", "Plugins", "UniversalSearchSuggestions", "plugin.json");
            var json = File.ReadAllText(jsonPath);
            var currentVersion = JsonDocument.Parse(json).RootElement.GetProperty("Version").GetString();
            // Write in the console the current version and the latest version

            Log.Warn(latestVersion, GetType());
            Log.Warn(currentVersion, GetType());

            if (latestVersion != currentVersion)
            {

                // Check if the updater is installed if yes, start the updater else download and install the updater and start it

                var updaterPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "PowerToys", "PowerToys Run", "Plugins", "PowerToys Run Plugin Updater", "PowerToys Run Plugin Updater.exe");

                if (!System.IO.File.Exists(updaterPath))
                {
                    string updater = await getReleaseInfoClient.GetStringAsync("https://api.github.com/repos/Fefedu973/PowerToys-Run-Plugin-Updater/releases/latest");
                    //Download the asset
                    var updaterUrl = JsonDocument.Parse(updater).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    var updaterDownloadPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "PowerToys", "PowerToys Run", "Plugins", "PowerToys Run Plugin Updater", "PowerToys Run Plugin Updater.exe");
                    using (var updaterClient = new HttpClient())
                    {
                        var updaterStream = await updaterClient.GetStreamAsync(updaterUrl);
                        using (var fileStream = new FileStream(updaterDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await updaterStream.CopyToAsync(fileStream);
                        }
                    }
                    System.Diagnostics.Process.Start(updaterDownloadPath);
                }
                else
                {
                    System.Diagnostics.Process.Start(updaterPath);
                }
            }

        }


    public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
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
            },
            new PluginAdditionalOption()
            {
                Key = CustomSearchEngine,
                DisplayDescription = "Enter the URL of the custom search engine (only if Custom is selected). The search term will be appended to the end of the URL.",
                DisplayLabel = "Custom Search Engine URL",
                PluginOptionType = AdditionalOptionType.Textbox,
                TextValue = "",
                PlaceholderText = "eg. https://www.example.com/search?q="
            },
            new PluginAdditionalOption()
            {
                Key = SettingSuggestionProvider,
                DisplayDescription = "Select the provider for the suggestions",
                DisplayLabel = "Selected Suggestion Provider",
                PluginOptionType = AdditionalOptionType.Combobox,
                ComboBoxOptions = searchEngines.ToList(),
                ComboBoxValue = 0,
                ComboBoxItems = searchEnginesSuggestionsName.Select((val, idx) =>
                {
                    return new KeyValuePair<string, string>(val, idx.ToString());
                }).ToList()
            },
            new PluginAdditionalOption()
            {
                Key = AlwaysShowAResult,
                DisplayDescription = "Always show a suggestion even if there are no results (this allows you to search with the selected search engine)",
                DisplayLabel = "Always show a result",
                Value = true,
            },
            new PluginAdditionalOption()
            {
                Key = UpdatePluginSetting,
                DisplayDescription = "Update the plugin to the latest version",
                DisplayLabel = "Automatically check for new updates",
                Value = true,
            }
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _selectedApi = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == SettingSuggestionProvider)?.ComboBoxValue ?? 0;

            _selectedSearchEngine = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == SettingSelectedSearchEngine)?.ComboBoxValue ?? 0;

            _customSearchEngineUrl = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == CustomSearchEngine)?.TextValue ?? string.Empty;

            searchEnginesUrls = searchEnginesUrls.Take(searchEnginesUrls.Length - 1).Concat(new string[] { _customSearchEngineUrl }).ToArray();

            _alwaysShowAResult = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == AlwaysShowAResult)?.Value ?? true;

            _updatePlugin = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == UpdatePluginSetting)?.Value ?? true;

            if (_updatePlugin && !_preventMultipleUpdates)
            {
                _preventMultipleUpdates = true;
                UpdatePlugin();
            }
            if (_updatePlugin == false)
            {
                _preventMultipleUpdates = false;
            }
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return new List<ContextMenuResult>(0);
        }

        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

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

        public List<Result> Query(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

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

            // If no results are found just add a result with the user query as title that will open the search engine with the user query
            if (results.Count == 0 && _alwaysShowAResult)
            {
                results.Add(new Result
                {
                    Title = query.Search,
                    SubTitle = "No suggestions found, search with the selected search engine",
                    QueryTextDisplay = query.Search,
                    IcoPath = _iconPath,
                    Action = action =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(query.Search)}",
                            UseShellExecute = true,
                        });
                        return true;
                    },
                });
            }

            return results;
        }

        public async Task<List<Result>> QueryAsync(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            if (string.IsNullOrEmpty(query.Search))
            {
                return results;
            }

            var searchTerm = Uri.EscapeDataString(query.Search);

            var requestUri = searchEnginesSuggestions[_selectedApi] + searchTerm;

            try
            {
                PurgeImagesFolder();

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(requestUri);

                    if (response != null)
                    {
                        switch (_selectedApi)
                        {
                            case 0:
                                results.AddRange(await ParseNewApiResponse(response));
                                break;
                            case 1:
                                results.AddRange(ParseOldApiResponse(response));
                                break;
                            case 2:
                                results.AddRange(ParseBingApiResponse(response));
                                break;
                            case 3:
                                results.AddRange(ParseYahooApiResponse(response));
                                break;
                            case 4:
                                results.AddRange(ParseDuckDuckGoApiResponse(response));
                                break;
                            case 5:
                                results.AddRange(ParseEcosiaApiResponse(response));
                                break;
                            case 6:
                                results.AddRange(await ParseBraveApiResponse(response));
                                break;
                            case 7:
                                results.AddRange(ParseQwantApiResponse(response));
                                break;
                            case 8:
                                results.AddRange(ParseSwissCowsApiResponse(response));
                                break;
                            default:
                                results.AddRange(ParseOldApiResponse(response));
                                break;
                        }
                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception($"Failed to query the Search Suggestions API: {ex.Message}", ex, GetType());
                results.Add(new Result
                {
                    Title = "ERROR: Failed to query the Search Suggestions API",
                    SubTitle = ex.Message,
                    IcoPath = _errorIconPath,
                    Action = action =>
                    {
                        System.Windows.Clipboard.SetText("Failed to query the Search Suggestions API: " + ex.Message);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/Fefedu973/PowerToys-Run-Search-Suggestions-Plugin/issues/new",
                            UseShellExecute = true,
                        });
                        return true;
                    }
                });
            }
            return results;
        }

        private IEnumerable<Result> ParseBingApiResponse(string response)
        {
            var results = new List<Result>();

            var json = JsonDocument.Parse(response);
            var suggestions = json.RootElement.GetProperty("suggestionGroups")[0].GetProperty("searchSuggestions");

            foreach (var suggestion in suggestions.EnumerateArray())
            {
                var title = suggestion.GetProperty("displayText").GetString();

                results.Add(new Result
                {
                    Title = title,
                    SubTitle = "",
                    QueryTextDisplay = title,
                    IcoPath = _iconPath,
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

            return results;
        }

        private IEnumerable<Result> ParseYahooApiResponse(string response)
        {
            var results = new List<Result>();

            var json = JsonDocument.Parse(response);
            var suggestions = json.RootElement.GetProperty("gossip").GetProperty("results");

            foreach (var suggestion in suggestions.EnumerateArray())
            {
                var title = suggestion.GetProperty("key").GetString();

                results.Add(new Result
                {
                    Title = title,
                    SubTitle = "",
                    QueryTextDisplay = title,
                    IcoPath = _iconPath,
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
            return results;
        }

        private IEnumerable<Result> ParseDuckDuckGoApiResponse(string response)
        {
            var results = new List<Result>();

            using (var doc = JsonDocument.Parse(response))
            {
                var root = doc.RootElement;
                foreach (var element in root.EnumerateArray())
                {
                    var phrase = element.GetProperty("phrase").GetString();
                    if (!string.IsNullOrEmpty(phrase))
                    {
                        results.Add(new Result
                        {
                            Title = phrase,
                            SubTitle = "",
                            IcoPath = _iconPath,
                            Action = action =>
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(phrase)}",
                                    UseShellExecute = true,
                                });
                                return true;
                            },
                        });
                    }
                }
            }
            return results;
        }

        private IEnumerable<Result> ParseEcosiaApiResponse(string response)
        {
            var results = new List<Result>();

            using (var doc = JsonDocument.Parse(response))
            {
                var root = doc.RootElement;
                var suggestions = root.GetProperty("suggestions").EnumerateArray();
                foreach (var suggestion in suggestions)
                {
                    var phrase = suggestion.GetString();
                    if (!string.IsNullOrEmpty(phrase))
                    {
                        results.Add(new Result
                        {
                            Title = phrase,
                            SubTitle = "",
                            IcoPath = _iconPath,
                            Action = action =>
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(phrase)}",
                                    UseShellExecute = true,
                                });
                                return true;
                            },
                        });
                    }
                }
            }

            return results;
        }

        private async Task<IEnumerable<Result>> ParseBraveApiResponse(string response)
        {
           var results = new List<Result>();
            using (JsonDocument document = JsonDocument.Parse(response))
            {
                JsonElement root = document.RootElement;
                JsonElement searchResults = root[1];

                foreach (JsonElement result in searchResults.EnumerateArray())
                {
                    if (result.TryGetProperty("is_entity", out JsonElement isEntity) && isEntity.GetBoolean())
                    {
                        string title = result.GetProperty("name").GetString();
                        string description = result.GetProperty("desc").GetString();

                        if (result.TryGetProperty("img", out JsonElement urlElement))
                        {
                            string url = urlElement.GetString();

                            string imagePath = await DownloadImageAsync(url);

                            results.Add(new Result
                            {
                                Title = title,
                                SubTitle = description,
                                IcoPath = imagePath,
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
                        else
                        {
                            results.Add(new Result
                            {
                                Title = title,
                                SubTitle = description,
                                IcoPath = _iconPath,
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
                    else
                    {
                        string title = result.GetProperty("q").GetString();

                        results.Add(new Result
                        {
                            Title = title,
                            SubTitle = "",
                            IcoPath = _iconPath,
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
            return results;
        }

        private IEnumerable<Result> ParseQwantApiResponse(string response)
        {
            var results = new List<Result>();

            using (var doc = JsonDocument.Parse(response))
            {
                var items = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray();
                foreach (var item in items)
                {
                    var value = item.GetProperty("value").GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        results.Add(new Result
                        {
                            Title = value,
                            SubTitle = "",
                            IcoPath = _iconPath,
                            Action = action =>
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(value)}",
                                    UseShellExecute = true,
                                });
                                return true;
                            },
                        });
                    }
                }
            }
            return results;
        }

        private IEnumerable<Result> ParseSwissCowsApiResponse(string response)
        {
            var results = new List<Result>();

            using (var doc = JsonDocument.Parse(response))
            {
                var suggestions = doc.RootElement.EnumerateArray();
                foreach (var suggestion in suggestions)
                {
                    var phrase = suggestion.GetString();
                    if (!string.IsNullOrEmpty(phrase))
                    {
                        results.Add(new Result
                        {
                            Title = phrase,
                            SubTitle = "",
                            IcoPath = _iconPath,
                            Action = action =>
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = $"{searchEnginesUrls[_selectedSearchEngine]}{Uri.EscapeDataString(phrase)}",
                                    UseShellExecute = true,
                                });
                                return true;
                            },
                        });
                    }
                }
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
                    SubTitle = "",
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
                    Action = action =>
                    {
                        System.Windows.Clipboard.SetText("Failed to parse Google Search Suggestions API response: JSON data not found");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/Fefedu973/PowerToys-Run-Search-Suggestions-Plugin/issues/new",
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
                    Action = action =>
                    {
                        System.Windows.Clipboard.SetText($"Failed to parse Google Search Suggestions API response: {ex.Message}");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/Fefedu973/PowerToys-Run-Search-Suggestions-Plugin/issues/new",
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

                        string streamFileTypeName = response.Content.Headers.ContentType.MediaType;

                        string randomFileName = Path.Combine(path, Guid.NewGuid().ToString() + "." + streamFileTypeName.Split('/')[1]);

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(randomFileName))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                        if (streamFileTypeName == "image/svg+xml")
                        {
                            var byteArray = Encoding.ASCII.GetBytes(File.ReadAllText(randomFileName));
                            using (var svgstream = new MemoryStream(byteArray))
                            {
                                var svgDocument = SvgDocument.Open<SvgDocument>(svgstream);
                                var bitmap = svgDocument.Draw();
                                var file = Path.Combine(path, Guid.NewGuid().ToString() + ".png");
                                bitmap.Save(file, ImageFormat.Png);
                                randomFileName = file;
                            }
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
                _errorIconPath = "Images/warn.light.png";
            }
            else
            {
                _iconPath = "Images/Search.dark.png";
                _errorIconPath = "Images/warn.dark.png";
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
