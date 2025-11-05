namespace Sts.Minimal.Api.Infrastructure.Auth;

/// <summary>
/// Centralized authorization constants to avoid magic strings.
/// </summary>
public static class AuthorizationConstants
{
    public static class Roles
    {
        public const string Reader = "reader";
        public const string Writer = "writer";
    }

    public static class Policies
    {
        // Policy names are typically PascalCase and may differ from role claim values
        public const string Reader = "Reader";
        public const string Writer = "Writer";
    }
}
