using ERPSystem.Data.Entities;
using ERPSystem.Shared.DTOs;
using ERPSystem.Utils.Enums;

namespace ERPSystem.Shared.BusinessLogic;

public class ContractRecipientResolver
{
    public DocumentRecipient? Resolve(IEnumerable<ContractParty> parties)
    {
        var guardian = parties
            .FirstOrDefault(p => p.Role == ContractPartyRole.Guardian)
            ?.Guardian;

        if (guardian is not null)
        {
            return new DocumentRecipient
            {
                Name = $"{guardian.FirstName} {guardian.LastName}",
                Email = guardian.Email
            };
        }

        var student = parties
            .FirstOrDefault(p => p.Role == ContractPartyRole.Student)
            ?.Student;

        if (student is null)
            return null;

        return new DocumentRecipient
        {
            Name = $"{student.FirstName} {student.LastName}",
            Email = student.Email
        };
    }
}