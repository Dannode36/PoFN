using Newtonsoft.Json;

namespace PoFN.models
{
    public class ApiAccessToken
    {
        [JsonProperty("refresh_token_expires_in")] public string RefreshTokenExpiresIn { get; set; } = string.Empty;
        [JsonProperty("api_product_list_json")] public string[] ApiProductListJson { get; set; } = [];
        [JsonProperty("organization_name")] public string OrganizationName { get; set; } = string.Empty;
        [JsonProperty("developer.email")] public string DeveloperEmail { get; set; } = string.Empty;
        [JsonProperty("token_type")] public string TokenType { get; set; } = string.Empty;
        [JsonProperty("issued_at")] public string IssuedAt { get; set; } = string.Empty;
        [JsonProperty("client_id")] public string ClientId { get; set; } = string.Empty;
        [JsonProperty("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("application_name")] public string ApplicationName { get; set; } = string.Empty;
        [JsonProperty("scope")] public string Scope { get; set; } = string.Empty;
        [JsonProperty("expires_in")] public string ExpiresIn { get; set; } = string.Empty;
        [JsonProperty("refresh_count")] public string RefreshCount { get; set; } = string.Empty;
        [JsonProperty("status")] public string Status { get; set; } = string.Empty;
    }
}
