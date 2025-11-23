# Autofac Migration - Debug Logging Guide

## 概述

为了帮助定位 `ConfigureServices` 阶段意外触发容器构建的问题，我们添加了详细的调试日志功能。

## 已实现的功能

### 1. 容器构建追踪

当容器被构建时，系统会自动输出：

```
=== CONTAINER BUILDING ===
Container is being built at:
[完整的调用栈]
=========================
```

### 2. 自动构建检测

当 `Resolve` 方法在容器构建之前被调用时，系统会输出：

```
=== AUTO-BUILDING CONTAINER ===
Container is being auto-built because Resolve<TypeName>() was called before BuildContainer().
Call stack:
[完整的调用栈]
================================
```

### 3. 模块追踪

每个模块的 `ConfigureServices` 调用都会被记录：

```
>>> ConfigureServices: ModuleName
...
<<< ConfigureServices completed: ModuleName
```

### 4. 增强的错误消息

当模块在 `ConfigureServices` 中触发容器构建时，会抛出详细的异常：

```
Module ModuleName.ConfigureServices() triggered container building!
ConfigureServices should only register services, not resolve them.

This usually happens when:
1. Calling IocManager.Resolve() in ConfigureServices
2. Accessing properties that lazy-resolve services
3. Calling Configuration.Get<T>() that resolves services

Check the debug output above to see the exact call stack.
```

## 使用示例

### 问题场景

运行应用时出现错误：

```
Unhandled exception. Abp.AbpException: Module AbpWebCommonModule.ConfigureServices() triggered container building!
```

### 调试步骤

1. **查看控制台输出**，找到 `=== AUTO-BUILDING CONTAINER ===` 部分
2. **查看调用栈**，找到触发 Resolve 的具体位置
3. **定位问题代码**

示例调用栈：

```
=== AUTO-BUILDING CONTAINER ===
Container is being auto-built because Resolve<IAbpWebCommonModuleConfiguration>() was called before BuildContainer().
Call stack:
   at Abp.Dependency.IocManager.Resolve[T]()
   at Abp.Configuration.Startup.AbpStartupConfiguration.Get[T]()
   at Abp.Configuration.Startup.AbpWebConfigurationExtensions.AbpWebCommon(IModuleConfigurations configurations)
   at Abp.Web.AbpWebCommonModule.ConfigureServices() in ...\AbpWebCommonModule.cs:line 46
```

从这个栈可以看出：
- **问题位置**：`AbpWebCommonModule.cs` 第 46 行
- **问题原因**：调用了 `Configuration.Modules.AbpWebCommon()`，这会触发 `Configuration.Get<T>()`，进而触发 `IocManager.Resolve<T>()`

### 修复方案

将触发服务解析的代码从 `ConfigureServices()` 移到 `Initialize()` 方法中。

**修复前：**

```csharp
public class AbpWebCommonModule : AbpModule
{
    public override void ConfigureServices()
    {
        // 服务注册
        IocManager.Register<IMyService, MyService>();

        // ❌ 错误：触发服务解析
        Configuration.Modules.AbpWebCommon().ApiProxyScripting.Generators[...] = ...;
    }

    public override void Initialize()
    {
    }
}
```

**修复后：**

```csharp
public class AbpWebCommonModule : AbpModule
{
    public override void ConfigureServices()
    {
        // 服务注册
        IocManager.Register<IMyService, MyService>();
    }

    public override void Initialize()
    {
        // ✅ 正确：在容器构建后使用服务
        Configuration.Modules.AbpWebCommon().ApiProxyScripting.Generators[...] = ...;
    }
}
```

## 常见问题模式

### 模式 1：访问 Configuration.Modules.XYZ()

**问题代码：**
```csharp
public override void ConfigureServices()
{
    // ❌ 触发容器构建
    Configuration.Modules.AbpWebCommon().Setting = value;
}
```

**原因**：`Configuration.Modules.XYZ()` 方法内部调用 `Configuration.Get<T>()`，这会尝试从容器中解析服务。

**解决方案**：移到 `Initialize()` 方法中

### 模式 2：使用 IocManager.Using()

**问题代码：**
```csharp
public override void ConfigureServices()
{
    // ❌ 触发容器构建
    IocManager.Using<IOptions<T>>(options => {
        // 配置 options
    });
}
```

**原因**：`IocManager.Using<T>()` 会调用 `IocManager.Resolve<T>()`。

**解决方案**：移到 `Initialize()` 方法中

### 模式 3：直接调用 IocManager.Resolve()

**问题代码：**
```csharp
public override void ConfigureServices()
{
    // ❌ 触发容器构建
    var service = IocManager.Resolve<IMyService>();
    service.DoSomething();
}
```

**原因**：直接调用 `Resolve()` 方法。

**解决方案**：移到 `Initialize()` 方法中

## 最佳实践

### ConfigureServices() 方法

**应该做的事：**
- ✅ 注册服务 (`IocManager.Register()`)
- ✅ 注册程序集 (`IocManager.RegisterAssemblyByConvention()`)
- ✅ 替换服务 (`Configuration.ReplaceService()`)
- ✅ 添加简单的配置项（不触发服务解析的配置）
- ✅ 添加本地化源 (`Configuration.Localization.Sources.Add()`)

**不应该做的事：**
- ❌ 调用 `IocManager.Resolve()`
- ❌ 调用 `IocManager.Using()`
- ❌ 访问 `Configuration.Modules.XYZ()`（如果它会触发服务解析）
- ❌ 访问 `Configuration.Get<T>()`
- ❌ 任何会触发服务解析的操作

### Initialize() 方法

**应该做的事：**
- ✅ 解析服务 (`IocManager.Resolve()`)
- ✅ 使用服务初始化应用 (`IocManager.Using()`)
- ✅ 配置已注册的服务（通过 `Configuration.Modules.XYZ()`）
- ✅ 访问需要服务的配置对象

**不应该做的事：**
- ❌ 注册新服务（容器已经构建，无法再注册）

## 修复的模块示例

### 1. AbpWebCommonModule

**问题：**
```csharp
public override void ConfigureServices()
{
    // ...注册服务...

    // ❌ 第 46 行触发容器构建
    Configuration.Modules.AbpWebCommon().ApiProxyScripting.Generators[JQueryProxyScriptGenerator.Name] = typeof(JQueryProxyScriptGenerator);
}
```

**修复：**
```csharp
public override void ConfigureServices()
{
    // ...注册服务...
}

public override void Initialize()
{
    // ✅ 移到 Initialize 中
    Configuration.Modules.AbpWebCommon().ApiProxyScripting.Generators[JQueryProxyScriptGenerator.Name] = typeof(JQueryProxyScriptGenerator);
}
```

### 2. AbpAspNetCoreModule

**问题：**
```csharp
public override void ConfigureServices()
{
    // ...注册服务...

    // ❌ 触发容器构建
    ConfigureAntiforgery();
    Configuration.Modules.AbpAspNetCore().FormBodyBindingIgnoredTypes.Add(typeof(IFormFile));
    Configuration.MultiTenancy.Resolvers.Add<DomainTenantResolveContributor>();
    // ...
}

private void ConfigureAntiforgery()
{
    // ❌ 触发 Resolve
    IocManager.Using<IOptions<AntiforgeryOptions>>(options => {
        options.Value.HeaderName = Configuration.Modules.AbpWebCommon().AntiForgery.TokenHeaderName;
    });
}
```

**修复：**
```csharp
public override void ConfigureServices()
{
    // ...注册服务...
}

public override void Initialize()
{
    // ✅ 移到 Initialize 中
    AddApplicationParts();
    ConfigureAntiforgery();
    Configuration.Modules.AbpAspNetCore().FormBodyBindingIgnoredTypes.Add(typeof(IFormFile));
    Configuration.MultiTenancy.Resolvers.Add<DomainTenantResolveContributor>();
    Configuration.MultiTenancy.Resolvers.Add<HttpHeaderTenantResolveContributor>();
    Configuration.MultiTenancy.Resolvers.Add<HttpCookieTenantResolveContributor>();
    Configuration.Caching.Configure(GetScriptsResponsePerUserCache.CacheName, cache => {
        cache.DefaultSlidingExpireTime = TimeSpan.FromMinutes(30);
    });
}
```

## 代码位置

调试日志相关代码位于：

1. **IocManager.cs** (`src/Abp/Dependency/IocManager.cs`)
   - 第 95-113 行：`BuildContainer()` 方法的日志
   - 第 328-343 行：`Resolve<T>()` 方法的日志
   - 其他 Resolve 重载方法也有类似的日志

2. **AbpModuleManager.cs** (`src/Abp/Modules/AbpModuleManager.cs`)
   - 第 89-121 行：模块 ConfigureServices 追踪和检测

## 下一步

使用这个调试功能，你可以：

1. ✅ 快速定位触发容器构建的具体代码行
2. ✅ 查看完整的调用栈，理解问题的根本原因
3. ✅ 按照修复模式，将问题代码移到正确的位置
4. ✅ 逐个修复所有模块中的类似问题

## 总结

调试日志功能提供了：

- 📍 **精确的问题定位**：显示触发问题的具体文件和行号
- 📊 **完整的调用栈**：理解问题的完整调用路径
- 💡 **清晰的指导**：错误消息中包含常见原因和修复建议
- 🔍 **实时追踪**：显示每个模块的 ConfigureServices 执行过程

使用这个工具，你可以快速识别和修复 Autofac 迁移过程中的所有容器构建问题。
