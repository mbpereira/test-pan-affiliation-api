﻿using System.Text.RegularExpressions;
using Pan.Affiliation.Domain.Shared.ValueObjects;
using Pan.Affiliation.Shared.Extensions;
using static Pan.Affiliation.Shared.Constants.Regex;

namespace Pan.Affiliation.Domain.Modules.Customers.ValueObjects
{
    public record PostalCode : ValueObject
    {
        public string? OriginalValue { get; }
        public string? Value { get; }
        
        private bool _isValid; 

        public PostalCode(string? cep)
        {
            OriginalValue = cep;
            Value = cep?.OnlyNumbers();
            _isValid = Validate(cep);
        }

        private static bool Validate(string? cep)
            => !string.IsNullOrEmpty(cep) && Regex.IsMatch(cep, ValidCep);

        public static implicit operator PostalCode(string? postalCode)
            => new(postalCode);

        public override string ToString()
            => Value;

        public override bool IsValid()
            => _isValid;
    }
}
