The `nc-hub` solution is sugar around streaming transformations.

Examples include:

|Example | Description |
|-|-|
|Document manipulation|Given an enumeration of document streams, OCR, classify and extract metadata, persisting the metadata to the documents|
|Data transformation|Given a stream of data, transform the data and persist the transformed data to a new stream|

# nc-cloud

The `nc-cloud` project defines interfaces around cloud management.

|Interface|Description|
|-|-|
|`ICloudFileService`|A cloud account that can manage the creation, listing and deletion of file storage. Think of this as a Computer, that may have multiple drives.|
|`ICloudFileProvider`|A cloud storage location used to manage the searching, reading, writing, and deletion of files. Think of this as a drive, that may have multiple folders.|
|`ICloudFileInfo`|Manage a file, mimicking the `FileInfo` class.|

Examples:

```csharp
Add an Azure Blob Storage account:

var manager = services.GetRequiredService<ICloudManager>();
var account1 = manager.AddBlobStorageAccount("account-1", "some-storage-account", "some-access-key");
var account2 = manager.AddBlobStorageAccount("account-2", "other-storage-account", "other-access-key");

Console.Log($"Registered services are: {manager.Keys.Join(", ")}");
// Registered services are: account-1, account-2
```

Abstractions

|Interface|Files|Emails|Database|
|-|-|-|-|
|ISource|Computer|Server|Database|
|IRepository|Drive|Account|Table|
|IInstance|File|Email|Row|