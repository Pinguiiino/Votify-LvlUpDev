using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Tests
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly Mock<IEventRepository> _eventRepoMock;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _eventRepoMock = new Mock<IEventRepository>();
            _service = new CategoryService(_categoryRepoMock.Object, _eventRepoMock.Object);
        }

        private static ModalityEvent CreateValidEvent(
            string id = "event-1",
            string organizer = "org-1",
            int maxProjects = 10,
            DateTime? startDate = null)
        {
            return new ModalityEvent(
                name: "Evento Test",
                maxProjects: maxProjects,
                startDate: startDate ?? DateTime.UtcNow.AddDays(10),
                endDate: DateTime.UtcNow.AddDays(20),
                modality: "TestModality")
            {
                Id = id,
                Organizer = organizer
            };
        }

        private static CreateVotingSessionData CreateTopNSessionData(int topN = 3)
        {
            return new CreateVotingSessionData
            {
                Name = "Sesión TopN",
                VoterType = VoterType.Public,
                EvaluationType = EvaluationType.TopN,
                TopN = topN,
                OpenAt = DateTime.UtcNow.AddDays(1),
                CloseAt = DateTime.UtcNow.AddDays(5)
            };
        }

        private static CreateVotingSessionData CreatePointDistributionSessionData(
            int pointsPerVoter = 100, int? maxPointsPerProject = null)
        {
            return new CreateVotingSessionData
            {
                Name = "Sesión Puntos",
                VoterType = VoterType.Public,
                EvaluationType = EvaluationType.PointDistribution,
                PointsPerVoter = pointsPerVoter,
                MaxPointsPerProject = maxPointsPerProject,
                OpenAt = DateTime.UtcNow.AddDays(1),
                CloseAt = DateTime.UtcNow.AddDays(5)
            };
        }

        private static CreateVotingSessionData CreateWeightedScaleSessionData()
        {
            return new CreateVotingSessionData
            {
                Name = "Sesión Baremo",
                VoterType = VoterType.Jury,
                EvaluationType = EvaluationType.WeightedScale,
                Criteria = new List<CreateCriterionData>
                {
                    new CreateCriterionData { Name = "Criterio1", Weight = 1.0 }
                },
                OpenAt = DateTime.UtcNow.AddDays(1),
                CloseAt = DateTime.UtcNow.AddDays(5)
            };
        }

        #region GetByEventAsync

        [Fact]
        public async Task GetByEventAsync_DelegaAlRepositorio()
        {
            // Arrange
            var eventId = "event-1";
            var categories = new List<Category> { new Category(eventId, "Cat1") };
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync(eventId))
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetByEventAsync(eventId);

            // Assert
            Assert.Single(result);
            _categoryRepoMock.Verify(r => r.GetByEventAsync(eventId), Times.Once);
        }

        #endregion

        #region GetWithDetailsAsync

        [Fact]
        public async Task GetWithDetailsAsync_DelegaAlRepositorio()
        {
            // Arrange
            var categoryId = "cat-1";
            var category = new Category("event-1", "Cat1");
            _categoryRepoMock
                .Setup(r => r.GetWithDetailsAsync(categoryId))
                .ReturnsAsync(category);

            // Act
            var result = await _service.GetWithDetailsAsync(categoryId);

            // Assert
            Assert.NotNull(result);
            _categoryRepoMock.Verify(r => r.GetWithDetailsAsync(categoryId), Times.Once);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_DatosValidos_RetornaCategoriaCreada()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "Mi Categoría"))
                .ReturnsAsync(false);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "Mi Categoría",
                Description = "Descripción",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            var result = await _service.CreateAsync(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("event-1", result.EventId);
            Assert.Equal("Mi Categoría", result.Name);
            Assert.Single(result.VotingSessions);
            _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            var data = new CreateCategoryData
            {
                EventId = "event-999",
                Name = "Cat",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Equal("Evento no existe.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_NombreVacio_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "   ",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Equal("Nombre obligatorio.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_NombreNulo_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = null!,
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Equal("Nombre obligatorio.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_CategoriaDuplicada_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "Duplicada"))
                .ReturnsAsync(true);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "Duplicada",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Equal("Categoría duplicada.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_SinVotaciones_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "SinVotaciones",
                VotingSessions = new List<CreateVotingSessionData>()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Equal("Debe haber al menos una votación.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_NombreConEspacios_TrimmedCorrectamente()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "Limpia"))
                .ReturnsAsync(false);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "  Limpia  ",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            var result = await _service.CreateAsync(data);

            // Assert
            Assert.Equal("Limpia", result.Name);
        }

        [Fact]
        public async Task CreateAsync_TopNSinTopN_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "TopNInvalido",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = null
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("Top N", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_TopNCero_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "TopNCero",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 0
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("Top N", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_PuntosSinTotal_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "PuntosInvalidos",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.PointDistribution,
                        PointsPerVoter = null
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("puntos por votante", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_MaxPuntosCero_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "MaxPuntosCero",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.PointDistribution,
                        PointsPerVoter = 100,
                        MaxPointsPerProject = 0
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("máximo de puntos por proyecto", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_MaxPuntosSuperaTotal_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "MaxPuntosMayor",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.PointDistribution,
                        PointsPerVoter = 50,
                        MaxPointsPerProject = 100
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("máximo de puntos por proyecto no puede superar", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_BaremoSinCriterios_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "BaremoSinCriterios",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Jury,
                        EvaluationType = EvaluationType.WeightedScale,
                        Criteria = null
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("criterio", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_CriteriosListaVacia_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "BaremoVacio",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Jury,
                        EvaluationType = EvaluationType.WeightedScale,
                        Criteria = new List<CreateCriterionData>()
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("criterio", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_CriterioPesoCero_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "CriterioPesoCero",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Jury,
                        EvaluationType = EvaluationType.WeightedScale,
                        Criteria = new List<CreateCriterionData>
                        {
                            new CreateCriterionData { Name = "Crit1", Weight = 0 }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("peso mayor que 0", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_PremiosSuperaMaxProyectos_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(maxProjects: 2);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "MuchosPremios",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 3,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = 1, Name = "P1" },
                            new CreatePrizeData { Position = 2, Name = "P2" },
                            new CreatePrizeData { Position = 3, Name = "P3" }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("premios", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_PremioSinNombre_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(maxProjects: 5);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "PremioSinNombre",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 3,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = 1, Name = "   " }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Equal("Nombre de premio obligatorio.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_PremioPosicionCero_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(maxProjects: 5);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "PremioPosCero",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 3,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = 0, Name = "P1" }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("posición debe ser mayor que 0", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_PremioPosicionNegativa_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(maxProjects: 5);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "PremioPosNeg",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 3,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = -1, Name = "P1" }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("posición debe ser mayor que 0", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_PremiosPosicionesRepetidas_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(maxProjects: 5);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "PremiosRepetidos",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 3,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = 1, Name = "P1" },
                            new CreatePrizeData { Position = 1, Name = "P2" }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(data));
            Assert.Contains("repetidas", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_JuradosEmails_TrimmedLowercased()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "ConJurados"))
                .ReturnsAsync(false);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "ConJurados",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Jury,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 3,
                        JurorEmails = new List<string>
                        {
                            "  JUROR@Email.COM  ",
                            "juror@email.com"
                        }
                    }
                }
            };

            // Act
            var result = await _service.CreateAsync(data);

            // Assert
            var sesion = Assert.Single(result.VotingSessions);
            var emailsUnicos = sesion.JurorEmails;
            Assert.Single(emailsUnicos);
            Assert.Equal("juror@email.com", emailsUnicos[0]);
        }

        [Fact]
        public async Task CreateAsync_DescriptionVacia_DescriptionEsNull()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "SinDesc"))
                .ReturnsAsync(false);

            var data = new CreateCategoryData
            {
                EventId = "event-1",
                Name = "SinDesc",
                Description = "   ",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            var result = await _service.CreateAsync(data);

            // Assert
            Assert.Null(result.Description);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_DatosValidos_ActualizaCategoria()
        {
            // Arrange
            var categoria = new Category("event-1", "Original");
            var evento = CreateValidEvent(startDate: DateTime.UtcNow.AddDays(10));

            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "Actualizada"))
                .ReturnsAsync(false);

            var data = new UpdateCategoryData
            {
                Name = "Actualizada",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            await _service.UpdateAsync("cat-1", "org-1", data);

            // Assert
            Assert.Equal("Actualizada", categoria.Name);
            _categoryRepoMock.Verify(r => r.RemoveVotingSessionsAsync(categoria), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CategoriaNoExiste_LanzaExcepcion()
        {
            // Arrange
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-999"))
                .ReturnsAsync((Category?)null);

            var data = new UpdateCategoryData
            {
                Name = "Test",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync("cat-999", "org-1", data));
            Assert.Equal("Categoría no encontrada.", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-999", "Cat");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            var data = new UpdateCategoryData
            {
                Name = "Test",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync("cat-1", "org-1", data));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_RequesterNoEsOrganizador_LanzaUnauthorized()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(organizer: "org-real");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryData
            {
                Name = "Test",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.UpdateAsync("cat-1", "otro-usuario", data));
            Assert.Contains("organizador", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_EventoYaComenzo_LanzaInvalidOperation()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(
                organizer: "org-1",
                startDate: DateTime.UtcNow.AddDays(-5));
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryData
            {
                Name = "Test",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync("cat-1", "org-1", data));
            Assert.Contains("ya ha comenzado", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_NombreVacio_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(organizer: "org-1");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryData
            {
                Name = "   ",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync("cat-1", "org-1", data));
            Assert.Equal("Nombre obligatorio.", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_NombreDuplicado_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-1", "Original");
            var evento = CreateValidEvent(organizer: "org-1");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.ExistsByNameInEventAsync("event-1", "OtraCateg"))
                .ReturnsAsync(true);

            var data = new UpdateCategoryData
            {
                Name = "OtraCateg",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync("cat-1", "org-1", data));
            Assert.Contains("Ya existe una categoría", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_MismoNombreCasoDiferente_NoDuplicado()
        {
            // Arrange
            var categoria = new Category("event-1", "MiCategoría");
            var evento = CreateValidEvent(organizer: "org-1");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryData
            {
                Name = "micategoría",
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            await _service.UpdateAsync("cat-1", "org-1", data);

            // Assert
            _categoryRepoMock.Verify(
                r => r.ExistsByNameInEventAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_SinVotaciones_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(organizer: "org-1");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryData
            {
                Name = "Cat",
                VotingSessions = new List<CreateVotingSessionData>()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync("cat-1", "org-1", data));
            Assert.Equal("Debe haber al menos una votación.", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_PremiosSuperaMaxProyectos_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(organizer: "org-1", maxProjects: 2);
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryData
            {
                Name = "Cat",
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 5,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = 1, Name = "P1" },
                            new CreatePrizeData { Position = 2, Name = "P2" },
                            new CreatePrizeData { Position = 3, Name = "P3" }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync("cat-1", "org-1", data));
            Assert.Contains("premios", ex.Message);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_DatosValidos_EliminaCategoria()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(organizer: "org-1");
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.DeleteAsync("cat-1", "org-1");

            // Assert
            _categoryRepoMock.Verify(r => r.DeleteAsync("cat-1"), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CategoriaNoExiste_LanzaExcepcion()
        {
            // Arrange
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-999"))
                .ReturnsAsync((Category?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteAsync("cat-999", "org-1"));
            Assert.Equal("Categoría no encontrada.", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-999", "Cat");
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteAsync("cat-1", "org-1"));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_RequesterNoEsOrganizador_LanzaUnauthorized()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(organizer: "org-real");
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.DeleteAsync("cat-1", "hacker"));
            Assert.Contains("organizador", ex.Message);
        }

        #endregion

        #region UpdateVotingTypeAsync

        [Fact]
        public async Task UpdateVotingTypeAsync_DatosValidos_Actualiza()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(maxProjects: 10);
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryVotingData
            {
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            await _service.UpdateVotingTypeAsync("cat-1", data);

            // Assert
            _categoryRepoMock.Verify(r => r.RemoveVotingSessionsAsync(categoria), Times.Once);
            _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_CategoriaNoExiste_LanzaExcepcion()
        {
            // Arrange
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-999"))
                .ReturnsAsync((Category?)null);

            var data = new UpdateCategoryVotingData
            {
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateVotingTypeAsync("cat-999", data));
            Assert.Equal("Categoría no existe.", ex.Message);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-999", "Cat");
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            var data = new UpdateCategoryVotingData
            {
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateVotingTypeAsync("cat-1", data));
            Assert.Equal("Evento no existe.", ex.Message);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_SinVotaciones_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent();
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryVotingData
            {
                VotingSessions = new List<CreateVotingSessionData>()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateVotingTypeAsync("cat-1", data));
            Assert.Equal("Debe haber al menos una votación.", ex.Message);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_VotacionYaAbierta_LanzaInvalidOperation()
        {
            // Arrange
            var sesionAbierta = new VotingSession(
                categoryId: "cat-1",
                name: "Sesión Abierta",
                voterType: VoterType.Public,
                evaluationType: EvaluationType.TopN,
                openAt: DateTime.UtcNow.AddDays(-5),
                closeAt: DateTime.UtcNow.AddDays(5))
            {
                TopN = 3
            };

            var categoria = new Category("event-1", "Cat")
            {
                VotingSessions = new List<VotingSession> { sesionAbierta }
            };
            var evento = CreateValidEvent();
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryVotingData
            {
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateVotingTypeAsync("cat-1", data));
            Assert.Contains("ya ha comenzado", ex.Message);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_PremiosSuperaMaxProyectos_LanzaExcepcion()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent(maxProjects: 2);
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryVotingData
            {
                VotingSessions = new List<CreateVotingSessionData>
                {
                    new CreateVotingSessionData
                    {
                        VoterType = VoterType.Public,
                        EvaluationType = EvaluationType.TopN,
                        TopN = 5,
                        Prizes = new List<CreatePrizeData>
                        {
                            new CreatePrizeData { Position = 1, Name = "P1" },
                            new CreatePrizeData { Position = 2, Name = "P2" },
                            new CreatePrizeData { Position = 3, Name = "P3" }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateVotingTypeAsync("cat-1", data));
            Assert.Contains("premios", ex.Message);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_CombineResults_ActualizaPesos()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat");
            var evento = CreateValidEvent();
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryVotingData
            {
                CombineResults = true,
                JuryWeight = 0.6,
                PublicWeight = 0.4,
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            await _service.UpdateVotingTypeAsync("cat-1", data);

            // Assert
            Assert.True(categoria.CombineResults);
            Assert.Equal(0.6, categoria.JuryWeight);
            Assert.Equal(0.4, categoria.PublicWeight);
        }

        [Fact]
        public async Task UpdateVotingTypeAsync_SinCombineResults_PesosNull()
        {
            // Arrange
            var categoria = new Category("event-1", "Cat")
            {
                CombineResults = true,
                JuryWeight = 0.5,
                PublicWeight = 0.5
            };
            var evento = CreateValidEvent();
            _categoryRepoMock
                .Setup(r => r.GetForUpdateAsync("cat-1"))
                .ReturnsAsync(categoria);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var data = new UpdateCategoryVotingData
            {
                CombineResults = false,
                JuryWeight = 0.7,
                PublicWeight = 0.3,
                VotingSessions = new List<CreateVotingSessionData> { CreateTopNSessionData() }
            };

            // Act
            await _service.UpdateVotingTypeAsync("cat-1", data);

            // Assert
            Assert.False(categoria.CombineResults);
            Assert.Null(categoria.JuryWeight);
            Assert.Null(categoria.PublicWeight);
        }

        #endregion
    }
}
