# ADR-001: Uso de TPH en jerarquías de herencia (EF Core)

Fecha: 10-04-2026  
Sprint: S1
Estado: Aprobado

## 1) Contexto

En Votify hay varias jerarquías de herencia que necesitan persistirse en base de datos: 
- Event: ModalityEvent  
- Project: AiProject, SustainabilityProject, GeneralProject  
- Vote: ExpertVote, PublicVote  
- User: Organizer, Jury, Participant, Public  

## 2) Opciones consideradas

EF Core permite mapear herencia de tres formas: 
- TPH: una sola tabla por jerarquía con un campo tipo (ProjectType, VoteType, etc.).  
- TPT: una tabla para la clase base y otra por cada subtipo (requiere JOINs).  
- TPC: cada subtipo tiene su propia tabla completa (sin JOINs, pero con duplicación de datos). 
Había que elegir una estrategia común para todas las herencias en este primer sprint. 

## 3) Criterios de decisión
- Buen rendimiento en lectura  
- Esquema sencillo  
- Fácil de mantener al añadir nuevos subtipos  
- Compatibilidad con EF Core  

## 4) Decisión tomada
Se usa TPH para todas las jerarquías, configurado con HasDiscriminator en el DbContext. 
Tiene sentido porque ahora mismo los subtipos no añaden propiedades propias, solo cambian comportamiento. 
Así que en la práctica no se generan columnas extra ni valores null innecesarios. 
Además, es la opción por defecto en EF Core, así que simplifica bastante la configuración. 

## 5) Consecuencias
- Ventajas: 
   - Consultas más simples (sin JOINs).  
   - Añadir subtipos es fácil (solo hay que registrar el discriminador).  
   - Menos configuración en general.  
   - Encaja bien con cómo se crean los objetos en el código.  
- Inconvenientes: 
   - Si en el futuro un subtipo tiene campos propios, habrá columnas null para el resto.  
   - No se pueden aplicar restricciones NOT NULL específicas por subtipo.  
- Riesgos: 
   - Si una tabla crece con demasiadas columnas, se valoraría cambiar a TPT.  
   - Errores con el discriminador → se controla registrando bien los tipos y con tests. 

## 6) Evidencia
- Tabla de Projects en la base de datos: incluye todas las subclases (AiProject, SustainabilityProject y GeneralProject) en una única tabla, diferenciadas mediante la columna ProjectType.
- Tabla de Usuarios en la base de datos: incluye las subclases (Participante, PublicoGeneral, Jurado, Organizador) en una única tabla, diferenciadas mediante la columna TipoUsuario.
- Commits relacionados:
   - Base de datos inizializada
   - Factory method añadido
   - Actualizada db y acabada funcionalidad para crear eventos