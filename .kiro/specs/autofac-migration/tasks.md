# Implementation Plan

- [x] 1. Update AbpModule base class with ConfigureServices lifecycle method





  - Add virtual ConfigureServices() method to AbpModule base class
  - Mark PreInitialize() with Obsolete attribute and migration message
  - Ensure Initialize(), PostInitialize(), and Shutdown() remain unchanged
  - Update XML documentation comments to explain the two-phase lifecycle
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 1.1 Write unit tests for AbpModule lifecycle methods


  - Test that ConfigureServices has default empty implementation
  - Test that PreInitialize is marked with Obsolete attribute
  - Test that all lifecycle methods exist with correct signatures
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 2. Add container build state tracking to IocManager





  - Add IsContainerBuilt boolean property to IocManager
  - Implement BuildContainer() method that sets IsContainerBuilt flag
  - Add validation to all Register methods to check IsContainerBuilt
  - Throw clear AbpException when registration attempted after build
  - Ensure BuildContainer() can only be called once
  - _Requirements: 11.1, 11.2, 11.3, 11.4_

- [x] 2.1 Write property test for container immutability


  - **Property 4: Container immutability after build**
  - **Validates: Requirements 11.1, 11.2, 11.4**
  - Generate random service types and verify registration after build throws exception
  - Verify exception message is clear and helpful
  - _Requirements: 11.1, 11.2, 11.4_

- [x] 2.2 Write unit tests for container build state


  - Test IsContainerBuilt flag is false initially
  - Test IsContainerBuilt flag is true after BuildContainer()
  - Test BuildContainer() throws exception if called twice
  - Test registration methods throw exception after container build
  - _Requirements: 11.1, 11.3_

- [x] 3. Update AbpModuleManager with ConfigureServices orchestration





  - Add ConfigureServices() method to AbpModuleManager
  - Implement module iteration in dependency order for ConfigureServices
  - Update StartModules() to only call Initialize and PostInitialize
  - Keep PreInitialize call in StartModules for backward compatibility (temporary)
  - Add logging for ConfigureServices phase completion
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 3.1 Write property test for module dependency order


  - **Property 1: Module dependency order preservation**
  - **Validates: Requirements 2.2, 2.5**
  - Generate random module collections with dependencies
  - Verify ConfigureServices and StartModules respect dependency order
  - Verify dependencies are always called before dependents
  - _Requirements: 2.2, 2.5_

- [x] 3.2 Write property test for StartModules lifecycle invocation


  - **Property 2: StartModules lifecycle method invocation**
  - **Validates: Requirements 2.3, 2.4**
  - Generate random module collections
  - Verify StartModules calls Initialize and PostInitialize
  - Verify StartModules does not call ConfigureServices
  - _Requirements: 2.3, 2.4_

- [x] 3.3 Write unit tests for AbpModuleManager


  - Test ConfigureServices method exists
  - Test ConfigureServices calls all modules in order
  - Test StartModules calls Initialize and PostInitialize
  - Test module dependency sorting
  - _Requirements: 2.1, 2.2, 2.3, 2.5_

- [x] 4. Refactor AbpBootstrapper for two-phase initialization




  - Remove container building from Initialize() method
  - Add validation in Initialize() to check IsContainerBuilt
  - Throw clear exception if Initialize() called before container built
  - Update Initialize() to only resolve services and call StartModules
  - Ensure PlugInManager and Configuration are initialized after container build
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 4.1 Write unit tests for AbpBootstrapper

  - Test Initialize() throws exception if container not built
  - Test Initialize() resolves services correctly
  - Test Initialize() calls ModuleManager.StartModules
  - Test bootstrapper lifecycle flow
  - _Requirements: 3.5, 3.6_

- [x] 5. Update AbpServiceCollectionExtensions for Autofac integration





  - Modify AddAbp() to register core services before module loading
  - Load all modules and create AbpModuleManager
  - Call ModuleManager.ConfigureServices() before container build
  - Populate Autofac builder with ASP.NET Core services
  - Build Autofac container after all registrations
  - Return AutofacServiceProvider wrapping the built container
  - Register AbpBootstrapper and AbpModuleManager as singletons
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 5.1 Write property test for service registration completeness


  - **Property 3: Service registration before container build**
  - **Validates: Requirements 4.5**
  - Generate random service registrations
  - Verify all services registered in ConfigureServices are resolvable after build
  - Verify services can be resolved in Initialize phase
  - _Requirements: 4.5_




- [x] 5.2 Write integration tests for AddAbp flow




  - Test complete AddAbp() execution flow
  - Test services are registered before container build
  - Test container is built after all registrations
  - Test returned service provider is AutofacServiceProvider
  - Test all module services are resolvable
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 6. Checkpoint - Verify infrastructure changes





  - Ensure all tests pass
  - Verify container build state enforcement works
  - Verify module lifecycle order is correct
  - Ask user if questions arise

- [x] 7. Migrate AbpKernelModule to use ConfigureServices




  - Move conventional registrar registration from PreInitialize to ConfigureServices
  - Move IScopedIocResolver registration to ConfigureServices
  - Move IAmbientScopeProvider registration to ConfigureServices
  - Move EventTriggerAsyncBackgroundJob registration to ConfigureServices
  - Move RegisterAssemblyByConvention call to ConfigureServices
  - Move interceptor registration to ConfigureServices
  - Update Initialize() to only execute service replacement actions
  - Keep EventBusModule.Install in Initialize (requires resolved services)
  - Empty PreInitialize() method
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 7.1 Write unit tests for AbpKernelModule migration


  - Test ConfigureServices registers all required services
  - Test conventional registrars are registered
  - Test core services are registered (IScopedIocResolver, IAmbientScopeProvider)
  - Test interceptors are registered
  - Test Initialize only uses resolved services
  - _Requirements: 5.3, 5.4, 5.5, 5.6_

- [x] 8. Migrate AbpCoreModule to use ConfigureServices





  - Review current AbpCoreModule implementation
  - Move all service registrations to ConfigureServices
  - Update as Autofac module if needed
  - Ensure compatibility with new lifecycle
  - _Requirements: 5.1, 5.2_

- [x] 8.1 Write unit tests for AbpCoreModule migration


  - Test all core services are registered in ConfigureServices
  - Test services are resolvable after container build
  - _Requirements: 5.1, 5.2_

- [x] 9. Migrate AbpWebCommonModule to use ConfigureServices





  - Move IWebMultiTenancyConfiguration registration to ConfigureServices
  - Move IApiProxyScriptingConfiguration registration to ConfigureServices
  - Move IAbpAntiForgeryConfiguration registration to ConfigureServices
  - Move IWebEmbeddedResourcesConfiguration registration to ConfigureServices
  - Move IAbpWebCommonModuleConfiguration registration to ConfigureServices
  - Move IJavaScriptMinifier registration to ConfigureServices
  - Keep configuration logic in PreInitialize (uses registered services)
  - Keep localization source registration in PreInitialize
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [x] 9.1 Write unit tests for AbpWebCommonModule migration


  - Test all web configuration services are registered
  - Test IWebMultiTenancyConfiguration is resolvable
  - Test IApiProxyScriptingConfiguration is resolvable
  - Test IAbpAntiForgeryConfiguration is resolvable
  - Test IWebEmbeddedResourcesConfiguration is resolvable
  - _Requirements: 6.2, 6.3, 6.4, 6.5_

- [x] 10. Migrate AbpAspNetCoreModule to use ConfigureServices





  - Review current service registrations in PreInitialize and Initialize
  - Move all service registrations to ConfigureServices
  - Update Initialize to only use resolved services
  - Ensure ASP.NET Core integration works correctly
  - _Requirements: 6.1_

- [x] 10.1 Write integration tests for AbpAspNetCoreModule


  - Test ASP.NET Core services are integrated correctly
  - Test module services are resolvable in controllers
  - Test middleware can resolve ABP services
  - _Requirements: 6.1_

- [x] 11. Checkpoint - Verify core and web modules





  - Ensure all tests pass
  - Verify core modules work with new lifecycle
  - Verify web modules work with ASP.NET Core
  - Ask user if questions arise

- [x] 12. Migrate AbpEntityFrameworkCoreModule to use ConfigureServices





  - Move all repository registrations to ConfigureServices
  - Move DbContext configuration registrations to ConfigureServices
  - Move UnitOfWork registrations to ConfigureServices
  - Update Initialize to only perform database initialization with resolved services
  - _Requirements: 7.1, 7.2, 7.3, 7.4_


- [x] 12.1 Write property test for data access module service registration

  - **Property 5: Data access module service registration**
  - **Validates: Requirements 7.2, 7.3, 7.4**
  - Verify repository implementations are registered in ConfigureServices
  - Verify DbContext configurations are registered in ConfigureServices
  - Verify Initialize does not register services
  - _Requirements: 7.2, 7.3, 7.4_

- [x] 12.2 Write integration tests for Entity Framework Core integration

  - Test repositories are resolvable
  - Test DbContext is resolvable
  - Test database operations work correctly
  - Test UnitOfWork integration
  - _Requirements: 7.1, 7.2, 7.3_

- [x] 13. Migrate AbpAutoMapperModule to use ConfigureServices





  - Move AutoMapper configuration registration to ConfigureServices
  - Move profile registrations to ConfigureServices
  - Update Initialize to only configure AutoMapper with resolved services
  - _Requirements: 8.1, 8.5_

- [x] 14. Migrate AbpFluentValidationModule to use ConfigureServices





  - Move validator registrations to ConfigureServices
  - Move FluentValidation configuration to ConfigureServices
  - Update Initialize to only use resolved services
  - _Requirements: 8.2, 8.5_

- [x] 15. Migrate AbpRedisCacheModule to use ConfigureServices





  - Move Redis cache configuration registration to ConfigureServices
  - Move cache provider registrations to ConfigureServices
  - Update Initialize to only configure Redis with resolved services
  - _Requirements: 8.3, 8.5_

- [x] 16. Migrate AbpHtmlSanitizerModule to use ConfigureServices





  - Move HTML sanitizer service registrations to ConfigureServices
  - Update Initialize to only use resolved services
  - _Requirements: 8.4, 8.5_

- [x] 16.1 Write property test for integration module service registration


  - **Property 6: Integration module service registration**
  - **Validates: Requirements 8.5**
  - Verify integration modules register services in ConfigureServices
  - Verify Initialize does not register services
  - Verify Initialize only uses resolved services
  - _Requirements: 8.5_

- [x] 16.2 Write integration tests for integration modules


  - Test AutoMapper integration works
  - Test FluentValidation integration works
  - Test Redis cache integration works
  - Test HTML sanitizer integration works
  - _Requirements: 8.1, 8.2, 8.3, 8.4_
-

- [x] 17. Checkpoint - Verify data access and integration modules




  - Ensure all tests pass
  - Verify Entity Framework integration works
  - Verify third-party integrations work
  - Ask user if questions arise

- [x] 18. Migrate AbpZeroCoreModule to use ConfigureServices




  - Move identity service registrations to ConfigureServices
  - Move authorization service registrations to ConfigureServices
  - Move user management service registrations to ConfigureServices
  - Move role management service registrations to ConfigureServices
  - Update Initialize to only use resolved services
  - _Requirements: 9.1, 9.3, 9.4, 9.5_

- [x] 19. Migrate AbpZeroCoreEntityFrameworkCoreModule to use ConfigureServices





  - Move Zero-specific repository registrations to ConfigureServices
  - Move identity DbContext registrations to ConfigureServices
  - Update Initialize to only use resolved services
  - _Requirements: 9.2, 9.3, 9.4, 9.5_

- [x] 19.1 Write property test for Zero module service registration


  - **Property 7: Zero module service registration**
  - **Validates: Requirements 9.3, 9.4, 9.5**
  - Verify identity services are registered in ConfigureServices
  - Verify authorization services are registered in ConfigureServices
  - Verify Initialize does not register services
  - _Requirements: 9.3, 9.4, 9.5_

- [x] 19.2 Write integration tests for Zero modules


  - Test identity services are resolvable
  - Test authorization services are resolvable
  - Test user management works correctly
  - Test role management works correctly
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [x] 20. Checkpoint - Verify Zero modules





  - Ensure all tests pass
  - Verify identity and authorization work
  - Verify user and role management work
  - Ask user if questions arise

- [x] 21. Add backward compatibility support





  - Implement default ConfigureServices that does nothing
  - Add deprecation warning logging for PreInitialize usage
  - Create module analyzer to detect PreInitialize overrides
  - Add helpful error messages for common migration issues
  - _Requirements: 10.1, 10.2, 10.3_

- [x] 21.1 Write unit tests for backward compatibility


  - Test modules without ConfigureServices override work
  - Test PreInitialize obsolete attribute has correct message
  - Test modules using PreInitialize still work
  - Test deprecation warnings are logged
  - _Requirements: 10.1, 10.2, 10.3_

- [x] 22. Create migration documentation





  - Write migration guide for custom modules
  - Document the two-phase lifecycle
  - Provide code examples for common scenarios
  - Document breaking changes and migration path
  - Create troubleshooting guide
  - _Requirements: 10.4, 10.5_

- [x] 23. Migrate AbpInterceptorBase to use Autofac.Extras.DynamicProxy








  - Update using statement to use Castle.DynamicProxy from Autofac.Extras.DynamicProxy
  - Verify IAsyncInterceptor interface is compatible
  - Ensure IInvocation interface works correctly
  - Add XML documentation comments explaining the change
  - Maintain the same public API for backward compatibility
  - _Requirements: 13.1, 13.2_

- [x] 23.1 Write property test for interceptor backward compatibility





  - **Property 8: Interceptor backward compatibility**
  - **Validates: Requirements 13.2, 13.5**
  - Create test interceptors that inherit from AbpInterceptorBase
  - Verify interceptors work with both synchronous and asynchronous methods
  - Verify existing interceptor implementations continue to work
  - _Requirements: 13.2, 13.5_

- [x] 24. Update interceptor registration to use Autofac's interception mechanism









  - Review all places where interceptors are registered
  - Update registration to use EnableInterfaceInterceptors or EnableClassInterceptors
  - Ensure InterceptedBy is properly configured
  - Test that interceptors are applied correctly
  - _Requirements: 13.3, 13.4_

- [x] 24.1 Write property test for Autofac interceptor application




  - **Property 9: Interceptor application through Autofac**
  - **Validates: Requirements 13.3, 13.4**
  - Generate random service registrations with interceptors
  - Verify interceptors are properly applied when services are resolved
  - Verify interceptor methods are invoked correctly
  - _Requirements: 13.3, 13.4_

- [x] 24.2 Write integration tests for interceptor functionality




  - Test interface interception works correctly
  - Test class interception works correctly
  - Test multiple interceptors on same service
  - Test interceptor ordering
  - Test async method interception
  - _Requirements: 13.3, 13.4, 13.5_

- [x] 25. Remove Castle.Core.AsyncInterceptor dependency














  - Remove Castle.Core.AsyncInterceptor package reference from Abp.csproj
  - Verify all tests still pass
  - Verify no compilation errors
  - Update documentation to reflect the change
  - _Requirements: 13.1_

- [ ] 26. Checkpoint - Verify interceptor migration








  - Ensure all interceptor tests pass
  - Verify existing interceptors work correctly
  - Verify Autofac interception mechanism works
  - Ask user if questions arise

- [ ] 27. Final integration testing







  - Run full test suite across all modules
  - Test complete application startup flow
  - Test with sample applications
  - Verify performance is acceptable
  - Test backward compatibility scenarios
  - Test interceptor functionality end-to-end
  - ReCheck the migration documentation if update the code
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

- [ ] 28. Final checkpoint - Complete migration verification
  - Ensure all tests pass
  - Verify all modules are migrated
  - Verify interceptors are migrated
  - Verify documentation is complete
  - Ask user if questions arise
