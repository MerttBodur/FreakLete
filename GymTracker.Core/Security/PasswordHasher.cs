using System.Security.Cryptography;

namespace GymTracker.Security;

public static class PasswordHasher
{
	private const int SaltSize = 16;
	private const int HashSize = 32;
	private const int IterationCount = 100_000;

	public static string HashPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
			password,
			salt,
			IterationCount,
			HashAlgorithmName.SHA256,
			HashSize);

		return $"{IterationCount}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
	}

	public static bool VerifyPassword(string password, string storedHash)
	{
		string[] parts = storedHash.Split('.');
		if (parts.Length != 3)
		{
			return false;
		}

		bool iterationParsed = int.TryParse(parts[0], out int iterations);
		if (!iterationParsed)
		{
			return false;
		}

		try
		{
			byte[] salt = Convert.FromBase64String(parts[1]);
			byte[] expectedHash = Convert.FromBase64String(parts[2]);
			byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
				password,
				salt,
				iterations,
				HashAlgorithmName.SHA256,
				expectedHash.Length);

			return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
