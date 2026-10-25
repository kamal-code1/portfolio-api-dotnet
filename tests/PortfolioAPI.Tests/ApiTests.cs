using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PortfolioAPI.Tests;

public class PublicEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly HttpClient _client;

    public PublicEndpointsTests(PortfolioApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_RetourneOk()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Projets_RetourneLesProjetsInitiauxAvecLienGithub()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/portfolio/projets");

        Assert.True(json.GetProperty("success").GetBoolean());
        var projets = json.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(2, projets.Count);
        Assert.All(projets, p => Assert.StartsWith("https://github.com/", p.GetProperty("lienGithub").GetString()));
        Assert.Contains(projets, p => p.GetProperty("titre").GetString() == "PortfolioAPI");
    }

    [Fact]
    public async Task CompetencesGroupees_RetourneUnSucces()
    {
        var response = await _client.GetAsync("/api/portfolio/competences/grouped");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Reponses_ContiennentLesEnTetesDeSecurite()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }
}

public class AuthTests : IClassFixture<PortfolioApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(PortfolioApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task EcrituresAdmin_SansJeton_Retourne401()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolio/projets", new { titre = "X", description = "Y" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LectureMessages_SansJeton_Retourne401()
    {
        var response = await _client.GetAsync("/api/portfolio/messages");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MauvaisMotDePasse_Retourne401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = PortfolioApiFactory.AdminEmail, motDePasse = "mauvais" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inscription_DesactiveeParDefaut_Retourne404()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "nouveau@test.local", motDePasse = "Password-123" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_Admin_DonneAccesAuxRoutesProtegees()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = PortfolioApiFactory.AdminEmail, motDePasse = PortfolioApiFactory.AdminPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("token").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/portfolio/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await _client.SendAsync(request);

        me.EnsureSuccessStatusCode();
        var data = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal(PortfolioApiFactory.AdminEmail, data.GetProperty("email").GetString());
        Assert.Equal("Admin", data.GetProperty("role").GetString());
    }
}

public class ContactTests : IClassFixture<PortfolioApiFactory>
{
    private readonly PortfolioApiFactory _factory;
    private readonly HttpClient _client;

    public ContactTests(PortfolioApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static object Message(string? website = null) => new
    {
        nom = "Visiteur Test",
        email = "visiteur@test.local",
        sujet = "Prise de contact",
        contenu = "Bonjour, ceci est un message de test.",
        website
    };

    // Les cas sont regroupés dans un seul test : la limite de 5 envois par IP
    // s'applique à toute l'instance de l'API partagée par cette classe.
    [Fact]
    public async Task FormulaireDeContact_ValideFiltreEtSignaleLesEchecs()
    {
        // Message valide : enregistré puis transmis par e-mail
        var ok = await _client.PostAsJsonAsync("/api/portfolio/messages", Message());
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Single(_factory.Email.Sent);

        // Champ piège rempli : réponse neutre, rien n'est envoyé
        var robot = await _client.PostAsJsonAsync("/api/portfolio/messages", Message(website: "http://spam"));
        Assert.Equal(HttpStatusCode.OK, robot.StatusCode);
        Assert.Single(_factory.Email.Sent);

        // Données invalides : refusées par la validation
        var invalide = await _client.PostAsJsonAsync("/api/portfolio/messages",
            new { nom = "A", email = "pas-un-email", sujet = "x", contenu = "court" });
        Assert.Equal(HttpStatusCode.BadRequest, invalide.StatusCode);

        // Échec d'envoi : signalé au visiteur, pas de faux succès
        _factory.Email.Fail = true;
        var echec = await _client.PostAsJsonAsync("/api/portfolio/messages", Message());
        Assert.Equal(HttpStatusCode.BadGateway, echec.StatusCode);
        _factory.Email.Fail = false;

        // 6e requête dans la fenêtre : limitée
        await _client.PostAsJsonAsync("/api/portfolio/messages", Message());
        var limite = await _client.PostAsJsonAsync("/api/portfolio/messages", Message());
        Assert.Equal(HttpStatusCode.TooManyRequests, limite.StatusCode);
    }
}
