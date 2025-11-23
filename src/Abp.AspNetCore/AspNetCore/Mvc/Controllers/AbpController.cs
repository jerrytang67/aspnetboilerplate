using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.Application.Features;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Events.Bus;
using Abp.Localization;
using Abp.Localization.Sources;
using Abp.ObjectMapping;
using Abp.Runtime.Session;
using Abp.Web.Mvc.Alerts;
using Abp.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.AspNetCore.Mvc.Controllers;

/// <summary>
/// Base class for all MVC Controllers in Abp system.
/// </summary>
public abstract class AbpController : Controller, ITransientDependency
{
    /// <summary>
    /// Gets current session information.
    /// </summary>
    private IAbpSession _abpSession;

    public IAbpSession AbpSession
    {
        get => _abpSession ??= LazyGetService(NullAbpSession.Instance);
        set => _abpSession = value;
    }

    /// <summary>
    /// Gets the event bus.
    /// </summary>
    public IEventBus EventBus { get; set; }

    /// <summary>
    /// Reference to the permission manager.
    /// </summary>
    public IPermissionManager PermissionManager { get; set; }

    /// <summary>
    /// Reference to the setting manager.
    /// </summary>
    public ISettingManager SettingManager { get; set; }

    /// <summary>
    /// Reference to the permission checker.
    /// </summary>
    public IPermissionChecker PermissionChecker { protected get; set; }

    /// <summary>
    /// Reference to the feature manager.
    /// </summary>
    public IFeatureManager FeatureManager { protected get; set; }

    /// <summary>
    /// Reference to the permission checker.
    /// </summary>
    public IFeatureChecker FeatureChecker { protected get; set; }

    /// <summary>
    /// Reference to the object to object mapper.
    /// </summary>
    private IObjectMapper _objectMapper;

    public IObjectMapper ObjectMapper
    {
        get => _objectMapper ??= LazyGetService(NullObjectMapper.Instance);
        set => _objectMapper = value;
    }

    /// <summary>
    /// Reference to the localization manager.
    /// </summary>
    private ILocalizationManager _localizationManager;

    public ILocalizationManager LocalizationManager
    {
        protected get => _localizationManager ??= LazyGetService(NullLocalizationManager.Instance);
        set => _localizationManager = value;
    }

    /// <summary>
    /// Gets/sets name of the localization source that is used in this application service.
    /// It must be set in order to use <see cref="L(string)"/> and <see cref="L(string,CultureInfo)"/> methods.
    /// </summary>
    protected string LocalizationSourceName { get; set; }

    /// <summary>
    /// Gets localization source.
    /// It's valid if <see cref="LocalizationSourceName"/> is set.
    /// </summary>
    protected ILocalizationSource LocalizationSource
    {
        get
        {
            if (LocalizationSourceName == null)
            {
                throw new AbpException("Must set LocalizationSourceName before, in order to get LocalizationSource");
            }

            if (_localizationSource == null || _localizationSource.Name != LocalizationSourceName)
            {
                _localizationSource = LocalizationManager.GetSource(LocalizationSourceName);
            }

            return _localizationSource;
        }
    }

    private ILocalizationSource _localizationSource;

    /// <summary>
    /// Reference to the logger to write logs.
    /// </summary>
    private ILogger _logger;

    public ILogger Logger
    {
        get => _logger ??= LazyGetService(
            HttpContext?.RequestServices
                ?.GetService<ILoggerFactory>()
                ?.CreateLogger(GetType().FullName ?? nameof(AbpController))
            ?? NullLogger.Instance
        );
        set => _logger = value;
    }

    /// <summary>
    /// Reference to <see cref="IUnitOfWorkManager"/>.
    /// </summary>
    public IUnitOfWorkManager UnitOfWorkManager
    {
        get
        {
            if (_unitOfWorkManager == null)
            {
                throw new AbpException("Must set UnitOfWorkManager before use it.");
            }

            return _unitOfWorkManager;
        }
        set { _unitOfWorkManager = value; }
    }

    private IAlertManager _alertManager;

    public IAlertManager AlertManager
    {
        get => _alertManager ??= LazyGetRequiredService<IAlertManager>();
        set => _alertManager = value;
    }

    public AlertList Alerts => AlertManager.Alerts;

    private IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// Gets current unit of work.
    /// </summary>
    protected IActiveUnitOfWork CurrentUnitOfWork { get { return UnitOfWorkManager.Current; } }

    protected T LazyGetRequiredService<T>()
    {
        if (HttpContext?.RequestServices == null)
        {
            throw new AbpException($"HttpContext.RequestServices is not available to resolve {typeof(T).FullName}.");
        }

        return HttpContext.RequestServices.GetRequiredService<T>();
    }

    protected T LazyGetService<T>(T defaultValue)
    {
        if (HttpContext?.RequestServices == null)
        {
            return defaultValue;
        }

        return HttpContext.RequestServices.GetService<T>() ?? defaultValue;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AbpController()
    {
        AbpSession = NullAbpSession.Instance;
        Logger = NullLogger.Instance;
        LocalizationManager = NullLocalizationManager.Instance;
        PermissionChecker = NullPermissionChecker.Instance;
        EventBus = NullEventBus.Instance;
        ObjectMapper = NullObjectMapper.Instance;
    }

    /// <summary>
    /// Gets localized string for given key name and current language.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <returns>Localized string</returns>
    protected virtual string L(string name)
    {
        return LocalizationSource.GetString(name);
    }

    /// <summary>
    /// Gets localized string for given key name and current language with formatting strings.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized string</returns>
    protected virtual string L(string name, params object[] args)
    {
        return LocalizationSource.GetString(name, args);
    }

    /// <summary>
    /// Gets localized string for given key name and specified culture information.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="culture">culture information</param>
    /// <returns>Localized string</returns>
    protected virtual string L(string name, CultureInfo culture)
    {
        return LocalizationSource.GetString(name, culture);
    }

    /// <summary>
    /// Gets localized string for given key name and current language with formatting strings.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="culture">culture information</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized string</returns>
    protected virtual string L(string name, CultureInfo culture, params object[] args)
    {
        return LocalizationSource.GetString(name, culture, args);
    }

    /// <summary>
    /// Checks if current user is granted for a permission.
    /// </summary>
    /// <param name="permissionName">Name of the permission</param>
    protected Task<bool> IsGrantedAsync(string permissionName)
    {
        return PermissionChecker.IsGrantedAsync(permissionName);
    }

    /// <summary>
    /// Checks if current user is granted for a permission.
    /// </summary>
    /// <param name="permissionName">Name of the permission</param>
    protected bool IsGranted(string permissionName)
    {
        return PermissionChecker.IsGranted(permissionName);
    }


    /// <summary>
    /// Checks if given feature is enabled for current tenant.
    /// </summary>
    /// <param name="featureName">Name of the feature</param>
    /// <returns></returns>
    protected virtual Task<bool> IsEnabledAsync(string featureName)
    {
        return FeatureChecker.IsEnabledAsync(featureName);
    }

    /// <summary>
    /// Checks if given feature is enabled for current tenant.
    /// </summary>
    /// <param name="featureName">Name of the feature</param>
    /// <returns></returns>
    protected virtual bool IsEnabled(string featureName)
    {
        return FeatureChecker.IsEnabled(featureName);
    }
}