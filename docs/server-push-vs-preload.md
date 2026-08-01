# Análisis: HTTP/2 Server Push vs. Resource Preload (<link rel="preload">)

## 1. Contexto y Estado de HTTP/2 Server Push
HTTP/2 Server Push permitía al servidor enviar recursos (CSS, JS, imágenes) al cliente antes de que este los solicitara explícitamente al procesar el HTML. 

**Estado actual:** Esta característica ha sido declarada **obsoleta y eliminada** de los navegadores principales (Chrome 106+, Edge y Firefox) por las siguientes razones:
- **Desperdicio de ancho de banda:** El servidor a menudo enviaba recursos que el navegador ya tenía en su caché local.
- **Complejidad de implementación:** Difícil de coordinar adecuadamente con el estado del caché del cliente.

## 2. Alternativa Moderna: `<link rel="preload">`
La directiva `<link rel="preload">` informa al parser del navegador que descargue recursos críticos con prioridad alta de inmediato, sin bloquear el renderizado inicial del DOM.

### Comparativa:
| Criterio | HTTP/2 Server Push | `<link rel="preload">` |
| :--- | :--- | :--- |
| **Soporte de Navegadores** | Deprecado / Deshabilitado | Soporte Universal Estándar |
| **Conciencia de Caché** | Pobre (riesgo de duplicación) | Excelente (el navegador verifica su caché primero) |
| **Implementación** | Compleja a nivel de Servidor/HTTP | Declarativa y limpia en HTML |

## 3. Conclusión para Shortly
En Shortly hemos implementado `<link rel="preload" href="...bulma.min.css" as="style" />` en el `_Layout.cshtml` para optimizar la carga del stylesheet sin los inconvenientes de Server Push.