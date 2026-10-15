using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Toolkit.Lang;
using Prism.Mvvm;

namespace HomeLedger.Avalonia.Services;

/// <summary>持久化八套配色、三种模式和语言，主题判断统一交给共享控制器。</summary>
public sealed class AppearanceService : BindableBase
{
    private readonly string _path;
    private readonly StudioThemeController _controller;
    private bool _loading;
    private string? _error;
    public static AppearanceService Current { get; private set; } = null!;

    public static void Initialize(Application app, string? settingsPath = null) => Current = new(app, settingsPath);

    public AppearanceService(Application app, string? settingsPath = null)
    {
        _path = settingsPath ?? Path.Combine(AppServices.DataDirectory, "appearance.json");
        _loading = true;
        var settings = Read(_path);
        AppText.Initialize(settings.CultureName);
        _controller = new StudioThemeController(app) { Palette = settings.Palette, Mode = settings.Mode };
        _controller.PropertyChanged += (_, _) => { RaisePropertyChanged(nameof(SelectedMode)); RaisePropertyChanged(nameof(SelectedPalette)); Save(); };
        I18nManager.Instance.CultureChanged += (_, _) =>
        {
            foreach (var option in Modes.Concat(Palettes).Concat(Languages)) option.Refresh();
            RaisePropertyChanged(nameof(SelectedLanguage));
            ApplyControlCulture();
            Save();
        };
        ApplyControlCulture();
        _loading = false;
    }

    public IReadOnlyList<AppearanceOption> Modes { get; } =
    [ new("System", Language.AppearanceSystem), new("Light", Language.AppearanceLight), new("Dark", Language.AppearanceDark) ];
    public IReadOnlyList<AppearanceOption> Palettes { get; } =
    [
        new("DotNet", Language.AppearanceDotNet), new("Aurora", Language.AppearanceAurora), new("Ember", Language.AppearanceEmber),
        new("Default", Language.AppearanceDefault), new("Aquatic", Language.AppearanceAquatic), new("Desert", Language.AppearanceDesert),
        new("Dusk", Language.AppearanceDusk), new("NightSky", Language.AppearanceNightSky)
    ];
    public IReadOnlyList<AppearanceOption> Languages { get; } =
    [ new("zh-CN", Language.AppearanceChinese), new("en-US", Language.AppearanceEnglish) ];
    public AppearanceOption SelectedMode { get => Modes.Single(x => x.Key == _controller.Mode.ToString()); set { if (value != null) _controller.Mode = Enum.Parse<StudioThemeMode>(value.Key); } }
    public AppearanceOption SelectedPalette { get => Palettes.Single(x => x.Key == _controller.Palette.ToString()); set { if (value != null) _controller.Palette = Enum.Parse<StudioPalette>(value.Key); } }
    public AppearanceOption SelectedLanguage { get => Languages.Single(x => x.Key == AppText.Culture.Name); set { if (value != null) I18nManager.Instance.Culture = CultureInfo.GetCultureInfo(value.Key); } }
    public string? Error { get => _error; private set => SetProperty(ref _error, value); }
    public StudioThemeController Controller => _controller;

    public static AppearanceSettings Read(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var result = JsonSerializer.Deserialize(File.ReadAllText(path), AppearanceJsonContext.Default.AppearanceSettings);
                if (result != null && Enum.IsDefined(result.Mode) && Enum.IsDefined(result.Palette) && result.CultureName is "zh-CN" or "en-US") return result;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) { }
        return new();
    }

    private void Save()
    {
        if (_loading) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(new AppearanceSettings(_controller.Mode, _controller.Palette, AppText.Culture.Name), AppearanceJsonContext.Default.AppearanceSettings));
            File.Move(_path + ".tmp", _path, true);
            Error = null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { Error = AppText.Format(Language.AppearanceSaveFailed, error.Message); }
    }

    private static void ApplyControlCulture()
    {
        if (!Dispatcher.UIThread.CheckAccess()) { Dispatcher.UIThread.Post(ApplyControlCulture); return; }
        if (Application.Current is not { } app) return;
        ResourceDictionary semi = AppText.Culture.Name == "en-US" ? new Semi.Avalonia.Locale.en_us() : new Semi.Avalonia.Locale.zh_cn();
        ResourceDictionary ursa = AppText.Culture.Name == "en-US" ? new Ursa.Themes.Semi.Locale.en_us() : new Ursa.Themes.Semi.Locale.zh_cn();
        foreach (var item in semi.Concat(ursa)) app.Resources[item.Key] = item.Value;
    }
}

public sealed class AppearanceOption(string key, string resourceKey) : BindableBase
{
    public string Key { get; } = key;
    public string DisplayName => AppText.Get(resourceKey);
    internal void Refresh() => RaisePropertyChanged(nameof(DisplayName));
}
public sealed record AppearanceSettings(StudioThemeMode Mode = StudioThemeMode.System, StudioPalette Palette = StudioPalette.DotNet, string CultureName = "zh-CN");
[JsonSerializable(typeof(AppearanceSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppearanceJsonContext : JsonSerializerContext;
