﻿namespace Pan.Affiliation.Shared.Constants
{
    public static class HttpServices
    {
        public static class Ibge
        {
            public const string GetSatesPath = "estados";
            public const string GetCitiesFromStatePath = "estados/{0}/municipios";
        }
        
        public static class ViaCep
        {
            public const string GetPostalCodeInformationPath = "ws/{0}/json";
        }
    }
}
