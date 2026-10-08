using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Share.Web.Pages;

public class DoNotPressModel : PageModel
{
    // YouTube refuses to play embeds on pages served from a bare IP address, but plays them on localhost
    // and real hostnames. So if the app was opened at 127.0.0.1, move to localhost for this page.
    public IActionResult OnGet()
    {
        if (Request.Host.Host == "127.0.0.1")
            return Redirect($"{Request.Scheme}://localhost:{Request.Host.Port}{Request.Path}");
        return Page();
    }
}
