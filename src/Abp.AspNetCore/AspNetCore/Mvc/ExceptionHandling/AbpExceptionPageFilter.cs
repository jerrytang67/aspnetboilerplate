using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Results;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Events.Bus;
using Abp.Events.Bus.Exceptions;
using Abp.Logging;
using Abp.Reflection;
using Abp.Runtime;
using Abp.Runtime.Validation;
using Abp.Web.Configuration;
using Abp.Web.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Abp.AspNetCore.Mvc.ExceptionHandling;

public class AbpExceptionPageFilter(
    IErrorInfoBuilder errorInfoBuilder,
    IAbpAspNetCoreConfiguration configuration,
    IAbpWebCommonModuleConfiguration abpWebCommonModuleConfiguration,
    ILogger<AbpExceptionPageFilter> logger,
    IEventBus eventBus)
    : IAsyncPageFilter, ITransientDependency {
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) {
        return Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next) {
        if (context.HandlerMethod == null) {
            await next();
            return;
        }

        var pageHandlerExecutedContext = await next();

        if (pageHandlerExecutedContext.Exception == null) {
            return;
        }

        var wrapResultAttribute = ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault(
            context.HandlerMethod.MethodInfo,
            configuration.DefaultWrapResultAttribute
        );

        if (wrapResultAttribute.LogError) {
            LogHelper.LogException(logger, pageHandlerExecutedContext.Exception);
        }

        HandleAndWrapException(pageHandlerExecutedContext, wrapResultAttribute);
    }

    protected virtual void HandleAndWrapException(PageHandlerExecutedContext context,
        WrapResultAttribute wrapResultAttribute) {
        if (!ActionResultHelper.IsObjectResult(context.HandlerMethod.MethodInfo.ReturnType)) {
            return;
        }

        var displayUrl = context.HttpContext.Request.GetDisplayUrl();
        if (abpWebCommonModuleConfiguration.WrapResultFilters.HasFilterForWrapOnError(displayUrl,
                out var wrapOnError)) {
            context.HttpContext.Response.StatusCode = GetStatusCode(context, wrapOnError);

            if (!wrapOnError) {
                return;
            }

            HandleError(context);
            return;
        }

        context.HttpContext.Response.StatusCode = GetStatusCode(context, wrapResultAttribute.WrapOnError);

        if (!wrapResultAttribute.WrapOnError) {
            return;
        }

        HandleError(context);
    }

    private void HandleError(PageHandlerExecutedContext context) {
        context.Result = new ObjectResult(
            new AjaxResponse(
                errorInfoBuilder.BuildForException(context.Exception),
                context.Exception is AbpAuthorizationException
            )
        );

        eventBus.Trigger(this, new AbpHandledExceptionData(context.Exception));

        context.Exception = null; // Handled!
    }

    protected virtual int GetStatusCode(PageHandlerExecutedContext context, bool wrapOnError) {
        if (context.Exception is AbpAuthorizationException) {
            return context.HttpContext.User.Identity.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;
        }

        if (context.Exception is AbpValidationException) {
            return (int)HttpStatusCode.BadRequest;
        }

        if (context.Exception is EntityNotFoundException) {
            return (int)HttpStatusCode.NotFound;
        }

        if (wrapOnError) {
            return (int)HttpStatusCode.InternalServerError;
        }

        return context.HttpContext.Response.StatusCode;
    }
}