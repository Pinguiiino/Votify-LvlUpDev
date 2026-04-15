# ADR-002: Uso del patrón Repository

Fecha: 10-04-2026  
Sprint: S1
Estado: Aprobado

## 1) Contexto

Los servicios (EventService, ProjectService, VoteService) necesitan acceder a datos. 
Si acceden directamente a VotifyDbContext, quedan muy acoplados a EF Core, lo que complica hacer tests unitarios y cambiar de base de datos o tecnología en el futuro  
Además, hay una inconsistencia: algunos controladores (VotesController, VotingSessionsController) sí están usando directamente el DbContext, lo que rompe el patrón y se deja como deuda técnica.  

## 2) Opciones consideradas

- Repository (interfaces): el dominio define interfaces (IEventRepository, etc.) y la implementación usa EF Core.  
- DbContext directo: más simple, pero acopla todo a EF Core.  
- Dapper / SQL directo: más control, pero peor testabilidad y más acoplamiento. 

## 3) Criterios de decisión

- Poder testear bien los servicios.
- Mantener desacoplado el dominio.
- Seguir la arquitectura por capas.
- No añadir demasiada complejidad.

## 4) Decisión tomada

Se usa Repository con interfaces en el dominio e implementaciones en infraestructura. 
Cada entidad principal tiene su repositorio (IEventRepository, IProjectRepository, etc.), y los servicios dependen solo de la interfaz. 
Esto permite cambiar la implementación o usar mocks en tests sin tocar la lógica de negocio. 

## 5) Consecuencias
- Ventajas: 
   - Los servicios no dependen de EF Core directamente.  
   - Se pueden hacer tests unitarios fácilmente con mocks.  
   - Cambiar de base de datos no afecta al dominio.  
- Inconvenientes: 
   - Hay inconsistencia: los controladores de votos usan DbContext directamente (deuda técnica).  
   - Algunos métodos son muy genéricos (GetAllAsync) y habrá que mejorarlos. 
- Riesgos: 
   - Que se siga usando DbContext directamente: se debe corregir en S2.  
   - Meter lógica de negocio en repositorios: deben limitarse a acceso a datos. 

## 6) Evidencia
Commits relacionados:
- Service para proyectos
- Service de categorías
- Service de eventos