using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Pages.MappingUi
{
    public class FileUploadModel : PageModel
    {
        [BindProperty]
        public IFormFile SourceJsonData { get; set; }
        [BindProperty]
        public IFormFile SourceJsonSchema { get; set; }
        [BindProperty]
        public IFormFile TargetJsonSchema { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SourceJsonData != null && SourceJsonSchema != null && TargetJsonSchema != null)
            {
                var sourceData = await ReadAsStringAsync(SourceJsonData);
                var sourceSchema = await ReadAsStringAsync(SourceJsonSchema);
                var targetSchema = await ReadAsStringAsync(TargetJsonSchema);

                // store the JSON strings in TempData to pass to the mapping page
                TempData["SourceData"] = sourceData;
                TempData["SourceSchema"] = sourceSchema;
                TempData["TargetSchema"] = targetSchema;

                return RedirectToPage("MapFields");
            }

            return Page();
        }

        private async Task<string> ReadAsStringAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}