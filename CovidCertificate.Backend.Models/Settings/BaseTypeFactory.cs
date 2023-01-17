using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.Settings
{
    public static class BaseTypeFactory
    {
        private delegate BaseSettings BaseTypeConstructor(IConfiguration configuration, string vaultKey);

        private static readonly Dictionary<Type, BaseTypeConstructor>
        mTypeConstructors = new Dictionary<Type, BaseTypeConstructor>
        {
            { typeof(EmailSenderCredentialSettings), (pParam1, pParam2) => new EmailSenderCredentialSettings(pParam1, pParam2) }
        };

        public static T BuildBaseType<T>(IConfiguration configuration, string vaultKey) where T : BaseSettings
        {
            T myObject = (T)mTypeConstructors[typeof(T)](configuration, vaultKey);
            return myObject;
        }
    }
}
