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

    public AuthController(IAmazonCognitoIdentityProvider cognitoClient, IConfiguration configuration)
    {
        _cognitoClient = cognitoClient;
        _userPoolId = configuration["AWS:UserPoolId"];
        _clientId = configuration["AWS:AppClientId"];
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(string username, string password, string email)
    {
        var request = new SignUpRequest
        {
            ClientId = _clientId,
            Password = password,
            Username = username,
            UserAttributes = new List<AttributeType>
            {
                new AttributeType { Name = "email", Value = email }
            }
        };

        try
        {
            var response = await _cognitoClient.SignUpAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(string username, string password)
    {
        var request = new AdminInitiateAuthRequest
        {
            UserPoolId = _userPoolId,
            ClientId = _clientId,
            AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                {"USERNAME", username},
                {"PASSWORD", password}
            }
        };

        try
        {
            var response = await _cognitoClient.AdminInitiateAuthAsync(request);
            return Ok(new
            {
                AccessToken = response.AuthenticationResult.AccessToken,
                IdToken = response.AuthenticationResult.IdToken,
                RefreshToken = response.AuthenticationResult.RefreshToken
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    string GenerateSecretHash(string username, string appClientId, string appSecretKey)
    {
        var message = username + appClientId;

        var secretKeyBytes = Encoding.UTF8.GetBytes(appSecretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using (var hmacSHA256 = new HMACSHA256(secretKeyBytes))
        {
            var hashMessage = hmacSHA256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashMessage);
        }
    }
}
