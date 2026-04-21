using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static ContactDto ToDto(this Contact entity)
    {
        return new ContactDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName ?? string.Empty,
            MaidenName = entity.MaidenName,
            FullName = entity.FullName,
            Company = entity.Company,
            JobTitle = entity.JobTitle,
            IsHidden = entity.IsHidden,
            CreatedDate = entity.CreatedDate,
            LastChangedDate = entity.LastChangedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedBy = entity.LastChangedBy,
            UserId = entity.UserId?.ToString(),
            ProfileImageId = entity.ProfileImageId,
            Pronouns = entity.Pronouns,
            Gender = entity.Gender,
            Religion = entity.Religion,
            IsPartial = entity.IsPartial
        };
    }

    public static ContactDetailDto ToDetailDto(this Contact entity)
    {
        return new ContactDetailDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName ?? string.Empty,
            MaidenName = entity.MaidenName,
            FullName = entity.FullName,
            Company = entity.Company,
            JobTitle = entity.JobTitle,
            Nickname = entity.Nickname,
            ProfileImageId = entity.ProfileImageId,
            Pronouns = entity.Pronouns,
            Gender = entity.Gender,
            Religion = entity.Religion,

            // Lists will be populated separately or via mapping if loaded
            Notes = entity.Notes?.Select(n => n.ToDto()) ?? [],
            SignificantDates = entity.SignificantDates?.Select(d => d.ToDto()) ?? [],
            Relationships = entity.Relationships?.Select(r => r.ToDto()) ?? [],
            RelatedTo = entity.RelatedTo?.Select(r => r.ToDto()) ?? [],
            ContactMethods = entity.ContactMethods?.Select(i => i.ToDto()) ?? [],
            Facts = entity.Facts?.Select(f => f.ToDto()) ?? [],
            Attachments = entity.Attachments?.Select(a => a.ToDto()) ?? [],
            Addresses = entity.Addresses?.Select(a => a.ToDto()) ?? [],
            ContactTasks = entity.ContactTasks?.Select(t => t.ToDto()) ?? [],
            // Pets to be populated by caller as they are not on Person
        };
    }

    public static Contact ToEntity(this ContactFormDto dto)
    {
        return new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MaidenName = dto.MaidenName,
            Nickname = dto.Nickname,
            JobTitle = dto.JobTitle,
            Company = dto.Company,
            IsHidden = dto.IsHidden,
            Pronouns = dto.Pronouns,
            Gender = dto.Gender,
            Religion = dto.Religion
        };
    }

    public static void UpdateEntity(this Contact entity, ContactFormDto dto)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.MaidenName = dto.MaidenName;
        entity.Nickname = dto.Nickname;
        entity.JobTitle = dto.JobTitle;
        entity.Company = dto.Company;
        entity.IsHidden = dto.IsHidden;
        entity.Pronouns = dto.Pronouns;
        entity.Gender = dto.Gender;
        entity.Religion = dto.Religion;
    }
}
