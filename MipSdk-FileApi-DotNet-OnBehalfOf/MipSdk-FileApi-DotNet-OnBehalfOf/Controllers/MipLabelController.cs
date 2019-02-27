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
using System.Linq;
using System.Net;
using System.Net.Http;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Claims;
using System.Web.Http;

namespace MipSdkFileApiDotNet.Controllers
{
    [Authorize]
    public class MipLabelController : ApiController
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientID"];        
        private static string appName = ConfigurationManager.AppSettings["ApplicationName"];
        private static string appVersion = ConfigurationManager.AppSettings["ApplicationVersion"];
        private FileApi _fileApi;

        // Initialize FileApi.
        public MipLabelController()
        {            
            _fileApi = new FileApi(clientId, appName, appVersion, ClaimsPrincipal.Current);
        }

        /// <summary>
        /// Uses custom FileApi implementation to get all available labels.
        /// </summary>
        /// <returns>List<Models.Label></returns>
        public List<Models.Label>GetAllLabels()
        {
            try
            {
                return _fileApi.ListAllLabels();
            }
            
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Uses custom FileApi implementation to apply a label to a stream or file. 
        /// Writes result to an output stream.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="fileName"></param>
        /// <param name="labelId"></param>
        /// <param name="outputStream"></param>
        /// <returns></returns>
        public bool ApplyLabel(Stream inputStream, string fileName, string labelId, Stream outputStream)
        {
            try
            {
                // Provide a stream and filename. Filename is used to generate audit events.
                var result = _fileApi.ApplyLabel(inputStream, outputStream, fileName, labelId, "");

                if(!result)
                {
                    throw new Exception("Failed To Apply Label");
                }

                return result;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
