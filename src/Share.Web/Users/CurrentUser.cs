namespace Share.Web.Users;

// The user for this request, remembered in a cookie set on the demo controls page.
// With no request (console commands, seeding) or no cookie, it is the central admin.
public class CurrentUser(IHttpContextAccessor http)
{
    public const string CookieName = "share-demo-user";

    public DemoUser User => DemoUser.Find(http.HttpContext?.Request.Cookies[CookieName]) ?? DemoUser.Default;

    public void SignInAs(DemoUser user) =>
        http.HttpContext!.Response.Cookies.Append(CookieName, user.Key,
            new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
}
