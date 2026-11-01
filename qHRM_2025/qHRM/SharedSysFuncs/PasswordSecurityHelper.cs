using System;
using Microsoft.AspNetCore.Identity;
using qHRM.Areas.Identity.Data;

namespace qHRM.SharedSysFuncs
{
    /// <summary>
    /// Helper for user password security implementing AES-256 two-way encryption policy (via CryptoSecurityHelper)
    /// with transparent backward-compatibility and automatic upgrade for legacy PBKDF2 hashes and plaintext.
    /// </summary>
    public static class PasswordSecurityHelper
    {
        private static readonly PasswordHasher<qHRMUser> _hasher = new PasswordHasher<qHRMUser>();

        /// <summary>
        /// Encrypts a plaintext password using the application's AES-256 encryption policy.
        /// </summary>
        public static string EncryptPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword)) return string.Empty;
            return CryptoSecurityHelper.Encrypt(plainPassword);
        }

        /// <summary>
        /// Encrypts a plaintext password using the current application AES-256 encryption policy.
        /// Retained for compatibility with existing callers.
        /// </summary>
        public static string HashPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword)) return string.Empty;
            return CryptoSecurityHelper.Encrypt(plainPassword);
        }

        /// <summary>
        /// Verifies a provided password against a stored password supporting:
        /// 1) Current application AES-256 encryption policy (ENC:...)
        /// 2) Legacy ASP.NET Core PBKDF2 hash (needsRehash = true to upgrade to AES-256)
        /// 3) Legacy plaintext (needsRehash = true to upgrade to AES-256)
        /// </summary>
        public static bool VerifyPassword(string storedPassword, string providedPassword, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(storedPassword) || string.IsNullOrEmpty(providedPassword))
            {
                return false;
            }

            // 1. Current Application Policy: AES-256 Encryption (CryptoSecurityHelper)
            if (CryptoSecurityHelper.IsEncrypted(storedPassword))
            {
                try
                {
                    string decrypted = CryptoSecurityHelper.Decrypt(storedPassword);
                    if (string.Equals(decrypted, providedPassword, StringComparison.Ordinal))
                    {
                        needsRehash = false; // Already modern AES-256 format
                        return true;
                    }
                }
                catch
                {
                    // If decryption fails, continue to legacy checks
                }
            }

            // 2. Legacy Check: ASP.NET Core PBKDF2 PasswordHasher
            try
            {
                var result = _hasher.VerifyHashedPassword(null, storedPassword, providedPassword);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    needsRehash = true; // Flag for upgrade to AES-256 policy
                    return true;
                }
            }
            catch
            {
                // Not a valid hash string, fallback to direct comparison
            }

            // 3. Fallback: direct string comparison for unencrypted legacy passwords
            if (string.Equals(storedPassword, providedPassword, StringComparison.Ordinal))
            {
                needsRehash = true; // Flag for upgrade to AES-256 policy
                return true;
            }

            return false;
        }
    }
}
