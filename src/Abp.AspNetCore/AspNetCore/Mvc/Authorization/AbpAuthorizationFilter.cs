using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.AspNetCore.Mvc.Results;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Events.Bus;
using Abp.Events.Bus.Exceptions;
using Abp.Web.Models;
using Abp.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Abp.AspNetCore.Mvc.Authorization;

public class AbpAuthorizationFilter(
    IAuthorizationHelper authorizationHelper,
    IErrorInfoBuilder errorInfoBuilder,
    IEventBus eventBus,
    ILogger<AbpAuthorizationFilter> logger)
    : IAsyncAuthorizationFilter, ITransientDependency {
    public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        var endpoint = context?.HttpContext?.GetEndpoint();
        // Allow Anonymous skips all authorization
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null) {
            return;
        }

        if (!context.ActionDescriptor.IsControllerAction()) {
            return;
        }

        //TODO: Avoid using try/catch, use conditional checking
        try {
            await authorizationHelper.AuthorizeAsync(
                context.ActionDescriptor.GetMethodInfo(),
                context.ActionDescriptor.GetMethodInfo().DeclaringType
            );
        }
        catch (AbpAuthorizationException ex) {
            logger.LogWarning(ex.ToString(), ex);

            await eventBus.TriggerAsync(this, new AbpHandledExceptionData(ex));

            var isAuthenticated = context.HttpContext.User.Identity.IsAuthenticated;

            if (ActionResultHelper.IsObjectResult(context.ActionDescriptor.GetMethodInfo().ReturnType)) {
                context.Result = new ObjectResult(new AjaxResponse(errorInfoBuilder.BuildForException(ex), true)) {
                    StatusCode = isAuthenticated
                        ? (int)System.Net.HttpStatusCode.Forbidden
                        : (int)System.Net.HttpStatusCode.Unauthorized
                };
            }
            else {
                if (isAuthenticated) {
                    context.Result = new ForbidResult();
                }
                else {
                    context.Result = new ChallengeResult();
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex.ToString(), ex);

            await eventBus.TriggerAsync(this, new AbpHandledExceptionData(ex));

            if (ActionResultHelper.IsObjectResult(context.ActionDescriptor.GetMethodInfo().ReturnType)) {
                context.Result = new ObjectResult(new AjaxResponse(errorInfoBuilder.BuildForException(ex))) {
                    StatusCode = (int)System.Net.HttpStatusCode.InternalServerError
                };
            }
            else {
                //TODO: How to return Error page?
                context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}