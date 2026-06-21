# Project Plan Parser

A high-performance, scalable .NET 9.0 API designed to parse Microsoft Project (.mpp) files and return structured project data. Built with containerization and microservice architectures in mind, it provides a secure and efficient way to integrate project schedule data into your applications.

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [How It Works](#how-it-works)
- [API Usage](#api-usage)
- [Selective Data Extraction](#selective-data-extraction)
- [Testing](#testing)
- [Licensing](#licensing)
- [Deployment](#deployment)
- [Scalability & Performance](#scalability--performance)
- [Security](#security)

## Overview
This service allows clients to upload Microsoft Project (`.mpp`) files and receive a JSON representation of the project's tasks, resources, assignments, calendars, and timephased data. It is powered by [Aspose.Tasks for .NET](https://products.aspose.com/tasks/net/).

## Features
- **Comprehensive Extraction**: Support for tasks, resources, assignments, calendars, baselines, and timephased work data.
- **Microservice Ready**: Includes CORS support, health checks, and response compression.
- **Secure by Default**: Validates both file extensions and magic bytes (OLE2 header) to ensure only valid project files are processed.
- **Scale-Optimized**: Configured for large file uploads (up to 100MB) and selective data extraction to minimize payload sizes.
- **Containerized**: Ready for deployment to Azure Container Apps, AWS Fargate, or Kubernetes.

## How It Works
1. **Request Reception**: The API receives a `multipart/form-data` request containing an `.mpp` file.
2. **Security Validation**: 
   - Checks the file extension is `.mpp`.
   - Inspects the first 8 bytes of the file to verify the OLE2 signature (`D0 CF 11 E0 A1 B1 1A E1`).
3. **Selective Parsing**: Based on query parameters, the `ProjectParserService` uses `Aspose.Tasks` to selectively load and map project data to DTOs.
4. **Response**: Returns a compressed, camelCase JSON object containing the requested project components.

## API Usage

### POST `/api/parse`
Parses an uploaded `.mpp` file.

**Request:**
- **URL**: `/api/parse`
- **Method**: `POST`
- **Content-Type**: `multipart/form-data`
- **Form Data**: `file` (Binary .mpp file)

**Query Parameters (Optional):**
- `includeTasks` (default: true)
- `includeResources` (default: true)
- `includeAssignments` (default: true)
- `includeCalendars` (default: true)
- `includeTimephased` (default: true)
- `includeBaselines` (default: true)

**Example Response:**
```json
{
  "projectName": "Annual Marketing Plan",
  "tasks": [
    {
      "id": 1,
      "uniqueId": 101,
      "name": "Market Research",
      "start": "2024-01-01T09:00:00",
      "finish": "2024-01-15T17:00:00",
      "percentComplete": 100,
      "isMilestone": false
    }
  ],
  "resources": [
    { "id": 1, "uniqueId": 1, "name": "Marketing Team", "type": "Work" }
  ]
}
```

## Selective Data Extraction
To optimize performance for large projects, you can request only the data you need. For example, to get only tasks and resources:
`POST /api/parse?includeAssignments=false&includeCalendars=false&includeTimephased=false&includeBaselines=false`

## Testing
The project includes a comprehensive test suite using xUnit, Moq, and FluentAssertions.

Run tests via CLI:
```bash
dotnet test
```

- **Unit Tests**: Verify the `ProjectParserService` logic and data mapping.
- **Integration Tests**: Verify the API endpoints, security validation, and health checks.

## Licensing
This application uses [Aspose.Tasks for .NET](https://products.aspose.com/tasks/net/). Without a license, the library operates in **Evaluation Mode**, which may include limitations such as watermarks or restricted data extraction.

### Applying a License
You can apply a license in three ways using environment variables or a dedicated directory:

1. **Metered License (Recommended for Cloud Native)**:
   - Set `ASPOSE_METERED_PUBLIC_KEY`
   - Set `ASPOSE_METERED_PRIVATE_KEY`
   - *Note: Requires internet access to communicate with Aspose servers.*
2. **Dedicated License Directory (Recommended for Containers & CI/CD)**:
   - Mount your license file to `/app/licenses/Aspose.Tasks.lic` in the container.
   - Or bake it into the image at `/app/licenses/Aspose.Tasks.lic`.
   - The application automatically checks this path if no other license is specified.
3. **Environment Variable**:
   - Set `ASPOSE_LICENSE_PATH` to the full path of your `.lic` file.

### Air-Gapped Deployments
For environments without internet access (air-gapped), you **must** use a traditional file-based license (`.lic`). 
- **Metered licensing is not supported** in air-gapped environments as it requires a connection to Aspose validation servers.
- Use the "Dedicated License Directory" or "Environment Variable" methods described above to apply your license.

### CI/CD Integration

#### GitHub Actions
Use secrets to handle your license file. You can create a temporary license file during the build process:
```yaml
- name: Bake license into image
  run: |
    echo "${{ secrets.ASPOSE_LICENSE_CONTENT }}" > ProjectPlanParser/licenses/Aspose.Tasks.lic
    docker build -t project-plan-parser .
```

#### Azure DevOps Pipelines
Use Secure Files or Secrets:
```yaml
- task: DownloadSecureFile@1
  name: AsposeLicense
  inputs:
    secureFile: 'Aspose.Tasks.lic'

- script: |
    mkdir -p ProjectPlanParser/licenses
    cp $(AsposeLicense.secureFilePath) ProjectPlanParser/licenses/Aspose.Tasks.lic
    docker build -t project-plan-parser .
```

## Deployment

### Docker
The project includes a multi-stage `Dockerfile` optimized for production. It includes `libgdiplus` and `libx11-dev` required by Aspose.Tasks on Linux.

Build the image:
```bash
docker build -t project-plan-parser .
```

Run the container:
```bash
docker run -p 8080:8080 project-plan-parser
```

### Azure Container Apps
The service is designed to be deployed as a Container App:
1. **Health Probes**: Use the `/health` endpoint for Liveness and Readiness probes.
2. **Ingress**: Enable HTTP ingress on port 8080.
3. **Scaling**: Set scaling rules based on HTTP concurrency or CPU/Memory usage.

## Scalability & Performance
- **Statelessness**: The service is fully stateless, allowing for horizontal scaling.
- **Response Compression**: Uses Brotli and Gzip to reduce JSON payload sizes.
- **Server Limits**: Kestrel is configured to handle large uploads (up to 100MB).
- **Optimization**: For extremely large datasets, consider using the selective extraction parameters or an asynchronous background processing pattern.

## Security
- **GDPR & ISO27001 Compliance**: Enhanced security through magic bytes validation ensures only genuine project files are parsed.
- **Minimal Surface Area**: Uses Minimal APIs and restricted middleware.
- **CORS**: Configurable CORS policy to restrict access to trusted front-end domains.
