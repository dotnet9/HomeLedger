using System.Globalization;
using CodeWF.Toolkit.Lang;
using CodeWF.Toolkit.Lang.Json;

namespace HomeLedger.Core.I18n;

/// <summary>所有业务文案共用 Toolkit 的国际化单例。</summary>
public static class AppText
{
    private static bool _initialized;
    public static CultureInfo Culture => I18nManager.Instance.Culture ?? CultureInfo.GetCultureInfo("zh-CN");

    public static void Initialize(string? cultureName = null)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName is "en-US" ? "en-US" : "zh-CN");
        if (!_initialized)
        {
            I18nManager.Instance.Register(new JsonLangPlugin
            {
                ResourceFolder = ResourceDirectory()
            }, culture);
            _initialized = true;
        }
        else I18nManager.Instance.Culture = culture;
    }

    private static string ResourceDirectory()
    {
        var adjacent = Path.Combine(AppContext.BaseDirectory, "I18n");
        if (Directory.Exists(adjacent)) return adjacent;
        // macOS bundle keeps JSON in Contents/Resources beside Contents/MacOS.
        var bundled = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "I18n"));
        return Directory.Exists(bundled) ? bundled : adjacent;
    }

    public static string Get(string key)
    {
        if (!_initialized) Initialize();
        return I18nManager.Instance.GetResource(key);
    }

    public static string Format(string key, params object?[] args) => string.Format(Culture, Get(key), args);
    public static string Invariant(string key)
    {
        if (!_initialized) Initialize();
        return I18nManager.Instance.GetResource(key, "zh-CN");
    }

    /// <summary>仅翻译既有内置分类的显示名称，数据库内容保持原样。</summary>
    public static string Category(string name)
    {
        foreach (var key in CategoryKeys)
            if (I18nManager.Instance.GetResource(key, "zh-CN") == name || I18nManager.Instance.GetResource(key, "en-US") == name)
                return Get(key);
        return name;
    }

    private static readonly string[] CategoryKeys =
    [
        Language.CategoryFood, Language.CategoryTransport, Language.CategoryDaily, Language.CategoryHousing,
        Language.CategoryMedical, Language.CategoryEducation, Language.CategoryEntertainment, Language.CategoryGifts,
        Language.CategoryClothing, Language.CategoryOtherExpense, Language.CategorySalary, Language.CategoryBonus,
        Language.CategoryAllowance, Language.CategoryInterest, Language.CategoryInvestment, Language.CategoryOtherIncome
    ];
}
