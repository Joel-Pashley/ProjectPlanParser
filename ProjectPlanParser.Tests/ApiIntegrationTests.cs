using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ProjectPlanParser.Tests
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Parse_NoFile_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new MultipartFormDataContent();
            // Adding a part that is NOT the 'file' part to ensure it's valid multipart but missing the expected file.
            content.Add(new StringContent("some-data"), "not-a-file");

            // Act
            var response = await client.PostAsync("/api/parse", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Parse_InvalidExtension_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", "test.txt");

            // Act
            var response = await client.PostAsync("/api/parse", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("Only .mpp files are supported");
        }

        [Fact]
        public async Task Parse_InvalidHeader_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new MultipartFormDataContent();
            var invalidHeaderBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var fileContent = new ByteArrayContent(invalidHeaderBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", "test.mpp");

            // Act
            var response = await client.PostAsync("/api/parse", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("Invalid file header");
        }

        [Fact]
        public async Task HealthCheck_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be("Healthy");
        }
    }
}
