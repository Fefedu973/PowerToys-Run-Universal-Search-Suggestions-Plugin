<h1 align="center">Universal Search Suggestion PowerToys Run Plugin</h1>

[<p align="center">]()
![GitHub all releases](https://img.shields.io/github/downloads/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin/total?style=for-the-badge)
![Discord](https://img.shields.io/discord/807892248935006208?style=for-the-badge)
![GitHub](https://img.shields.io/github/license/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin?style=for-the-badge)
![GitHub repo size](https://img.shields.io/github/repo-size/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin?style=for-the-badge)
[<p align="center">]()
![GitHub forks](https://img.shields.io/github/forks/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin?style=for-the-badge)
![GitHub Repo stars](https://img.shields.io/github/stars/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin?style=for-the-badge)
![GitHub commit activity](https://img.shields.io/github/commit-activity/w/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin?style=for-the-badge)

---

# ⚠️ THIS EXTENSION IS PLANNED TO BE PORTED TO THE NEW WINDOWS COMMAND PALETTE IN THE COMING WEEKS, I WILL STILL SUPPORT BOTH VERSIONS

[<p align="center">]()This is a simple [PowerToys Run](https://docs.microsoft.com/en-us/windows/powertoys/run) plugin that adds search suggestions when searching something.

## Preview

![Preview of the plugin in action](./images/preview.gif)

## Requirements

- PowerToys v0.81.1

## Installation

- Download the [latest release](https://github.com/Fefedu973/PowerToys-Run-Universal-Search-Suggestions-Plugin/releases/) by selecting the architecture that matches your machine: `x64` (more common) or `ARM64`
- Close PowerToys (including from the system tray)
- Extract the archive to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
- Open PowerToys

## Usage

- Just type the trigger keyword (`!s` by default) and start seaching, results will show up

## Configuration

- The default keyword for triggering the plugin is `!s` but it should be triggered everytime by default.
- You can change the search engine that you are redirected to.
- - Supported browsers: Google, Bing, Yahoo, Baidu, Yandex, DuckDuckGo, Naver, Ask, Ecosia, Brave, Qwant, Startpage, Swisscows
- You can set a custom search engine to be redirected to
- You can change the search suggestion provider. That's right, not just Google!
- - Supported suggestions providers: Google (rich content and classic), Bing, Yahoo, DuckDuckGo, Ecosia, Brave (rich content), Qwant, Swisscows

## Credits

- This project can only be completed under the guidance of [this article](https://conductofcode.io/post/creating-custom-powertoys-run-plugins/). Thanks to @hlaueriksson for his great work.

- This project has been greatly simplified and accelerated in its realization by [this visual studio template](https://github.com/8LWXpg/PowerToysRun-PluginTemplate) for creating PowerToys plugins. Thanks to [@8LWXpg](https://github.com/8LWXpg) for his great work.

## Thanks

Thank you to [@Daydreamer-riri](https://github.com/Daydreamer-riri) and [@thatgaypigeon](https://github.com/thatgaypigeon) for writing the excellent documentation!

_Thanks ChatGPT for helping me out in this project as it was my first C# project_

## License

[MIT](./LICENSE) License © 2024 [Fefe_du_973](https://github.com/Fefedu973/)
