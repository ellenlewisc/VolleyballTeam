using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace VolleyballTeam.Web.Email;

public class RazorViewRenderer(
    IRazorViewEngine viewEngine,
    ITempDataProvider tempDataProvider,
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider)
{
    public async Task<string> RenderAsync<TModel>(string viewPath, TModel model)
    {
        var httpContext = httpContextAccessor.HttpContext ?? new DefaultHttpContext { RequestServices = serviceProvider };
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        var result = viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: true);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Email view '{viewPath}' was not found.");
        }

        await using var output = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            result.View,
            new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
            new TempDataDictionary(httpContext, tempDataProvider),
            output,
            new HtmlHelperOptions());

        await result.View.RenderAsync(viewContext);
        return output.ToString();
    }
}
