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
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;

namespace MipSdkFileApiDotNet
{
    public class FileApi
    {

        // This is the location used to store MIP SDK state information and logs.
        private static string mipData = ConfigurationManager.AppSettings["MipData"];
        private string mipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mipData);

        internal static readonly FileProfileFactory FileProfileFactory = new FileProfileFactory();
        private readonly ApplicationInfo _appInfo;        
        private AuthDelegateImplementation _authDelegate;

        private IFileProfile _fileProfile;
        private IFileEngine _fileEngine;
        
        /// <summary>
        /// Constructor FileApi object using clientId (from Azure AD), application friendly name, and ClaimsPrincipal representing the user
        /// </summary>
        /// <param name="clientId">Client is the Application ID displayed in the Azure AD App Registration Portal</param>
        /// <param name="applicationName">The application friendly name</param>
        /// <param name="claimsPrincipal">ClaimsPrincipal representing the authenticated user</param>
        public FileApi(string clientId, string applicationName, ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                // Store ApplicationInfo and ClaimsPrincipal for SDK operations.
                _appInfo = new ApplicationInfo()
                {
                    ApplicationId = clientId,
                    ApplicationName = applicationName
                };
                
                // Initialize new AuthDelegate providing the claimsprincipal.
                _authDelegate = new AuthDelegateImplementation(claimsPrincipal);

                // Set path to bins folder.
                var path = Path.Combine(
                        Directory.GetParent(Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath)).FullName,
                        Environment.Is64BitProcess ? "binsx64" : "binsx86");

                // Use custom UnsafeKernel32NativeMethods class to configure managed to unmanaged marshalling.
                Utilities.UnsafeKernel32NativeMethods.SetDllDirectory(path);
                
                // Initialize FileProfileFactory.
                FileProfileFactory.Initialize();

                // Call CreateFileProfile. Result is stored in global.
                CreateFileProfile();

                // Call CreateFileEngine, providing the user UPN, null client data, and locale.
                CreateFileEngine(ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value, "", "en-US");
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new IFileProfile object and stores in private _fileProfile.
        /// </summary>
        private void CreateFileProfile()
        {
            try
            {                
                var profileSettings = new FileProfileSettings(mipPath, false, _authDelegate, new ConsentDelegateImplementation(), _appInfo, LogLevel.Trace);
                _fileProfile = Task.Run(async () => await new FileProfileFactory().LoadAsync(profileSettings)).Result;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Initializes a new IFileEngine using the provided username, custom client data string, and locale.
        /// Stores result in _fileEngine.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="clientData"></param>
        /// <param name="locale"></param>
        private void CreateFileEngine(string username, string clientData, string locale)
        {
            try
            {
                var engineSettings = new FileEngineSettings(username, clientData, locale);
                engineSettings.ProtectionCloudEndpointBaseUrl = "https://api.aadrm.com";
                _fileEngine = Task.Run(async () => await _fileProfile.AddEngineAsync(engineSettings)).Result;                
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates an IFileHandler for the specified file or stream.
        /// </summary>
        /// <param name="stream">Can be null. If null, fileName must be the full path to the file.</param>
        /// <param name="fileName">File name or full path. Should be full path is stream is null</param>
        /// <returns>IFileHandler</returns>
        private IFileHandler CreateFileHandler(Stream stream, string fileName)
        {
            IFileHandler handler;

            try
            {
                if (stream != null)
                    handler = Task.Run(async () => await _fileEngine.CreateFileHandlerAsync(stream, fileName, fileName, ContentState.Motion, true)).Result;
                else
                    handler = Task.Run(async () => await _fileEngine.CreateFileHandlerAsync(fileName, fileName, ContentState.Motion, true)).Result;

                return handler;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Applies the specified label to the provided file or stream.
        /// Justification message may be required if downgrading or removing label.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="outputStream"></param>
        /// <param name="fileName"></param>
        /// <param name="labelId"></param>
        /// <param name="justificationMessage"></param>
        /// <returns></returns>
        public bool ApplyLabel(Stream stream, Stream outputStream, string fileName, string labelId, string justificationMessage)
        {
            IFileHandler handler;
                        
            try
            {
                // Try to create an IFileHandler using private CreateFileHandler().
                if (stream != null)
                {
                    handler = CreateFileHandler(stream, fileName);
                }
                
                // Try to create an IFileHandler using private CreateFileHandler().
                else
                {
                    handler = CreateFileHandler(null, fileName);
                }

                // Applying a label requires LabelingOptions. Hard coded values here, but could be provided by user. 
                LabelingOptions labelingOptions = new LabelingOptions()
                {
                    JustificationMessage = justificationMessage,
                    ActionSource = ActionSource.Manual,
                    AssignmentMethod = AssignmentMethod.Standard,
                    ExtendedProperties = new List<KeyValuePair<string, string>>()
                };

                // Set the label on the input stream or file.
                handler.SetLabel(labelId, labelingOptions);

                // Call CommitAsync to write result to output stream. 
                // Returns a bool to indicate true or false.
                var result = Task.Run(async () => await handler.CommitAsync(outputStream)).Result;
                return result;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Calls _fileEngine to list all available labels, iterates through list and puts in List<Models.Label>
        /// </summary>
        /// <returns>List<Models.Label></returns>
        public List<Models.Label> ListAllLabels()
        {
            try
            {
                var labels = _fileEngine.ListSensitivityLabels();
                var returnLabels = new List<Models.Label>();

                foreach (var label in labels)
                {
                    var _label = new Models.Label()
                    {
                        Name = label.Name,
                        Id = label.Id,
                        Description = label.Description,
                        Sensitivity = label.Sensitivity
                    };

                    _label.Children = new List<Models.Label>();

                    // If the label has an children, iterate through each. 
                    if (label.Children.Count > 0)
                    {
                        foreach (var child in label.Children)
                        {
                            var _child = new Models.Label()
                            {
                                Name = child.Name,
                                Id = child.Id,
                                Description = child.Description,
                                Sensitivity = child.Sensitivity
                            };
                            _label.Children.Add(_child);
                        }
                    }
                    returnLabels.Add(_label);
                }

                return returnLabels;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}