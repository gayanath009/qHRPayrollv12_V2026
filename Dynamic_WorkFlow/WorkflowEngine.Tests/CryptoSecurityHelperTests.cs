using WorkflowEngine.Infrastructure.Common;
using Xunit;

namespace WorkflowEngine.Tests;

public class CryptoSecurityHelperTests
{
    [Theory]
    [InlineData("Password123!")]
    [InlineData("Admin@2026#Secure")]
    [InlineData("Special#$*(&@!123")]
    [InlineData("SimplePwd")]
    public void Encrypt_And_Decrypt_ShouldPreserveOriginalPassword(string plainText)
    {
        // Act
        string encrypted = CryptoSecurityHelper.Encrypt(plainText);

        // Assert
        Assert.NotNull(encrypted);
        Assert.StartsWith("ENC:", encrypted);
        Assert.True(CryptoSecurityHelper.IsEncrypted(encrypted));

        // Act - Decrypt
        string decrypted = CryptoSecurityHelper.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt_ShouldReturnSame_WhenAlreadyEncrypted()
    {
        string original = "SecretPassword123!";
        string encrypted1 = CryptoSecurityHelper.Encrypt(original);
        string encrypted2 = CryptoSecurityHelper.Encrypt(encrypted1);

        Assert.Equal(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_ShouldReturnAsIs_WhenLegacyPlaintext()
    {
        string plaintext = "LegacyUnencryptedPassword";
        string result = CryptoSecurityHelper.Decrypt(plaintext);

        Assert.Equal(plaintext, result);
    }
}
