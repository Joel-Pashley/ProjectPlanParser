# Project Plan Parser API

A scalable API for parsing Microsoft Project (.mpp) files using Aspose.Tasks.

## Features

- Parse `.mpp` files and extract:
  - Project Name
  - Tasks (Id, UniqueId, Name, Start, Finish, Milestone status, Percent Complete)
  - Resources (Id, UniqueId, Name, Type)
  - Resource Assignments (Task link, Resource link, Units, Work, Cost)
  - Calendars (Name, Base calendar status)
  - Task Baselines (Start, Finish, Cost, Work)
  - Timephased Data (Work distribution)
- Security:
  - File extension validation (.mpp only).
  - File header validation (Magic Bytes check for OLE2 signature) to prevent malicious file uploads.
- Large File Handling:
  - Max file size configured (default 100MB).
  - Memory-efficient parsing using Aspose.Tasks.
  - **Selective Data Extraction**: Use query parameters to request only the specific data objects needed, reducing the response size.
    - `includeTasks` (bool)
    - `includeResources` (bool)
    - `includeAssignments` (bool)
    - `includeCalendars` (bool)
    - `includeTimephased` (bool)
    - `includeBaselines` (bool)
- Microservice & Front-end Optimization:
  - **CORS Support**: Configured to allow cross-origin requests from front-end services.
  - **Response Compression**: Brotli and Gzip compression enabled to minimize JSON payload size over the network.
  - **JSON Optimization**: CamelCase naming and exclusion of null values to reduce data transfer.
  - **Health Checks**: `/health` endpoint included for container liveness/readiness probes in Azure Container Apps.
  - For extremely large files (>100MB), consider horizontal scaling or an asynchronous "Job" based architecture.
- Containerized for easy deployment to Azure Container Apps or AWS Fargate.
- Built with .NET 9.0 and Minimal APIs for performance.
- **Thoroughly Tested**:
  - Unit tests for the parser service using xUnit and Moq.
  - Integration tests for the API endpoints ensuring validation and health checks work as expected.
  - Test project: `ProjectPlanParser.Tests`.

## Licensing

This API is powered by **Aspose.Tasks for .NET**. 

### Evaluation Mode
If no license is provided, Aspose.Tasks operates in evaluation mode. For parsing (reading) tasks, this usually means:
- Limited number of tasks/resources may be processed.
- Watermarks may be present in some exported data (though not directly in the JSON return, metadata might be affected).

### Applying a License
To use the full version, configure one of the following:

- **Metered (SaaS/Cloud)**: Set `ASPOSE_METERED_PUBLIC_KEY` and `ASPOSE_METERED_PRIVATE_KEY`. Requires internet access.
- **License Directory**: Place your `.lic` file at `/app/licenses/Aspose.Tasks.lic` (preferred for Docker/CI/CD).
- **Environment Variable**: Set `ASPOSE_LICENSE_PATH` to the location of your `.lic` file.

## Environment Configurations

The API supports `Development` and `Production` environments with pre-populated options in `appsettings.json` and `appsettings.Development.json`.

### Development
- **Logging**: Set to `Debug` for detailed output.
- **CORS**: Allows any origin (`*`) for easier local development.
- **Error Handling**: Detailed stack traces are returned in API responses.
- **License**: Provides more detailed console output if license application fails.

### Production
- **Logging**: Set to `Information` for concise logs.
- **CORS**: Restricted to a specific domain (default: `https://your-frontend-domain.com`). Update `appsettings.json` with your actual domain.
- **Error Handling**: Returns generic error messages to avoid leaking internal system details.
- **License**: Standard success/fail messages.

To switch environments, set the `ASPNETCORE_ENVIRONMENT` environment variable to either `Development` or `Production`.

### Air-Gapped Deployments

In air-gapped or restricted network environments, **Metered licensing will not work**. You must provide a physical `.lic` file using either the **License Directory** or **Environment Variable** method. These methods perform local validation and do not require any outbound internet connectivity.

## Testing

To run the tests, use the following command from the root of the project:

```bash
dotnet test
```

The test suite includes:
- **Unit Tests**: Validates the `ProjectParserService` logic, including selective data extraction.
- **Integration Tests**: Verifies the `/api/parse` endpoint, file validation (extension and magic bytes), and `/health` check.

## API Usage

### POST `/api/parse`

Accepts a multipart form data request with an `.mpp` file.

**Request:**
- Content-Type: `multipart/form-data`
- Body: `file` (the .mpp file)

**Response (JSON):**
```json
{
  "projectName": "Sample Project",
  "tasks": [
    {
      "id": 1,
      "name": "Task 1",
      "start": "2023-01-01T09:00:00",
      "finish": "2023-01-05T17:00:00",
      "isMilestone": false,
      "timephasedData": [
        { "start": "2023-01-01T09:00:00", "finish": "2023-01-01T17:00:00", "value": 8.0 }
      ]
    }
  ],
  "resources": [
    { "id": 1, "name": "Resource 1", "type": "Work" }
  ]
}
```

## Scalability Considerations

1. **Statelessness**: The API is entirely stateless. Each request is independent, allowing for easy horizontal scaling across multiple container instances.
2. **Container Apps Scaling**:
   - **HTTP Scaling**: Set scaling rules based on HTTP concurrent requests.
   - **Memory/CPU**: Parsing large MPP files can be CPU and Memory intensive. Monitor your container app's resource usage and set appropriate limits.
3. **Async Processing**: For very large project files that might exceed request timeouts, consider moving to an asynchronous pattern:
   - Accept the file and return a `202 Accepted` with a Job ID.
   - Process the file in the background (using a queue or background task).
   - Provide an endpoint to poll for the result.
4. **Caching**: If the same files are parsed frequently, consider caching the results in a distributed cache like Redis.

## Deployment

The included `Dockerfile` is optimized for Linux containers. It installs `libgdiplus` which is required by Aspose.Tasks for some rendering and data processing tasks.
