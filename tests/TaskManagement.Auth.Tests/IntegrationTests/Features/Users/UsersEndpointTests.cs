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
using TaskManagement.Shared.Models;

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
            user.Roles.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUserById_WhenNotFound_ShouldReturnNotFound()
        {
            var token = await GetAdminAccessTokenAsync();
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
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetUsers_WithSearch_WhenAdmin_ShouldReturnMatchingUsers()
        {
            var token = await GetAdminAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/users?search=authorized");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await response.Content.ReadFromJsonAsync<UserListResponse>();
            list.Should().NotBeNull();
            list!.Total.Should().BeGreaterThan(0);
            list.Items.Should().ContainSingle(u => u.Email == TestData.User.Email);
            list.Items.Should().Contain(u => u.Email == TestData.User.Email && u.Roles != null);
        }

        [Fact]
        public async Task GetUsers_WithIsActiveFilter_ShouldReturnOnlyMatchingUsers()
        {
            var userId = string.Empty;
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var uniqueEmail = $"inactive-{Guid.NewGuid():N}@example.com";
                var user = new ApplicationUser
                {
                    UserName = uniqueEmail,
                    Email = uniqueEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, "StrongPassword@123");
                createResult.Succeeded.Should().BeTrue();

                userId = user.Id;
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await userManager.UpdateAsync(user);
            }

            var token = await GetAdminAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var inactiveResponse = await _client.GetAsync("/api/users?isActive=false");
            inactiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var inactiveList = await inactiveResponse.Content.ReadFromJsonAsync<UserListResponse>();
            inactiveList.Should().NotBeNull();
            inactiveList!.Items.Should().Contain(u => u.Id == userId && !u.IsActive);

            var activeResponse = await _client.GetAsync("/api/users?isActive=true");
            activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var activeList = await activeResponse.Content.ReadFromJsonAsync<UserListResponse>();
            activeList.Should().NotBeNull();
            activeList!.Items.Should().NotContain(u => u.Id == userId);
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

        [Fact]
        public async Task SetUserStatus_WhenAdmin_ShouldDeactivateAndReactivateUser()
        {
            var userId = await GetSeededUserIdAsync();
            var token = await GetAdminAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var deactivateResponse = await _client.PatchAsJsonAsync($"/api/users/{userId}/status", new SetUserStatusRequest
            {
                IsActive = false
            });
            deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                user.Should().NotBeNull();
                user!.LockoutEnabled.Should().BeTrue();
                user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
            }

            var reactivateResponse = await _client.PatchAsJsonAsync($"/api/users/{userId}/status", new SetUserStatusRequest
            {
                IsActive = true
            });
            reactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                user.Should().NotBeNull();
                user!.LockoutEnabled.Should().BeFalse();
                user.LockoutEnd.Should().BeNull();
            }
        }

        [Fact]
        public async Task SetUserStatus_WhenAdminDeactivatesSelf_ShouldReturnBadRequest()
        {
            const string adminEmail = "admin-users-test@example.com";

            var token = await GetAdminAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string adminUserId;
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                adminUser.Should().NotBeNull();
                adminUserId = adminUser!.Id;
            }

            var response = await _client.PatchAsJsonAsync($"/api/users/{adminUserId}/status", new SetUserStatusRequest
            {
                IsActive = false
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var adminUser = await userManager.FindByIdAsync(adminUserId);
                adminUser.Should().NotBeNull();
                adminUser!.LockoutEnabled.Should().BeFalse();
                adminUser.LockoutEnd.Should().BeNull();
            }
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
            => await GetAccessTokenAsync(TestData.User.Email, TestData.User.Password);

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var authorizationResponse = await InitiateAuthorizationRequest(new AuthorizationParameters());

            authorizationResponse.StatusCode.Should().Be(HttpStatusCode.Found);
            authorizationResponse.Headers.Location?.OriginalString.Should().Contain(AuthorizationTestConfiguration.LoginPath);

            var (loginResponse, _) = await PerformLogin(
                authorizationResponse.Headers.Location?.OriginalString
                    ?? throw new InvalidOperationException("Missing location header"),
                email,
                password);

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

        private async Task<string> GetAdminAccessTokenAsync()
        {
            const string adminEmail = "admin-users-test@example.com";
            const string adminPassword = "StrongPassword@123";

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!await roleManager.RoleExistsAsync(Roles.Administrator))
                {
                    await roleManager.CreateAsync(new IdentityRole(Roles.Administrator));
                }

                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                    createResult.Succeeded.Should().BeTrue();
                }

                if (!await userManager.IsInRoleAsync(adminUser, Roles.Administrator))
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Administrator);
                }

                if (adminUser.LockoutEnabled || adminUser.LockoutEnd.HasValue)
                {
                    adminUser.LockoutEnabled = false;
                    adminUser.LockoutEnd = null;
                    adminUser.AccessFailedCount = 0;
                    var updateResult = await userManager.UpdateAsync(adminUser);
                    updateResult.Succeeded.Should().BeTrue();
                }
            }

            return await GetAccessTokenAsync(adminEmail, adminPassword);
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
