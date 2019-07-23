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
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.InformationProtection;
using MipSdkFileApiDotNet.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;

namespace MipSdkFileApiDotNet
{
    public class AuthDelegateImplementation : IAuthDelegate
    {
        private static readonly string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"]; 
        private static readonly string tenant = ConfigurationManager.AppSettings["ida:Tenant"]; 
        private static readonly string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
        private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private static readonly string thumbprint = ConfigurationManager.AppSettings["ida:Thumbprint"];
        private static readonly bool doCertAuth = Convert.ToBoolean(ConfigurationManager.AppSettings["ida:DoCertAuth"]);
        private static readonly string clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];


        private ClaimsPrincipal _claimsPrincipal;
                
        public AuthDelegateImplementation(ClaimsPrincipal claimsPrincipal)
        {
            _claimsPrincipal = claimsPrincipal;
        }

        public string AcquireToken(Identity identity, string authority, string resource, string claim)
        {            
            //Call method to get access token, providing the identity, authority, and resource.
            //Uses the claims principal provided to the contructor to get the bootstrap context
            var authResult = Task.Run(async () => await GetAccessTokenOnBehalfOfUser(authority, resource));
            return authResult.Result;
        }


        public async Task<string> GetAccessTokenOnBehalfOfUser(string authority, string resource)
        {
            AuthenticationResult result = null;
             
            if (doCertAuth)
            {
                // Read X509 cert from local store and build ClientAssertionCertificate.
                X509Certificate2 cert = Utilities.ReadCertificateFromStore(thumbprint);
                ClientAssertionCertificate certCred = new ClientAssertionCertificate(clientId, cert);

                // Store the claims identity, then read the BootstrapContext (JWT) from the identity.
                var ci = (ClaimsIdentity)_claimsPrincipal.Identity;
                string userAccessToken = (string)ci.BootstrapContext;

                // Read the UserPrincipalName from the claim, then generate a user assertion with the UPN and access token.
                string userName = _claimsPrincipal.FindFirst(ClaimTypes.Upn).Value;
                UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

                // Build a new AuthContext, then use the certificate credentials + the user assertion to acquire a token for the provided resource. 
                var authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority, new TokenCache());
                result = await authContext.AcquireTokenAsync(resource, certCred, userAssertion);
            }

            else
            {
                var ci = (ClaimsIdentity)_claimsPrincipal.Identity;
                string userAccessToken = (string)ci.BootstrapContext;

                ClientCredential clientCred = new ClientCredential(clientId, clientSecret);

                // Read the UserPrincipalName from the claim, then generate a user assertion with the UPN and access token.
                string userName = _claimsPrincipal.FindFirst(ClaimTypes.Upn).Value;
                UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);
                var authContext = new AuthenticationContext(authority, new TokenCache());
                result = await authContext.AcquireTokenAsync(resource, clientCred, userAssertion);
            }


            // Return the token to the API caller
            return (result.AccessToken);
        }      
    }
}