---
page_type: sample
languages:
- javascript
- csharp
products:
- azure
description: "See the ease of integrating the MIP SDK with custom applications, as well as the experience in a line-of-business/SaaS application."
urlFragment: mipsdk-fileapi-dotnet-onbehalfof
---

# Using MIP SDK with ASP.NET to Label Files on Download

## Summary

This sample is intended to demonstrate the ease of integrating the MIP SDK with custom applications, as well to demonstrate the experience one might find in a line-of-business or SaaS application. A user simply works as they've always worked, downloading some set of data to an Excel sheet. In this case, the file is labeled and protected on download, transparent to the end user. This allows organizations to allow their teams to work in familiar ways while still maintaining the security of sensitive information once extracted from controlled environments.

The ASP.NET web application displays a set of data in a GridView then allows the user to select an MIP label. After selecting a label, the user may click Download to get a copy of the data in Excel format. The downloaded file will have the selected MIP label applied.

Authentication in the sample is implemented via bearer token and an on-behalf-of flow, as detailed [here](https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof).

- Users authenticate to the ASP.NET web application
- The application stores their JSON web token.
- The service, using certificate based auth, obtains a new token, on behalf of the user, for use against the backend policy and protection services.

The sample has already implemented all of the UI and MIP SDK specific controls. While going through the sample, you'll perform the following tasks:

- Register the application in Azure Active Directory and configure certificate based authentication
- Update the web.config authentication settings
- Configure the MIP SDK binaries (This will move to NuGet soon)

At the end of the sample, you'll be able to run the web application, authenticate, view labels available to the user, and download an Excel file with the selected label applied.

## Getting Started

The application leverages an on-behalf-of authentication flow. The service will authenticate **as the user** to the backend services, meaning the labels and protection actions will be performed in the context of the user.

## Requirements

- An O365 E3 Tenant and global admin account
- Labels configured in [Office 365 Security and Compliance Center Portal](https://protection.office.com)

## Clone the Repository

1. Open a command prompt
2. Create a new folder `mkdir c:\samples`
3. Navigate to the new folder using `cd c:\samples`
4. Clone the repository by running `git clone https://github.com/azure-samples/MipSdk-Fileapi-DotNet-OnBehalfOf`
5. In explorer, navigate to *c:\samples\MipSdk-FileApi-DotNet-OnBehalfOf* and open the MipSdk-FileApi-DotNet-OnBehalfOf.sln in Visual Studio 2017.

## Add the NuGet Package

1. In Visual Studio, right click the MipSdkFileApiDotNet project.
2. Click **Manage NuGet Packages**
3. In the **Browse** tab, search for *Microsoft.InformationProtection.File* and install.

## Authentication

To enable the ASP.NET application to authenticate to Azure AD on behalf of the user, the following will be performed:

- Generate an X509 certificate
- Create an Application Registration in Azure AD
- Update the App Registration settings to allow access to the **Azure RMS** and **Microsoft Information Protection Sync Service** APIs
- Update the App Registration to accept certificate-based authentication

### App Registration

To allow clients to authenticate against the web application, as well as to enable the web application to connect on behalf of clients, a new application registration must be configured in the **Azure AD management portal**.

### Create an Azure AD App Registration

Authentication against the Azure AD tenant requires creating a native application registration. The client ID created in this step is used in a later step to generate an OAuth2 token.

> Skip this step if you've already created a registration for previous sample. You may continue to use that client ID.

1. Go to https://portal.azure.com and log in as a global admin.
> Your tenant may permit standard users to register applications. If you aren't a global admin, you can attempt these steps, but may need to work with a tenant administrator to have an application registered or be granted access to register applications.
2. Click Azure Active Directory, then **App Registrations** in the menu blade.
3. Click **New Registration**
4. Under *Supported account types* select **Accounts in this directory only**
5. Under *Redirect URI* select **Public client**
6. For *Redirect URI*, enter **mipsdk-auth-sample://authorize**   
  > Note: This can be anything you'd like.
8. Click **Register**

The registered app should now be displayed.

### Add API Permissions

1. Select **API permissions**.
2. Select **Add a permission**.
3. Select **Microsoft APIs**.
4. Select **Azure Rights Management Services**.
5. Select **Delegated Permissions** and then **user_impersonation**.
6. Select **Add permissions**.
7. Again, Select **Add a permission**.
8. Select **APIs my organization uses**.
9. In the search box, type **Microsoft Information Protection Sync Service** then select the service.
10. Select **Delegated permissions**.
11. Check **UnifiedPolicy.User.Read**.
12. Select **Add permissions**.
13. In the **API permissions** blade, Select **Grant admin consent for <Your Tenant>** and confirm.

### Set Redirect URI and Implicit Grant

1. Select **Authentication**.
2. Select **Add a platform**.
3. Select **Web**
4. Enter **https://localhost:44376/**
5. Select **configure**. 
6. Check **ID Tokens** under the *Implicit grant* section. 
7. Save changes.

### Generate a client secret

If you'd prefer to use certificate based auth, skip ahead to [Generate a client certificate](#Generate-a-client-certificate).

1. In the Azure AD application regisration menu, find the application you registered.
2. Select **Certificates and secrets**.
3. Click **New client secret**.
4. For the description, enter "MIP SDK Test App".
5. Select **In 1 year** for expiration
  > This can be 1 year, 2 years, or never.
6. Click **Add**.

The secret will be displayed in the portal. **Copy the secret now, as it will disappear after page refresh.**

  > Storing client secrets in plaintext isn't a best practice

### Generate a client certificate

This step generates a self-signed certificate, writes the thumbprint to the console, then exports the certificate to a cert file. If you used a [client secret](#Generate-a-client-secret), skip ahead to [update application configuration settings](#Update-application-configuration-settings).

Run the following PowerShell script:

```powershell
mkdir c:\temp
cd c:\temp

#Generate the certificate
$cert = New-SelfSignedCertificate -Subject "CN=MipSdkFileApiDotNet" -CertStoreLocation "Cert:\CurrentUser\My"  -KeyExportPolicy Exportable -KeySpec Signature
$cert.Thumbprint
$certFile = (Get-ChildItem -Path Cert:\CurrentUser\my\$($cert.thumbprint))
Export-Certificate -cert $cert -FilePath cba.cer -Type:CERT

# take the CER file and upload to AAD App Registration portal
```

### Import the certificate to the application registration

In this step, the public certificate generated in the previous section will be imported to the application registration.

1. In the Azure AD application regisration menu, find the application you registered.
2. Select **Certificates and secrets**
3. Click **Upload certificate**
4. Browse to the CER file generated in the previous section, then click **Add**

The certificate will appear in the list, displaying the thumbprint and validity period.

### Install Additional NuGet Packages

The required NuGet packages must be restored. To restore the packages:

1. Right-click the **MipSdkFileApiDotNet** project
2. Go to **Manage NuGet Packages**
3. A yellow banner will indicate that NuGet packages are missing. Click **Restore** to fetch the missing packages.

### Update Web.Config

The web.config file must be updated to store several identity and application-specific settings. Several of these settings should already be populated if you created the project from scratch and configured authentication in the wizard.

#### Update/Add appSettings

1. In the MipSdkFileApiDotNet project, open the **web.config** file and find the **appSettings** section.
2. Find `<!-- TODO: Update ida: settings below for your tenant -->`
3. Update the values in bold below with settings from the Azure AD tenant.
4. Use the table below to find the value for each setting and update the web.config.

> Ensure that the certificate name matches the name [used above](#create-self-signed-certificate)

| Key                           | Value or Value Location                                                                                                      |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **ida:ClientId**              | Azure AD App Registration Portal - [Detailed here](#app-registration): Copy the Application ID                               |
| **ida:AADInstance**           | https://login.microsoftonline.com                                                                                            |
| **ida:Domain**                | Domain of AAD Tenant - e.g. Contoso.Onmicrosoft.com                                                                          |
| **ida:TenantId**              | [AAD Properties Blade](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Properties) - Directory ID |
| **ida:PostLogoutRedirectUri** | Set to site root (https://localhost:44376 in sample), and set in **App Registration->Settings->Logout URL**                  |
| **ida:Thumbprint**            | Thumbprint of the certificate generated above.                                                                               |
| **MipData**                   | App_Data\mip_data                                                                                                            |
| **DataEndpoint**              | Any public web service to load data for GridView.                                                                            |

#### Update IdentityConfiguration

To save the bootstrap context token for the on behalf of authentication flow, the setting must be enabled in `identityConfiguration`.

Skipping this step will result in the on-behalf-of flow failing in later steps.

1. In web.config find `saveBootstrapContext`
2. Ensure that the value is set to **true**.

```xml
<system.identityModel>
    <identityConfiguration saveBootstrapContext="true"/>
</system.identityModel>
```

3. Save the changes to web.config.

**At this point the application should build and run. Read on to learn more about the details of the sample.** [Jump here](#build) to see the test steps.

### The Auth Delegate

The MIP SDK exposes a class called `Microsoft.InformationProtection.AuthDelegate`. This is an abstract class, intended to be implemented by the application developer to leverage their desired authentication libraries. The MIP SDK doesn't implement auth itself, rather it enables the developer to implement any auth library they wish.

To implement the authentication delegate, we create a new class, inheriting `Microsoft.InformationProtection.IAuthDelegate` and implement the `AcquireToken` function.

The sample leverages ADAL as part of the ASP.NET application. Specifically, the service will use certificate based authentication to perform operations on behalf of the user against the MIP endpoints. **Certificate based authentication is required to use the MIP policy endpoints.**

1. Open **AuthDelegateImplementation.cs**
2. Find `public string AcquireToken()`
3. Review the code path for obtaining an access token on behalf of a user.

The `IAuthDelegate` is passed to the `IFileProfile` at creation. When adding an engine for a specific user, `IAuthDelegate.AcquireToken()` is called, and should accept `Identity`, the authority URL, and the resource URL, in string format, as parameters. The API will pass this values to the method.

> The `IAuthDelegate` provides flexibility in that it allows the developer to implement **any OAuth2 token acquisition library** to meet their needs. In this sample the Microsoft Authentication Library (MSAL) is used, but 3rd party libraries, or even hard-coding a token, would work as far as the MIP SDK is concerned. It only expects that it will pass in some parameters to `AcquireToken` and get back an OAuth2 token in string format. The token issuer and audience should match the authority and resource provided as input.

## Custom Objects

This sample leverages two custom classes to read and parse data from the JSON endpoint and from the MIP SDK.

- `Models.Label`: A custom object to store basic information about the MIP Label. Used to populate the treeview. 
- `Models.CustomClass`: An intentionally generic name as this class stores whatever data is pulled from the service defined as **DataEndpoint** in web.config. If a new endpoint or data source is used, this class must be updated to accommodate that new data source.

This class is used in Default.aspx.cs when populating the "data" object.

## Implement the File API Class

The MIP SDK File API functionality has been implemented in a class called `FileApi`. This will help to ensure that the API can be used across the project without recycling any code.

For the purposes of the tutorial, the samples will implement five methods, plus a constructor.

| Method              | Purpose                                                                                       |
| ------------------- | --------------------------------------------------------------------------------------------- |
| Constructor         | Set ApplicationInfo, Initialize MIP SDK managed components, create profile and engine.        |
| CreateFileProfile() | Create a new `Microsoft.InformationProtection.File.IFileProfile` object.                      |
| CreateFileEngine()  | Add a new `Microsoft.InformationProtection.File.IFileEngine` to the `IFileProject` object.    |
| CreateFileHandler() | Create a new `IFileHandler` for the specified `Stream`.                                       |
| ApplyLabel()        | Apply the specified MIP label to the specified `Stream` and write to provided output `Stream` |
| ListAllLabels()     | Retrieves all labels available to the specified user and returns as `List<Models.Label>`      |

### Constructor and Private Variables

When the FileApi object is constructed, it will setup the ApplicationInfo object, initialize the AuthDelegate using the ClaimsPrincipal, configure managed-to-unmanaged marshalling, initialize the FileProfileFactory, then create a `FileProfile` and `FileEngine`.

### Review CreateProfile()

The `Profile`, whether policy, file, or protection, is the base class for all SDK operations. Before any action can be taken by the SDK, the `Profile` must be instantiated.

1. Open **FileApi.cs**
2. Review `CreateFileProfile()`

The `IFileProfile` is created by first initializing some profile settings. `FileProfileSettings` describes the storage location for MIP SDK state storage, whether to use in memory storage, the auth and consent delegates, `ApplicationInfo`, and the logging level.

The settings object is passed in to the `MIP.LoadFileProfileAsync()` method, which returns an object of IFileProfile.

### Review CreateFileEngine()

The File Engine, exposed via `IFileEngine` is the class used in the SDK to take any actions specific to the authenticated user. The FileEngine allows the developer to list labels specific to the user and to construct a `FileHandler` for working with files or streams. The engine is created **by the profile object's AddEngineAsync() method**.

Constructing an object of IFileEngine requires creating `FileEngineSettings`, where the settings object is constructed by passing three values:

- Username: In UPN format
- Client Data string: Custom string for telemetry or debugger. Allowed to be empty.
- Locale, in "en-US" format. en-US is the default value.

The method tries to create a new `FileEngineSettings` object, then uses that object to call `AddEngineAsync()` on the `IFileProfile` object. The result is stored in the class's _fileEngine object so it's accessible by all methods without being passed back to the caller.

1. In **FileApi.cs**, find `CreateFileEngine()`. 
2. Review the function.

### Review ListLabels()

The first action typically implemented with `IFileEngine` is to fetch the available labels. The `IFileEngine` has a property called `SensitivityLabels` that returns a list of all sensitivity labels defined by the organization. Labels which are out of scope for the user will be set to `Enabled = false`. It's important that your application understands the concept of enabled versus disabled labels. Enabled labels are displayed to a user and selectable; disabled labels are used only to read the label metadata.

The sample below reads `IFileEngine.SensitivityLabels` and stores the result in a `List<Models.Label>` collection. It iterates through the list of labels and child labels, then stores in the `List<Models.Label>` collection.

> The code to read the labels and put in the `List<>` is already implemented. The only step here is to implement the call to `IFileEngine.SensitivityLabels`.

1. In **FileApi.cs**, locate `ListLabels()`
2. Review the implementation. Note that getting the labels is as easy as `_fileEngine.SensitivityLabels;`

### Review CreateFileHandler()

The  `IFileHandler` the MIP SDK for C# handles all file or stream-specific operations that apply to a file format the SDK can manage. Reading labels or protection, applying labels or protection, removing labels or protection, etc.

`IFileHandler` can work with both streams and files. Here implement a method called `CreateFileHandler()` that returns `IFileHandler`, and accepts a `Stream` and `string FileName` as the parameters. The `Stream` contains the input in to the handler, and the string for `FileName` will be the name of the file as reported to auditing.

1. In **FileApi.cs**, locate `CreateFileHandler()`
2. Review the implementation.

### Review ApplyLabel()

The last method that is required in FileApi as part of the sample is `ApplyLabel()`. This method will accept the input and output `Stream` objects, `string FileName`, the label ID in string format, and, optionally, a justification message as a string.

The provided parameters are used to call `CreateFileHandler()`. The `IFileHandler` object is returned to the `ApplyLabel()` function.

Similar to `FileProfile` and `FileEngine`, the `FileHandler` also requires a type of settings object, except in this case that object is `LabelingOptions`. The `LabelingOptions` object describes the various settings that can be apply to a label and stamped as part of the label metadata:

- **ActionSource**: Manual, Automatic, Recommended, Default, or Mandatory.
- **AssignmentMethod**: Standard, Auto, and Privileged.
- **JustificationMessage**: May be required when downgrading or removing and existing label.
- **ExtendedProperties**: Custom key/value pairs that can be applied in addition to default metadata.

The label is applied to the handler by calling `SetLabel` and providing the labelId and `LabelingOptions`. The result isn't persisted to the output file or stream until the `IFileHandler.CommitAsync()` method is called, with the output file or stream provided as a parameter.

1. In **FileApi.cs**, locate `ApplyLabel()`
2. Review the implementation. Note that the label action options as passed as part of the `LabelingOptions` object below.

```csharp
LabelingOptions labelingOptions = new LabelingOptions()
{
    JustificationMessage = justificationMessage,
    ActionSource = ActionSource.Manual,
    AssignmentMethod = AssignmentMethod.Standard,
    ExtendedProperties = new List<KeyValuePair<string, string>>()
};
```

Finally, an audit event can be generated by notifying that the commit was a success:

```csharp
 if(result)
{
  // Submit an audit event if the change was successful.
  handler.NotifyCommitSuccessful(fileName);
}
```

## Review MipController.cs

MipController is the interface between the user interfaces and the service/data components. The `MipController` constructor initializes `FileApi _fileApi`. This is an object of the custom `FileApi` class implemented above.

1. In Visual Studio, expand **Controllers** and open **MipLabelController.cs**
2. Review the constructor.

### Review GetAllLabels()

Returning the list of labels to the view is implemented simply by calling `_fileApi.ListAllLabels()`. The set of labels available to the authenticated user will be downloaded from the service and returned as a `List<Models.Label>`, the custom class created earlier in the sample. This collection is used later to populate the TreeView in the default page.

1. In **MipLabelController.cs**, locate `GetAllLabels()`
2. Review the function.

```csharp
return _fileApi.ListAllLabels();
```

### Review ApplyLabel() in MipLabelController.cs

Applying the label has been abstracted by the custom FileApi class. The label is applied to data provided by an input `Stream` and written to an output `Stream`. The stream is then written as an `HttpResponse`.

1. In **MipLabelController.cs**, locate `ApplyLabel()`
2. Note that the label is applied to a stream.

## Review the OnClick Handler for the Download Button

When the user has selected a label and clicks the download button, the application will export the data stored in the GridView to an Excel spreadsheet. The label that has been selected will apply the MIP label and any metadata or protection that goes along with that label.

1. Open **Default.aspx.cs**
2. Find **ButtonDownload_Click()**
3. Review the implementation. Note that the `MemoryStream` for the excel file is passed to the `labelController` object.

> The provided code calls the label controller, passing in the `MemoryStream` that contains the data visible in the grid view, the intended filename, the label ID, and the output stream. `MipLabelController` calls `FileApi`, then the SDK. The `Stream` object is labeled, and since it's passed by reference, the object in the calling control is updated.

## Build

At this stage, it should be possible to build and run the application. When prompted to authenticate, provide user credentials for a user account in the configured tenant. **Press F5 to build and run!**

Test the application by:

- Clicking a label to apply
- Clicking the download button
- Opening the Excel file and observe that the file is labeled.
  - The [AIP Unified Labeling Preview Client](https://www.microsoft.com/en-us/download/details.aspx?id=57440) is required to display labels natively.
  - To see the labels without preview client, click **File->Info->Properties->Advanced Properties->Custom**
  - If the label applies protection, the yellow protection banner will also be displayed.

## Troubleshooting

## Sources/Attribution/License/3rd Party Code

Unless otherwise noted, all content is licensed under MIT license.

Authentication code modeled/copied primarily from the [Active Directory DotNet WebApi On Behalf Of](https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof) sample.

Excel output generated by [EPPlus](https://www.nuget.org/packages/EPPlus).

JSON de/serialization provided by [Json.NET](https://www.nuget.org/packages/Newtonsoft.Json/)
