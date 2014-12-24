using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace TwidgetApi.CustomFilters
{
    /// <summary>
    /// An Authorization filter attribute to allow access
    /// Code from:
    /// http://blogs.msdn.com/b/carlosfigueira/archive/2012/03/09/implementing-requirehttps-with-asp-net-web-api.aspx
    /// </summary>
    public class RequireHttpsAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var request = actionContext.Request;

            if (request.RequestUri.Scheme != Uri.UriSchemeHttps)
            {
                HttpResponseMessage response;
                UriBuilder uri = new UriBuilder(request.RequestUri);
                uri.Scheme = Uri.UriSchemeHttps;
                uri.Port = 443;
                string body = string.Format("<p>The resource can be found at <a href=\"{0}\">{0}</a>.</p>",
                                            uri.Uri.AbsoluteUri);

                if (request.Method.Equals(HttpMethod.Get) || request.Method.Equals(HttpMethod.Head))
                {
                    response = request.CreateResponse(HttpStatusCode.Found);
                    response.Headers.Location = uri.Uri;

                    if (request.Method.Equals(HttpMethod.Get))
                    {
                        response.Content = new StringContent(body, Encoding.UTF8, "text/html");
                    }
                }

                else
                {
                    response = request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent(body, Encoding.UTF8, "text/html");
                }

                actionContext.Response = response;
            }
        }
    }
}