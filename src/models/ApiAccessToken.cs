namespace PoFN.models
{
    public class ApiAccessToken
    {
        public string RefreshTokenExpiresIn { get; set; } = string.Empty;
        public string[] ApiProductListJson { get; set; } = [];
        public string OrganizationName { get; set; } = string.Empty;
        public string DeveloperEmail { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public string IssuedAt { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string ExpiresIn { get; set; } = string.Empty;
        public string RefreshCount { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
