using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CleanArchitecture.Infrastructure.Helpers
{
    public static class JWTHelper
    {
        // Centralized key to ensure token generation and validation always use the exact same bytes.
        // Using a hardcoded fallback to prevent any configuration mismatch in production/deploy.
        public const string StaticKey = "NEW_TEST_KEY_FOR_DEPLOY_VERIFICATION_2026_XYZ_789_!@#";

        public static SymmetricSecurityKey GetSymmetricSecurityKey(string configKey = null)
        {
            var keyStr = !string.IsNullOrEmpty(configKey) ? configKey : StaticKey;
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        }
    }
}
