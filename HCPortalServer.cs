using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace HC.Function
{
    public class HCPortalServer
    {
        private readonly ILogger<HCPortalServer> _logger;

        public HCPortalServer(ILogger<HCPortalServer> logger)
        {
            _logger = logger;
        }

        [Function("SignIn")]
        public async Task<IActionResult> SignIn([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("SignIn function called.");

            if (!req.Query.ContainsKey("username") || !req.Query.ContainsKey("password"))
            {
                _logger.LogInformation("Invalid params when trying to login");
                return new BadRequestObjectResult("Username and password are required");
            }

            string username = req.Query["username"];
            string password = req.Query["password"];

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT user_password FROM users WHERE username = @username;";
                        command.Parameters.AddWithValue("@username", username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string hashedPassword = reader.GetString(0);
                                if (BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                                    return new OkObjectResult("User authenticated successfully.");

                                return new BadRequestObjectResult("Invalid password.");
                            }

                            return new BadRequestObjectResult("User not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("SignUp")]
        public async Task<IActionResult> SignUp([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("SignUp function called.");

            if (!req.Query.ContainsKey("username") || !req.Query.ContainsKey("password"))
            {
                _logger.LogInformation("Invalid params when trying to signup");
                return new BadRequestObjectResult("Username and password are required");
            }

            string username = req.Query["username"];
            string password = req.Query["password"];
            string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO users (username, user_password) VALUES (@username, @password);";

                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", passwordHash);

                        var queryResult = await command.ExecuteNonQueryAsync();
                        return new OkObjectResult("User registered successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("ListPatients")]
        public async Task<IActionResult> CreatePatient([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "patients")] HttpRequest req)
        {
            _logger.LogInformation("CreatePatient function called.");

            if (!req.Query.ContainsKey("first_name") || !req.Query.ContainsKey("last_name") || !req.Query.ContainsKey("email"))
            {
                _logger.LogInformation("Invalid params when trying to signup");
                return new BadRequestObjectResult("first name, last name and email are required");
            }

            string firstName = req.Query["first_name"];
            string lastName = req.Query["last_name"];
            string email = req.Query["email"];

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO patients (first_name, last_name, email) VALUES (@firstname, @lastname, @email);";

                        command.Parameters.AddWithValue("@firstname", firstName);
                        command.Parameters.AddWithValue("@lastname", lastName);
                        command.Parameters.AddWithValue("@email", email);

                        var queryResult = await command.ExecuteNonQueryAsync();
                        return new OkObjectResult("Patient created successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("CreatePatients")]
        public async Task<IActionResult> ListPatients([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "patients")] HttpRequest req)
        {
            _logger.LogInformation("ListPatients function called.");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT patient_id, first_name, last_name, email, created_at, updated_at FROM patients;";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            List<Patient> patients = new List<Patient>();
                            while (await reader.ReadAsync())
                            {
                                patients.Add(new Patient
                                {
                                    PatientId = reader.GetInt32(0),
                                    FirstName = reader.GetString(1),
                                    LastName = reader.GetString(2),
                                    Email = reader.GetString(3),
                                    CreatedAt = reader.GetString(4),
                                    UpdatedAt = reader.GetString(5)
                                });
                            }

                            return new OkObjectResult(patients);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private string GetConnectionString()
        {
            string connectionStringKey = Environment.GetEnvironmentVariable("SECRET_NAME");

            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                return Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
            }
            else
            {
                // In production, get the secret value from Azure Key Vault
                string tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                string keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");

                var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new ClientSecretCredential(tenantId, clientId, clientSecret));
                KeyVaultSecret secret = client.GetSecret(connectionStringKey);

                return secret.Value;
            }
        }
    }
}
