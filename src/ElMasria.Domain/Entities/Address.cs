using System.Text.RegularExpressions;

namespace ElMasria.Domain.Entities;

/// <summary>
/// Customer delivery address with Egyptian location format.
/// </summary>
public sealed class Address : BaseEntity
{
    /// <summary>Label (e.g. "المنزل", "العمل").</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Recipient full name.</summary>
    public string RecipientName { get; private set; } = string.Empty;

    /// <summary>Egyptian mobile phone (01XXXXXXXXX).</summary>
    public string Phone { get; private set; } = string.Empty;

    /// <summary>Egyptian governorate (محافظة).</summary>
    public string Governorate { get; private set; } = string.Empty;

    /// <summary>City name.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>District/neighborhood (الحي).</summary>
    public string? District { get; private set; }

    /// <summary>Street address.</summary>
    public string StreetAddress { get; private set; } = string.Empty;

    /// <summary>Building number.</summary>
    public string? BuildingNo { get; private set; }

    /// <summary>Apartment/floor number.</summary>
    public string? ApartmentNo { get; private set; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; private set; }

    /// <summary>Whether this is the user's default address.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Owning user ID.</summary>
    public string UserId { get; private set; } = string.Empty;

    private Address() { }

    private static readonly Regex EgyptPhoneRegex = new(@"^01[0125]\d{8}$", RegexOptions.Compiled);

    /// <summary>Creates a new delivery address with Egyptian phone validation.</summary>
    public static Address Create(string userId, string label, string recipientName,
        string phone, string governorate, string city, string streetAddress,
        string? district = null, string? buildingNo = null, string? apartmentNo = null,
        string? postalCode = null, bool isDefault = false)
    {
        if (!EgyptPhoneRegex.IsMatch(phone))
            throw new Exceptions.DomainException(
                "رقم الهاتف غير صحيح. يجب أن يبدأ بـ 01 ويتكون من 11 رقم",
                "Invalid Egyptian phone number. Must start with 01 and be 11 digits.");

        return new Address
        {
            UserId = userId,
            Label = label,
            RecipientName = recipientName,
            Phone = phone,
            Governorate = governorate,
            City = city,
            StreetAddress = streetAddress,
            District = district,
            BuildingNo = buildingNo,
            ApartmentNo = apartmentNo,
            PostalCode = postalCode,
            IsDefault = isDefault
        };
    }

    /// <summary>Updates address details.</summary>
    public void Update(string label, string recipientName, string phone,
        string governorate, string city, string streetAddress,
        string? district, string? buildingNo, string? apartmentNo, string? postalCode)
    {
        if (!EgyptPhoneRegex.IsMatch(phone))
            throw new Exceptions.DomainException(
                "رقم الهاتف غير صحيح",
                "Invalid Egyptian phone number.");

        Label = label;
        RecipientName = recipientName;
        Phone = phone;
        Governorate = governorate;
        City = city;
        StreetAddress = streetAddress;
        District = district;
        BuildingNo = buildingNo;
        ApartmentNo = apartmentNo;
        PostalCode = postalCode;
    }

    /// <summary>Sets this as the default address.</summary>
    public void SetDefault() => IsDefault = true;

    /// <summary>Removes default status.</summary>
    public void ClearDefault() => IsDefault = false;
}
