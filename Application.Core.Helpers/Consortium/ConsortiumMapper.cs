using Application.Core.DTOs.Consortium.Request;
using Global.Helpers.Date;
using Infrastructure.Core.Models.Consortium;

namespace Application.Core.Helpers.Consortium;

public static class ConsortiumMapper
{
    public static ConsortiumCompany ToDatabaseObject(this CreateConsortiumCompanyRequest request, Guid consortiumCompanyId, Guid consortiumLegalRepresentativeId) =>
        new()
        {
            ConsortiumCompanyId = consortiumCompanyId,
            CompanyId = request.CompanyId,
            Ruc = request.Ruc,
            RnpRegistration = request.RnpRegistration,
            RazonSocial = request.RazonSocial,
            NombreComercial = request.NombreComercial,
            RnpValidUntil = request.RnpValidUntil,
            MainActivity = request.MainActivity,
            DomicilioFiscal = request.DomicilioFiscal,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            LegalRepresentative = new ConsortiumCompanyLegalRepresentative
            {
                ConsortiumLegalRepresentativeId = consortiumLegalRepresentativeId,
                ConsortiumCompanyId = consortiumCompanyId,
                Dni = request.Dni,
                FullName = request.FullName,
                Position = request.Position
            }
        };
    
    public static ConsortiumCompany ToDatabaseObject(this UpdateConsortiumCompanyRequest request) =>
        new()
        {
            ConsortiumCompanyId = request.ConsortiumCompanyId,
            CompanyId = request.CompanyId,
            Ruc = request.Ruc,
            RnpRegistration = request.RnpRegistration,
            RazonSocial = request.RazonSocial,
            NombreComercial = request.NombreComercial,
            RnpValidUntil = request.RnpValidUntil,
            MainActivity = request.MainActivity,
            DomicilioFiscal = request.DomicilioFiscal,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            LegalRepresentative = new ConsortiumCompanyLegalRepresentative
            {
                ConsortiumLegalRepresentativeId = request.ConsortiumLegalRepresentativeId,
                ConsortiumCompanyId = request.ConsortiumCompanyId,
                Dni = request.Dni,
                FullName = request.FullName,
                Position = request.Position
            }
        };
}