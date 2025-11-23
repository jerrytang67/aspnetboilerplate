# Requirements Document

## Introduction

This document specifies the requirements for migrating ASP.NET Boilerplate from Castle Windsor to Autofac as the dependency injection container. The migration addresses a fundamental architectural incompatibility: Castle Windsor supports dynamic registration after container construction, while Autofac requires all registrations to be completed before the container is built. This migration adopts a two-phase lifecycle model (service configuration phase and application initialization phase) similar to ABP Framework 2.0.

#[autofac-migration-architecture.md]autofac-migration-architecture.md


## Glossary

- **ABP**: ASP.NET Boilerplate framework
- **Container**: The dependency injection container (Autofac or Castle Windsor)
- **Module**: A unit of functionality in ABP that can register services and perform initialization
- **IocManager**: The ABP abstraction over the dependency injection container
- **Service Configuration Phase**: The period before container construction when services are registered
- **Application Initialization Phase**: The period after container construction when registered services are used
- **Bootstrapper**: The component responsible for initializing the ABP framework
- **Lifecycle Method**: Methods called at specific points in module initialization (PreInitialize, Initialize, PostInitialize, ConfigureServices)

## Requirements

### Requirement 1

**User Story:** As a framework developer, I want to introduce a new ConfigureServices lifecycle method to AbpModule, so that modules can register services before the container is built.

#### Acceptance Criteria

1. WHEN the AbpModule base class is modified THEN the system SHALL add a new virtual ConfigureServices method that accepts IIocManager as a parameter
2. WHEN the ConfigureServices method is added THEN the system SHALL provide a default empty implementation
3. WHEN the PreInitialize method exists THEN the system SHALL mark it with the Obsolete attribute indicating developers should use ConfigureServices for service registration
4. WHEN module lifecycle methods are defined THEN the system SHALL maintain Initialize, PostInitialize, and Shutdown methods without modification
5. WHERE backward compatibility is required THEN the system SHALL preserve all existing lifecycle methods

### Requirement 2

**User Story:** As a framework developer, I want to modify AbpModuleManager to call ConfigureServices on all modules before container construction, so that all service registrations happen at the correct time.

#### Acceptance Criteria

1. WHEN AbpModuleManager is modified THEN the system SHALL add a new ConfigureServices method
2. WHEN the ConfigureServices method is invoked THEN the system SHALL call ConfigureServices on all loaded modules in dependency order
3. WHEN the StartModules method is invoked THEN the system SHALL only call Initialize and PostInitialize methods on modules
4. WHEN the StartModules method is invoked THEN the system SHALL not call PreInitialize methods on modules
5. WHEN modules are processed THEN the system SHALL maintain the sorted dependency order for all lifecycle method invocations

### Requirement 3

**User Story:** As a framework developer, I want to refactor AbpBootstrapper to separate service configuration from initialization, so that the container is built at the correct time.

#### Acceptance Criteria

1. WHEN AbpBootstrapper is refactored THEN the system SHALL add a new ConfigureServices method
2. WHEN the ConfigureServices method is called THEN the system SHALL register core services before calling module ConfigureServices methods
3. WHEN the ConfigureServices method is called THEN the system SHALL invoke ModuleManager.ConfigureServices
4. WHEN the ConfigureServices method is called THEN the system SHALL not build the container
5. WHEN the Initialize method is called THEN the system SHALL verify the container is built before proceeding
6. WHEN the Initialize method is called THEN the system SHALL only invoke ModuleManager.StartModules after container construction

### Requirement 4

**User Story:** As a framework developer, I want to update AbpServiceCollectionExtensions to orchestrate the two-phase initialization, so that ASP.NET Core integration works correctly with Autofac.

#### Acceptance Criteria

1. WHEN AddAbp is invoked THEN the system SHALL call AbpBootstrapper.ConfigureServices before building the container
2. WHEN AddAbp is invoked THEN the system SHALL populate the Autofac container builder with ASP.NET Core services after ConfigureServices
3. WHEN AddAbp is invoked THEN the system SHALL build the Autofac container after all registrations are complete
4. WHEN AddAbp is invoked THEN the system SHALL return an AutofacServiceProvider wrapping the built container
5. WHEN the container is built THEN the system SHALL ensure all module service registrations are included

### Requirement 5

**User Story:** As a framework developer, I want to migrate AbpKernelModule to use ConfigureServices, so that core framework services are registered before container construction.

#### Acceptance Criteria

1. WHEN AbpKernelModule is migrated THEN the system SHALL move all service registrations from PreInitialize to ConfigureServices
2. WHEN AbpKernelModule is migrated THEN the system SHALL move all service registrations from Initialize to ConfigureServices
3. WHEN AbpKernelModule ConfigureServices is called THEN the system SHALL register conventional registrars
4. WHEN AbpKernelModule ConfigureServices is called THEN the system SHALL register core services including IScopedIocResolver and IAmbientScopeProvider
5. WHEN AbpKernelModule ConfigureServices is called THEN the system SHALL register interceptors
6. WHEN AbpKernelModule Initialize is called THEN the system SHALL only execute logic that requires resolved services

### Requirement 6

**User Story:** As a framework developer, I want to migrate AbpWebCommonModule to use ConfigureServices, so that web-related services are registered before container construction.

#### Acceptance Criteria

1. WHEN AbpWebCommonModule is migrated THEN the system SHALL move service registrations from PreInitialize to ConfigureServices
2. WHEN AbpWebCommonModule ConfigureServices is called THEN the system SHALL register IWebMultiTenancyConfiguration
3. WHEN AbpWebCommonModule ConfigureServices is called THEN the system SHALL register IApiProxyScriptingConfiguration
4. WHEN AbpWebCommonModule ConfigureServices is called THEN the system SHALL register IAbpAntiForgeryConfiguration
5. WHEN AbpWebCommonModule ConfigureServices is called THEN the system SHALL register IWebEmbeddedResourcesConfiguration
6. WHEN AbpWebCommonModule PreInitialize is called THEN the system SHALL only execute configuration logic that uses already-registered services

### Requirement 7

**User Story:** As a framework developer, I want to migrate data access modules to use ConfigureServices, so that Entity Framework and other ORM integrations work correctly.

#### Acceptance Criteria

1. WHEN AbpEntityFrameworkCoreModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
2. WHEN data access modules register services THEN the system SHALL register repository implementations in ConfigureServices
3. WHEN data access modules register services THEN the system SHALL register DbContext configurations in ConfigureServices
4. WHEN data access modules initialize THEN the system SHALL only execute database initialization logic that requires resolved services

### Requirement 8

**User Story:** As a framework developer, I want to migrate integration modules to use ConfigureServices, so that third-party library integrations are registered correctly.

#### Acceptance Criteria

1. WHEN AbpAutoMapperModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
2. WHEN AbpFluentValidationModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
3. WHEN AbpRedisCacheModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
4. WHEN AbpHtmlSanitizerModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
5. WHEN integration modules initialize THEN the system SHALL only execute configuration logic that requires resolved services

### Requirement 9

**User Story:** As a framework developer, I want to migrate Zero modules to use ConfigureServices, so that identity and authorization services are registered correctly.

#### Acceptance Criteria

1. WHEN AbpZeroCoreModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
2. WHEN AbpZeroCoreEntityFrameworkCoreModule is migrated THEN the system SHALL move all service registrations to ConfigureServices
3. WHEN Zero modules register services THEN the system SHALL register identity services in ConfigureServices
4. WHEN Zero modules register services THEN the system SHALL register authorization services in ConfigureServices
5. WHEN Zero modules initialize THEN the system SHALL only execute logic that requires resolved services

### Requirement 10

**User Story:** As a framework developer, I want to ensure backward compatibility during migration, so that existing user modules continue to work during the transition period.

#### Acceptance Criteria

1. WHEN the ConfigureServices method is not overridden in a module THEN the system SHALL provide a default implementation
2. WHEN PreInitialize is marked obsolete THEN the system SHALL include a message directing developers to use ConfigureServices
3. WHEN existing modules use PreInitialize THEN the system SHALL continue to support them with a deprecation warning
4. WHEN the migration is complete THEN the system SHALL provide clear documentation on updating custom modules
5. WHEN breaking changes are introduced THEN the system SHALL document the migration path for users

### Requirement 11

**User Story:** As a framework developer, I want to validate that the container is immutable after construction, so that Autofac compatibility is guaranteed.

#### Acceptance Criteria

1. WHEN the container is built THEN the system SHALL prevent any further service registrations
2. WHEN a module attempts to register services after container construction THEN the system SHALL throw a clear exception
3. WHEN the IocManager BuildContainer method is called THEN the system SHALL set an IsContainerBuilt flag
4. WHEN service registration is attempted THEN the system SHALL check the IsContainerBuilt flag
5. WHEN the container is built THEN the system SHALL ensure all module ConfigureServices methods have been invoked

### Requirement 12

**User Story:** As a framework developer, I want comprehensive tests for the two-phase lifecycle, so that the migration is verified to work correctly.

#### Acceptance Criteria

1. WHEN tests are written THEN the system SHALL verify ConfigureServices is called before container construction
2. WHEN tests are written THEN the system SHALL verify Initialize is called after container construction
3. WHEN tests are written THEN the system SHALL verify services registered in ConfigureServices are resolvable in Initialize
4. WHEN tests are written THEN the system SHALL verify the module dependency order is maintained
5. WHEN tests are written THEN the system SHALL verify that attempting to register services after container construction fails appropriately

### Requirement 13

**User Story:** As a framework developer, I want to migrate interceptor infrastructure from Castle.DynamicProxy to Autofac.Extras.DynamicProxy, so that the framework uses a consistent interception mechanism compatible with Autofac.

#### Acceptance Criteria

1. WHEN AbpInterceptorBase is refactored THEN the system SHALL use Castle.DynamicProxy.IAsyncInterceptor from Autofac.Extras.DynamicProxy instead of Castle.Core.AsyncInterceptor
2. WHEN AbpInterceptorBase is refactored THEN the system SHALL maintain the same public API surface for backward compatibility
3. WHEN interceptor registration occurs THEN the system SHALL use Autofac's EnableInterfaceInterceptors or EnableClassInterceptors methods
4. WHEN services with interceptors are registered THEN the system SHALL ensure interceptors are properly applied through Autofac's interception mechanism
5. WHEN existing interceptor implementations are used THEN the system SHALL continue to work without modification
