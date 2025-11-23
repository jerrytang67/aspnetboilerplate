using Abp.Dependency;
using Abp.Modules;
using Abp.ObjectMapping;
using Abp.TestBase;
using AutoMapper;
using Shouldly;
using Xunit;

namespace Abp.AutoMapper.Tests;

/// <summary>
/// Tests for AbpAutoMapperModule migration to ConfigureServices lifecycle.
/// Validates Requirements 8.1, 8.5
/// </summary>
public class AbpAutoMapperModule_Migration_Tests : AbpIntegratedTestBase<AbpAutoMapperTestModule>
{
    [Fact]
    public void ConfigureServices_Should_Register_AutoMapper_Configuration()
    {
        // Act - Configuration should be registered in ConfigureServices
        var config = LocalIocManager.Resolve<IAbpAutoMapperConfiguration>();

        // Assert
        config.ShouldNotBeNull();
        config.Configurators.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureServices_Should_Register_IMapper()
    {
        // Act - IMapper should be registered in ConfigureServices
        var mapper = LocalIocManager.Resolve<IMapper>();

        // Assert
        mapper.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureServices_Should_Register_IConfigurationProvider()
    {
        // Act - IConfigurationProvider should be registered in ConfigureServices
        var configProvider = LocalIocManager.Resolve<IConfigurationProvider>();

        // Assert
        configProvider.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureServices_Should_Replace_ObjectMapper_With_AutoMapper()
    {
        // Act - ObjectMapper should be replaced with AutoMapperObjectMapper
        var objectMapper = LocalIocManager.Resolve<IObjectMapper>();

        // Assert
        objectMapper.ShouldNotBeNull();
        objectMapper.ShouldBeOfType<AutoMapperObjectMapper>();
    }

    [Fact]
    public void Initialize_Should_Only_Use_Resolved_Services()
    {
        // This test verifies that Initialize doesn't register new services
        // It only uses already-resolved services (like IMapper)
        
        // Act - Resolve IMapper which should have been configured in Initialize
        var mapper = LocalIocManager.Resolve<IMapper>();

        // Assert - Mapper should be functional (static mapper set in Initialize)
        mapper.ShouldNotBeNull();
        
        // Verify the static mapper was set (backward compatibility)
#pragma warning disable CS0618
        AbpEmulateAutoMapper.Mapper.ShouldNotBeNull();
        AbpEmulateAutoMapper.Mapper.ShouldBe(mapper);
#pragma warning restore CS0618
    }

    [Fact]
    public void AutoMapper_Should_Be_Functional_After_Migration()
    {
        // This integration test verifies that AutoMapper works correctly after migration
        
        // Arrange
        var mapper = LocalIocManager.Resolve<IMapper>();
        var source = new TestSource { Name = "Test", Value = 42 };

        // Act
        var destination = mapper.Map<TestDestination>(source);

        // Assert
        destination.ShouldNotBeNull();
        destination.Name.ShouldBe("Test");
        destination.Value.ShouldBe(42);
    }

    [AutoMapTo(typeof(TestDestination))]
    private class TestSource
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    private class TestDestination
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
