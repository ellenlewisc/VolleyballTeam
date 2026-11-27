using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace VolleyballTeam.Web.TagHelpers;

[HtmlTargetElement("label", Attributes = ForAttributeName)]
public class RequiredLabelTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression? For { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Only an explicit [Required] counts; non-nullable selects and checkboxes always have a value.
        if (For?.Metadata.ValidatorMetadata.OfType<RequiredAttribute>().Any() == true)
        {
            output.PostContent.AppendHtml("<span class=\"required-mark\" aria-hidden=\"true\"> *</span>");
        }
    }
}
