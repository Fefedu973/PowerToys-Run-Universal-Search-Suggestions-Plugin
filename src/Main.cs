using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text;
using System.Windows.Controls;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Google_Search_Results_Plugin
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable, IDelayedExecutionPlugin
    {
        private const string Setting = nameof(Setting);

        // current value of the setting
        private bool _setting;

        private PluginInitContext _context;

        private string _iconPath;

        private bool _disposed;

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        // TODO: remove dash from ID below and inside plugin.json
        public static string PluginID => "64861420-a0ca-442d-ae1c-35054e15a4b7";

        // TODO: add additional options (optional)
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _setting = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == Setting)?.Value ?? false;
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
                    Title = "Start typing to search",
                    SubTitle = Description,
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

        // Modified to return List<Result>
        public List<Result> Query(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                return results;
            }

            // Call the asynchronous method and wait for the results
            var task = QueryAsync(query, delayedExecution);
            task.Wait();
            results.AddRange(task.Result);

            return results;
        }

        // Async method to perform the Google search suggestions query
        public async Task<List<Result>> QueryAsync(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                return results;
            }

            // Do a query to Google Search Suggestions API
            var searchTerm = Uri.EscapeDataString(query.Search);
            var requestUri = $"http://google.com/complete/search?output=toolbar&q={searchTerm}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(requestUri);

                    // Parse the XML response
                    var xml = XDocument.Parse(response);
                    var suggestions = xml.Descendants("suggestion")
                                         .Select(element => element.Attribute("data")?.Value)
                                         .Where(suggestion => !string.IsNullOrEmpty(suggestion))
                                         .ToList();

                    // Add parsed results
                    foreach (var suggestion in suggestions)
                    {
                        results.Add(new Result
                        {
                            Title = suggestion,
                            SubTitle = "Google Search Suggestion",
                            QueryTextDisplay = suggestion,
                            IcoPath = _iconPath,
                            Action = action =>
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = $"https://www.google.com/search?q={Uri.EscapeDataString(suggestion)}",
                                    UseShellExecute = true
                                });
                                return true;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception($"Failed to get suggestions from Google API: {ex.Message}", ex, GetType());
                results.Add(new Result
                {
                    Title = "Error",
                    SubTitle = $"Failed to fetch suggestions: {ex.Message}",
                    IcoPath = _iconPath
                });
            }

            return results;
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
            }
            else
            {
                _iconPath = "Images/Search.dark.png";
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
