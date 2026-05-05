# ADR-003: Gestión de ventanas de votación mediante VotingSession

Fecha: 05-05-2026 
Sprint: S1
Estado: Aprobado

## 1) Contexto

Un evento de Votify no se vota de una sola vez: normalmente tiene varias fases (por ejemplo, una votación abierta al público y otra reservada al jurado), y cada fase necesita sus propias fechas de apertura y cierre, además de reglas distintas sobre quién puede participar. Si esas fechas se metieran directamente como campos dentro de Event, habría que añadir más columnas cada vez que apareciera una nueva fase, y sería imposible tener dos periodos de votación distintos en el mismo evento sin duplicar información.  

## 2) Opciones consideradas

- **Opción A:** Añadir campos de fecha de inicio y fin directamente en la entidad Event. 
- **Opción B:** Crear una entidad VotingSession independiente, relacionada con Event mediante una relación uno a muchos.
- **Opción C:** No modelar las ventanas en la base de datos y controlar las fechas manualmente desde código o configuración.

## 3) Criterios de decisión

Flexibilidad para soportar varias fases por evento, extensibilidad ante futuros tipos de votación, mantenibilidad del modelo y claridad a la hora de saber qué votación está activa en cada momento. 

## 4) Decisión tomada

Se elige la Opción B: crear una entidad VotingSession separada que se asocia al evento. Cada sesión guarda sus propias fechas, su estado y la estrategia de votación que se le aplica, de modo que un mismo evento puede tener tantas ventanas como necesite (pública, jurado, repesca, etc.) sin tocar Event. 
Esta separación encaja bien con el patrón Strategy ya usado para los algoritmos de votación, porque cada VotingSession puede asociarse a una estrategia distinta. 

## 5) Consecuencias
- Ventajas: Permite varios periodos de votación por evento, facilita añadir nuevas fases o reglas en el futuro, mantiene la entidad Event limpia y enfocada solo a la información del evento, y simplifica las consultas del tipo "¿qué votaciones están abiertas ahora mismo?".
- Inconvenientes: Añade una entidad más al modelo, con su repositorio, servicio y controlador correspondientes, lo que supone algo más de código y un join adicional en algunas consultas.
- Riesgos y mitigaciones: Puede haber ambigüedad si dos sesiones del mismo evento se solapan en el tiempo.

## 6) Evidencia
Commits relacionados:
- Corregidas clases y añadidas nuevas
- Cambiado crear categorias y votaciones
- Creado jurado para votaciones y asignaos premios por votación
- Ventana temporal modificar