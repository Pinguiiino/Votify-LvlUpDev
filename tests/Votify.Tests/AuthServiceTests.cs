using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using Votify.Domain.UserFolder;

namespace Votify.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _service = new AuthService(_userRepoMock.Object);
        }

        #region RegisterUserAsync

        [Fact]
        public async Task RegisterUserAsync_DatosValidos_RetornaUsuarioCreado()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.CheckForDuplicatesAsync("TestUser", "test@test.com"))
                .ReturnsAsync((false, false));

            // Act
            var result = await _service.RegisterUserAsync("TestUser", "test@test.com", "pass123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestUser", result.Name);
            Assert.Equal("test@test.com", result.Email);
            Assert.Equal("pass123", result.Password);
            Assert.IsType<GeneralUser>(result);
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterUserAsync_EmailDuplicado_LanzaExcepcion()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.CheckForDuplicatesAsync("TestUser", "dup@test.com"))
                .ReturnsAsync((false, true));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterUserAsync("TestUser", "dup@test.com", "pass"));
            Assert.Equal("Ese correo ya está registrado.", ex.Message);
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterUserAsync_NombreDuplicado_LanzaExcepcion()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.CheckForDuplicatesAsync("DupName", "new@test.com"))
                .ReturnsAsync((true, false));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterUserAsync("DupName", "new@test.com", "pass"));
            Assert.Equal("El nombre de usuario ya está en uso.", ex.Message);
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterUserAsync_AmbosDuplicados_EmailPrevalece()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.CheckForDuplicatesAsync("Dup", "dup@test.com"))
                .ReturnsAsync((true, true));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterUserAsync("Dup", "dup@test.com", "pass"));
            Assert.Equal("Ese correo ya está registrado.", ex.Message);
        }

        [Fact]
        public async Task RegisterUserAsync_GuardaContrasena_Plano()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.CheckForDuplicatesAsync("User", "u@test.com"))
                .ReturnsAsync((false, false));

            // Act
            var result = await _service.RegisterUserAsync("User", "u@test.com", "miPass");

            // Assert
            Assert.Equal("miPass", result.Password);
        }

        #endregion

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_CredencialesValidas_RetornaUsuario()
        {
            // Arrange
            var user = new GeneralUser("TestUser", "test@test.com", "pass123");
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            // Act
            var result = await _service.LoginAsync("test@test.com", "pass123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestUser", result.Name);
        }

        [Fact]
        public async Task LoginAsync_UsuarioNoExiste_LanzaExcepcion()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("noexiste@test.com"))
                .ReturnsAsync((GeneralUser?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LoginAsync("noexiste@test.com", "pass"));
            Assert.Equal("El correo electrónico o la contraseña son incorrectos.", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_ContrasenaIncorrecta_LanzaExcepcion()
        {
            // Arrange
            var user = new GeneralUser("TestUser", "test@test.com", "correcta");
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LoginAsync("test@test.com", "incorrecta"));
            Assert.Equal("El correo electrónico o la contraseña son incorrectos.", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_ContrasenaNula_LanzaExcepcion()
        {
            // Arrange
            var user = new GeneralUser("TestUser", "test@test.com", "pass");
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LoginAsync("test@test.com", null!));
            Assert.Contains("incorrectos", ex.Message);
        }

        #endregion

        #region ChangePasswordAsync

        [Fact]
        public async Task ChangePasswordAsync_DatosValidos_CambiaContrasena()
        {
            // Arrange
            var user = new GeneralUser("User", "u@test.com", "oldPass") { Id = "user-1" };
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act
            await _service.ChangePasswordAsync("user-1", "oldPass", "newPass");

            // Assert
            Assert.Equal("newPass", user.Password);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_UsuarioNoExiste_LanzaExcepcion()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-999"))
                .ReturnsAsync((GeneralUser?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChangePasswordAsync("user-999", "old", "new"));
            Assert.Equal("Usuario no encontrado.", ex.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_ContrasenaActualIncorrecta_LanzaExcepcion()
        {
            // Arrange
            var user = new GeneralUser("User", "u@test.com", "correcta") { Id = "user-1" };
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChangePasswordAsync("user-1", "mala", "new"));
            Assert.Equal("La contraseña actual es incorrecta.", ex.Message);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region CheckEmailExistsAsync

        [Fact]
        public async Task CheckEmailExistsAsync_Existe_RetornaTrue()
        {
            // Arrange
            var user = new GeneralUser("User", "u@test.com", "pass");
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("u@test.com"))
                .ReturnsAsync(user);

            // Act
            var result = await _service.CheckEmailExistsAsync("u@test.com");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckEmailExistsAsync_NoExiste_RetornaFalse()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("no@test.com"))
                .ReturnsAsync((GeneralUser?)null);

            // Act
            var result = await _service.CheckEmailExistsAsync("no@test.com");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetUserByEmailAsync

        [Fact]
        public async Task GetUserByEmailAsync_Existe_RetornaUsuario()
        {
            // Arrange
            var user = new GeneralUser("User", "u@test.com", "pass");
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("u@test.com"))
                .ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByEmailAsync("u@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("u@test.com", result!.Email);
        }

        [Fact]
        public async Task GetUserByEmailAsync_NoExiste_RetornaNull()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("no@test.com"))
                .ReturnsAsync((GeneralUser?)null);

            // Act
            var result = await _service.GetUserByEmailAsync("no@test.com");

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}
