using AngleSharp.Html.Dom;
using System.Net;
using System.Text.Json;
using System.Web;
using TaskManagement.Auth.Tests.Common.Data;
using TaskManagement.Auth.Tests.Common.Factory;
using TaskManagement.Auth.Tests.Helpers;
using TaskManagement.Auth.Tests.IntegrationTests.Authorization.Configurations;
using TaskManagement.Auth.Tests.IntegrationTests.Authorization.Helpers;
using TaskManagement.Auth.Tests.IntegrationTests.Authorization.Models;
using Xunit.Abstractions;

namespace TaskManagement.Auth.Tests.IntegrationTests.Authorization
{
    [Trait("Category", "Integration")]
    public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output;

        public AuthorizationTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = factory.CreateClientWithNoRedirects();
            _output = output;
        }

        private async Task<HttpResponseMessage> InitiateAuthorizationRequest(AuthorizationParameters parameters)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, AuthorizationTestConfiguration.AuthorizeEndpoint)
            {
                Content = AuthorizationTestHelpers.CreateTokenRequestContent(new Dictionary<string, string>
                {
                    ["client_id"] = parameters.ClientId,
                    ["client_secret"] = parameters.ClientSecret,
                    ["response_type"] = parameters.ResponseType,
                    ["redirect_uri"] = parameters.RedirectUri,
                    ["scope"] = parameters.Scope
                })
            };

            var response = await _client.SendAsync(request);
            _output.WriteLine($"Authorization Request Status: {response.StatusCode}");
            return response;
        }

        private async Task<(HttpResponseMessage Response, string? AntiForgeryToken)> PerformLogin(
            string loginPageUrl,
            string email = TestData.User.Email,
            string password = TestData.User.Password)
        {
            try
            {
                var loginPageResponse = await _client.GetAsync(loginPageUrl);
                var document = await HtmlHelpers.GetDocumentAsync(loginPageResponse);
                var loginForm = (IHtmlFormElement)document.QuerySelector("form")
                    ?? throw new InvalidOperationException("Login form not found");

                var antiForgeryToken = loginForm["__RequestVerificationToken"]?.GetAttribute("value");

                var response = await _client.SendAsync(
                    loginForm,
                    (HtmlElement)loginForm.QuerySelector("[type=submit]")
                        ?? throw new InvalidOperationException("Submit button not found"),
                    new Dictionary<string, string>
                    {
                        ["Input.Email"] = email,
                        ["Input.Password"] = password,
                        ["__RequestVerificationToken"] = antiForgeryToken ?? string.Empty
                    });

                _output.WriteLine($"Login Response Status: {response.StatusCode}");
                return (response, antiForgeryToken);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Login Error: {ex.Message}");
                throw;
            }
        }

        private async Task<HttpResponseMessage> ProvideConsent(
            string consentPageUrl,
            bool accept = true,
            CancellationToken cancellationToken = default)
        {
            var consentPageResponse = await _client.GetAsync(consentPageUrl, cancellationToken);
            var consentDocument = await HtmlHelpers.GetDocumentAsync(consentPageResponse);
            var consentForm = (IHtmlFormElement)consentDocument.QuerySelector("form[action='/connect/authorize']")
                ?? throw new InvalidOperationException("Consent form not found");

            var submitButton = (HtmlElement)consentForm.QuerySelector($"[name='submit.{(accept ? "Accept" : "Deny")}']")
                ?? throw new InvalidOperationException("Submit button not found");

            var response = await _client.SendAsync(
                consentForm,
                submitButton,
                new Dictionary<string, string>
                {
                { accept ? "submit.Accept" : "submit.Deny", "Yes" }
                });

            _output.WriteLine($"Consent Response Status: {response.StatusCode}");
            return response;
        }

        private string ExtractAuthorizationCode(string redirectUrl)
        {
            var uri = new Uri(redirectUrl);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var code = queryParams["code"];

            if (string.IsNullOrEmpty(code))
            {
                throw new InvalidOperationException("Authorization code not found in redirect URL");
            }

            return code;
        }

        private async Task<HttpResponseMessage> ExchangeCodeForTokens(
            string code,
            string clientId = TestData.Client.Id,
            string clientSecret = TestData.Client.Secret,
            string redirectUri = TestData.Client.RedirectUri)
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, AuthorizationTestConfiguration.TokenEndpoint)
            {
                Content = AuthorizationTestHelpers.CreateTokenRequestContent(new Dictionary<string, string>
                {
                    ["grant_type"] = AuthorizationTestConfiguration.DefaultGrantType,
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri
                })
            };

            var response = await _client.SendAsync(tokenRequest);
            _output.WriteLine($"Token Exchange Response Status: {response.StatusCode}");
            return response;
        }

        [Theory]
        [InlineData("openid profile", true)]
        [InlineData("openid profile email", true)]
        [InlineData("openid", true)]
        [InlineData("invalid_scope", false)]
        public async Task AuthorizationCodeFlow_WithValidCredentials_ReturnsAuthorizationCode(string scope, bool shouldSucceed)
        {
            var parameters = new AuthorizationParameters(Scope: scope);

            var authorizationResponse = await InitiateAuthorizationRequest(parameters);

            if (shouldSucceed)
            {
                Assert.Equal(HttpStatusCode.Found, authorizationResponse.StatusCode);
                Assert.Contains(AuthorizationTestConfiguration.LoginPath,
                    authorizationResponse.Headers.Location?.OriginalString ?? string.Empty);

                var (loginResponse, _) = await PerformLogin(
                    authorizationResponse.Headers.Location?.OriginalString
                        ?? throw new InvalidOperationException("Missing location header"));

                Assert.Equal(HttpStatusCode.Found, loginResponse.StatusCode);

                var consentResponse = await ProvideConsent(
                    loginResponse.Headers.Location?.OriginalString
                        ?? throw new InvalidOperationException("Missing location header"));

                Assert.Equal(HttpStatusCode.Found, consentResponse.StatusCode);

                Assert.Contains("code=", consentResponse.Headers.Location?.OriginalString);
                var authorizationCode = ExtractAuthorizationCode(
                    consentResponse.Headers.Location?.ToString()
                        ?? throw new InvalidOperationException("Missing location header"));

                var tokenResponse = await ExchangeCodeForTokens(authorizationCode);
                await AssertValidTokenResponse(tokenResponse);
            }
            else
            {
                Assert.Equal(HttpStatusCode.BadRequest, authorizationResponse.StatusCode);
                var content = await authorizationResponse.Content.ReadAsStringAsync();
                Assert.NotNull(content);
                Assert.Contains("invalid_scope", content);
            }
        }

        private async Task AssertValidTokenResponse(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var tokenResponse = await AuthorizationTestHelpers.DeserializeTokenResponse(response);
            Assert.NotNull(tokenResponse);
            Assert.NotEmpty(tokenResponse.AccessToken);
            Assert.Equal("Bearer", tokenResponse.TokenType);
            Assert.True(tokenResponse.ExpiresIn > 0);
            Assert.NotEmpty(tokenResponse.IdToken);
        }

        [Fact]
        public async Task AuthorizationCodeFlow_WithInvalidCredentials_ReturnsLoginError()
        {
            var parameters = new AuthorizationParameters();
            var authorizationResponse = await InitiateAuthorizationRequest(parameters);

            var (loginResponse, _) = await PerformLogin(
                authorizationResponse.Headers.Location?.OriginalString
                    ?? throw new InvalidOperationException("Missing location header"),
                email: "invalid@example.com",
                password: "WrongPassword123!");

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var document = await HtmlHelpers.GetDocumentAsync(loginResponse);
            var errorMessage = document.QuerySelector(".validation-summary-errors");
            Assert.NotNull(errorMessage);
        }

        [Fact]
        public async Task AuthorizationCodeFlow_WithDeniedConsent_RedirectsWithError()
        {
            var parameters = new AuthorizationParameters();

            var authorizationResponse = await InitiateAuthorizationRequest(parameters);

            Assert.Equal(HttpStatusCode.Found, authorizationResponse.StatusCode);
            Assert.Contains(AuthorizationTestConfiguration.LoginPath,
                authorizationResponse.Headers.Location?.OriginalString ?? string.Empty);

            var (loginResponse, _) = await PerformLogin(
                authorizationResponse.Headers.Location?.OriginalString
                    ?? throw new InvalidOperationException("Missing location header"));

            Assert.Equal(HttpStatusCode.Found, loginResponse.StatusCode);

            var consentResponse = await ProvideConsent(
                loginResponse.Headers.Location?.OriginalString
                    ?? throw new InvalidOperationException("Missing location header"),
                accept: false);

            Assert.Equal(HttpStatusCode.Found, consentResponse.StatusCode);
            Assert.Contains("error=access_denied", consentResponse.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task AuthorizationCodeFlow_WithInvalidClient_ReturnsBadRequest()
        {
            var parameters = new AuthorizationParameters
            {
                ClientId = "invalid-client",
                ClientSecret = "invalid-secret"
            };

            var response = await InitiateAuthorizationRequest(parameters);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AuthorizationCodeFlow_WithInvalidRedirectUri_ReturnsBadRequest()
        {
            var parameters = new AuthorizationParameters
            {
                RedirectUri = "https://malicious-site.com"
            };

            var response = await InitiateAuthorizationRequest(parameters);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TokenEndpoint_WithInvalidCode_ReturnsError()
        {
            var response = await ExchangeCodeForTokens("invalid_code");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<JsonDocument>(content)?.RootElement;
            Assert.NotNull(error);
            Assert.Equal("invalid_grant", error?.GetProperty("error").GetString());
        }
    }
}
