namespace TaskManagement.Auth.Tests.IntegrationTests.Authorization.Configurations
{
    public static class AuthorizationTestConfiguration
    {
        public const string AuthorizeEndpoint = "/connect/authorize";
        public const string TokenEndpoint = "/connect/token";
        public const string LoginPath = "/Identity/Account/Login";
        public const string DefaultScope = "openid profile";
        public const string DefaultGrantType = "authorization_code";
    }
}
