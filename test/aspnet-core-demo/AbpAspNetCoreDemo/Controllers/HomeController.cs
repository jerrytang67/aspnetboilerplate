using System;
using System.Collections.Generic;
using System.Globalization;
using Abp.AspNetCore;
using Abp.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AbpAspNetCoreDemo.Controllers;

public class HomeController : DemoControllerBase {
    private readonly ILocalizationManager _localizationManager;

    public HomeController(ILocalizationManager localizationManager) {
        _localizationManager = localizationManager;
        base.LocalizationSourceName = "demo";
    }

    public IActionResult Index() {
        return View();
    }

    public IActionResult About() {
        var l = _localizationManager.GetAllSources();


        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        //
        // CultureInfo.CurrentCulture = new CultureInfo("zh-Hans");
        // CultureInfo.CurrentUICulture = new CultureInfo("zh-Hans");
        ViewData["Message"] = L("AboutDescription");

        var source = _localizationManager.GetSource("demo");
        // 测试各个翻译键
        List<string> translationKeys = ["HelloWorld", "WelcomeMessage", "AboutDescription"];

        foreach (var key in translationKeys) {
            var translation = source.GetString(key);
            // 输出翻译结果，方便调试
            Logger.LogInformation($"Key: {key}, Translation: {translation}");
            Console.WriteLine($"Key: {key}, Translation: {translation}");
        }

        return View();
    }

    public IActionResult Contact() {
        ViewData["Message"] = "Your contact page.";

        return View();
    }

    public IActionResult Error() {
        return View();
    }
}