# Using MIP SDK with ASP.NET to Label Files on Download

- [Using MIP SDK with ASP.NET to Label Files on Download](#using-mip-sdk-with-aspnet-to-label-files-on-download)
    - [Summary](#summary)
    - [Getting Started](#getting-started)
    - [Requirements](#requirements)
        - [Ensure Labels are Configured](#ensure-labels-are-configured)
        - [Publishing Labels](#publishing-labels)
        - [Create Self-Signed Certificate](#create-self-signed-certificate)
    - [Create the Project](#create-the-project)
        - [Create the new ASP.NET Web Application](#create-the-new-aspnet-web-application)
        - [Create/Update App Registration](#createupdate-app-registration)
            - [Updating the App Registration](#updating-the-app-registration)
            - [Creating the App Registration](#creating-the-app-registration)
            - [Add the certificate credentials to the Azure AD Application](#add-the-certificate-credentials-to-the-azure-ad-application)
        - [Install NuGet Packages](#install-nuget-packages)
        - [Update Web.Config](#update-webconfig)
            - [Update/Add appSettings](#updateadd-appsettings)
            - [Update IdentityConfiguration](#update-identityconfiguration)
            - [Add Config Sections](#add-config-sections)
            - [Save Web.Config](#save-webconfig)
    - [Implement Utilities Class](#implement-utilities-class)
    - [Adding the MIP Components](#adding-the-mip-components)
        - [Add the MIP SDK Binaries to the Project](#add-the-mip-sdk-binaries-to-the-project)
        - [Implement the Consent Delegate](#implement-the-consent-delegate)
        - [Implement the Auth Delegate](#implement-the-auth-delegate)
            - [Constructor and Variables](#constructor-and-variables)
            - [Implement AcquireToken](#implement-acquiretoken)
            - [Implement GetAccessTokenOnBehalfOfUser](#implement-getaccesstokenonbehalfofuser)
    - [Adding Custom Objects](#adding-custom-objects)
        - [Implement Label](#implement-label)
        - [Implement CustomClass](#implement-customclass)
    - [Implement the File API Class](#implement-the-file-api-class)
        - [Create the FileApi Class](#create-the-fileapi-class)
        - [Add the Using Declarations](#add-the-using-declarations)
        - [Add the Constructor and private variables](#add-the-constructor-and-private-variables)
        - [Add CreateProfile()](#add-createprofile)
        - [Add CreateFileEngine()](#add-createfileengine)
        - [Add ListLabels()](#add-listlabels)
        - [Add CreateFileHandler()](#add-createfilehandler)
        - [Add ApplyLabel()](#add-applylabel)
        - [Implement MipController.cs](#implement-mipcontrollercs)
            - [Decorate with Authorize](#decorate-with-authorize)
            - [Set Using Statements](#set-using-statements)
            - [Implement Constructor and Variables](#implement-constructor-and-variables)
            - [Implement GetAllLabels](#implement-getalllabels)
            - [Implement ApplyLabel()](#implement-applylabel)
    - [Implementing Default.aspx](#implementing-defaultaspx)
        - [Adding Default.aspx Controls](#adding-defaultaspx-controls)
        - [Add Using Statements to Default.aspx.cs](#add-using-statements-to-defaultaspxcs)
        - [Add Privates Variables to Default.aspx.cs](#add-privates-variables-to-defaultaspxcs)
        - [Add PopulateLabels() to Default.aspx.cs](#add-populatelabels-to-defaultaspxcs)
        - [Add PopulateDataGrid()](#add-populatedatagrid)
        - [Add the OnChanged Handler for the Tree View](#add-the-onchanged-handler-for-the-tree-view)
        - [Add the OnClick Handler for the Download Button](#add-the-onclick-handler-for-the-download-button)
        - [Update the Page_Load Event Handler](#update-the-pageload-event-handler)
    - [Build](#build)
    - [Troubleshooting](#troubleshooting)
    - [Sources/Attribution/License/3rd Party Code](#sourcesattributionlicense3rd-party-code)

## Summary

This sample is intended to demonstrate the ease of integrating the MIP SDK with custom applications, as well to demonstrate the experience one might find in a line-of-business or SaaS application. A user simply works as they've always worked, downloading some set of data to an Excel sheet. In this case, the file is labeled and protected on download, transparent to the end user. This allows organizations to allow their teams to work in familiar ways while still maintaining the security of sensitive information once extracted from controlled environments. 

The ASP.NET web application displays a set of data in a GridView then allows the user to select an MIP label. After selecting a label, the user may click Download to get a copy of the data in Excel format. The downloaded file will have the selected MIP label applied.

Authentication in the sample is implemented via bearer token and an on-behalf-of flow, as detailed [here](https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof).

- Users authenticate to the ASP.NET web application
- The application stores their JSON web token.
- The service, using certificate based auth, obtains a new token, on behalf of the user, for use against the backend policy and protection services.

The sample itself is code complete. To make functional, the following actions must be performed:

- Register the application in Azure Active Directory and configure certificate based authentication
- Migrate Labels from Azure Information Protection to Security and Compliance Center
- Implement the MIP SDK Authentication Delegate
- Configure the MIP SDK binaries (This will move to NuGet soon)
- Implement a FileApi class to call the MIP SDK
- Implement the MipLabelController to call the custom FileApi class

At the end of the lab, you'll be able to run the web application, authenticate, view labels available to the user, and download an Excel file with the selected label applied.

## Getting Started

The application leverages an on-behalf-of authentication flow. The service will authenticate **as the user** to the backend services, meaning the labels and protection actions will be performed in the context of the user.

## Requirements

- An O365 E3 Tenant and global admin account
- Ensure labels are configured in Office 365 Security and Compliance Center

### Ensure Labels are Configured

1. Navigate to [Security and Compliance Center Portal](https://protection.office.com)
2. Click **Classifications**
3. Click **Labels**
4. Verify that one or more labels exist
5. Click **Label policies**
6. Verify that a label policy exists and that it contains labels by clicking the policy and reviewing the **Publish labels for your users** section. If one or more labels are present, labels are correctly configured.
7. If labels or the label policy are not configured, jump to the [Publishing Labels](#publishing-labels) section.
8. If labels look OK, jump to [Create Self-Signed Certificate](#create-self-signed-certificate)

### Publishing Labels

1. Navigate to [Security and Compliance Center Portal](https://protection.office.com)
2. Click **Classifications**
3. Click **Labels**
4. Click **Create a label**
5. Specify the **Name** and **Tooltip**. Click **Next**
6. *Optional*: Enable encryption. Leave the defaults. Click **Add permissions to users or groups** and click **Add all tenant members**. Click **Save**. Click **Next**
7. Click **Next** on each of the settings until reaching **Review your settings**. Click **Create**

Repeat steps 1 - 7 above to create as many labels as desired.

To publish the labels:

1. Navigate to [Security and Compliance Center Portal](https://protection.office.com)
2. Click **Classifications**
3. Click **Label policies**
4. Click **Publish labels**
5. Click **Choose labels to publish**, click **Add**, the select the desired labels.
6. Click **Done**
7. Click **Next** until prompted to enter a name. Input a name for the policy and click **Next**
8. Click **Publish**

### Create Self-Signed Certificate

Authentication against the policy service using a service principal requires certificate based authentication. For this sample, we'll use PowerShell to generate the self-signed certificate, then export that to a text file.

1. Launch PowerShell
2. Create the certificate and export the credential information to a text file:

```powershell
mkdir c:\temp
cd c:\temp

#Generate the certificate
$cert = New-SelfSignedCertificate -Subject "CN=MipSdkIgniteCert" -CertStoreLocation "Cert:\CurrentUser\My"  -KeyExportPolicy Exportable -KeySpec Signature

# Export certificate details
$bin = $cert.RawData
$base64Value = [System.Convert]::ToBase64String($bin)
$bin = $cert.GetCertHash()
$base64Thumbprint = [System.Convert]::ToBase64String($bin)
$keyid = [System.Guid]::NewGuid().ToString()
$jsonObj = @{customKeyIdentifier=$base64Thumbprint;keyId=$keyid;type="AsymmetricX509Cert";usage="Verify";value=$base64Value}
$keyCredentials=ConvertTo-Json @($jsonObj) | Out-File "keyCredentials.txt"
```

Keep **keyCredentials.txt** as the contents are required for a later step.

## Create the Project

Building this sample requires Visual Studio 2017 with the following workloads installed:

- ASP.NET and web development
- Azure development

### Create the new ASP.NET Web Application

1. Launch Visual Studio 2017
2. File -> New -> New Project
3. Select Visual C# -> Web -> ASP.NET Web Application
4. Set a name for the project: **MipSdkIgnite**
5. Ensure .NET Framework 4.6.1 is selected. 
6. Click OK
7. Select **Web API** from the list of templates
8. Click **Change Authentication**
9. Select **Work or School Accounts**
10. The application type should be **Cloud - Single Organization**
11. In the **Domain** box, enter the domain name of the Office 365 tenant. For example, **Contoso.onmicrosoft.com**
12. Click OK, then OK. You may be prompted for global administrator credentials here to provision the app registration

The last steps will create the application registration in Azure AD for the specified tenant, if the user had the appropriate rights. If this was created, jump to the section on [Updating the App Registration](#updating-the-app-registration). If not, jump to the section on [Creating the App Registration](#creating-the-app-registration).

### Create/Update App Registration

To allow clients to authenticate against the web application, as well as to enable the web application to connect on behalf of clients, a new application registration must be configured in the **Azure AD mangement portal**.

#### Updating the App Registration

If you pulled the project directly from GitHub, [skip to Creating the App Registration](#creating-the-app-registration)

1. Go to https://portal.azure.com and log in as a global admin (or go directly [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM))
1. Click Azure Active Directory, then **App Registrations** in the menu blade.  
1. Click **View all applications** and find **MipSdkIgnite**, the custom project name you used, or the name returned by the authentication wizard in the list. Click it.
1. Click **Settings**
1. Click **Required Permissions**
1. Click **Add**
1. Click **Select an API**
1. Select **Microsoft Rights Management Service** and click **Select**
1. Under **Select Permissions** select **Create and access protected content for users**
1. Click **Select** then **Done**
1. Click **Add**
1. Click **Select an API**
1. In the search box, type **Microsoft Information Protection Sync Service** then select the service and click **Select**
1. Under **Select Permissions** select **Read all unified policies a user has access to.**
1. Click **Select** then **Done**
1. In the **Required Permissions** blade, click **Grant Permissions** and confirm.

#### Creating the App Registration

If you pulled the project from GitHub, you'll need to create a new application registration.

1. Go to https://portal.azure.com and log in as a global admin (or go directly [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM))
2. Click Azure Active Directory, then **App Registrations** in the menu blade.
3. Click **New Applications Registration**
4. For name, enter **MipSdkIgnite**
5. For Sign-on URL, enter **https://localhost:44376**
  > Note: If you updated the project settings, this may change.
6. Click **Create**

The **Registered app** blade should now be displayed.

1. Click **Settings**
1. Click **Required Permissions**
1. Click **Add**
1. Click **Select an API**
1. Select **Microsoft Rights Management Service** and click **Select**
1. Under **Select Permissions** select **Create and access protected content for users**, **Read protected content on behalf of a user**, and **Create protected content on behalf of a user**
1. Click **Select** then **Done**
1. Click **Add**
1. Click **Select an API**
1. In the search box, type **Microsoft Information Protection Sync Service** then select the service and click **Select**
1. Under **Select Permissions** select all permissions
1. Click **Select** then **Done**
1. In the **Required Permissions** blade, click **Grant Permissions** and confirm.

#### Add the certificate credentials to the Azure AD Application

1. In **Azure Active Directory** under **App Registrations**, find the **MipSdkIgnite** application. Click **Edit Manifest**.
1. Find **keyCredentials** in the manifest. By default, it should be similar to this:

```json
  "keyCredentials": [],
```

1. Remove the existing brackets and replace with the contents of the text file generated in the [certificate generation step](#create-self-signed-certificate).  
2. **Important**: Don't forget the trailing comma.
3. Click **Save**

When complete, the section should be similar to this:

```json
 "keyCredentials": [
    [
        {
        "keyId":  "470980fd-2973-43e8-9d7d-254e073f55df",
        "value":  "This will be the public key data",
        "type":  "AsymmetricX509Cert",
        "usage":  "Verify",
        "customKeyIdentifier":  "This will be the custom key ID"
        }
    ],
```

### Install NuGet Packages

The NuGet packages will auto-restore if pulling the complete or partially complete project from GitHub. The Packages required in the project can be installed via the VS2017 Package Manager Console by running: 

```PowerShell
Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory
Install-Package Microsoft.IdentityModel.Protocols.WsFederation
Install-Package Microsoft.Owin.Security.Cookies
Install-Package Microsoft.Owin.Security.OpenIdConnect
Install-Package EPPlus
Install-Package Newtonsoft.Json
```

### Update Web.Config

The web.config file must be updated to store several identity and application-specific settings. Several of these settings should already be populated if you created the project from scratch and configured authentication in the wizard.

#### Update/Add appSettings

In the MipSdkIgnite project, open the **web.config** file and find the **appSettings** section.

```xml
<appSettings>
    <add key="ida:ClientId" value="74164b8d-5c4b-42fb-b66a-172544423a9f" />
    <add key="ida:AADInstance" value="https://login.microsoftonline.com/" />
    <add key="ida:Domain" value="contoso.onmicrosoft.com" />
    <add key="ida:TenantId" value="94f6984e-8d31-4794-bdeb-3ac89ad2b660" />
    <add key="ida:PostLogoutRedirectUri" value="https://localhost:44376/" />
    <add key="ida:CertName" value="CN=MipSdkIgniteCert" />
    <!--Directory that will be used to store MIP SDK state data-->
    <add key="MipData" value="App_Data\mip_data" />
    <!-- remove and suggest user finds their own? -->
    <add key="DataEndpoint" value="https://jsonplaceholder.typicode.com/todos" />
  </appSettings>
```

If you created the project as new, several of the settings should already exist (ClientId, TenantId, etc.). Copy the missing details in to the **appSettings** section of the web.config. 

The table below contains the location of each of the values.

| Key                       | Value or Value Location                                                                                       |
|---------------------------|---------------------------------------------------------------------------------------------------------------|
| ida:ClientId              | Azure AD App Registration Portal                                                                              |
| ida:AADInstance           | https://login.microsoftonline.com                                                                             |
| ida:Domain                | Domain of AAD Tenant                                                                                          |
| ida:TenantId              | [AAD Properties Blade](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Properties) |
| ida:PostLogoutRedirectUri | Set to site root (https://localhost:44376 in sample), and set in **App Registration->Settings->Logout URL**                                                        |
| ida:CertName              | CN=MipSdkIgniteCert                                                                                           |
| MipData                   | App_Data\mip_data                                                                                             |
| DataEndpoint              | Any public web service to load data for GridView.                                                             |

#### Update IdentityConfiguration

To save the bootstrap context token for the on behalf of authentication flow, the setting must be enabled in `identityConfiguration`.

Skipping this step will result in the on-behalf-of flow failing in later steps.

```xml
<system.identityModel>
    <identityConfiguration saveBootstrapContext="true"/>
</system.identityModel>
```

#### Add Config Sections

Verify that these two sections exist under `configSections`. If not, add them.

```xml
<section name="system.identityModel" type="System.IdentityModel.Configuration.SystemIdentityModelSection, System.IdentityModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"/>
<section name="system.identityModel.services" type="System.IdentityModel.Services.Configuration.SystemIdentityModelServicesSection, System.IdentityModel.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"/>
```

#### Save Web.Config

Save the changes to web.config.

## Implement Utilities Class

There are a couple of utility-type functions that will be used throughout the sample. To make accessing these easier, they'll be placed in a static class called `MipSdkIgnite.Utilities`.

1. In Visual Studio 2017, right click the project and click **Add** then **Class**
2. Name the new class `Utilities`. Set the class to **public static** and add the two following methods.

```csharp
//Headers
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
```

```csharp
//Methods
public static X509Certificate2 ReadCertificateFromStore(string certName)
{
    X509Certificate2 cert = null;
    X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadOnly);
    X509Certificate2Collection certCollection = store.Certificates;

    // Find unexpired certificates.
    X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

    // From the collection of unexpired certificates, find the ones with the correct name.
    X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);

    // Return the first certificate in the collection, has the right name and is current.
    cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
    store.Close();
    return cert;
}

public static string EnsureTrailingSlash(string value)
{
    if (value == null)
    {
        value = string.Empty;
    }

    if (!value.EndsWith("/", StringComparison.Ordinal))
    {
        return value + "/";
    }

    return value;
}

internal static class UnsafeKernel32NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode,
    CallingConvention = CallingConvention.StdCall)]
    internal static extern bool SetDllDirectory([In] [MarshalAs(UnmanagedType.LPWStr)] string lpPathName);
}
```

## Adding the MIP Components

In the following steps, the components necessary to use the MIP SDK will be added to the project. This includes the unmanaged C++ DLLs, the managed wrappers, and the wrapper code.

### Add the MIP SDK Binaries to the Project

Using the MIP SDK for C# requires to components:

- The MIP SDK File API for Windows
- The MIP SDK C# Wrapper

To download the SDK and C# wrapper, navigate to https://aka.ms/MIPSDKPreviewBins and save the ZIP file to a local folder.

1. In Explorer, navigate to the folder where the SDK download was saved and extract the contents.
1. In Visual Studio, right click the MipSdkIgnite project and click **Open Folder in File Explorer**
1. Create three new folders in the project folder **binsX86**, **binsX64**, and **sdkbins**
1. Navigate to the bins\release\amd64 folder in the extracted ZIP and copy all DLLs from amd64 to the binsx64 folder created above.
1. Navigate to the bins\release\x86 folder in the extracted ZIP and copy all DLLs from amd64 to the binsx86 folder created above.
1. Extract the DotNet wrapper ZIP. 
1. Navigate to the bins folder. Copy the two DLLs, **dotnet4_wrapper.dll** and **mip_dotnet_api.dll** to the sdkbins folder.
1. Copy **sdk_wrapper_dotnet.dll** from the amd64 and x86 folders in the DotNet package to the corresponding folder in the project directory

### Implement the Consent Delegate

The `Microsoft.InformationProtection.ConsentDelegate` enables the developer to expose a method to permit users to consent to using the service. Concretely, any call that logs information about the user must give the user an opportunity to consent prior to making the call. At GA, only the protection endpoints require consent.

The consent delegate is implemented by deriving a custom class from `Microsoft.InformationProtection.ConsentDelegate` and implementing an override for the `GetUserConsent` method.

1. Right-click the **MipSdkIgnite** project and add a new class.
2. Name the class `ConsentDelegateImplementation`
3. Add using statements for the SDK.
4. Add declaration for GetUserConsent and return accept value.

```csharp
using Microsoft.InformationProtection;

namespace MipSdkIgnite
{
    public class ConsentDelegateImplementation : IConsentDelegate
    {
        public Consent GetUserConsent(string url)
        {
            return Consent.Accept;
        }
    }
}
```

### Implement the Auth Delegate

The MIP SDK exposes a class called `Microsoft.InformationProtection.AuthDelegate`. This is an abstract class, intended to be implemented by the application developer to leverage the desired authentication libraries. The MIP SDK doesn't implement auth itself, rather it enables the developer to implement any auth library they wish.

To implement the authentication delegate, we create a new class, inheriting `Microsoft.InformationProtection.IAuthDelegate` and implement the `AcquireToken` function.

In the lab, we'll leverage ADAL as part of the MVC application. Specifically, the service will use certificate based authentication to perform operations on behalf of the user against the MIP endpoints. **Certificate based authentication is required to use the MIP policy endpoints.**

1. Right-click the **MipSdkIgnite** project and add a new class.
2. Name the class `AuthDelegateImplementation`
3. Add using statements for the SDK.
4. Add override method for AcquireToken and implement.

#### Constructor and Variables

Add the following variables to the `AuthDelegateImplementation` class. 

```csharp
private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
private static string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
private static string certName = ConfigurationManager.AppSettings["ida:CertName"];
private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

private ClaimsPrincipal _claimsPrincipal;
```

Next, implement the constructor so that it accepts a `ClaimsPrincipal` object and sets to the private variable.

```csharp
public AuthDelegateImplementation(ClaimsPrincipal claimsPrincipal)
{
    _claimsPrincipal = claimsPrincipal;
}
```

#### Implement AcquireToken

The AcquireToken method is at the core of the `AuthDelegate` class. When the SDK attempts any action that requires authentication, it will make a call to the `AcquireToken` function, passing in the calling `Identity`, the authority URL, and resource URL (either policy or protection endpoint). The code to obtain an OAuth2 token and return to the caller should be implemented in this function.

In the sample code, the SDK makes a call to another function, `GetAccessTokenOnBehalfOfUser` and obtains the OAuth2 token via On-Behalf-Of (OBO) authentication.

```csharp
public string AcquireToken(Identity identity, string authority, string resource)
{
//Call method to get access token, providing the identity, authority, and resource.
    //Uses the claims principal provided to the contructor to get the bootstrap context
    var authResult = Task.Run(async () => await GetAccessTokenOnBehalfOfUser(identity, authority, resource));
    return authResult.Result;
}
```

The function will asynchronously call GetAccessTokenOnBehalfOfUser, providing the identity, authority, and resource. That method passes back the auth token in the **Result** attribute.

#### Implement GetAccessTokenOnBehalfOfUser

The details on this method are somewhat out of the scope of this lab. For a deep dive (and the source material!) for this method, and the auth patterns used in this sample, review [this sample](https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof).

In summary, this method:

- Reads the self-signed certificate from the local certificate storage.
- Builds a client assertion certificate.
- Obtains the [bootstrap context](https://docs.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.bootstrapcontext?redirectedfrom=MSDN&view=netframework-4.7.2)
- Builds a user assertion from the user's access token (bootstrap context) and user name
- Initialize a new `AuthenticationContext`
- Acquire a token for the `resource` via certificate credential and user assertion

```csharp
public async Task<string> GetAccessTokenOnBehalfOfUser(Identity identity, string authority, string resource)
{
    X509Certificate2 cert = Utilities.ReadCertificateFromStore(certName);
    ClientAssertionCertificate certCred = new ClientAssertionCertificate(clientId, cert);

    var ci = (ClaimsIdentity)_claimsPrincipal.Identity;
    string userAccessToken = (string)ci.BootstrapContext;
    string userName = _claimsPrincipal.FindFirst(ClaimTypes.Upn).Value;
    UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);
    var authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority, new TokenCache());
    var result = await authContext.AcquireTokenAsync(resource, certCred, userAssertion);
    return (result.AccessToken);
}
```

Add this method to `AuthDelegateImplementation`.

## Adding Custom Objects

This sample leverages two custom classes to read and parse data from the JSON endpoint and from the MIP SDK. 

- `Models.Label`: A custom object to store basic information about the MIP Label. Used to populate the treeview. 
- `Models.CustomClass`: An intentionally generic name as this class stores whatever data is pulled from the service defined as **DataEndpoint** in web.config. If a new endpoint or data source is used, this class must be updated to accommodate that new data source.

### Implement Label

1. In Visual Studio 2017, under the MipSdkIgnite project, right-click **Models** and add a new class called **Label.cs**
2. Label is a rudimentary class that stores the label ID, name, a `List<>` of child labels, the sensitivity, and the description.

Implement the following snip and save:

```csharp
public class Label
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Label> Children { get; set; }
    public int Sensitivity { get; set; }
    public string Description { get; set; }
}
```

### Implement CustomClass

1. In Visual Studio 2017, under the MipSdkIgnite project, right-click **Models** and add a new class called **CustomClass.cs**
2. CustomClass is a rudimentary class that stores the properties representing the schema presented in the `DataEndpoint` web service.

Implement the following and save:

```csharp
public class CustomClass
{
    public int UserId { get; set; }
    public int Id { get; set; }
    public string Title { get; set; }
    bool Completed { get; set; }
}
```

This class is used in Default.aspx.cs when populating the "data" object.

## Implement the File API Class

The MIP SDK File API functionality will be implemented in a class called `FileApi`. This will help to ensure that the API can be used across the project without recycling any code. 

For the purposes of the tutorial, the sample will implement five methods, plus a constructor.

| Method              | Purpose                                                                                       |
|---------------------|-----------------------------------------------------------------------------------------------|
| Constructor         | Set ApplicationInfo, Initialize MIP SDK managed components, create profile and engine.        |
| CreateFileProfile() | Create a new `Microsoft.InformationProtection.File.IFileProfile` object.                      |
| CreateFileEngine()  | Add a new `Microsoft.InformationProtection.File.IFileEngine` to the `IFileProject` object.    |
| CreateFileHandler() | Create a new `IFileHandler` for the specified `Stream`.                                       |
| ApplyLabel()        | Apply the specified MIP label to the specified `Stream` and write to provided output `Stream` |
| ListAllLabels()     | Retrieves all labels available to the specified user and returns as `List<Models.Label>`      |

### Create the FileApi Class

1. In Visual Studio 2017, **right click** the MipSdkIgnite project and click **Add->Class**
2. Enter **FileApi** for the name and click **Add**.

### Add the Using Declarations

The `FileApi` class requires the following using statements:

```csharp
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
```

### Add the Constructor and private variables

When the FileApi object is constructed, it will setup the ApplicationInfo object, initialize the AuthDelegate using the ClaimsPrincipal, configure managed-to-unmanaged marshalling, initialize the FileProfileFactory, then create a `FileProfile` and `FileEngine`.

```csharp

//This is the location used to store MIP SDK state information and logs
private static string mipData = ConfigurationManager.AppSettings["MipData"];
private string mipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mipData);

internal static readonly FileProfileFactory FileProfileFactory = new FileProfileFactory();
private readonly ApplicationInfo _appInfo;
private AuthDelegateImplementation _authDelegate;

private IFileProfile _fileProfile;
private IFileEngine _fileEngine;

public FileApi(string clientId, string applicationName, ClaimsPrincipal claimsPrincipal)
{
    try
    {
        //store ApplicationInfo and ClaimsPrincipal for SDK operations
        _appInfo = new ApplicationInfo()
        {
            ApplicationId = clientId,
            FriendlyName = applicationName
        };

        //initialize new AuthDelegate providing the claimsprincipal
        _authDelegate = new AuthDelegateImplementation(claimsPrincipal);

        //set path to bins folder
        var path = Path.Combine(Directory.GetParent(Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath)).FullName, Environment.Is64BitProcess ? "binsx64" : "binsx86");
        //Use custom UnsafeKernel32NativeMethods class to configure managed to unmanaged marshalling
        Utilities.UnsafeKernel32NativeMethods.SetDllDirectory(path);

        //Initialize FileProfileFactory
        FileProfileFactory.Initialize();

        //Call CreateFileProfile. Result is stored in global
        CreateFileProfile();

        //Call FileEngine, providing the user UPN, null client data, and locale
        CreateFileEngine(ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value, "", "en-US");
    }

    catch (Exception ex)
    {
        throw ex;
    }
}
```

### Add CreateProfile()

The `Profile`, whether policy, file, or protection, is the base class for all SDK operations. Before any action can be taken by the SDK, the `Profile` must be instantiated.

In this sample, the profile is an `FileProfile` that we access via 

```csharp
private void CreateFileProfile()
{
    try
    {
        var profileSettings = new FileProfileSettings(mipPath, false, _authDelegate, new ConsentDelegateImplementation(), _appInfo, LogLevel.Trace);
        _fileProfile = Task.Run(async () => await new FileProfileFactory().(profileSettings)).Result;
    }

    catch (Exception ex)
    {
        throw ex;
    }
}
```

### Add CreateFileEngine()

The File Engine, exposed via `IFileEngine` is the class used in the SDK to take any actions specific to the authenticated user. The FileEngine allows the developer to list labels specific to the user and to construct a `FileHandler` for working with files or streams. The engine is created **by the profile object's AddEngineAsync() method**.

Constructing an object of IFileEngine requires creating `FileEngineSettings`, where the settings object is constructed by passing three values:

- Username: In UPN format
- Client Data string: Custom string for telemetry or debugger. Allowed to be empty.
- Locale, in "en-US" format. en-US is the default value.

In `FileApi`, implement a method called CreateFileEngine as detailed below. The method tries to create a new `FileEngineSettings` object, then uses that object to call `AddEngineAsync()` on the `IFileProfile` object. The result is stored in the class's _fileEngine object so it's accessible by all methods without being passed back to the caller.

```csharp
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
```

### Add ListLabels()

The first action typically implemented with `FileEngine` is to `ListSensitivityLabels()`. This function returns a list of all sensitivy labels defined by the organization. Labels which are out of scope for the user will be set to `Enabled = false`. It's important that your application understands the concept of enabled versus disabled labels. Enabled labels are displayed to a user and selectable; disabled labels are used only to read the label metadata.

The sample below calls `ListSensitivityLabels()` on the `IFileEngine` object and stores the result in a `List<Label>` collection. It this enumerates the list of labels, reading some inform

```csharp
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
```

### Add CreateFileHandler()

The `FileHandler`, or `IFileHandler` in C# SDK, handles all file or stream-specific operations in the SDK that apply to a file format the SDK can manage. Reading labels or protection, applying labels or protection, removing labels or protection, etc. 

`IFileHandler` can work with both streams and files. In this sample, implement a method called `CreateFileHandler()` that returns `IFileHandler`, and accepts a `Stream` and `string FileName` as the parameters. The `Stream` contains the input in to the handler, and the string for `FileName` will be the name of the file as reported to auditing.

Using the sample below, it's possible to accommodate for both streams and filename, depending on the input type.

```csharp
 private IFileHandler CreateFileHandler(Stream stream, string fileName)
{
    IFileHandler handler;
    try
    {
        if (stream != null)
            handler = Task.Run(async () => await _fileEngine.CreateFileHandlerAsync(stream, fileName)).Result;
        else
            handler = Task.Run(async () => await _fileEngine.CreateFileHandlerAsync(fileName)).Result;

        return handler;
    }

    catch (Exception ex)
    {
        throw ex;
    }
}
```

### Add ApplyLabel()

The last method that is required in FileApi as part of the sample is `ApplyLabel()`. This method will accept the input and output `Stream`, `string FileName`, the label ID in string format, and, optionally, a justification message as a string.

The provided parameters are used to call CreateFileHandler(). The `IFileHandler` object is returned to the `ApplyLabel()` function.

Similar to `FileProfile` and `FileEngine`, the `FileHandler` also requires a type of settings object, except in this case that object is `LabelingOptions`. The `LabelingOptions` object describes the various settings that can be apply to a label and stamped as part of the label metadata:

- ActionSource: Manual, Automatic, Recommended, Default, or Mandatory. [Corresponds to the various ways in which a MIP label can be applied]().
- AssignmentMethod: Standard, Auto, and Privileged.
- JustificationMessage: May be required when downgrading or removing and existing label.
- ExtendedProperties: Custom key/value pairs that can be applied in addition to default metadata.

The label is applied to the handler by calling `SetLabel` and providing the labelId and `LabelingOptions`. The result isn't persisted to the output file or stream until the `IFileHandler.CommitAsync()` method is called, with the output file or stream provided as a parameter.

```csharp
public bool ApplyLabel(Stream stream, Stream outputStream, string fileName, string labelId, string justificationMessage)
{
    //create a local file handler object
    IFileHandler handler;

    try
    {
        if (stream != null)
        {
            handler = CreateFileHandler(stream, fileName);
        }
        else
        {
            handler = CreateFileHandler(null, fileName);
        }

        LabelingOptions labelingOptions = new LabelingOptions()
        {
            JustificationMessage = justificationMessage,
            ActionSource = ActionSource.Manual,
            AssignmentMethod = AssignmentMethod.Standard,
            ExtendedProperties = new List<KeyValuePair<string, string>>()
        };

        handler.SetLabel(labelId, labelingOptions);

        var result = Task.Run(async () => await handler.CommitAsync(outputStream)).Result;
        return result;
    }

    catch (Exception ex)
    {
        throw ex;
    }
}
```

### Implement MipController.cs

With the FileApi and authentication complete, the MipController can now be created and implemented. 

1. In Visual Studio 2017, find the MipSdkIgnite project.
2. **Right click** the Controllers folder, click **Add**, then **Controller**
3. Select **Web API 2 Controller - Empty** and click **Add**
4. Set the controller name to **MipLabelController** and click **Add**

#### Decorate with Authorize

The controller should require that the client is authenticated and authorized to use the service. Decorate the class declaration with `[Authorize]`

```csharp
[Authorize]
public class MipLabelController : ApiController
{
}
```

#### Set Using Statements

The MipLabelController requires the following using statements.

```csharp
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Claims;
using System.Web.Http;
```

#### Implement Constructor and Variables

The `MipLabelController` reads two settings from the web.config: ClientID and the friendly application name. A private FileApi object is created to be used to access the MIP SDK FileApi. This FileApi object is the implementation created in previous steps.

The `MipLabelController` constructor must be parameterless. In the contructor, we instantiate the `FileApi` object in _fileApi, providing the client ID and application name from the web.config, and the ClaimsPrincipal from the security claims provided by the client.

```csharp
private static string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
private static string appName = ConfigurationManager.AppSettings["ApplicationName"];
private FileApi _fileApi;

public MipLabelController()
{
    _fileApi = new FileApi(clientId, appName, ClaimsPrincipal.Current);
}
```

#### Implement GetAllLabels

Returning the list of labels to the view is implemented simply by calling `_fileApi.ListAllLabels()`. The set of labels available to the authenticated user will be downloaded from the service and returned as a `List<Models.Label>`, the custom class created earlier in the sample. This collection is used later to populate the TreeView in the default page.

```csharp
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
```

#### Implement ApplyLabel()

Applying the label has been abstracted by the custom FileApi class. In this sample, the label is applied to data provided by an input `Stream` and written to an output `Stream`. The stream is then written as an `HttpResponse`.

```csharp
public bool ApplyLabel(Stream inputStream, string fileName, string labelId, Stream outputStream)
{
    try
    {
        //Provide a stream and filename. Filename is used to generate audit events.
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
```

## Implementing Default.aspx

The default page does the following:

1. Redirects the user to authenticate
2. Displays a list of labels available to the user in a TreeView
3. Displays data from a web service (DataEndpoint in the web.config) in a GridView
4. Allows the user to select a label from the TreeView
5. Exposes a download button that, when clicked, apply

### Adding Default.aspx Controls

In Visual Studio 2017, open Default.Aspx in the source view, remove everything between `<asp:Content></asp:Content>` and add:

```xml
<div class="jumbotron">
    <h1>MIP SDK @ Ignite</h1>
    <p class="lead">The Microsoft Information Protection SDK enables Microsoft customers to label and protect information, no matter where its born.</p>
    <p><a href="https://aka.ms/mipsdk" class="btn btn-primary btn-lg">Learn more &raquo;</a></p>
</div>

<div class="wrapper">
    <div class="box b">
        <asp:TreeView ID="treeViewLabels" runat="server" ShowLines="True" Width=200 Height=300  ExpandDepth="FullyExpand" SelectedNodeStyle-BackColor="White" SelectedNodeStyle-ForeColor="Black"></asp:TreeView>
    </div>
    <div class="box c">
        <asp:Button ID="ButtonDownload" runat="server" Text="Download!"  BackColor="Black" ForeColor="Red"/>
    </div>
    <div class="box d">
        <asp:Label ID="labelSelectedLabel" runat="server" Text="null"></asp:Label>
    </div>
    <div class="box a">
        <asp:GridView ID="gridViewData" runat="server" Width="1000" Height="219px" AllowPaging="True" AllowCustomPaging="True"  AutoGenerateColumns="true" PageSize="20">
    </asp:GridView>
    </div>
</div>
```

In the Visual Studio project, navigate to **Content** and find **Site.css**. Add the following to the file:

```css
.wrapper {
    display: grid;
    grid-gap: 10px;
    grid-template-columns: 500px 500px 300px;
    background-color: #fff;
    color: #444;
}

.box {
    background-color: #444;
    color: #fff;
    border-radius: 5px;
    padding: 20px;
    font-size: 75%;
}

.a {
    grid-column: 1 / 3;
    grid-row: 2;
}

.b {
    grid-column: 3;
    grid-row: 1 / 3;
}

.c {
    grid-column: 1;
    grid-row: 1;
}

.d {
    grid-column: 2;
    grid-row: 1;
}
```

### Add Using Statements to Default.aspx.cs

```csharp
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using OfficeOpenXml;
using MipSdkIgnite.Controllers;
using MipSdkIgnite.Models;
```

### Add Privates Variables to Default.aspx.cs

The following should be added as part of `public partial class _Default`

```csharp
public partial class _Default : Page
{

    MipLabelController _labelController = new MipLabelController();
    private static List<CustomClass> data = new List<CustomClass>();

    //rest of class here
```

### Add PopulateLabels() to Default.aspx.cs

This method calls in to `MipLabelController` to list all labels, then iterates through the parent and child labels to populate the tree view.

The tree view item text is set to the label name. The value is set to the label identifier (guid), and the tooltip is set using the description information.

Child labels will be displayed in the tree view as a child node.

```csharp
private void PopulateLabels()
{
    treeViewLabels.Nodes.Clear();

    var labels = _labelController.GetAllLabels();

    foreach (var _label in labels)
    {
        TreeNode node = new TreeNode
        {
            Text = _label.Name,
            Value = _label.Id,
            ToolTip = _label.Description
        };

        if (_label.Children.Count > 0)
        {
            foreach (var _child in _label.Children)
            {
                TreeNode childNode = new TreeNode
                {
                    Text = _child.Name,
                    Value = _child.Id,
                    ToolTip = _child.Description
                };
                node.ChildNodes.Add(childNode);
            }
        }

        treeViewLabels.Nodes.Add(node);
    }
}
```

### Add PopulateDataGrid()

The `PopulateDataGrid` method retrieves data from the endpoint defined in the web.config as **DataEndpoint**. This endpoint can be updated to any desired endpoint, assuming that the web service returns JSON **and** the CustomClass.cs is updated to reflect the new schema.

The method retrieves the web service data, deserializes as a `List<CustomClass>`, then sets that deserialized collection as the datasource and binds to the grid view.

```csharp
protected void PopulateDataGrid()
{
    string DataEndpoint = ConfigurationManager.AppSettings["DataEndpoint"];

    using (WebClient wc = new WebClient())
    {
        var json = wc.DownloadString(DataEndpoint);
        data = JsonConvert.DeserializeObject<List<CustomClass>>(json);
        gridViewData.DataSource = data;
        gridViewData.DataBind();
    }
}
```

### Add the OnChanged Handler for the Tree View

1. Open Default.aspx in the source view.
2. Find the `<asp:TreeView>` tag. Add **OnSelectionChanged=** to the tag and click **Create New Event Handler** in the Intellisense box.
3. Navigate to **default.aspx.cs** and find the treeViewLabels_SelectedNodeChanged event handler.
4. Implement the code snip below.

This handler reads the selected label when changed and updates the Label UI element to display the name of the selected label.

```csharp
protected void treeViewLabels_SelectedNodeChanged(object sender, EventArgs e)
{
    labelSelectedLabel.Text = "";

    //Update Label UI element to display the selected MIP Label
    try
    {
        //HACK. Fix this.
        labelSelectedLabel.Text = treeViewLabels.SelectedNode.Parent.Text + " \\ ";
    }

    catch (Exception ex)
    {

    }
    labelSelectedLabel.Text += treeViewLabels.SelectedNode.Text;
}
```

### Add the OnClick Handler for the Download Button

When the user has selected a label and clicks the download button, the application will export the data stored in the GridView to an Excel spreadsheet. The label that has been selected will apply the MIP label and any metadata or protection that goes along with that label.

1. Open Default.aspx in the source view.
2. Find the `<asp:Button>` tag. Add **OnClick=** to the tag and click **Create New Event Handler** in the Intellisense box.
3. Navigate to **default.aspx.cs** and find the ButtonDownload_Click event handler.
4. Implement the code snip below.

```csharp
protected void ButtonDownload_Click(object sender, EventArgs e)
{
    string FileName = "MyAppOutput.xlsx";

    if (treeViewLabels.SelectedNode == null)
    {
        labelSelectedLabel.Text = "Please select a label";
        return;
    }

    //Using EPPlus, generate a spreadsheet using the data from the web service
    MemoryStream excelStream = new MemoryStream();
    using (var Excel = new ExcelPackage(excelStream))
    {
        var Worksheet = Excel.Workbook.Worksheets.Add("MyData");
        Worksheet.Cells["A1"].LoadFromCollection(data, true, OfficeOpenXml.Table.TableStyles.Dark10);
        Excel.Save();
    }

    using (var outputStream = new MemoryStream())
    {
        bool result = _labelController.ApplyLabel(excelStream, FileName, treeViewLabels.SelectedValue, outputStream);

        if (result)
        {
            HttpResponse Response = HttpContext.Current.Response;
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + FileName +";");
            Response.BinaryWrite(outputStream.ToArray());
            Response.Flush();
            Response.End();
        }
    }
}
```

### Update the Page_Load Event Handler

Finally, update the Page_Load event handler to populate labels and data grid for the initial page load only. This prevents unnecessary trips and calls to the data source and MIP SDK.

1. Navigate to **default.aspx.cs** and find the Page_Load event handler.
1. Implement the code snip below.

```csharp
protected void Page_Load(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        PopulateLabels();
        PopulateDataGrid();
    }
}
```

## Build

At this stage, it should be possible to build and run the application. When prompted to authenticate, provide user credentials for a user account in the configured tenant. 

Test the application by:

- Clicking a label to apply
- Clicking the download button
- Opening the Excel file and observing that the file is labeled. 
  - At time of publishing, the AIP client won't natively display the labels.
  - To see the labels, click **File->Info->Properties->Advanced Properties->Custom**
  - If the label applies protection, the yellow protection banner will also be displayed

## Troubleshooting

## Sources/Attribution/License/3rd Party Code

Unless otherwise noted, all content is licensed under MIT license.

Authentication code modeled/copied primarily from the [Active Directory DotNet WebApi On Behalf Of](https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof) sample.

Excel output generated by [EPPlus](https://www.nuget.org/packages/EPPlus).

JSON deserialization provided by [Json.NET](https://www.nuget.org/packages/Newtonsoft.Json/)
