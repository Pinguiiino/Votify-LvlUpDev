# AI Usage Log — Sprint S1

## 1) Herramientas usadas

- Herramienta/modelo: Claude (Anthropic) 
- Para qué se usó (2–3 bullets):
  - Refactorizar el frontend para desacoplar la capa de datos.  
  - Transformar accesos directos a la base de datos en peticiones HTTP a la API.  
  - Mejorar la arquitectura siguiendo un modelo cliente-servidor adecuado. 

## 2) Prompts clave (copiar/pegar) + enlace

Incluye 3-5 prompts que hayan influido en el resultado final (no hace falta todos los prompts, pero si los más importantes, pueden ser más de 5).

- **Prompt 1:** “Reescribe estas clases de frontend para que en lugar de acceder directamente a la capa de datos realicen peticiones HTTP a la API manteniendo la estructura lo más similar posible.”
- **Prompt 2:** “Añade los endpoints necesarios a los controladores de la API para soportar todas las operaciones del frontend, asegurando coherencia total con esta.”

## 3) Salidas relevantes (resumen corto)

Qué propuso la IA (1–2 bullets por prompt).

**Prompt 1:** Propuso sustituir llamadas directas a la capa de datos por funciones que realizan peticiones HTTP y generó DTOs para estructurar los datos intercambiados con la API.

**Prompt 2:** Añadió endpoints REST en los controladores de la API para cubrir operaciones CRUD.

## 4) Qué aceptamos y qué rechazamos (mínimo 3 ejemplos)

- Aceptado:
  - Sustitución del acceso directo a datos por peticiones HTTP, ya que mejora la separación de capas.
  - Uso de DTOs para estructurar la comunicación con la API.

- Rechazado/corregido:
  - Algunos endpoints no coincidían exactamente con la estructura real de la API, así que se ajustaron manualmente. 

## 5) Cómo lo verificamos

- Revisión por pares: Se validó que el frontend no accede directamente a la capa de datos. 

## 6) Resultado final / decisión humana

Qué decidió el equipo finalmente y cómo quedó reflejado en el repo (PR/commit/ADR).

El equipo decidió refactorizar completamente el frontend para que interactúe exclusivamente con la API mediante peticiones HTTP, eliminando el acceso directo a la capa de datos. 

Además, se incorporaron DTOs para estructurar correctamente la comunicación entre frontend y backend. 

El resultado final fue una arquitectura desacoplada y coherente con el modelo cliente-servidor, reflejada en el repositorio mediante commits de refactorización del frontend y actualización de los controladores de la API. 