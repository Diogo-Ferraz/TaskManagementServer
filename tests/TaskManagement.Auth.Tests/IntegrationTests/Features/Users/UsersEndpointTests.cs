using AngleSharp.Html.Dom;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using TaskManagement.Auth.Features.Users.Models;
using TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Configuration;
using TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Helpers;
using TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Models;
using TaskManagement.Auth.Tests.TestHelpers.Data;
using TaskManagement.Auth.Tests.TestHelpers.Extensions;
using TaskManagement.Auth.Tests.TestHelpers.Fixtures;
using TaskManagement.Auth.Features.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManagement.Auth.Tests.IntegrationTests.Features.Users
{
    [Trait("Category", "Integration")]
    public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public UsersEndpointTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClientWithNoRedirects();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetUsers_WhenUnauthenticated_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUserById_WhenAuthenticated_ShouldReturnUser()
        {
            var userId = await GetSeededUserIdAsync();
            var token = await GetAccessTokenAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/users/{userId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
            user.Should().NotBeNull();
            user!.Id.Should().Be(userId);
            user.Email.Should().Be(TestData.User.Email);
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserById_WhenNotFound_ShouldReturnNotFound()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUsers_WithSearch_ShouldReturnMatchingUsers()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/users?search=authorized");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await response.Content.ReadFromJsonAsync<UserListResponse>();
            list.Should().NotBeNull();
            list!.Total.Should().BeGreaterThan(0);
            list.Items.Should().ContainSingle(u => u.Email == TestData.User.Email);
        }

        [Fact]
        public async Task SetUserStatus_WhenAuthenticatedButNotAdmin_ShouldReturnForbidden()
        {
            var userId = await GetSeededUserIdAsync();
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PatchAsJsonAsync($"/api/users/{userId}/status", new SetUserStatusRequest
            {
                IsActive = false
            });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private async Task<string> GetSeededUserIdAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(TestData.User.Email);
            user.Should().NotBeNull();
            return user!.Id;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var authorizationResponse = await InitiateAuthorizationRequest(new AuthorizationParameters());

            authorizationResponse.StatusCode.Should().Be(HttpStatusCode.Found);
            authorizationResponse.Headers.Location?.OriginalString.Should().Contain(AuthorizationTestConfiguration.LoginPath);

            var (loginResponse, _) = await PerformLogin(
                authorizationResponse.Headers.Location?.OriginalString
                    ?? throw new InvalidOperationException("Missing location header"));

            loginResponse.StatusCode.Should().Be(HttpStatusCode.Found);

            var nextLocation = loginResponse.Headers.Location?.ToString()
                ?? throw new InvalidOperationException("Missing location header");

            if (!nextLocation.Contains("code=", StringComparison.OrdinalIgnoreCase))
            {
                var followResponse = await _client.GetAsync(nextLocation);

                if (followResponse.StatusCode == HttpStatusCode.Found &&
                    followResponse.Headers.Location?.ToString().Contains("code=", StringComparison.OrdinalIgnoreCase) == true)
                {
                    nextLocation = followResponse.Headers.Location.ToString();
                }
                else
                {
                    var consentResponse = await ProvideConsent(nextLocation);
                    consentResponse.StatusCode.Should().Be(HttpStatusCode.Found);
                    nextLocation = consentResponse.Headers.Location?.ToString()
                        ?? throw new InvalidOperationException("Missing location header");
                }
            }

            nextLocation.Should().Contain("code=");

            var authorizationCode = ExtractAuthorizationCode(nextLocation);

            var tokenResponse = await ExchangeCodeForTokens(authorizationCode);
            tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var tokens = await AuthorizationTestHelpers.DeserializeTokenResponse(tokenResponse);
            tokens.Should().NotBeNull();
            tokens!.AccessToken.Should().NotBeEmpty();
            return tokens.AccessToken;
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

            return await _client.SendAsync(request);
        }

        private async Task<(HttpResponseMessage Response, string? AntiForgeryToken)> PerformLogin(
            string loginPageUrl,
            string email = TestData.User.Email,
            string password = TestData.User.Password)
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

            return (response, antiForgeryToken);
        }

        private async Task<HttpResponseMessage> ProvideConsent(string consentPageUrl)
        {
            var consentPageResponse = await _client.GetAsync(consentPageUrl);
            var consentDocument = await HtmlHelpers.GetDocumentAsync(consentPageResponse);
            var consentForm = (IHtmlFormElement)consentDocument.QuerySelector("form[action='/connect/authorize']")
                ?? throw new InvalidOperationException("Consent form not found");

            var submitButton = (HtmlElement)consentForm.QuerySelector("[name='submit.Accept']")
                ?? throw new InvalidOperationException("Submit button not found");

            var response = await _client.SendAsync(
                consentForm,
                submitButton,
                new Dictionary<string, string>
                {
                    { "submit.Accept", "Yes" }
                });

            return response;
        }

        private static string ExtractAuthorizationCode(string redirectUrl)
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

        private async Task<HttpResponseMessage> ExchangeCodeForTokens(string code)
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, AuthorizationTestConfiguration.TokenEndpoint)
            {
                Content = AuthorizationTestHelpers.CreateTokenRequestContent(new Dictionary<string, string>
                {
                    ["grant_type"] = AuthorizationTestConfiguration.DefaultGrantType,
                    ["client_id"] = TestData.Client.Id,
                    ["client_secret"] = TestData.Client.Secret,
                    ["code"] = code,
                    ["redirect_uri"] = TestData.Client.RedirectUri
                })
            };

            return await _client.SendAsync(tokenRequest);
        }
    }
}
