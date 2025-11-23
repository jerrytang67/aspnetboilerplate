using Abp.Application.Services.Dto;

namespace AbpAspNetCoreDemo.Core.Application.Dtos;

public class ProductDto : EntityDto
{
    public string Name { get; set; }

    public float Price { get; set; }
}