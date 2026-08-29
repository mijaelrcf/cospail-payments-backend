using Application.Auth;
using Application.DTOs.Admin.Requests;
using Application.Options;
using Application.Services;
using Application.Validators;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class AdminAuthTests
{
    [TestClass]
    public sealed class PasswordHasherTests
    {
        [TestMethod]
        public void HashAndVerify_WithSamePassword_ReturnsTrue()
        {
            var hash = PasswordHasher.Hash("clave-secreta");

            hash.Should().StartWith("PBKDF2$");
            PasswordHasher.Verify("clave-secreta", hash).Should().BeTrue();
        }

        [TestMethod]
        public void Verify_WithWrongPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("correcta");

            PasswordHasher.Verify("incorrecta", hash).Should().BeFalse();
        }

        [TestMethod]
        public void Hash_ProducesDifferentSaltsForSamePassword()
        {
            var a = PasswordHasher.Hash("misma");
            var b = PasswordHasher.Hash("misma");

            a.Should().NotBe(b);
        }

        [TestMethod]
        public void Verify_WithMalformedHash_ReturnsFalse()
        {
            PasswordHasher.Verify("pass", "no-es-un-hash").Should().BeFalse();
        }
    }

    [TestClass]
    public sealed class AuthServiceTests
    {
        private static AuthOptions CreateOptions(string hash) =>
            new()
            {
                Issuer = "cospail-admin",
                Audience = "cospail-payments-api",
                SecretKey = "dev_secret_change_me_0123456789abcdef0123456789abcdef",
                TokenLifetimeMinutes = 60,
                Users =
                [
                    new AuthUserOptions
                    {
                        Username = "admin",
                        PasswordHash = hash,
                        DisplayName = "Administrador"
                    }
                ]
            };

        private static AuthService CreateService(AuthOptions options) =>
            new(Options.Create(options), new AuthLoginRequestDtoValidator());

        [TestMethod]
        public async Task LoginAsync_WithValidCredentials_ReturnsToken()
        {
            var options = CreateOptions(PasswordHasher.Hash("clave123"));
            var service = CreateService(options);

            var result = await service.LoginAsync(
                new AuthLoginRequestDto { Username = "admin", Password = "clave123" }
            );

            result.Token.Should().NotBeNullOrWhiteSpace();
            result.DisplayName.Should().Be("Administrador");
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [TestMethod]
        public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorized()
        {
            var options = CreateOptions(PasswordHasher.Hash("clave123"));
            var service = CreateService(options);

            var act = () => service.LoginAsync(
                new AuthLoginRequestDto { Username = "admin", Password = "incorrecta" }
            );

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [TestMethod]
        public async Task LoginAsync_WithUnknownUser_ThrowsUnauthorized()
        {
            var options = CreateOptions(PasswordHasher.Hash("clave123"));
            var service = CreateService(options);

            var act = () => service.LoginAsync(
                new AuthLoginRequestDto { Username = "otro", Password = "clave123" }
            );

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [TestMethod]
        public async Task LoginAsync_WithEmptyPassword_ThrowsValidation()
        {
            var options = CreateOptions(PasswordHasher.Hash("clave123"));
            var service = CreateService(options);

            var act = () => service.LoginAsync(
                new AuthLoginRequestDto { Username = "admin", Password = "" }
            );

            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
