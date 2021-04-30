/*
 The MIT License (MIT)
 
Copyright (c) 2018 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace MipSdkFileApiDotNet
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private static string aadInstance = Utilities.EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];        
        private static string thumbprint = ConfigurationManager.AppSettings["ida:Thumbprint"];
        private static readonly bool doCertAuth = Convert.ToBoolean(ConfigurationManager.AppSettings["ida:DoCertAuth"]);
        private static readonly string clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];        
        
        private string authority = aadInstance + tenantId;
        
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            IConfidentialClientApplication _app;

            if (doCertAuth)
            {
                app.UseOpenIdConnectAuthentication(
                    new OpenIdConnectAuthenticationOptions
                    {
                        ClientId = clientId,
                        Authority = authority,
                        TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                        {
                            SaveSigninToken = true
                        },
                        PostLogoutRedirectUri = postLogoutRedirectUri,
                        Notifications = new OpenIdConnectAuthenticationNotifications()
                        {
                            //
                            // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                            //
                            AuthorizationCodeReceived = (context) =>
                                {
                                    var code = context.Code;
                                    string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                                    var request = HttpContext.Current.Request;
                                    var currentUri = "https://localhost:44376/";

                                    _app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                .WithRedirectUri(currentUri)
                                                .WithAuthority(authority)
                                                .WithCertificate(Utilities.ReadCertificateFromStore(thumbprint))
                                                .Build();

                                    var resource = clientId;                                    

                                     // Append .default to the resource passed in to AcquireToken().
                                    List<string> scopes = new List<string>() { resource[resource.Length - 1].Equals('/') ? $"{resource}.default" : $"{resource}/.default" };

                                    _app.AcquireTokenByAuthorizationCode(scopes, code)
                                        .ExecuteAsync()
                                        .GetAwaiter()
                                        .GetResult();
                                                                                                            
                                    return Task.FromResult(0);
                                }
                        }
                    }
                    );
            }

            else
            {
                app.UseOpenIdConnectAuthentication(
                    new OpenIdConnectAuthenticationOptions
                    {
                        ClientId = clientId,
                        Authority = authority,
                        TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                        {
                            SaveSigninToken = true
                        },
                        PostLogoutRedirectUri = postLogoutRedirectUri,
                        Notifications = new OpenIdConnectAuthenticationNotifications()
                        {
                            //
                            // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                            //
                            AuthorizationCodeReceived = (context) =>
                            {
                                var code = context.Code;
                                string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                                var currentUri = "https://localhost:44376/";

                                _app = ConfidentialClientApplicationBuilder.Create(clientId)
                                            .WithRedirectUri(currentUri)
                                            .WithAuthority(authority)
                                            .WithClientSecret(clientSecret)
                                            .Build();


                                // Append .default to the resource passed in to AcquireToken().                                
                                List<string> scopes = new List<string>() { OpenIdConnectScope.OpenIdProfile };
                                _app.AcquireTokenByAuthorizationCode(scopes, code)
                                    .ExecuteAsync()
                                    .GetAwaiter()
                                    .GetResult();

                                return Task.FromResult(0);
                            }
                        }
                    }
                    );
            }

            // This makes any middleware defined above this line run before the Authorization rule is applied in web.config.
            app.UseStageMarker(PipelineStage.Authenticate);
        }                   
    }
}
