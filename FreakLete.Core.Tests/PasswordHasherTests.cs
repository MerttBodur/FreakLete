using FreakLete.Security;

namespace FreakLete.Core.Tests;

public class PasswordHasherTests
{
	[Fact]
	public void HashPassword_CreatesNonEmptyHash()
	{
		string hash = PasswordHasher.HashPassword("StrongPass!");

		Assert.False(string.IsNullOrWhiteSpace(hash));
		Assert.Contains('.', hash);
	}

	[Fact]
	public void VerifyPassword_CorrectPassword_ReturnsTrue()
	{
		string hash = PasswordHasher.HashPassword("StrongPass!");

		bool verified = PasswordHasher.VerifyPassword("StrongPass!", hash);

		Assert.True(verified);
	}

	[Fact]
	public void VerifyPassword_WrongPassword_ReturnsFalse()
	{
		string hash = PasswordHasher.HashPassword("StrongPass!");

		bool verified = PasswordHasher.VerifyPassword("WrongPass!", hash);

		Assert.False(verified);
	}

	[Fact]
	public void VerifyPassword_InvalidStoredHash_ReturnsFalse()
	{
		bool verified = PasswordHasher.VerifyPassword("StrongPass!", "not-a-valid-hash");

		Assert.False(verified);
	}
}
