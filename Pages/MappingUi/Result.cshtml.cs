using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestProject.Pages.MappingUi
{
    public class ResultModel : PageModel
    {
            public string JsonOutput { get; set; }

            public void OnGet()
            {
                JsonOutput = TempData["OutputJson"] as string;
            }


    }
}
