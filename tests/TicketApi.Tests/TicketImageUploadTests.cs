using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketApi.Dtos;
using TicketApi.Services;

namespace TicketApi.Tests;

public class TicketImageUploadTests
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
    ];

    private static readonly byte[] WindowsExecutableBytes =
    [
        0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
    ];

    [Theory]
    [EveryTicketStore]
    public async Task ATicketCreatedWithAnImageExposesItOnALaterRead(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsync("/api/tickets", NewTicket(PngBytes));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(created);
        Assert.NotEmpty(created.ImageUrl);

        var reread = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{created.Id}");
        Assert.Equal(created.ImageUrl, reread?.ImageUrl);

        var image = await client.GetAsync($"/{created.ImageUrl}");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal(PngBytes, await image.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [EveryTicketStore]
    public async Task ATicketCreatedWithoutAnImageHasNoImageUrl(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsync("/api/tickets", NewTicket(image: null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal(string.Empty, created?.ImageUrl);
    }

    [Fact]
    public async Task AnOversizedImageIsRejectedAndNamesTheField()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();
        var oversized = new byte[TicketImageStore.MaxBytes + 1];
        PngBytes.CopyTo(oversized, 0);

        var response = await client.PostAsync("/api/tickets", NewTicket(oversized, "huge.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Image", await ValidationErrors.FieldNamesAsync(response));
    }

    [Fact]
    public async Task AFileThatIsNotAnImageIsRejectedDespiteAnImageExtension()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();

        var response = await client.PostAsync(
            "/api/tickets",
            NewTicket(WindowsExecutableBytes, "payload.png")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Image", await ValidationErrors.FieldNamesAsync(response));
    }

    private static MultipartFormDataContent NewTicket(
        byte[]? image = null,
        string fileName = "photo.png"
    )
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("Ada Lovelace"), "name" },
            { new StringContent("ada@example.com"), "email" },
            {
                new StringContent("The analytical engine jams on every third card."),
                "description"
            },
        };

        if (image is not null)
        {
            var file = new ByteArrayContent(image);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            form.Add(file, "image", fileName);
        }

        return form;
    }
}
