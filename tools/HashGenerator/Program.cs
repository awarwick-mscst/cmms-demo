using BCrypt.Net;

string password = "Admin@123";
string hash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(11));

Console.WriteLine($"Password: {password}");
Console.WriteLine($"BCrypt Hash: {hash}");
Console.WriteLine();
Console.WriteLine("Run this SQL to update your admin password:");
Console.WriteLine();
Console.WriteLine($"UPDATE core.users SET password_hash = '{hash}', is_locked = 0, failed_login_attempts = 0 WHERE username = 'admin';");
Console.WriteLine();

// Verify it works
bool verified = BCrypt.Net.BCrypt.Verify(password, hash);
Console.WriteLine($"Verification test: {(verified ? "PASSED" : "FAILED")}");
