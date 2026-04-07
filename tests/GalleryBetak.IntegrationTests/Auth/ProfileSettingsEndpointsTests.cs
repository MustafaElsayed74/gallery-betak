using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GalleryBetak.IntegrationTests.Infrastructure;
using Xunit;

namespace GalleryBetak.IntegrationTests.Auth;

public sealed class ProfileSettingsEndpointsTests : IClassFixture<GalleryBetakApiFactory>
{
    private readonly GalleryBetakApiFactory _factory;

    public ProfileSettingsEndpointsTests(GalleryBetakApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_And_Addresses_Flow_Should_Work_EndToEnd()
    {
        using var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updatedEmail = $"updated_{Guid.NewGuid():N}@gallery-betak.com";

        var updateProfileResponse = await client.PutAsJsonAsync("/api/v1/Auth/profile", new
        {
            firstName = "Updated",
            lastName = "Customer",
            email = updatedEmail,
            phoneNumber = "01012345678"
        });

        updateProfileResponse.EnsureSuccessStatusCode();

        var profileResponse = await client.GetAsync("/api/v1/Auth/profile");
        profileResponse.EnsureSuccessStatusCode();

        var profilePayload = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        profilePayload.Should().NotBeNull();
        profilePayload!.Success.Should().BeTrue();
        profilePayload.Data.Should().NotBeNull();
        profilePayload.Data!.FirstName.Should().Be("Updated");
        profilePayload.Data.LastName.Should().Be("Customer");
        profilePayload.Data.Email.Should().Be(updatedEmail);
        profilePayload.Data.PhoneNumber.Should().Be("01012345678");

        var createAddress1 = await client.PostAsJsonAsync("/api/v1/Auth/profile/addresses", new
        {
            label = "Home",
            recipientName = "Updated Customer",
            phone = "01012345678",
            governorate = "Cairo",
            city = "Nasr City",
            district = "District 1",
            streetAddress = "Street 1",
            buildingNo = "12",
            apartmentNo = "6",
            postalCode = "11765",
            isDefault = false
        });

        createAddress1.EnsureSuccessStatusCode();
        var address1Payload = await createAddress1.Content.ReadFromJsonAsync<ApiResponse<AddressDto>>();
        address1Payload.Should().NotBeNull();
        address1Payload!.Data.Should().NotBeNull();

        var createAddress2 = await client.PostAsJsonAsync("/api/v1/Auth/profile/addresses", new
        {
            label = "Work",
            recipientName = "Updated Customer",
            phone = "01098765432",
            governorate = "Giza",
            city = "Dokki",
            district = "District 2",
            streetAddress = "Street 2",
            buildingNo = "20",
            apartmentNo = "3",
            postalCode = "12611",
            isDefault = false
        });

        createAddress2.EnsureSuccessStatusCode();
        var address2Payload = await createAddress2.Content.ReadFromJsonAsync<ApiResponse<AddressDto>>();
        address2Payload.Should().NotBeNull();
        address2Payload!.Data.Should().NotBeNull();

        var address1Id = address1Payload.Data!.Id;
        var address2Id = address2Payload.Data!.Id;

        var setPriorityResponse = await client.PatchAsJsonAsync($"/api/v1/Auth/profile/addresses/{address2Id}/default", new { });
        setPriorityResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync("/api/v1/Auth/profile/addresses");
        listResponse.EnsureSuccessStatusCode();
        var listPayload = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<AddressDto>>>();

        listPayload.Should().NotBeNull();
        listPayload!.Data.Should().NotBeNull();
        listPayload.Data!.Should().HaveCount(2);

        listPayload.Data.Single(a => a.Id == address2Id).IsDefault.Should().BeTrue();
        listPayload.Data.Single(a => a.Id == address1Id).IsDefault.Should().BeFalse();

        var deleteResponse = await client.DeleteAsync($"/api/v1/Auth/profile/addresses/{address2Id}");
        deleteResponse.EnsureSuccessStatusCode();

        var afterDelete = await client.GetAsync("/api/v1/Auth/profile/addresses");
        afterDelete.EnsureSuccessStatusCode();
        var afterDeletePayload = await afterDelete.Content.ReadFromJsonAsync<ApiResponse<List<AddressDto>>>();

        afterDeletePayload.Should().NotBeNull();
        afterDeletePayload!.Data.Should().NotBeNull();
        afterDeletePayload.Data!.Should().HaveCount(1);
        afterDeletePayload.Data.Single().Id.Should().Be(address1Id);
        afterDeletePayload.Data.Single().IsDefault.Should().BeTrue();
    }

    private static async Task<string> RegisterAndGetTokenAsync(HttpClient client)
    {
        var email = $"it_{Guid.NewGuid():N}@gallery-betak.com";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/Auth/register", new
        {
            firstName = "Integration",
            lastName = "User",
            email,
            password = "StrongPass@123",
            confirmPassword = "StrongPass@123",
            phoneNumber = "01012345678"
        });

        registerResponse.EnsureSuccessStatusCode();

        var payload = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();

        return payload.Data.AccessToken;
    }

    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class UserProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    private sealed class AddressDto
    {
        public int Id { get; set; }
        public bool IsDefault { get; set; }
    }
}
