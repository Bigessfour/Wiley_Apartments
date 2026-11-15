using System.Text;
using System.Text.Json;
using Wiley.Apartments.Web.Infrastructure;

namespace Wiley.Apartments.Tests.Infrastructure;

public class CognitoStaffAuthTests
{
    [Fact]
    public void TryReadIdentity_AcceptsCognitoSubject_AndGroups()
    {
        var sub = Guid.NewGuid();
        var token = UnsignedToken(new Dictionary<string, object?>
        {
            ["sub"] = sub.ToString(),
            ["aud"] = CognitoStaffAuth.DefaultClientId,
            ["email"] = "clerk@townofwiley.gov",
            ["cognito:groups"] = new[] { "Staff" }
        });

        var ok = CognitoStaffAuth.TryReadIdentity(
            token,
            CognitoStaffAuth.DefaultClientId,
            out var readSub,
            out var email,
            out var groups);

        ok.Should().BeTrue();
        readSub.Should().Be(sub.ToString());
        email.Should().Be("clerk@townofwiley.gov");
        groups.Should().Contain("Staff");
    }

    [Fact]
    public void TryReadIdentity_RejectsEmailAsSubject()
    {
        var token = UnsignedToken(new Dictionary<string, object?>
        {
            ["sub"] = "clerk@townofwiley.gov",
            ["aud"] = CognitoStaffAuth.DefaultClientId
        });

        CognitoStaffAuth.TryReadIdentity(
            token,
            CognitoStaffAuth.DefaultClientId,
            out _,
            out _,
            out _).Should().BeFalse();
    }

    [Fact]
    public void SafeReturnPath_DropsEmailAndTokens()
    {
        CognitoStaffAuth.SafeReturnPath("/units?email=clerk@townofwiley.gov").Should().Be("/");
        CognitoStaffAuth.SafeReturnPath("/?access_token=abc").Should().Be("/");
        CognitoStaffAuth.SafeReturnPath("https://evil.example/units").Should().Be("/");
        CognitoStaffAuth.SafeReturnPath("/units").Should().Be("/units");
    }

    [Fact]
    public void BuildLogoutUrl_HasClientAndLogoutUri_NotAnAccessToken()
    {
        var url = CognitoStaffAuth.BuildLogoutUrl(
            "tow-gov-staff.auth.us-east-2.amazoncognito.com",
            CognitoStaffAuth.DefaultClientId,
            "http://localhost:8082/");

        url.Should().StartWith("https://tow-gov-staff.auth.us-east-2.amazoncognito.com/logout?");
        url.Should().Contain("client_id=" + CognitoStaffAuth.DefaultClientId);
        url.Should().Contain("logout_uri=");
        url.Should().NotContain("access_token");
    }

    private static string UnsignedToken(Dictionary<string, object?> payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "e." + body + ".sig";
    }
}
