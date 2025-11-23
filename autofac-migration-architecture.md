# ASP.NET Boilerplate Autofac 迁移架构方案

## 背景

将 ASP.NET Boilerplate 从 Castle Windsor 迁移到 Autofac 时，遇到了根本性的架构问题：

- **Castle Windsor**：支持在容器构建后动态添加注册
- **Autofac**：容器构建后不可变，所有注册必须在构建前完成

## 问题分析

### 当前架构（ASP.NET Boilerplate 1代）

```
ConfigureServices() {
    AddAbp() {
        1. 创建 AbpBootstrapper
        2. 注册核心服务
        3. BuildContainer()  ← 容器在这里构建
        4. 返回 ServiceProvider
    }
}

Configure() {
    UseAbp() {
        Initialize() {
            foreach (module in modules) {
                module.PreInitialize()   ← 尝试注册服务（失败！）
                module.Initialize()       ← 尝试注册服务（失败！）
                module.PostInitialize()   ← 尝试注册服务（失败！）
            }
        }
    }
}
```

**问题**：模块在 `PreInitialize()` 和 `Initialize()` 中注册服务，但此时容器已经构建完成。

### ABP Framework 2代解决方案

```
ConfigureServices() {
    AddApplication() {
        1. 创建 AbpApplication
        2. 加载所有模块

        // 配置阶段 - 容器构建前
        foreach (module in modules) {
            module.PreConfigureServices()
            module.ConfigureServices()      ← 所有注册在这里
            module.PostConfigureServices()
        }

        3. BuildContainer()  ← 容器在所有注册完成后构建
        4. 返回 ServiceProvider
    }
}

Configure() {
    InitializeApplication() {
        // 初始化阶段 - 容器构建后
        foreach (module in modules) {
            module.OnPreApplicationInitialization()   ← 只使用服务
            module.OnApplicationInitialization()      ← 只使用服务
            module.OnPostApplicationInitialization()  ← 只使用服务
        }
    }
}
```

**关键设计原则**：
1. 配置阶段：只注册服务，不解析服务
2. 初始化阶段：只使用已注册的服务，不再注册

## 迁移方案

### 方案概述

将 ASP.NET Boilerplate 的模块生命周期改造为两阶段模式：

1. **服务配置阶段**（容器构建前）
2. **应用初始化阶段**（容器构建后）

### 详细实现步骤

#### 第一步：修改 AbpModule 基类

**文件**：`src/Abp/Modules/AbpModule.cs`

添加新的生命周期方法：

```csharp
public abstract class AbpModule
{
    // 新增：服务配置方法（容器构建前调用）
    public virtual void ConfigureServices(IIocManager iocManager)
    {
        // 默认实现：调用原有的 PreInitialize
        // 子类应该重写此方法来注册服务
    }

    // 保留但修改用途：初始化方法（容器构建后调用）
    public virtual void Initialize()
    {
        // 不再在这里注册服务
        // 只执行需要已解析服务的初始化逻辑
    }

    // 保留
    public virtual void PostInitialize() { }

    // 保留
    public virtual void Shutdown() { }

    // 废弃标记
    [Obsolete("Use ConfigureServices instead for service registration")]
    public virtual void PreInitialize() { }
}
```

#### 第二步：修改 AbpModuleManager

**文件**：`src/Abp/Modules/AbpModuleManager.cs`

添加新的配置服务方法：

```csharp
public class AbpModuleManager : IAbpModuleManager
{
    // 新增：在容器构建前调用
    public virtual void ConfigureServices()
    {
        var sortedModules = _modules.GetSortedModuleListByDependency();

        // 调用所有模块的 ConfigureServices
        foreach (var module in sortedModules)
        {
            module.Instance.ConfigureServices(IocManager);
        }
    }

    // 修改：只执行初始化逻辑，不再注册服务
    public virtual void StartModules()
    {
        var sortedModules = _modules.GetSortedModuleListByDependency();

        // 不再调用 PreInitialize（已废弃）
        sortedModules.ForEach(module => module.Instance.Initialize());
        sortedModules.ForEach(module => module.Instance.PostInitialize());
    }
}
```

#### 第三步：修改 AbpBootstrapper

**文件**：`src/Abp/AbpBootstrapper.cs`

重构初始化流程：

```csharp
public class AbpBootstrapper
{
    // 新增：配置服务（在容器构建前调用）
    public virtual void ConfigureServices()
    {
        RegisterBootstrapper();

        // 注册核心服务
        var coreModule = new AbpCoreModule();
        ((IocManager)IocManager).Builder.RegisterModule(coreModule);

        // 加载所有模块
        _moduleManager = new AbpModuleManager(IocManager, ...);
        _moduleManager.Initialize(StartupModule);

        // 调用所有模块的 ConfigureServices
        _moduleManager.ConfigureServices();

        // 此时不构建容器，由外部调用
    }

    // 修改：只执行初始化（在容器构建后调用）
    public virtual void Initialize()
    {
        // 确保容器已构建
        if (!((IocManager)IocManager).IsContainerBuilt)
        {
            ((IocManager)IocManager).BuildContainer();
        }

        // 初始化插件
        IocManager.Resolve<AbpPlugInManager>().PlugInSources.AddRange(PlugInSources);
        IocManager.Resolve<AbpStartupConfiguration>().Initialize();

        // 启动模块（只执行 Initialize 和 PostInitialize）
        _moduleManager.StartModules();
    }
}
```

#### 第四步：修改 AbpServiceCollectionExtensions

**文件**：`src/Abp.AspNetCore/AspNetCore/AbpServiceCollectionExtensions.cs`

调整 AddAbp 流程：

```csharp
public static IServiceProvider AddAbp<TStartupModule>(this IServiceCollection services, ...)
{
    var abpBootstrapper = AbpBootstrapper.Create<TStartupModule>(optionsAction);
    var iocManager = (IocManager)abpBootstrapper.IocManager;

    // 1. 配置服务（包括所有模块的 ConfigureServices）
    abpBootstrapper.ConfigureServices();

    // 2. 填充 ASP.NET Core 服务
    iocManager.Builder.Populate(services);

    // 3. 注册 bootstrapper
    iocManager.Builder.RegisterInstance(abpBootstrapper).As<AbpBootstrapper>().SingleInstance();

    // 4. 构建容器
    iocManager.BuildContainer();

    return new AutofacServiceProvider(iocManager.IocContainer);
}
```

#### 第五步：迁移现有模块

需要将现有模块的注册逻辑从 `PreInitialize()` 和 `Initialize()` 移动到 `ConfigureServices()`。

**示例：AbpKernelModule**

```csharp
public sealed class AbpKernelModule : AbpModule
{
    // 新增
    public override void ConfigureServices(IIocManager iocManager)
    {
        // 从 PreInitialize 移动过来
        iocManager.AddConventionalRegistrar(new BasicConventionalRegistrar());
        iocManager.Register<IScopedIocResolver, ScopedIocResolver>(DependencyLifeStyle.Transient);
        iocManager.Register(typeof(IAmbientScopeProvider<>), typeof(DataContextAmbientScopeProvider<>), DependencyLifeStyle.Transient);

        // 从 Initialize 移动过来
        iocManager.Register(typeof(EventTriggerAsyncBackgroundJob<>), DependencyLifeStyle.Transient);
        iocManager.RegisterAssemblyByConvention(typeof(AbpKernelModule).GetAssembly(), ...);

        // 注册拦截器
        RegisterInterceptors();
    }

    public override void Initialize()
    {
        // 只保留需要已解析服务的逻辑
        foreach (var replaceAction in ((AbpStartupConfiguration)Configuration).ServiceReplaceActions.Values)
        {
            replaceAction();
        }

        EventBusModule.Install(IocManager);
    }

    // PreInitialize 保持为空或调用 ConfigureServices
    public override void PreInitialize()
    {
        // 已废弃，配置逻辑移到 ConfigureServices
    }
}
```

**示例：AbpWebCommonModule**

```csharp
public class AbpWebCommonModule : AbpModule
{
    public override void ConfigureServices(IIocManager iocManager)
    {
        // 从 PreInitialize 移动过来
        iocManager.Register<IWebMultiTenancyConfiguration, WebMultiTenancyConfiguration>();
        iocManager.Register<IApiProxyScriptingConfiguration, ApiProxyScriptingConfiguration>();
        iocManager.Register<IAbpAntiForgeryConfiguration, AbpAntiForgeryConfiguration>();
        iocManager.Register<IWebEmbeddedResourcesConfiguration, WebEmbeddedResourcesConfiguration>();
        iocManager.Register<IAbpWebCommonModuleConfiguration, AbpWebCommonModuleConfiguration>();
        iocManager.Register<IJavaScriptMinifier, NUglifyJavaScriptMinifier>();
    }

    public override void PreInitialize()
    {
        // 只保留使用已注册服务的配置逻辑
        Configuration.Modules.AbpWebCommon().ApiProxyScripting.Generators[JQueryProxyScriptGenerator.Name] = typeof(JQueryProxyScriptGenerator);

        Configuration.Localization.Sources.Add(...);
    }

    public override void Initialize()
    {
        // 从这里移除 RegisterAssemblyByConvention，移到 ConfigureServices
    }
}
```

### 需要迁移的模块清单

以下模块需要将服务注册从 `PreInitialize()`/`Initialize()` 移动到 `ConfigureServices()`：

#### 核心模块
- [ ] `AbpKernelModule` - src/Abp/AbpKernelModule.cs
- [ ] `AbpCoreModule` - src/Abp/Dependency/Installers/AbpCoreModule.cs

#### Web 模块
- [ ] `AbpWebCommonModule` - src/Abp.Web.Common/Web/AbpWebCommonModule.cs
- [ ] `AbpAspNetCoreModule` - src/Abp.AspNetCore/AspNetCore/AbpAspNetCoreModule.cs

#### 数据访问模块
- [ ] `AbpEntityFrameworkCoreModule` - src/Abp.EntityFrameworkCore/EntityFrameworkCore/AbpEntityFrameworkCoreModule.cs

#### 其他模块
- [ ] `AbpAutoMapperModule` - src/Abp.AutoMapper/AutoMapper/AbpAutoMapperModule.cs
- [ ] `AbpHtmlSanitizerModule` - src/Abp.HtmlSanitizer/HtmlSanitizer/AbpHtmlSanitizerModule.cs
- [ ] `AbpFluentValidationModule` - src/Abp.FluentValidation/FluentValidation/AbpFluentValidationModule.cs
- [ ] `AbpRedisCacheModule` - src/Abp.RedisCache/Runtime/Caching/Redis/AbpRedisCacheModule.cs

#### Zero 模块
- [ ] `AbpZeroCoreModule` - src/Abp.Zero.Common/Zero/AbpZeroCoreModule.cs
- [ ] `AbpZeroCoreEntityFrameworkCoreModule` - src/Abp.ZeroCore.EntityFrameworkCore/Zero/EntityFrameworkCore/AbpZeroCoreEntityFrameworkCoreModule.cs

## 兼容性考虑

### 向后兼容

为了保持向后兼容，可以：

1. 保留 `PreInitialize()` 方法，但标记为 `[Obsolete]`
2. 默认 `ConfigureServices()` 实现调用 `PreInitialize()`
3. 逐步迁移各模块

### 迁移路径

1. **阶段一**：添加新的 `ConfigureServices()` 方法，修改框架流程
2. **阶段二**：迁移核心模块（AbpKernelModule, AbpWebCommonModule 等）
3. **阶段三**：迁移其他模块
4. **阶段四**：标记旧方法为废弃，更新文档

## 风险评估

### 高风险
- 大量代码需要修改
- 可能影响现有用户的自定义模块

### 中风险
- 需要更新文档和示例
- 测试覆盖需要全面更新

### 低风险
- 架构变更是必要的，长期收益明显
- ABP Framework 2代已验证此方案可行

## 预期收益

1. **完全兼容 Autofac**：不再有动态注册问题
2. **更清晰的生命周期**：配置和初始化分离
3. **更好的可测试性**：服务配置更加集中
4. **与现代 .NET 一致**：遵循 ASP.NET Core 的配置模式

## 参考资料

- ABP Framework 源码：https://github.com/abpframework/abp
- Autofac 文档：https://autofac.readthedocs.io
- ASP.NET Core DI 文档：https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
