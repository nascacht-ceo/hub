# Overview

Nascacht (Gaelic for 'connect' or 'bind') is a suite of wrappers 
that enable implementation of opinionated architectures. 
The target audience for this suite include:

- developers that need to refactor existing systems
- business analysts that need to implement domain-specific solutions from existing systems

# Cloud

The `nc-cloud` project provides abstractions across Amazon Web Services, Microsoft Azure, and Google Cloud Platform.

|Term|Description|
|-|-|
|ITenantManager|Add or remove cloud tenants to be accessed.|
|ITenantAccessor|Set or get the default tenant to use by ITenantManager.|

## ITenantManager

```csharp
var config = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json")
  .Build();
var services = new ServiceCollection()
  .AddNascachtServices(config.GetSection("nc"))
  .BuildServiceProvider();

var manager = services.GetRequiredService<AmazonTenantManager>();

// Access S3 using default credentials (e.g. implicit EC2)
var s3 = manager.GetServiceAsync<IAmazonS3>();

// Add a new tenant
await manager.AddTenantAsync(new AmazonTenant
{
	Name = "SomeTenant",
	AccessKey = "abc",
	SecretKey = "123"
});

// Access S3 using SomeTenant
var someS3 = manager.GetServiceAsync<IAmazonS3>("SomeTenant");

// Set SomeTenant as default for a scope
var tenantAccessor = services.GetRequiredService<ITenantAccessor>();
using (var tenantScope = tenantAccessor.SetTenant("SomeTenant")
{
  // This will use SomeTenant
  var s3 = manager.GetServiceAsync<IAmazonS3>();
}
```

# Solutions

```csharp
var solutions = new SolutionsBuilder()
	.AddDatabase("Db1", "connection-string")
	.AddAssembly("MyClass", typeof(MyClass).Assembly)
	.AddType("OtherClass", typeof(OtherClass))
	.AddOpenApi("SampleApi", "https://example.com/openapi.json");
```

# File Storage

```csharp
var manager = services.GetRequiredService<IStorageManager>();
manager.AddDisk("bucketA", "s3://aws-bucket-name");
manager.AddDisk("blobB", "az://azure-blob-name");
manager.AddDisk("bucketB", "gs://gcp-bucket-name");

var source = manager.GetDisk("bucketA");
var destination = manager.GetDisk("blobB");

var pipeline = new TplPipeline()
  .From(source.SearchAsync("some/prefix/"))
  .Transform<IStorageFileInfo, IStorageFileInfo>(async file =>
  {
	using var stream = await file.ReadAsync();
	return destination.WriteAsync($"/inbound/{file.RelativePath}, stream);
  })
  .Filter<IStorageFileInfo>(file => file.RelativePath.EndsWith(".pdf"))
  .TransformMany<IStorageFileInfo, IStorageFileInfo>(async file =>
  {
	
  })
var source = manager.GetDisk("bucketA");
var files = await source.SearchAsync("some/prefix/");
var destination = manager.GetDisk("blobB");
destination.PostAsync(files);
```

# Configuration

All configuration should exist under a root 'nc' section.
```json
{
  "nc": {
	"ai": {...},
	"aws": {...},
	"azure": {...},
	"gcp": {...},
	"solutions": {...}
  }
}
```

## Data Wrappers

- Point to a database, and instantly have endpoints to manipulate enumerations of any table's rows.
- Map which columns (properties) should be exposed, masked, hidden, or encrypted.
- Enforce state transitions of models, ensuring that they follow a defined lifecycle.
- Raise notifications when data changes, consumable by other systems.

## Open API Wrappers

|Use Case|Description|
|-|-|
|Data|Point a Solutio to a connection string, and instantly have RESTful endpoints to manipilate the data.|
|CLR Classes|Point a Solution to an assembly (or classes within an assembly), and instantly have RESTful endpoints to manipulate the data.|)

# Patterns

|Pattern|Description|
|-|-|
|State Management|Enforce state transitions of models, ensuring that they follow a defined lifecycle.|
|Rules|Define and enforce business rules on models, ensuring they adhere to specific conditions and constraints.|
|Encryption|Apply encryption to model properties, ensuring data security and privacy.|
|Data|Apply these patterns to existing data models.|


# Terminology

|Term|Description|
|-|-|
|Solution|A collection of models, endpoints, and behaviors that implement a domain-specific solution.|
|Model|A representation of a business entity, including its properties and behaviors.|
|Routes|Represent functional endpoints (http, CLR methods, etc.)|
|Behavior|A specific action or operation that can be performed on a model, often encapsulated as a service or function.|




Build solutions via configuration:

{
	"nc": {
		"solutions": 
	}
}


The `nc-hub` solution is sugar around streaming transformations.

Examples include:

|Example|Description|
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

# Workflow

| Feature                            | Azure Logic Apps        | AWS Step Functions       | GCP Workflows             |
| ---------------------------------- | ----------------------- | ------------------------ | ------------------------- |
| Low-code visual designer           | ✅ Yes                   | ✅ Yes (basic visualizer) | ❌ No (YAML only)          |
| Coding style                       | Designer + JSON         | JSON State Machine       | YAML / JSON               |
| Serverless                         | ✅ Yes                   | ✅ Yes                    | ✅ Yes                     |
| Built-in connectors                | ✅ Hundreds of services  | Limited integrations     | Limited integrations      |
| Express/High-speed mode            | ❌ No direct equivalent  | ✅ Express workflows      | ❌ No direct equivalent    |
| Long-running workflows             | ✅ Yes (days/months)     | ✅ Yes (up to 1 year)     | ✅ Yes (up to 1 year)      |
| Enterprise on-premise connectivity | ✅ Strong                | ❌ Limited                | ❌ Limited                 |
| Pricing model                      | Per action / fixed plan | Per state transition     | Per step / execution time |

Bonus: Portable Workflow Engines

| **BPMN Concept**                | **AWS Equivalent**                                               | **Azure Equivalent**                                         | **GCP Equivalent**                        | **Description**                                                                                       |
| ------------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------ | ----------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| **Start Event**                 | Starting state of the state machine                              | Trigger (HTTP, timer, etc.)                                  | Entry step in workflow                    | The point where the workflow begins; an event that kicks off the process.                             |
| **End Event**                   | `End` field in state definition                                  | Last action in Logic App                                     | `return` statement in YAML                | The point where the workflow finishes and stops executing further steps.                              |
| **Task**                        | `Task` state calling a Lambda or AWS Service                     | Action (API call, connector)                                 | `call` step (HTTP/API calls)              | A unit of work in the process, such as calling an API or running a script.                            |
| **Script Task**                 | Task invoking Lambda function                                    | Inline code (limited via Azure Functions)                    | Inline expressions or Cloud Functions     | A task where the workflow executes custom logic or code directly.                                     |
| **Service Task**                | Task state calling AWS service APIs                              | Logic App connector                                          | GCP connector / HTTP call                 | A task that calls an external service, system, or cloud API.                                          |
| **User Task** (human approvals) | No native support; needs external system                         | Logic App approvals (Office 365)                             | No native support; needs external systems | A task requiring human interaction, like an approval step in a business process.                      |
| **Exclusive Gateway**           | `Choice` state                                                   | Condition / Control block                                    | `switch` step                             | A decision point where only one path out of multiple is chosen based on conditions.                   |
| **Parallel Gateway**            | `Parallel` state                                                 | Parallel branches                                            | `parallel` block                          | A point where multiple tasks run simultaneously and converge afterward.                               |
| **Inclusive Gateway**           | No direct equivalent; simulate via Choice and Parallel           | No direct equivalent                                         | No direct equivalent                      | A decision point where one or more branches may be taken in parallel depending on conditions.         |
| **Event-Based Gateway**         | Complex to model; requires multiple Choice states                | Requires custom design with parallel branches and conditions | Custom branching logic                    | A decision point that waits for different possible events and follows the path triggered first.       |
| **Boundary Events**             | No direct support; simulate via error handling or separate state | Simulate via scopes and error handling                       | Simulate via error handling               | An event attached to the boundary of a task that interrupts or influences its execution if it occurs. |
| **Timer Intermediate Event**    | `Wait` state                                                     | Delay action                                                 | `sleep` step                              | A timer that pauses the workflow for a specified time before proceeding.                              |
| **Message Intermediate Event**  | Depends on integration; typically EventBridge or SQS             | Trigger-based pattern                                        | Pub/Sub event triggers                    | A point in the workflow that waits for a specific message to arrive.                                  |
| **Error Intermediate Event**    | `Catch` in `Catch` block                                         | `Scope` with error handling                                  | `try/catch` block                         | An event that handles errors or exceptions occurring during task execution.                           |
| **Escalation Event**            | Requires custom error handling and logic                         | Requires custom error handling                               | Requires custom error handling            | An event that triggers higher-level actions if issues occur, without treating it as a pure error.     |
| **Compensation**                | No direct equivalent; must manually design rollback logic        | Requires manual logic                                        | Requires manual logic                     | A mechanism to undo previously completed steps if something goes wrong later.                         |
| **Subprocess**                  | Nested state machine                                             | Nested Logic App                                             | Sub-workflow or callable workflow         | A reusable set of steps defined as a separate flow that can be invoked from the main process.         |
| **Call Activity**               | Nested workflow executions (via separate state machines)         | Call child Logic App                                         | Call sub-workflow                         | A step that calls an entirely separate workflow, passing control and data.                            |
| **Loop**                        | Looping `Map` state or manually transition back to a prior state | `Until` or `For Each` loops                                  | `for` loops or recursion                  | Repeating steps multiple times until a condition is met.                                              |
| **Terminate Event**             | `End` state                                                      | Terminate control                                            | `return` or `abort` logic                 | An event that immediately stops the workflow, even if other paths were running.                       |
| **Data Objects**                | Input/output JSON payloads in state transitions                  | Inputs and outputs between actions                           | Data variables passed through steps       | Data used or produced during process steps, passed between tasks in the workflow.                     |
| **Signal Events**               | No native support; requires external pub/sub systems             | Trigger-based pattern                                        | Pub/Sub topics                            | Broadcast-style events that can trigger workflows without a direct message exchange.                  |
| **Escalation Handling**         | Requires custom logic                                            | Requires custom logic                                        | Requires custom logic                     | Handling non-fatal issues that require attention but don't stop the workflow entirely.                |


## Apache Airflow

- Multi-cloud support.
- Great for ETL pipelines.

## Temporal.io

Code-based workflow orchestration.

Language SDKs:

- Go
- Java
- .NET
- Node.js

## Camunda

- BPMN-based workflow engine.
- Works across clouds.

# Development Policies

## Keep Packages Updated

- Use `dotnet outdated` to check for outdated packages.

```bash
dotnet tool install --global dotnet-outdated-tool
dotnet outdated
dotnet outdated -u
dotnet outdated -u -pre Always -inc "nc-*"
```