using System.Security.Cryptography;
using System.Text;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _clientId;
    private readonly string _cognitoSecret;

    public AuthController(IAmazonCognitoIdentityProvider cognitoClient, IConfiguration configuration, IConfiguration cognitoSecret)
    {
        _cognitoClient = cognitoClient;
        _cognitoSecret = configuration["AWS:CognitoSecret"];
        _userPoolId = configuration["AWS:UserPoolId"];
        _clientId = configuration["AWS:AppClientId"];
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(string email, string password) {
        var secretHash = GenerateSecretHash(email, _clientId, _cognitoSecret);
        var request = new SignUpRequest {
            ClientId = _clientId,
            SecretHash = secretHash,
            Password = password,
            Username = email,
            UserAttributes = new List<AttributeType> {
                new AttributeType { Name = "email", Value = email },
                new AttributeType { Name = "birthdate", Value = "1990-01-01" }, // aws:cognito:system.birthdate
                new AttributeType { Name = "gender", Value = "male" }, // aws:cognito:system.gender
                new AttributeType { Name = "phone_number", Value = "+5511912345678" }, // phoneNumbers
                new AttributeType { Name = "given_name", Value = "PrimeiroNome" }, // name.givenName
                new AttributeType { Name = "middle_name", Value = "NomeDoMeio" }, // name.middleName
                // Atributo de endereço deve ser formatado como JSON
                new AttributeType { Name = "address", Value = "{\"formatted\":\"Endereço completo do usuário, Cidade, Estado, País\"}" } // addresses
            }
        };

        try {
            var response = await _cognitoClient.SignUpAsync(request);
            return Ok(response);
        }
        catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("confirm-signup")]
    public async Task<IActionResult> ConfirmSignUp(string email, string code) {
        var secretHash = GenerateSecretHash(email, _clientId, _cognitoSecret);
        var confirmSignUpRequest = new ConfirmSignUpRequest {
            ClientId = _clientId, // O ID do App Client do seu User Pool no Cognito
            SecretHash = secretHash,
            Username = email,
            ConfirmationCode = code
        };

        try {
            var confirmSignUpResponse = await _cognitoClient.ConfirmSignUpAsync(confirmSignUpRequest);
            return Ok(new { message = "Usuário confirmado com sucesso!" });
        }
        catch (Exception ex) {
            // Um tratamento de erro mais robusto é recomendado
            return BadRequest(new { message = ex.Message });
        }
    }

    
    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string password) {
        var secretHash = GenerateSecretHash(email, _clientId, _cognitoSecret);
        var request = new AdminInitiateAuthRequest {
            UserPoolId = _userPoolId,
            ClientId = _clientId,
            AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                {"USERNAME", email},
                {"PASSWORD", password},
                {"SECRET_HASH", secretHash}
            }
        };

        try {
            var response = await _cognitoClient.AdminInitiateAuthAsync(request);
            return Ok(new
            {
                AccessToken = response.AuthenticationResult.AccessToken,
                IdToken = response.AuthenticationResult.IdToken,
                RefreshToken = response.AuthenticationResult.RefreshToken
            });
        }
        catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }
    
    string GenerateSecretHash(string email, string appClientId, string appSecretKey) {
        var message = email + appClientId;

        var secretKeyBytes = Encoding.UTF8.GetBytes(appSecretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using (var hmacSHA256 = new HMACSHA256(secretKeyBytes)) {
            var hashMessage = hmacSHA256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashMessage);
        }
    }
}
