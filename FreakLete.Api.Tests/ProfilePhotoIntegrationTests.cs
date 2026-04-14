using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class ProfilePhotoIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    public ProfilePhotoIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth guard ─────────────────────────────────────────────────

    [Fact]
    public async Task UploadPhoto_WithoutToken_Returns401()
    {
        var content = BuildJpegFormContent();
        var response = await _client.PostAsync("/api/profilephoto", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPhoto_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeletePhoto_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Happy path ─────────────────────────────────────────────────

    [Fact]
    public async Task UploadJpeg_Under2MB_Returns200WithTimestamp()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        var response = await authed.PostAsync("/api/profilephoto", BuildJpegFormContent());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("profilePhotoUpdatedAtUtc", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPhoto_AfterUpload_ReturnsSameBytesAndContentType()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        var jpegBytes = BuildMinimalJpegBytes();
        await authed.PostAsync("/api/profilephoto", BuildFormContent(jpegBytes, "image/jpeg", "photo.jpg"));

        var getResponse = await authed.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal("image/jpeg", getResponse.Content.Headers.ContentType?.MediaType);

        var returnedBytes = await getResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(jpegBytes, returnedBytes);
    }

    [Fact]
    public async Task DeletePhoto_Returns204_ThenGetReturns404()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        await authed.PostAsync("/api/profilephoto", BuildJpegFormContent());

        var deleteResponse = await authed.DeleteAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await authed.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetPhoto_WhenNoPhotoUploaded_Returns404()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        var response = await authed.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Validation ─────────────────────────────────────────────────

    [Fact]
    public async Task Upload_UnsupportedContentType_Returns400()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        var bytes = Encoding.UTF8.GetBytes("not an image");
        var response = await authed.PostAsync("/api/profilephoto",
            BuildFormContent(bytes, "image/gif", "photo.gif"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OversizedFile_Returns400()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        // 2 MB + 1 byte
        var oversized = new byte[2 * 1024 * 1024 + 1];
        var response = await authed.PostAsync("/api/profilephoto",
            BuildFormContent(oversized, "image/jpeg", "big.jpg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        var response = await authed.PostAsync("/api/profilephoto",
            BuildFormContent([], "image/jpeg", "empty.jpg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── User isolation ─────────────────────────────────────────────

    [Fact]
    public async Task GetPhoto_UserIsolation_SecondUserSeesOwnPhotoOnly()
    {
        // User 1 uploads
        var auth1 = await AuthTestHelper.RegisterAsync(_client);
        var client1 = _factory.CreateClient();
        AuthTestHelper.Authenticate(client1, auth1.Token);
        await client1.PostAsync("/api/profilephoto", BuildJpegFormContent());

        // User 2 has no photo
        var auth2 = await AuthTestHelper.RegisterAsync(_client);
        var client2 = _factory.CreateClient();
        AuthTestHelper.Authenticate(client2, auth2.Token);

        // User 2 GET own photo returns 404
        var r2Get = await client2.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.NotFound, r2Get.StatusCode);

        // User 1 GET own photo returns 200
        var r1Get = await client1.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.OK, r1Get.StatusCode);
    }

    // ── Account deletion cascade ───────────────────────────────────

    [Fact]
    public async Task DeleteAccount_RemovesPhotoWithUser()
    {
        const string password = "TestPassword123!";
        var auth = await AuthTestHelper.RegisterAsync(_client, password: password);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        // Upload photo
        await authed.PostAsync("/api/profilephoto", BuildJpegFormContent());
        var before = await authed.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        // Delete account
        var del = await authed.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { currentPassword = password })
        });
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Token no longer valid — photo is gone with the user
        var after = await authed.GetAsync("/api/profilephoto");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    // ── PNG and WebP ───────────────────────────────────────────────

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task Upload_AcceptedImageTypes_Returns200(string contentType)
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authed = _factory.CreateClient();
        AuthTestHelper.Authenticate(authed, auth.Token);

        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header-ish
        var response = await authed.PostAsync("/api/profilephoto",
            BuildFormContent(bytes, contentType, "photo.img"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildJpegFormContent()
        => BuildFormContent(BuildMinimalJpegBytes(), "image/jpeg", "photo.jpg");

    private static MultipartFormDataContent BuildFormContent(byte[] bytes, string contentType, string fileName)
    {
        var form = new MultipartFormDataContent();
        var sc = new ByteArrayContent(bytes);
        sc.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(sc, "file", fileName);
        return form;
    }

    /// <summary>Minimal valid JPEG — SOI + EOI markers.</summary>
    private static byte[] BuildMinimalJpegBytes()
        => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00,
            0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9];
}
