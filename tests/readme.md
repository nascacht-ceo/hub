# Docker Containers

## AWS

docker run -d --rm -it -p 4566:4566 -p 4571:4571 -e SERVICES=lambda,s3,secretsmanager,dynamodb -e LAMBDA_RUNTIME_ENVIRONMENT_TIMEOUT=120 -e DEBUG=1 -v /var/run/docker.sock:/var/run/docker.sock localstack/localstack


## SQL Server

```bash
# Microsoft SQL Server
docker run -d --rm -it -p 1453:1433 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=nc_Test_Pipeline!" mcr.microsoft.com/mssql/server:2022-latest
# Microsoft SQL Server with AdventureWorks Sample Database
docker run -d --rm -it -p 1453:1433 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=nc_Test_Pipeline!" chriseaton/adventureworks:latest
```

## Secret synching

Sync the `nc-hub` user secrets to GitHub repo secrets.

```pwsh
$secretsPath = "$env:APPDATA\Microsoft\UserSecrets\nc-hub\secrets.json"
$secrets = Get-Content $secretsPath | ConvertFrom-Json
    
# Iterate through each secret
$secrets.PSObject.Properties | ForEach-Object {
    $name = $_.Name.Replace(":", "__") # Convert .NET colons to GitHub double underscores
    $value = $_.Value
        
    Write-Host "Syncing nc_hub__$name $value"
    # Set the secret using GitHub CLI
    echo $value | gh secret set "nc_hub__$name"
}
```

# Credentialing GitHub to AWS | GCP | Azure

## GCP

Here's the complete setup for your repo nascacht-ceo/hub:

Step 1: Enable APIs       
```bash
gcloud services enable iamcredentials.googleapis.com iam.googleapis.com --project=seventh-seeker-476512-r1
```

Step 2: Create Workload Identity Pool

```bash
gcloud iam workload-identity-pools create "github-pool" `
  --project="seventh-seeker-476512-r1" `
  --location="global" `
  --display-name="GitHub Actions Pool"
```
Step 3: Create OIDC Provider

```bash
gcloud iam workload-identity-pools providers create-oidc "github-provider" --project="seventh-seeker-476512-r1" --location="global" --workload-identity-pool="github-pool" --display-name="GitHub Provider"                                                                    --attribute-mapping="google.subject=assertion.sub,attribute.actor=assertion.actor,attribute.repository=assertion.repository,attribute.repository_owner=assertion.repository_owner" --issuer-uri="https://token.actions.githubusercontent.com"  --attribute-condition="assertion.repository_owner=='nascacht-ceo'"
```

Step 4: Create Service Account

```bash
gcloud iam service-accounts create "github-actions-sa" --project="seventh-seeker-476512-r1" --display-name="GitHub Actions Service Account"
```

Step 5: Grant Service Account permissions (adjust roles as needed)

```bash
gcloud projects add-iam-policy-binding seventh-seeker-476512-r1 --member="serviceAccount:github-actions-sa@seventh-seeker-476512-r1.iam.gserviceaccount.com" --role="roles/viewer"
```

Step 6: Allow GitHub to impersonate the Service Account

```bash
gcloud iam service-accounts add-iam-policy-binding "github-actions-sa@seventh-seeker-476512-r1.iam.gserviceaccount.com" --project="seventh-seeker-476512-r1" --role="roles/iam.workloadIdentityUser" --member="principalSet://iam.googleapis.com/projects/967035478217/locations/global/workloadIdentityPools/github-pool/attribute.repository/nascacht-ceo/hub"
```

Step 7: Use in your workflow

permissions:
  contents: read
  id-token: write

steps:
  - uses: google-github-actions/auth@v2
    with:
      workload_identity_provider: projects/967035478217/locations/global/workloadIdentityPools/github-pool/providers/github-provider
      service_account: github-actions-sa@seventh-seeker-476512-r1.iam.gserviceaccount.com

### Integrations test account:

```pwsh
$SA = "integration-tests"
$PROJECT = "seventh-seeker-476512-r1"

# Create the service account
gcloud iam service-accounts create $SA --project=$PROJECT --display-name=$SA

# Storage: read/write/delete on your bucket
g_cacheil iam ch "serviceAccount:${SA}@${PROJECT}.iam.gserviceaccount.com:roles/storage.objectAdmin" gs://nascacht-ai-tests

# Vision API
gcloud services enable vision.googleapis.com --project=$PROJECT   
gcloud projects add-iam-policy-binding $PROJECT --member="serviceAccount:${SA}@${PROJECT}.iam.gserviceaccount.com" --role="roles/documentai.apiUser"

# Vertex AI (Gemini)
gcloud projects add-iam-policy-binding $PROJECT --member="serviceAccount:${SA}@${PROJECT}.iam.gserviceaccount.com" --role="roles/aiplatform.user"
```


## AWS

aws sts get-caller-identity --query Account --output text --profile nc

Step 1: Create the OIDC Identity Provider                                                                                                                                                                                                                                    

aws iam create-open-id-connect-provider --url "https://token.actions.githubusercontent.com" --client-id-list "sts.amazonaws.com" --thumbprint-list "6938fd4d98bab03faadb97b34396831e3780aea1" --profile nc

Step 2: Create the IAM Role trust policy

Save this as trust-policy.json (I can create it for you if you give me your AWS account ID):

{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::YOUR_ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:nascacht-ceo/hub:*"
        }
      }
    }
  ]
}

Step 3: Create the Role

aws iam create-role --role-name github-actions-role --assume-role-policy-document file://aws-trust-policy.json --profile nc

Step 4: Attach permissions (adjust as needed)
aws iam attach-role-policy --role-name github-actions-role --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess --profile nc

Step 5: Use in workflow

- name: Configure AWS credentials
  uses: aws-actions/configure-aws-credentials@v4
  with:
    role-to-assume: arn:aws:iam::YOUR_ACCOUNT_ID:role/github-actions-role
    aws-region: us-east-1

## Azure

Step 1: Create App Registration

az ad app create --display-name "github-actions-app"

Copy the appId from the output for the next steps.

Step 2: Create Service Principal

az ad sp create --id "74489200-76e9-47ab-a895-8de67b56c64e"

Step 3: Add Federated Credential

```bash
// az ad app federated-credential create --id "74489200-76e9-47ab-a895-8de67b56c64e" --parameters "./azure.credential.config"
// Create the ref-based credential                                                                                                                                                                                                                                              
(Get-Content azure.credential.config | ConvertFrom-Json)[0] | ConvertTo-Json | Out-File -FilePath temp-cred.json -Encoding utf8
az ad app federated-credential create --id "74489200-76e9-47ab-a895-8de67b56c64e" --parameters "@temp-cred.json"                                                                                                                                                             
// Create the environment-based credential
(Get-Content azure.credential.config | ConvertFrom-Json)[1] | ConvertTo-Json | Out-File -FilePath temp-cred.json -Encoding utf8
az ad app federated-credential create --id "74489200-76e9-47ab-a895-8de67b56c64e" --parameters "@temp-cred.json"
```


Step 4: Grant Contributor role

```bash
az role assignment create --assignee "74489200-76e9-47ab-a895-8de67b56c64e" --role Contributor --scope /subscriptions/200a04a0-23e0-4937-8d5d-05daa4ea9d81
```

Step 5: Workflow step (I'll add this to commit.yaml)

```yaml
- name: Authenticate to Azure
  uses: azure/login@v2
  with:
    client-id: YOUR_APP_ID
    tenant-id: efd9003e-a958-4355-8f3b-1a8730f8769f
    subscription-id: 200a04a0-23e0-4937-8d5d-05daa4ea9d81
```