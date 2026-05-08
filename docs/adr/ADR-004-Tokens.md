# ADR-003: Autenticación y autorización mediante JWT con roles

Fecha: 29-04-2026
Sprint: S2
Estado: Aprobado

## 1) Contexto

Votify maneja múltiples tipos de usuario (Organizer, Auditor, Jury, Participant, Public) que tienen acceso diferenciado a recursos: crear eventos, auditar, votar, subir proyectos, etc. El sistema está dividido en una API REST (ASP.NET Core) y un frontend Blazor Server que se comunican via HTTP.
Era necesario decidir cómo autenticar a los usuarios entre requests y cómo propagar su identidad y rol al frontend, dado que la arquitectura es stateless por diseño y los componentes Blazor necesitan conocer el rol del usuario para condicionar la UI (botones de "Crear categoría", "Auditar", "Subir proyecto", etc.).

## 2) Opciones consideradas

- **Opción A: JWT con claims de rol (elegida).** Generar un token firmado con HMAC-SHA256 que incluye NameIdentifier, Email y Role. El frontend lo almacena y lo adjunta en cada request. El CustomAuthStateProvider de Blazor lee un objeto UserSession desde ProtectedSessionStorage y construye los claims (NameIdentifier, Name, Email, Role) manualmente para exponer el estado de autenticación a los componentes.
- **Opción B: Sesiones en servidor (cookie + session store).** El servidor mantiene el estado de sesión. El cliente recibe una cookie de sesión. Requeriría un store distribuido (Redis, SQL) para escalar y acopla el backend al estado de sesión.
- **Opción C: No hacer nada (autenticación básica por header).** Pasar credenciales en cada request. Inseguro, no escalable.

## 3) Criterios de decisión

- Simplicidad de implementación.
- Compatibilidad con Blazor Server y su modelo de AuthenticationStateProvider.
- Statelessness de la API para no requerir infraestructura adicional (session store).
- Control de acceso por rol granular para condicionar UI y endpoints.
- Seguridad mínima aceptable.

## 4) Decisión tomada

Se implementa autenticación basada en JWT generado por TokenService al hacer login exitoso. El token incluye tres claims: NameIdentifier (userId), Email y Role, firmado con HMAC-SHA256 usando una clave simétrica configurada en appsettings. El token expira en 8 horas.
El frontend Blazor lo recibe en el body del login, lo persiste (en ProtectedSessionStorage vía CustomAuthStateProvider) y lo usa tanto para adjuntarlo en los requests a la API como para que los componentes Razor puedan leer el rol y el email del usuario actual y condicionar la UI sin necesidad de llamadas adicionales al servidor.

## 5) Consecuencias

**Positivas:**
- Sin estado en servidor: la API es completamente stateless y escala horizontalmente sin infraestructura adicional.
- El rol y el email viajan en el token, lo que permite que componentes como Categories.razor tomen decisiones de UI (mostrar botón de auditor, jurado, organizador) sin roundtrips extras.
- Integración directa con el sistema de claims de ASP.NET Core y con AuthenticationStateProvider de Blazor.

**Negativas / trade-offs:**
- La clave de firma es simétrica (Jwt:Key en configuración). Si se filtra, cualquiera puede emitir tokens válidos. En producción requeriría rotar a RSA asimétrico o gestión de secretos externa.
- No hay revocación de tokens. Si un usuario cambia de rol o es expulsado, su token sigue siendo válido hasta que expire (8 horas). No existe blacklist.
- El rol se fija en el momento del login. Si el rol cambia en base de datos, el usuario necesita volver a autenticarse para que el cambio se refleje.
- El token se almacena en ProtectedSessionStorage (sessionStorage cifrado por ASP .NET Core Data Protection), lo que mitiga parcialmente el riesgo XSS respecto a localStorage plano, aunque no lo elimina por completo.

**Riesgos y mitigaciones:**
- Filtrado de clave: Usar variables de entorno o un secrets manager (Azure Key Vault, etc.) en lugar de appsettings.json en producción.
- Sin revocación: Aceptable en el contexto del sprint; si se necesitara, se podría añadir una tabla de tokens revocados o reducir el TTL.
- XSS: Mitigable con Content Security Policy y validación de inputs.

## 6) Evidencia

Commits relacionados:
- Establecer tokens para permisos de roles.