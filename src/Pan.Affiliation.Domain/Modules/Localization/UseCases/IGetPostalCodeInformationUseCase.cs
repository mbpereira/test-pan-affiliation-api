using Pan.Affiliation.Domain.Modules.Localization.Entities;
using Pan.Affiliation.Domain.Shared.UseCase;
using Pan.Affiliation.Domain.Shared.ValueObjects;

namespace Pan.Affiliation.Domain.Modules.Localization.UseCases;

public interface IGetPostalCodeInformationUseCase : IUseCase<PostalCode, PostalCodeInformation?>
{
}