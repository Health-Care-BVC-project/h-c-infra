using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Azure.Identity;
using System.Text.Json;
using Azure.Security.KeyVault.Secrets;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace HC.Function
{
    public class UserData
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class PatientData
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class HCPortalServer
    {
        private readonly ILogger<HCPortalServer> _logger;
        JsonSerializerOptions _jsonOptions;

        public HCPortalServer(ILogger<HCPortalServer> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Function("SignIn")]
        public async Task<IActionResult> SignIn([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<UserData>(requestBody, _jsonOptions);

            if (string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(data.Password))
            {
                _logger.LogInformation("Invalid params when trying to login");
                return new BadRequestObjectResult("Username and password are required");
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT user_password, user_id FROM users WHERE username = @username;";
                        command.Parameters.AddWithValue("@username", data.Username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string hashedPassword = reader.GetString(0);
                                string userId = reader.GetValue(1)?.ToString();
                                if (BCrypt.Net.BCrypt.EnhancedVerify(data.Password, hashedPassword))
                                {
                                    var tokenHandler = new JwtSecurityTokenHandler();
                                    var key = Encoding.ASCII.GetBytes(GetJwtSecretKey());

                                    var tokenDescriptor = new SecurityTokenDescriptor
                                    {
                                        Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
                                        Expires = DateTime.UtcNow.AddDays(7),
                                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                                    };
                                    var token = tokenHandler.CreateToken(tokenDescriptor);
                                    var tokenString = tokenHandler.WriteToken(token);

                                    return new OkObjectResult(new { Token = tokenString });
                                }

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

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<UserData>(requestBody, _jsonOptions);

            if (string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(data.Password))
            {
                _logger.LogInformation("Invalid params when trying to login");
                return new BadRequestObjectResult("Username and password are required");
            }

            string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(data.Password);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO users (username, user_password) VALUES (@username, @password);";

                        command.Parameters.AddWithValue("@username", data.Username);
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

        [Function("CreatePatients")]
        public async Task<IActionResult> CreatePatient([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "patients")] HttpRequest req)
        {
            _logger.LogInformation("CreatePatient function called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<PatientData>(requestBody, _jsonOptions);

            if (string.IsNullOrEmpty(data.FirstName) || string.IsNullOrEmpty(data.LastName) || string.IsNullOrEmpty(data.Email))
            {
                _logger.LogInformation("Invalid params when trying to login");
                return new BadRequestObjectResult("Username and password are required");
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO patients (first_name, last_name, email) VALUES (@firstname, @lastname, @email);";

                        command.Parameters.AddWithValue("@firstname", data.FirstName);
                        command.Parameters.AddWithValue("@lastname", data.LastName);
                        command.Parameters.AddWithValue("@email", data.Email);

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

        [Function("ListPatients")]
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
                                    CreatedAt = reader.GetDateTime(4).ToString("yyyy-MM-dd"),
                                    UpdatedAt = reader.GetDateTime(5).ToString("yyyy-MM-dd")
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

        [Function("RemovePatient")]
        public async Task<IActionResult> RemovePatient([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "patients/{id}")] HttpRequest req, int id)
        {
            _logger.LogInformation("RemovePatient function called.");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM patients WHERE patient_id = @id;";
                        command.Parameters.AddWithValue("@id", id);

                        var queryResult = await command.ExecuteNonQueryAsync();

                        if (queryResult > 0)
                        {
                            return new OkObjectResult($"Patient with ID {id} removed successfully.");
                        }
                        else
                        {
                            return new NotFoundResult();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("EditPatient")]
        public async Task<IActionResult> EditPatient([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "patients/{id}")] HttpRequest req, int id)
        {
            _logger.LogInformation("EditPatient function called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<PatientData>(requestBody, _jsonOptions);

            if (string.IsNullOrEmpty(data.FirstName) && string.IsNullOrEmpty(data.LastName) && string.IsNullOrEmpty(data.Email))
            {
                _logger.LogInformation("Invalid params when trying to update patient");
                return new BadRequestObjectResult("At least one of the following properties must be changed: First name, last name or email");
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "UPDATE patients SET first_name = @firstname, last_name = @lastname, email = @email WHERE patient_id = @id;";

                        command.Parameters.AddWithValue("@firstname", data.FirstName);
                        command.Parameters.AddWithValue("@lastname", data.LastName);
                        command.Parameters.AddWithValue("@email", data.Email);
                        command.Parameters.AddWithValue("@id", id);

                        var queryResult = await command.ExecuteNonQueryAsync();

                        if (queryResult > 0)
                        {
                            return new OkObjectResult($"Patient with ID {id} updated successfully.");
                        }
                        else
                        {
                            return new NotFoundResult();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("GetPatientById")]
        public async Task<IActionResult> GetPatientById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "patients/{id}")] HttpRequest req, int id)
        {
            _logger.LogInformation("GetPatientById function called.");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT patient_id, first_name, last_name, email, created_at, updated_at FROM patients WHERE patient_id = @id;";
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var patient = new Patient
                                {
                                    PatientId = reader.GetInt32(0),
                                    FirstName = reader.GetString(1),
                                    LastName = reader.GetString(2),
                                    Email = reader.GetString(3),
                                    CreatedAt = reader.GetDateTime(4).ToString("yyyy-MM-dd"),
                                    UpdatedAt = reader.GetDateTime(5).ToString("yyyy-MM-dd")
                                };

                                return new OkObjectResult(patient);
                            }
                            else
                            {
                                return new NotFoundResult();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private string GetJwtSecretKey() => GetSecret("SECRET_JWT_KEY");
        private string GetConnectionString() => GetSecret("MYSQL_CONNECTION_STRING");

        private string GetSecret(string key)
        {
            if (GetSecretFromEnvValues("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                return GetSecretFromEnvValues(key);
            }
            else
            {
                return GetSecretFromKeyVault(key);
            }
        }

        private string GetSecretFromEnvValues(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value == null)
                throw new Exception($"{key} environment variable is not set.");

            return value;
        }

        private string GetSecretFromKeyVault(string secretName)
        {
            string tenantId = GetSecretFromEnvValues("TENANT_ID");
            string clientId = GetSecretFromEnvValues("CLIENT_ID");
            string clientSecret = GetSecretFromEnvValues("CLIENT_SECRET");
            string keyVaultUrl = GetSecretFromEnvValues("KEY_VAULT_URL");

            var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new ClientSecretCredential(tenantId, clientId, clientSecret));
            KeyVaultSecret secret = client.GetSecret(secretName);

            return secret.Value;
        }

    }
}
